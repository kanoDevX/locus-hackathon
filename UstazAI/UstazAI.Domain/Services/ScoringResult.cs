using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Services;

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
