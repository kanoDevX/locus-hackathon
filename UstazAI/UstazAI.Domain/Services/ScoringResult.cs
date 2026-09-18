using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Services;

/// <summary>
/// Pure output of the deterministic hybrid scorer (§5.1) for one (profile, program) pair.
/// Gemini is only ever allowed to narrate these numbers, never to invent them.
/// </summary>
public sealed record ScoringResult(
    double AcademicFitScore,
    double FinancialFitScore,
    double CareerFitScore,
    double TimelineFitScore,
    double OverallScore,
    UncertaintyEstimate AdmissionProbability,
    string AcademicNote,
    string FinancialNote,
    string CareerNote,
    string TimelineNote);
