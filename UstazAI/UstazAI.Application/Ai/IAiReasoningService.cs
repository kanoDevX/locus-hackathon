using UstazAI.Domain.Enums;

namespace UstazAI.Application.Ai;

public sealed record DiagnosticsAiInput(
    string FullName, int Grade, decimal? Gpa, IReadOnlyList<string> Interests,
    IReadOnlyList<string> TargetCountries, BudgetBand BudgetBand, IReadOnlyList<string> Constraints,
    Locale Locale);

public sealed record DiagnosticsAiOutput(
    List<string> Strengths, List<string> ConstraintsFound, string InferredGoal,
    double ConfidenceScore, bool FallbackUsed);

public sealed record RecommendationNarrationInput(
    string ProgramName, string FieldOfStudy, string University, string Country,
    double AcademicFitScore, double FinancialFitScore, double CareerFitScore, double TimelineFitScore,
    string AcademicNote, string FinancialNote, string CareerNote, string TimelineNote,
    double AdmissionProbabilityPoint, Locale Locale);

public sealed record RecommendationNarrationOutput(
    string NarrativeSummary, string AcademicExplanation, string FinancialExplanation,
    string CareerExplanation, string TimelineExplanation, bool FallbackUsed);

public sealed record PersonaClassificationOutput(PersonaTone Tone, bool FallbackUsed);

public sealed record EssayReviewAiInput(string EssayText, string? Prompt, Locale Locale);

public sealed record EssayReviewAiOutput(
    List<string> Strengths, List<string> SuggestedImprovements, string ClarityFeedback,
    string StructureFeedback, bool FallbackUsed);

public sealed record ChatTurn(string Role, string Content);

public sealed record ChatAiInput(
    string UserMessage, string ScopedContextJson, List<ChatTurn> History, Locale Locale);

public sealed record ChatAiOutput(string Reply, bool FallbackUsed);

public sealed record StudyGuideAiInput(
    string Subject, decimal? CurrentScore, decimal? MaxScore, decimal? GapHeadroomPoints, Locale Locale,
    string TaskCategory = "SubjectPrep", string? TaskDescription = null);

public sealed record StudyGuideStepAiOutput(string Title, string Description, int EstimatedMinutes, string VideoSearchQuery);

public sealed record StudyGuideAiOutput(List<StudyGuideStepAiOutput> Steps, bool FallbackUsed);

public interface IAiReasoningService
{
    Task<DiagnosticsAiOutput> GenerateDiagnosticsAsync(DiagnosticsAiInput input, CancellationToken ct);

    Task<RecommendationNarrationOutput> NarrateRecommendationAsync(RecommendationNarrationInput input, CancellationToken ct);

    Task<PersonaClassificationOutput> ClassifyPersonaAsync(string freeText, Locale locale, CancellationToken ct);

    Task<EssayReviewAiOutput> ReviewEssayAsync(EssayReviewAiInput input, CancellationToken ct);

    Task<ChatAiOutput> ChatAsync(ChatAiInput input, CancellationToken ct);

    Task<StudyGuideAiOutput> GenerateStudyGuideAsync(StudyGuideAiInput input, CancellationToken ct);

    Task<ProgramResearchOutput> ResearchProgramAsync(ProgramResearchInput input, CancellationToken ct);

    Task<ThresholdResearchOutput> ResearchThresholdsAsync(ThresholdResearchInput input, CancellationToken ct);
}

public sealed record ProgramResearchInput(string ProgramName, string UniversityName, string Country, string DegreeLevel);

public sealed record ProgramResearchSource(string Title, string Url);

public sealed record ProgramResearchOutput(
    decimal? TuitionPerYearUsd, decimal? LivingCostPerYearUsd, DateOnly? ApplicationDeadline,
    bool? ScholarshipAvailable, decimal? ScholarshipCoveragePercent, List<ProgramResearchSource> Sources);

public sealed record ThresholdResearchInput(string ProgramName, string UniversityName, string TrackLabel);

public sealed record ThresholdResearchOutput(
    decimal? StateThreshold, decimal? UniversityThreshold, decimal? CutoffMin, decimal? CutoffMax,
    decimal? CutoffMedian, int? CutoffYearsCount, List<ProgramResearchSource> Sources);
