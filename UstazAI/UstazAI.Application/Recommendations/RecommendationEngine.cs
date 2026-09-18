using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UstazAI.Application.Ai;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Common.Services;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;

namespace UstazAI.Application.Recommendations;

/// <summary>
/// Orchestrates the hybrid scorer (§5.1) against the full catalog and, best-effort, asks Gemini
/// to narrate the top results. Always produces a usable recommendation batch even if every
/// Gemini call fails — narration falls back to the scorer's own deterministic notes.
/// </summary>
public sealed class RecommendationEngine(
    IAppDbContext db, IAiReasoningService ai, DecisionLedgerWriter ledger, ICurrentUser user, ILogger<RecommendationEngine> logger)
{
    private const int MaxResults = 5;

    public async Task<List<Recommendation>> GenerateAsync(StudentProfile profile, CancellationToken ct)
    {
        var programs = await db.ProgramOfferings
            .Include(p => p.University)
            .Include(p => p.AdmitArchetypes)
            .ToListAsync(ct);

        var scored = programs
            .Select(p => (Program: p, Result: HybridScoringEngine.Score(profile, p, p.AdmitArchetypes)))
            .OrderByDescending(x => x.Result.OverallScore)
            .Take(MaxResults)
            .ToList();

        // Narration calls are independent per program — fired concurrently rather than awaited
        // one at a time in the loop, since a sequential await here means the user waits N times
        // Gemini's real-world latency (each call already showed ~10-14s live) for what could be a
        // single round-trip's worth of wall-clock time.
        var narrations = await Task.WhenAll(scored.Select(x => SafeNarrateAsync(x.Program, x.Result, user.UiLocale ?? profile.PreferredLanguage, ct)));

        // A repeat call at the same profile version (page refresh, double-click, a retried
        // request — nothing here bumps Version) must replace that version's batch, not append a
        // second one alongside it: GetLatestRecommendationsQuery filters strictly on
        // ProfileVersion == profile.Version with no de-duplication, so leftover rows from an
        // earlier call at this same version would double up every RankPosition and silently skew
        // the delta this method's own caller computes against the *previous* version.
        var staleAtThisVersion = await db.Recommendations
            .Where(r => r.StudentProfileId == profile.Id && r.ProfileVersion == profile.Version)
            .ToListAsync(ct);
        if (staleAtThisVersion.Count > 0) db.Recommendations.RemoveRange(staleAtThisVersion);

        var recommendations = new List<Recommendation>();
        for (var i = 0; i < scored.Count; i++)
        {
            var (program, result) = scored[i];
            var narration = narrations[i];
            var affordability = HybridScoringEngine.EvaluateAffordability(profile, program);

            recommendations.Add(new Recommendation
            {
                StudentProfileId = profile.Id,
                ProfileVersion = profile.Version,
                ProgramId = program.Id,
                Program = program,
                RankPosition = i + 1,
                OverallScore = result.OverallScore,
                AcademicFitScore = result.AcademicFitScore,
                FinancialFitScore = result.FinancialFitScore,
                CareerFitScore = result.CareerFitScore,
                TimelineFitScore = result.TimelineFitScore,
                AcademicFitExplanation = narration.AcademicExplanation,
                FinancialFitExplanation = narration.FinancialExplanation,
                CareerFitExplanation = narration.CareerExplanation,
                TimelineFitExplanation = narration.TimelineExplanation,
                NarrativeSummary = narration.NarrativeSummary,
                AdmissionProbability = result.AdmissionProbability,
                Provenance = program.Provenance,
                IsAiNarrated = !narration.FallbackUsed,
                FallbackUsed = narration.FallbackUsed,
                AffordabilityTier = affordability.Tier,
                AffordabilityEffectiveCostUsd = affordability.EffectiveCostUsd,
                AffordabilityBudgetCeilingUsd = affordability.BudgetCeilingUsd
            });
        }

        db.Recommendations.AddRange(recommendations);

        await ledger.AppendAsync(profile.Id, AiDecisionType.Recommendation, new
        {
            profile.Version,
            Programs = recommendations.Select(r => new { r.ProgramId, r.OverallScore, r.AdmissionProbability.PointEstimate })
        }, ct);

        return recommendations;
    }

    private async Task<RecommendationNarrationOutput> SafeNarrateAsync(
        ProgramOffering program, Domain.Services.ScoringResult result, Locale locale, CancellationToken ct)
    {
        try
        {
            return await ai.NarrateRecommendationAsync(new RecommendationNarrationInput(
                program.Name, program.FieldOfStudy, program.University.Name, program.University.Country,
                result.AcademicFitScore, result.FinancialFitScore, result.CareerFitScore, result.TimelineFitScore,
                result.AcademicNote, result.FinancialNote, result.CareerNote, result.TimelineNote,
                result.AdmissionProbability.PointEstimate, locale), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI narration failed for program {ProgramId}; using deterministic fallback", program.Id);
            return new RecommendationNarrationOutput(
                L10n.Tr(locale,
                    $"Deterministic fit score {result.OverallScore:0.0}/100, based on academic, financial, career and timeline factors (simplified explanation — AI narration unavailable).",
                    $"Расчётная оценка соответствия {result.OverallScore:0.0}/100 по академическим, финансовым, карьерным и временным факторам (упрощённое объяснение — ИИ-комментарий недоступен).",
                    $"Академиялық, қаржылық, мансаптық және уақыттық факторлар бойынша есептелген сәйкестік бағасы {result.OverallScore:0.0}/100 (жеңілдетілген түсіндірме — ЖИ түсіндірмесі қолжетімсіз)."),
                result.AcademicNote, result.FinancialNote, result.CareerNote, result.TimelineNote, true);
        }
    }
}
