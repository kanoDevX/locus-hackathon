using UstazAI.Domain.Entities;

namespace UstazAI.Domain.Services;

/// <summary>One subject's gap versus the applicant's own exam performance — never versus another
/// student's, so this stays honest about what "improvement" would mean for this exact
/// SubjectScore row.</summary>
public sealed record SubjectGap(
    string SubjectName,
    decimal CurrentScore,
    decimal MaxScore,
    decimal HeadroomPoints,
    int PriorityRank);

/// <summary>
/// Deterministic gap-to-course analysis (§13): given the applicant's own exam subject breakdown
/// and the threshold their target program actually requires, ranks which subjects have the most
/// unclaimed points — the same "no fabricated numbers, only computation over real data" principle
/// as HybridScoringEngine and GrantEligibilityEngine. Gemini never computes a gap; it only narrates
/// the ranked list this engine already produced (see IAiReasoningService.ChatAsync).
/// </summary>
public static class GapAnalysisEngine
{
    /// <summary>Returns subjects ranked by headroom (MaxScore - CurrentScore) descending — the
    /// subjects where the applicant is leaving the most points on the table are the best places
    /// to invest prep time. Returns an empty list when there is no scored subject breakdown to
    /// analyze (e.g. ContinuingSpecialtyPaid, which is document-only and has no exam) or when the
    /// applicant already meets the program's university threshold (no gap to close).</summary>
    public static List<SubjectGap> AnalyzeGaps(ExamRecord examRecord, AdmissionThreshold? threshold)
    {
        if (examRecord.SubjectBreakdown.Count == 0 || examRecord.TotalScore is null)
            return [];

        if (threshold is not null && examRecord.TotalScore >= threshold.UniversityInternalThreshold)
            return [];

        var ranked = examRecord.SubjectBreakdown
            .Select(s => new { s.SubjectName, s.Score, s.MaxScore, Headroom = s.MaxScore - s.Score })
            .Where(s => s.Headroom > 0)
            .OrderByDescending(s => s.Headroom)
            .ToList();

        return [.. ranked.Select((s, i) => new SubjectGap(s.SubjectName, s.Score, s.MaxScore, s.Headroom, PriorityRank: i + 1))];
    }
}
