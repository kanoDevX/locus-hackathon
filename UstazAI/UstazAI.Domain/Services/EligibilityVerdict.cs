using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Services;

public sealed record EligibilityVerdict(
    bool MeetsStateThreshold,
    bool MeetsUniversityThreshold,
    UncertaintyEstimate GrantCompetitiveness,
    bool PaidTrackEligible,
    bool IsDocumentOnlyVerdict,
    string Notes);
