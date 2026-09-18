using UstazAI.Application.Dtos;
using UstazAI.Domain.Entities;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Application.Common;

public static class Mappings
{
    public static DataProvenanceDto ToDto(this DataProvenance p) => new(p.Source, p.IsDemoData, p.LastVerifiedUtc);

    public static UncertaintyEstimateDto ToDto(this UncertaintyEstimate u) =>
        new(u.PointEstimate, u.LowerBound, u.UpperBound, u.SampleSize, u.Basis);

    public static ExamScoreDto ToDto(this ExamScore e) => new(e.ExamType, e.Score, e.MaxScore, e.DateTaken);

    public static ProfileDto ToDto(this StudentProfile p) => new(
        p.Id, p.Version, p.FullName, p.Grade, p.Age, p.PreferredLanguage,
        p.Interests, p.Gpa, [.. p.ExamScores.Select(e => e.ToDto())], p.TargetCountries,
        p.BudgetBand, p.TimelineMonthsToApplication, p.Constraints, p.PersonaTone,
        p.EducationStage, p.FundingTrackPreference, p.CollegeBackground?.ToDto(),
        p.CreatedAtUtc, p.UpdatedAtUtc);

    public static ProgramSummaryDto ToDto(this ProgramOffering p) => new(
        p.Id, p.Name, p.University?.Name ?? "", p.University?.Country ?? "", p.University?.City ?? "",
        p.FieldOfStudy, p.DegreeLevel, p.LanguageOfInstruction, p.TuitionPerYearUsd, p.LivingCostPerYearUsd,
        p.ApplicationDeadline, p.ScholarshipAvailable, p.ScholarshipCoveragePercent, p.TypicalAdmitRatePercent,
        p.AverageStartingSalaryUsd);

    public static RecommendationDto ToDto(this Recommendation r) => new()
    {
        RecommendationId = r.Id, Program = r.Program.ToDto(), RankPosition = r.RankPosition, OverallScore = r.OverallScore,
        AcademicFitScore = r.AcademicFitScore, FinancialFitScore = r.FinancialFitScore,
        CareerFitScore = r.CareerFitScore, TimelineFitScore = r.TimelineFitScore,
        AcademicFitExplanation = r.AcademicFitExplanation, FinancialFitExplanation = r.FinancialFitExplanation,
        CareerFitExplanation = r.CareerFitExplanation, TimelineFitExplanation = r.TimelineFitExplanation,
        NarrativeSummary = r.NarrativeSummary, AdmissionProbability = r.AdmissionProbability.ToDto(),
        Provenance = (r.Program?.Provenance ?? r.Provenance).ToDto(), IsAiNarrated = r.IsAiNarrated, FallbackUsed = r.FallbackUsed,
        AffordabilityTier = r.AffordabilityTier, AffordabilityEffectiveCostUsd = r.AffordabilityEffectiveCostUsd,
        AffordabilityBudgetCeilingUsd = r.AffordabilityBudgetCeilingUsd
    };

    public static ResourceLinkDto ToDto(this ResourceLink r) => new(r.Title, r.Url, r.ResourceType, r.Provenance.ToDto());

    public static RoadmapTaskDto ToDto(this RoadmapTask t) => new(
        t.Id, t.Title, t.Description, t.Category, t.DueDate, t.Status, t.UrgencyScore, t.ImpactScore,
        [.. t.Prerequisites.Select(p => p.PrerequisiteTaskId)], t.Subject, [.. t.Resources.Select(r => r.ToDto())], t.ProgramId);

    public static ChatMessageDto ToDto(this ChatMessage m) => new()
    {
        Id = m.Id, Role = m.Role, Content = m.Content, IsAiGenerated = m.IsAiGenerated,
        FallbackUsed = m.FallbackUsed, CreatedAtUtc = m.CreatedAtUtc
    };

    public static ProfileDiffDto ToDto(this ProfileDiff d) => new(
        d.FromVersion, d.ToVersion, d.ChangedFields, d.ImpactSummary, d.LikelyAffectedStages);

    public static DiagnosticsDto ToDto(this Domain.Entities.Diagnostics d) => new()
    {
        DiagnosticsId = d.Id, ProfileVersion = d.ProfileVersion, Strengths = d.Strengths, ConstraintsFound = d.ConstraintsFound,
        InferredGoal = d.InferredGoal, ConfidenceScore = d.ConfidenceScore, IsAiGenerated = d.IsAiGenerated, FallbackUsed = d.FallbackUsed
    };

    public static SubjectScoreDto ToDto(this SubjectScore s) => new(s.SubjectName, s.Score, s.MaxScore);

    public static CollegeBackgroundDto ToDto(this CollegeBackground c) => new(
        c.CollegeSpecialtyName, c.DiplomaAverageScore, c.GraduationYear, c.TargetSpecialtyMatchesCollegeSpecialty);

    public static ExamRecordDto ToDto(this ExamRecord r) => new(
        r.Id, r.Track, [.. r.SubjectBreakdown.Select(s => s.ToDto())], r.TotalScore, r.DateTaken, r.Provenance.ToDto());

    public static SupplementaryExamRecordDto ToDto(this SupplementaryExamRecord r) => new(
        r.Id, r.ExamType, r.Score, r.MaxScore, r.DateTaken, r.Provenance.ToDto());

    public static EligibilityResultDto ToDto(this EligibilityResult r) => new(
        r.Id, r.Program.ToDto(), r.Track, r.MeetsStateThreshold, r.MeetsUniversityThreshold,
        r.GrantCompetitiveness.ToDto(), r.PaidTrackEligible, r.IsDocumentOnlyVerdict, r.Notes, r.Provenance.ToDto());

    public static StudyGuideStepDto ToDto(this StudyGuideStep s) => new(
        s.StepNumber, s.Title, s.Description, s.EstimatedMinutes, s.VideoSearchQuery,
        $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(s.VideoSearchQuery)}");

    public static StudyGuideDto ToDto(this Domain.Entities.StudyGuide g) => new()
    {
        Id = g.Id, RoadmapTaskId = g.RoadmapTaskId, Subject = g.Subject, Steps = [.. g.Steps.Select(s => s.ToDto())],
        IsAiGenerated = g.IsAiGenerated, FallbackUsed = g.FallbackUsed, Provenance = g.Provenance.ToDto(), CreatedAtUtc = g.CreatedAtUtc
    };
}
