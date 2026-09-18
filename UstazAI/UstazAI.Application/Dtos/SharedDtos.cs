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

/// <summary>Batch-level rollup of the Affordability & Fairness Audit (§10.6) — how many of THIS
/// batch's recommendations actually fit the applicant's own stated budget, computed fresh on
/// every call directly from the just-generated Recommendation rows (never persisted separately,
/// so it can never drift from the batch it describes).</summary>
public sealed record AffordabilitySummaryDto(int AffordableCount, int StretchCount, int OverBudgetCount);

public sealed record ProgramSummaryDto(
    int ProgramId, string ProgramName, string UniversityName, string Country, string City,
    string FieldOfStudy, DegreeLevel DegreeLevel, string LanguageOfInstruction,
    decimal TuitionPerYearUsd, decimal LivingCostPerYearUsd, DateOnly ApplicationDeadline,
    bool ScholarshipAvailable, decimal ScholarshipCoveragePercent, decimal TypicalAdmitRatePercent,
    decimal? AverageStartingSalaryUsd);

/// <summary>Mutable (not a record) like ChatMessageDto/EssayReviewDto — this is the single
/// highest-risk surface for fabricated-certainty language (§5.3: admission-probability
/// narration), so GuardrailBehavior's SanitizeAiText last-resort net has to be able to run on it
/// too. It originally couldn't: as a plain record with no ISanitizableAiResponse implementation,
/// every one of its Gemini-narrated *FitExplanation/NarrativeSummary fields reached the client
/// completely unfiltered.</summary>
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

/// <summary>Mutable (not a record) — see RecommendationDto's doc comment for why: Strengths,
/// ConstraintsFound and InferredGoal all carry Gemini-narrated free text that needs
/// GuardrailBehavior's SanitizeAiText last-resort net to run on it.</summary>
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

/// <summary>Mutable (not a record) like EssayReviewDto — GuardrailBehavior's SanitizeAiText
/// mutates Content in place as the last-resort safety net, on top of the explicit
/// GuardrailRules.Sanitize call the chat handler already makes before persisting (§13), so the
/// stored ChatMessage row and the returned DTO can never disagree on what text was shown.</summary>
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
