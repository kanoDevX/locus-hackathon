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

/// <summary>One prior turn of the conversation, replayed back to Gemini so a reply can refer to
/// earlier context ("that program you asked about").</summary>
public sealed record ChatTurn(string Role, string Content);

/// <summary>Everything the result-aware chat (§13) is allowed to talk about, already computed by
/// deterministic engines (GrantEligibilityEngine, GapAnalysisEngine, HybridScoringEngine) —
/// Gemini narrates this JSON, it never computes a number from scratch, so a reply can never cite a
/// figure that doesn't already exist in the caller's own data.</summary>
public sealed record ChatAiInput(
    string UserMessage, string ScopedContextJson, List<ChatTurn> History, Locale Locale);

public sealed record ChatAiOutput(string Reply, bool FallbackUsed);

/// <summary>Grounds the study guide in the applicant's own situation without ever asking Gemini
/// to invent a video — `GapHeadroomPoints` is the same number GapAnalysisEngine already computed
/// (never re-derived by the model), and each returned step carries only a *search query*
/// (`VideoSearchQuery`), never a fabricated video title, channel or id. See
/// StudyGuideStep's own doc comment for why.</summary>
public sealed record StudyGuideAiInput(
    string Subject, decimal? CurrentScore, decimal? MaxScore, decimal? GapHeadroomPoints, Locale Locale,
    string TaskCategory = "SubjectPrep", string? TaskDescription = null);

public sealed record StudyGuideStepAiOutput(string Title, string Description, int EstimatedMinutes, string VideoSearchQuery);

public sealed record StudyGuideAiOutput(List<StudyGuideStepAiOutput> Steps, bool FallbackUsed);

/// <summary>
/// Provider-agnostic port for every Gemini call in the product. Infrastructure implements this
/// on top of Microsoft.Extensions.AI's IChatClient so the vendor can be swapped without touching
/// any Application handler. Every method must degrade gracefully (FallbackUsed = true) instead
/// of throwing, so a live demo never goes blank on a rate limit — see
/// UstazAI.Infrastructure.Ai.GeminiReasoningService.
/// </summary>
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

/// <summary>Live web research for one catalog program (Gemini + Google Search grounding). Every
/// numeric field is nullable: the model only fills what it actually found on a cited page.
/// `Sources` are the real URLs Gemini reports it grounded on (from groundingMetadata, not from
/// the model's own text), and are required — no source, no update.</summary>
public sealed record ProgramResearchInput(string ProgramName, string UniversityName, string Country, string DegreeLevel);

public sealed record ProgramResearchSource(string Title, string Url);

public sealed record ProgramResearchOutput(
    decimal? TuitionPerYearUsd, decimal? LivingCostPerYearUsd, DateOnly? ApplicationDeadline,
    bool? ScholarshipAvailable, decimal? ScholarshipCoveragePercent, List<ProgramResearchSource> Sources);

/// <summary>Live web research of Kazakhstan's ENT admission scores for one program + track:
/// national state threshold, the program's own minimum, and the recent grant cutoffs.</summary>
public sealed record ThresholdResearchInput(string ProgramName, string UniversityName, string TrackLabel);

public sealed record ThresholdResearchOutput(
    decimal? StateThreshold, decimal? UniversityThreshold, decimal? CutoffMin, decimal? CutoffMax,
    decimal? CutoffMedian, int? CutoffYearsCount, List<ProgramResearchSource> Sources);
