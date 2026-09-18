using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Services;

/// <summary>
/// Deterministic, framework-free multi-factor scorer. This is the "not just a chatbot" core:
/// a statistical model computes fit + admission probability first; the AI reasoning layer is
/// only ever allowed to explain these already-computed numbers (see
/// UstazAI.Application.Ai.IAiReasoningService), never to invent its own score.
/// </summary>
public static class HybridScoringEngine
{
    private static readonly Dictionary<BudgetBand, decimal> BudgetCeilingUsd = new()
    {
        [BudgetBand.Low] = 6_000m,
        [BudgetBand.Medium] = 15_000m,
        [BudgetBand.High] = 30_000m,
        [BudgetBand.VeryHigh] = 60_000m
    };

    public static ScoringResult Score(StudentProfile profile, ProgramOffering program, IReadOnlyList<AdmitArchetype> archetypes)
    {
        var academic = ScoreAcademicFit(profile, program, out var academicNote);
        var financial = ScoreFinancialFit(profile, program, out var financialNote);
        var career = ScoreCareerFit(profile, program, out var careerNote);
        var timeline = ScoreTimelineFit(profile, program, out var timelineNote);

        var overall = Math.Round(academic * 0.35 + financial * 0.25 + career * 0.20 + timeline * 0.20, 1);
        var probability = EstimateAdmissionProbability(profile, program, archetypes, academic);

        return new ScoringResult(academic, financial, career, timeline, overall, probability,
            academicNote, financialNote, careerNote, timelineNote);
    }

    private static double ScoreAcademicFit(StudentProfile profile, ProgramOffering program, out string note)
    {
        var components = new List<double>();
        var notes = new List<string>();

        if (program.MinGpa is { } minGpa && minGpa > 0 && profile.Gpa is { } gpa)
        {
            var ratio = Math.Clamp((double)(gpa / minGpa) * 100, 0, 130);
            components.Add(Math.Min(ratio, 100));
            notes.Add(gpa >= minGpa
                ? $"GPA {gpa:0.00} meets the {minGpa:0.00} minimum"
                : $"GPA {gpa:0.00} is below the {minGpa:0.00} minimum");
        }

        foreach (var req in program.RequiredExams)
        {
            var best = profile.ExamScores.Where(e => e.ExamType == req.ExamType)
                .OrderByDescending(e => e.Score).FirstOrDefault();
            if (best is null)
            {
                components.Add(40); // unknown — partial credit, flagged
                notes.Add($"{req.ExamType} score not provided (required min {req.MinScore})");
            }
            else
            {
                var ratio = Math.Clamp((double)(best.Score / req.MinScore) * 100, 0, 130);
                components.Add(Math.Min(ratio, 100));
                notes.Add(best.Score >= req.MinScore
                    ? $"{req.ExamType} {best.Score} meets the required {req.MinScore}"
                    : $"{req.ExamType} {best.Score} is below the required {req.MinScore}");
            }
        }

        note = notes.Count > 0 ? string.Join("; ", notes) : "No academic requirements on record for this program.";
        return components.Count > 0 ? Math.Round(components.Average(), 1) : 60;
    }

    private static double ScoreFinancialFit(StudentProfile profile, ProgramOffering program, out string note)
    {
        var assessment = EvaluateAffordability(profile, program);
        var ceiling = assessment.BudgetCeilingUsd;
        var effectiveCost = assessment.EffectiveCostUsd;

        // "Grant only" applicants aren't paying tuition out of a budget, so the budget band is
        // meaningless for them — what matters is whether a grant/scholarship actually covers it.
        if (profile.FundingTrackPreference == FundingTrackPreference.GrantOnly)
        {
            var coverage = program.ScholarshipAvailable ? program.ScholarshipCoveragePercent : 0m;
            var grantScore = assessment.Tier switch
            {
                AffordabilityTier.Affordable => 100d,
                AffordabilityTier.Stretch => 40d + 50d * (double)coverage / 100d,
                _ => 10d
            };
            note = assessment.Tier switch
            {
                AffordabilityTier.Affordable => "Tuition is fully covered by a grant/scholarship (living costs are still yours)",
                AffordabilityTier.Stretch => $"A grant/scholarship covers only {coverage:0}% of tuition — you'd still pay the rest plus living costs",
                _ => "No grant or scholarship on file for this program — it doesn't fit a grant-only plan"
            };
            return Math.Round(grantScore, 1);
        }

        double score;
        if (assessment.Tier == AffordabilityTier.Affordable)
        {
            score = 100;
            note = program.ScholarshipAvailable
                ? $"Effective cost ${effectiveCost:N0}/yr (after scholarship) fits your {profile.BudgetBand} budget band"
                : $"Total cost ${effectiveCost:N0}/yr fits your {profile.BudgetBand} budget band";
        }
        else
        {
            score = Math.Max(10, (double)(ceiling / effectiveCost) * 100);
            note = $"Effective cost ${effectiveCost:N0}/yr exceeds your {profile.BudgetBand} budget ceiling of ${ceiling:N0}/yr";
        }

        return Math.Round(score, 1);
    }

    /// <summary>Affordability & Fairness Audit (§10.6) — the single source of truth for "does this
    /// program's real cost fit this applicant's budget", shared with ScoreFinancialFit above so
    /// the 0-100 score and the disclosed tier can never silently disagree with each other.</summary>
    public static AffordabilityAssessment EvaluateAffordability(StudentProfile profile, ProgramOffering program)
    {
        if (profile.FundingTrackPreference == FundingTrackPreference.GrantOnly)
        {
            // Tiers here mean grant coverage, not budget fit: covered / partly covered / not covered.
            // EffectiveCostUsd is what remains out of pocket (uncovered tuition + living); no ceiling applies.
            var coverage = program.ScholarshipAvailable ? program.ScholarshipCoveragePercent : 0m;
            var outOfPocket = program.LivingCostPerYearUsd + program.TuitionPerYearUsd * (1 - coverage / 100m);
            var grantTier = program.TuitionPerYearUsd == 0m || coverage >= 100m
                ? AffordabilityTier.Affordable
                : coverage > 0m ? AffordabilityTier.Stretch : AffordabilityTier.OverBudget;
            return new AffordabilityAssessment(grantTier, Math.Round(outOfPocket, 0), 0m);
        }

        var ceiling = BudgetCeilingUsd[profile.BudgetBand];
        var totalCost = program.TuitionPerYearUsd + program.LivingCostPerYearUsd;
        var effectiveCost = program.ScholarshipAvailable
            ? totalCost * (1 - program.ScholarshipCoveragePercent / 100m)
            : totalCost;

        var tier = effectiveCost <= ceiling
            ? AffordabilityTier.Affordable
            : effectiveCost <= ceiling * 1.5m
                ? AffordabilityTier.Stretch
                : AffordabilityTier.OverBudget;

        return new AffordabilityAssessment(tier, Math.Round(effectiveCost, 0), ceiling);
    }

    private static double ScoreCareerFit(StudentProfile profile, ProgramOffering program, out string note)
    {
        var overlap = profile.Interests.Any(i =>
            program.FieldOfStudy.Contains(i, StringComparison.OrdinalIgnoreCase) ||
            program.Name.Contains(i, StringComparison.OrdinalIgnoreCase));

        var baseScore = overlap ? 85 : 55;
        var salaryScore = program.AverageStartingSalaryUsd is { } salary
            ? Math.Min(100, (double)salary / 400)
            : 60;

        var score = Math.Round(baseScore * 0.7 + salaryScore * 0.3, 1);
        note = overlap
            ? $"Field '{program.FieldOfStudy}' matches your stated interests"
            : $"Field '{program.FieldOfStudy}' does not directly match your stated interests";
        return score;
    }

    private static double ScoreTimelineFit(StudentProfile profile, ProgramOffering program, out string note)
    {
        var monthsUntilDeadline = MonthsBetween(DateOnly.FromDateTime(DateTime.UtcNow), program.ApplicationDeadline);
        var slack = monthsUntilDeadline - profile.TimelineMonthsToApplication;

        double score;
        if (slack >= 0)
        {
            score = Math.Min(100, 60 + slack * 5);
            note = $"Deadline is {monthsUntilDeadline} months away — comfortably within your {profile.TimelineMonthsToApplication}-month plan";
        }
        else
        {
            score = Math.Max(10, 100 + slack * 15);
            note = $"Deadline is only {monthsUntilDeadline} months away — tighter than your {profile.TimelineMonthsToApplication}-month plan";
        }

        return Math.Round(score, 1);
    }

    private static int MonthsBetween(DateOnly from, DateOnly to)
    {
        if (to <= from) return 0;
        return (to.Year - from.Year) * 12 + (to.Month - from.Month);
    }

    /// <summary>
    /// Uncertainty-quantified admission probability (§10.3): never a bare percentage. Prefers
    /// matched seeded archetypes; falls back to the program's own base rate, adjusted by
    /// academic fit, with an explicitly widened interval when the peer sample is thin.
    /// </summary>
    private static UncertaintyEstimate EstimateAdmissionProbability(
        StudentProfile profile, ProgramOffering program, IReadOnlyList<AdmitArchetype> archetypes, double academicScore)
    {
        var matched = archetypes.Where(a =>
        {
            var gpaOverlaps = profile.Gpa is null ||
                ((double)profile.Gpa >= (double)a.GpaMin - 0.3 && (double)profile.Gpa <= (double)a.GpaMax + 0.3);
            var budgetMatches = a.BudgetBand == profile.BudgetBand;
            return gpaOverlaps && budgetMatches;
        }).ToList();

        var totalWeight = matched.Sum(a => a.Weight);

        if (totalWeight >= 5)
        {
            var admittedWeight = matched.Where(a => a.Outcome == AdmitOutcome.Admitted).Sum(a => a.Weight);
            var point = (double)admittedWeight / totalWeight * 100;
            var margin = Math.Clamp(50 / Math.Sqrt(totalWeight), 5, 25);
            return new UncertaintyEstimate
            {
                PointEstimate = Math.Round(point, 1),
                LowerBound = Math.Round(Math.Clamp(point - margin, 0, 100), 1),
                UpperBound = Math.Round(Math.Clamp(point + margin, 0, 100), 1),
                SampleSize = totalWeight,
                Basis = $"{totalWeight} similar seeded profiles matched on GPA range and budget band"
            };
        }

        var basePoint = (double)program.TypicalAdmitRatePercent * (0.5 + academicScore / 200.0);
        var wideMargin = 22.0;
        return new UncertaintyEstimate
        {
            PointEstimate = Math.Round(Math.Clamp(basePoint, 2, 98), 1),
            LowerBound = Math.Round(Math.Clamp(basePoint - wideMargin, 0, 100), 1),
            UpperBound = Math.Round(Math.Clamp(basePoint + wideMargin, 0, 100), 1),
            SampleSize = totalWeight,
            Basis = "insufficient matched peer data — estimated from the program's historical base rate, widened for low confidence"
        };
    }
}
