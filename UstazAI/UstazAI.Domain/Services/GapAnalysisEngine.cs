using UstazAI.Domain.Entities;

namespace UstazAI.Domain.Services;

public sealed record SubjectGap(
    string SubjectName,
    decimal CurrentScore,
    decimal MaxScore,
    decimal HeadroomPoints,
    int PriorityRank);

public static class GapAnalysisEngine
{
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
