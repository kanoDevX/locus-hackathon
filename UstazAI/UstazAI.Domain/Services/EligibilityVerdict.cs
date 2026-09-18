using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Services;

/// <summary>Pure output of GrantEligibilityEngine for one (ExamRecord, Program) pair — the
/// eligibility-domain counterpart to ScoringResult.</summary>
public sealed record EligibilityVerdict(
    bool MeetsStateThreshold,
    bool MeetsUniversityThreshold,
    UncertaintyEstimate GrantCompetitiveness,
    bool PaidTrackEligible,
    bool IsDocumentOnlyVerdict,
    string Notes);
