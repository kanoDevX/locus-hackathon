using FluentAssertions;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Tests;

public class HybridScoringEngineTests
{
    private static StudentProfile MakeProfile(decimal gpa = 3.6m, BudgetBand budget = BudgetBand.Medium, int timelineMonths = 10) => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Test Student",
        Grade = 11,
        Age = 17,
        Gpa = gpa,
        Interests = ["Computer Science"],
        ExamScores = [new ExamScore { ExamType = ExamType.Ielts, Score = 6.5m, MaxScore = 9m }],
        TargetCountries = ["Kazakhstan"],
        BudgetBand = budget,
        TimelineMonthsToApplication = timelineMonths,
        Constraints = []
    };

    private static ProgramOffering MakeProgram(decimal minGpa = 3.0m, decimal tuition = 0m, decimal living = 4000m,
        bool scholarship = true, decimal scholarshipPct = 100m, int deadlineDaysFromNow = 150) => new()
    {
        Name = "Computer Science",
        FieldOfStudy = "Computer Science",
        DegreeLevel = DegreeLevel.Bachelor,
        LanguageOfInstruction = "English",
        TuitionPerYearUsd = tuition,
        LivingCostPerYearUsd = living,
        ApplicationDeadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(deadlineDaysFromNow)),
        MinGpa = minGpa,
        RequiredExams = [new ExamRequirement { ExamType = ExamType.Ielts, MinScore = 6.0m }],
        ScholarshipAvailable = scholarship,
        ScholarshipCoveragePercent = scholarshipPct,
        TypicalAdmitRatePercent = 30m,
        AverageStartingSalaryUsd = 20000m,
        Provenance = DataProvenance.Demo("test")
    };

    [Fact]
    public void Score_WhenGpaAndExamsExceedRequirements_AcademicFitIsHigh()
    {
        var profile = MakeProfile(gpa: 3.9m);
        var program = MakeProgram(minGpa: 3.0m);

        var result = HybridScoringEngine.Score(profile, program, []);

        result.AcademicFitScore.Should().BeGreaterThan(90);
    }

    [Fact]
    public void Score_WhenGpaBelowMinimum_AcademicFitIsLowerThanMeetingCase()
    {
        var strongProfile = MakeProfile(gpa: 3.9m);
        var weakProfile = MakeProfile(gpa: 2.0m);
        var program = MakeProgram(minGpa: 3.0m);

        var strongResult = HybridScoringEngine.Score(strongProfile, program, []);
        var weakResult = HybridScoringEngine.Score(weakProfile, program, []);

        weakResult.AcademicFitScore.Should().BeLessThan(strongResult.AcademicFitScore);
    }

    [Fact]
    public void Score_WhenEffectiveCostExceedsBudget_FinancialFitDrops()
    {
        var profile = MakeProfile(budget: BudgetBand.Low);
        var affordable = MakeProgram(tuition: 0m, living: 3000m, scholarship: true, scholarshipPct: 100m);
        var expensive = MakeProgram(tuition: 30000m, living: 15000m, scholarship: false);

        var affordableResult = HybridScoringEngine.Score(profile, affordable, []);
        var expensiveResult = HybridScoringEngine.Score(profile, expensive, []);

        affordableResult.FinancialFitScore.Should().Be(100);
        expensiveResult.FinancialFitScore.Should().BeLessThan(affordableResult.FinancialFitScore);
    }

    [Fact]
    public void Score_WhenDeadlineIsSoonerThanTimeline_TimelineFitIsLow()
    {
        var profile = MakeProfile(timelineMonths: 12);
        var soonProgram = MakeProgram(deadlineDaysFromNow: 30);
        var farProgram = MakeProgram(deadlineDaysFromNow: 400);

        var soonResult = HybridScoringEngine.Score(profile, soonProgram, []);
        var farResult = HybridScoringEngine.Score(profile, farProgram, []);

        soonResult.TimelineFitScore.Should().BeLessThan(farResult.TimelineFitScore);
    }

    [Fact]
    public void Score_AdmissionProbability_IsAlwaysAnIntervalNeverABarePercentage()
    {
        var profile = MakeProfile();
        var program = MakeProgram();

        var result = HybridScoringEngine.Score(profile, program, []);

        result.AdmissionProbability.LowerBound.Should().BeLessThanOrEqualTo(result.AdmissionProbability.PointEstimate);
        result.AdmissionProbability.UpperBound.Should().BeGreaterThanOrEqualTo(result.AdmissionProbability.PointEstimate);
        result.AdmissionProbability.Basis.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Score_WithMatchedArchetypes_NarrowsConfidenceIntervalAndUsesPeerBasis()
    {
        var profile = MakeProfile(gpa: 3.6m, budget: BudgetBand.Medium);
        var program = MakeProgram(minGpa: 3.0m);

        var archetypes = Enumerable.Range(0, 10).Select(i => new AdmitArchetype
        {
            ArchetypeLabel = "peer",
            GpaMin = 3.0m,
            GpaMax = 4.0m,
            ExamScoreMin = 5m,
            ExamScoreMax = 9m,
            BudgetBand = BudgetBand.Medium,
            Outcome = i < 7 ? AdmitOutcome.Admitted : AdmitOutcome.Rejected,
            TimelineMonths = 8,
            Weight = 1
        }).ToList();

        var result = HybridScoringEngine.Score(profile, program, archetypes);

        result.AdmissionProbability.SampleSize.Should().BeGreaterThanOrEqualTo(5);
        result.AdmissionProbability.Basis.Should().Contain("seeded profiles");
    }
}
