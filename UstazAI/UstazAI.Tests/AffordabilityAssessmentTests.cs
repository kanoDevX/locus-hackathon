using FluentAssertions;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Tests;

/// <summary>Affordability & Fairness Audit (§10.6) — HybridScoringEngine.EvaluateAffordability is
/// the deterministic source of truth both the Recommendation entity's persisted tier and
/// FinancialFitScore are derived from, so its three tier boundaries are covered directly.</summary>
public class AffordabilityAssessmentTests
{
    private static StudentProfile MakeProfile(BudgetBand budget) => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Test Student",
        Grade = 11,
        Age = 17,
        Gpa = 3.6m,
        Interests = ["Computer Science"],
        ExamScores = [],
        TargetCountries = ["Kazakhstan"],
        BudgetBand = budget,
        TimelineMonthsToApplication = 10,
        Constraints = []
    };

    private static ProgramOffering MakeProgram(decimal tuition, decimal living, bool scholarship = false, decimal scholarshipPct = 0m) => new()
    {
        Name = "Computer Science",
        FieldOfStudy = "Computer Science",
        DegreeLevel = DegreeLevel.Bachelor,
        LanguageOfInstruction = "English",
        TuitionPerYearUsd = tuition,
        LivingCostPerYearUsd = living,
        ApplicationDeadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(150)),
        RequiredExams = [],
        ScholarshipAvailable = scholarship,
        ScholarshipCoveragePercent = scholarshipPct,
        TypicalAdmitRatePercent = 30m,
        Provenance = DataProvenance.Demo("test")
    };

    [Fact]
    public void EvaluateAffordability_WhenEffectiveCostIsWithinCeiling_TierIsAffordable()
    {
        // BudgetBand.Low ceiling is $6,000/yr.
        var profile = MakeProfile(BudgetBand.Low);
        var program = MakeProgram(tuition: 3000m, living: 2000m);

        var assessment = HybridScoringEngine.EvaluateAffordability(profile, program);

        assessment.Tier.Should().Be(AffordabilityTier.Affordable);
        assessment.EffectiveCostUsd.Should().Be(5000m);
        assessment.BudgetCeilingUsd.Should().Be(6000m);
    }

    [Fact]
    public void EvaluateAffordability_WhenEffectiveCostExceedsCeilingByLessThanHalf_TierIsStretch()
    {
        // $8,000 effective cost vs a $6,000 ceiling — 33% over, within the 50% stretch band.
        var profile = MakeProfile(BudgetBand.Low);
        var program = MakeProgram(tuition: 5000m, living: 3000m);

        var assessment = HybridScoringEngine.EvaluateAffordability(profile, program);

        assessment.Tier.Should().Be(AffordabilityTier.Stretch);
    }

    [Fact]
    public void EvaluateAffordability_WhenEffectiveCostExceedsCeilingByMoreThanHalf_TierIsOverBudget()
    {
        // $20,000 effective cost vs a $6,000 ceiling — more than 50% over.
        var profile = MakeProfile(BudgetBand.Low);
        var program = MakeProgram(tuition: 15000m, living: 5000m);

        var assessment = HybridScoringEngine.EvaluateAffordability(profile, program);

        assessment.Tier.Should().Be(AffordabilityTier.OverBudget);
    }

    [Fact]
    public void EvaluateAffordability_AccountsForScholarshipCoverage()
    {
        // $30,000 total cost, but a 100%-covering scholarship makes it fully affordable.
        var profile = MakeProfile(BudgetBand.Low);
        var program = MakeProgram(tuition: 20000m, living: 10000m, scholarship: true, scholarshipPct: 100m);

        var assessment = HybridScoringEngine.EvaluateAffordability(profile, program);

        assessment.Tier.Should().Be(AffordabilityTier.Affordable);
        assessment.EffectiveCostUsd.Should().Be(0m);
    }

    [Fact]
    public void EvaluateAffordability_AgreesWithFinancialFitScore_AffordableTierAlwaysScoresMax()
    {
        var profile = MakeProfile(BudgetBand.Medium);
        var affordableProgram = MakeProgram(tuition: 5000m, living: 3000m);
        var overBudgetProgram = MakeProgram(tuition: 40000m, living: 10000m);

        var affordableAssessment = HybridScoringEngine.EvaluateAffordability(profile, affordableProgram);
        var affordableScore = HybridScoringEngine.Score(profile, affordableProgram, []);
        var overBudgetAssessment = HybridScoringEngine.EvaluateAffordability(profile, overBudgetProgram);
        var overBudgetScore = HybridScoringEngine.Score(profile, overBudgetProgram, []);

        affordableAssessment.Tier.Should().Be(AffordabilityTier.Affordable);
        affordableScore.FinancialFitScore.Should().Be(100);
        overBudgetAssessment.Tier.Should().Be(AffordabilityTier.OverBudget);
        overBudgetScore.FinancialFitScore.Should().BeLessThan(affordableScore.FinancialFitScore);
    }

    [Fact]
    public void GrantOnly_IgnoresBudgetBand_AndTiersByGrantCoverage()
    {
        // A "Low" budget would call a $40k program OverBudget — but a grant-only applicant isn't
        // paying tuition from a budget; only coverage matters.
        var profile = MakeProfile(BudgetBand.Low);
        profile.FundingTrackPreference = FundingTrackPreference.GrantOnly;

        var covered = HybridScoringEngine.EvaluateAffordability(profile, MakeProgram(30000m, 5000m, scholarship: true, scholarshipPct: 100m));
        var partial = HybridScoringEngine.EvaluateAffordability(profile, MakeProgram(30000m, 5000m, scholarship: true, scholarshipPct: 50m));
        var none = HybridScoringEngine.EvaluateAffordability(profile, MakeProgram(30000m, 5000m));

        covered.Tier.Should().Be(AffordabilityTier.Affordable);
        covered.EffectiveCostUsd.Should().Be(5000m, "only living costs remain");
        partial.Tier.Should().Be(AffordabilityTier.Stretch);
        none.Tier.Should().Be(AffordabilityTier.OverBudget);
    }
}
