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

        var narrations = await Task.WhenAll(scored.Select(x => SafeNarrateAsync(x.Program, x.Result, user.UiLocale ?? profile.PreferredLanguage, ct)));

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
