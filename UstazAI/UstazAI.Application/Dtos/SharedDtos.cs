using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;

namespace UstazAI.Application.Dtos;

public sealed record DataProvenanceDto(string Source, bool IsDemoData, DateTime LastVerifiedUtc);

public sealed record UncertaintyEstimateDto(
    double Estimate, double LowerBound, double UpperBound, int SampleSize, string Basis);

public sealed record ExamScoreDto(ExamType ExamType, decimal Score, decimal MaxScore, DateOnly? DateTaken);

public sealed record ProfileDto(
    Guid Id, int Version, string FullName, int Grade, int Age, Locale PreferredLanguage,
    List<string> Interests, decimal? Gpa, List<ExamScoreDto> ExamScores, List<string> TargetCountries,
    BudgetBand BudgetBand, int TimelineMonthsToApplication, List<string> Constraints, PersonaTone PersonaTone,
    EducationStage? EducationStage, FundingTrackPreference FundingTrackPreference, CollegeBackgroundDto? CollegeBackground,
    DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

public sealed record ProfileDiffDto(
    int FromVersion, int ToVersion, List<string> ChangedFields, string ImpactSummary,
    List<string> LikelyAffectedStages);

public sealed record RecommendationDeltaDto(
    bool HasPreviousBatch, int? PreviousProfileVersion, List<int> AddedProgramIds,
    List<int> RemovedProgramIds, List<int> RankChangedProgramIds);

public sealed record AffordabilitySummaryDto(int AffordableCount, int StretchCount, int OverBudgetCount);

public sealed record ProgramSummaryDto(
    int ProgramId, string ProgramName, string UniversityName, string Country, string City,
    string FieldOfStudy, DegreeLevel DegreeLevel, string LanguageOfInstruction,
    decimal TuitionPerYearUsd, decimal LivingCostPerYearUsd, DateOnly ApplicationDeadline,
    bool ScholarshipAvailable, decimal ScholarshipCoveragePercent, decimal TypicalAdmitRatePercent,
    decimal? AverageStartingSalaryUsd);

public sealed class RecommendationDto : ISanitizableAiResponse
{
    public int RecommendationId { get; set; }
    public required ProgramSummaryDto Program { get; set; }
    public int RankPosition { get; set; }
    public double OverallScore { get; set; }
    public double AcademicFitScore { get; set; }
    public double FinancialFitScore { get; set; }
    public double CareerFitScore { get; set; }
    public double TimelineFitScore { get; set; }
    public string AcademicFitExplanation { get; set; } = "";
    public string FinancialFitExplanation { get; set; } = "";
    public string CareerFitExplanation { get; set; } = "";
    public string TimelineFitExplanation { get; set; } = "";
    public string NarrativeSummary { get; set; } = "";
    public required UncertaintyEstimateDto AdmissionProbability { get; set; }
    public required DataProvenanceDto Provenance { get; set; }
    public bool IsAiNarrated { get; set; }
    public bool FallbackUsed { get; set; }
    public AffordabilityTier AffordabilityTier { get; set; }
    public decimal AffordabilityEffectiveCostUsd { get; set; }
    public decimal AffordabilityBudgetCeilingUsd { get; set; }

    public void SanitizeAiText()
    {
        AcademicFitExplanation = GuardrailRules.Sanitize(AcademicFitExplanation);
        FinancialFitExplanation = GuardrailRules.Sanitize(FinancialFitExplanation);
        CareerFitExplanation = GuardrailRules.Sanitize(CareerFitExplanation);
        TimelineFitExplanation = GuardrailRules.Sanitize(TimelineFitExplanation);
        NarrativeSummary = GuardrailRules.Sanitize(NarrativeSummary);
    }
}

public sealed record RoadmapTaskDto(
    int TaskId, string Title, string Description, RoadmapTaskCategory Category, DateOnly? DueDate,
    RoadmapTaskStatus Status, int UrgencyScore, int ImpactScore, List<int> PrerequisiteTaskIds,
    string? Subject, List<ResourceLinkDto> Resources, int? ProgramId);

public sealed record ResourceLinkDto(string Title, string Url, ResourceType ResourceType, DataProvenanceDto Provenance);

public sealed class DiagnosticsDto : ISanitizableAiResponse
{
    public int DiagnosticsId { get; set; }
    public int ProfileVersion { get; set; }
    public List<string> Strengths { get; set; } = [];
    public List<string> ConstraintsFound { get; set; } = [];
    public string InferredGoal { get; set; } = "";
    public double ConfidenceScore { get; set; }
    public bool IsAiGenerated { get; set; }
    public bool FallbackUsed { get; set; }

    public void SanitizeAiText()
    {
        Strengths = [.. Strengths.Select(GuardrailRules.Sanitize)];
        ConstraintsFound = [.. ConstraintsFound.Select(GuardrailRules.Sanitize)];
        InferredGoal = GuardrailRules.Sanitize(InferredGoal);
    }
}

public sealed record FavoriteDto(int FavoriteId, ProgramSummaryDto Program, string? Note, DateTime CreatedAtUtc);

public sealed record CalendarEntryDto(
    string Type, string Title, DateOnly Date, string? Category, int? RelatedProgramId, int? RelatedTaskId);

public sealed record ScholarshipSearchResultDto(
    int ScholarshipId, string Name, decimal CoveragePercent, string EligibilityCriteria,
    DateOnly? DeadlineDate, ProgramSummaryDto? Program, DataProvenanceDto Provenance);

public sealed class ChatMessageDto : ISanitizableAiResponse
{
    public int Id { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = "";
    public bool IsAiGenerated { get; set; }
    public bool FallbackUsed { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public void SanitizeAiText()
    {
        if (Role == ChatRole.Assistant)
            Content = GuardrailRules.Sanitize(Content);
    }
}

public sealed class EssayReviewDto : ISanitizableAiResponse
{
    public List<string> Strengths { get; set; } = [];
    public List<string> SuggestedImprovements { get; set; } = [];
    public string ClarityFeedback { get; set; } = "";
    public string StructureFeedback { get; set; } = "";
    public bool IsAiGenerated { get; set; }
    public bool FallbackUsed { get; set; }

    public void SanitizeAiText()
    {
        Strengths = [.. Strengths.Select(GuardrailRules.Sanitize)];
        SuggestedImprovements = [.. SuggestedImprovements.Select(GuardrailRules.Sanitize)];
        ClarityFeedback = GuardrailRules.Sanitize(ClarityFeedback);
        StructureFeedback = GuardrailRules.Sanitize(StructureFeedback);
    }
}
