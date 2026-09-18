using UstazAI.Domain.Entities;

namespace UstazAI.Domain.Services;

/// <summary>
/// Pure field-level comparer at the heart of the Explainable Diff Engine (§5.2). Application
/// layer wraps this with persistence + recommendation re-scoring + SignalR push; this class only
/// answers "what changed" in a way a human can read.
/// </summary>
public static class ProfileDiffCalculator
{
    public static List<string> DetectChangedFields(StudentProfile before, StudentProfile after)
    {
        var changed = new List<string>();

        if (before.BudgetBand != after.BudgetBand) changed.Add(nameof(StudentProfile.BudgetBand));
        if (before.Gpa != after.Gpa) changed.Add(nameof(StudentProfile.Gpa));
        if (!before.TargetCountries.OrderBy(x => x).SequenceEqual(after.TargetCountries.OrderBy(x => x)))
            changed.Add(nameof(StudentProfile.TargetCountries));
        if (!before.Interests.OrderBy(x => x).SequenceEqual(after.Interests.OrderBy(x => x)))
            changed.Add(nameof(StudentProfile.Interests));
        if (before.TimelineMonthsToApplication != after.TimelineMonthsToApplication)
            changed.Add(nameof(StudentProfile.TimelineMonthsToApplication));
        if (!ExamScoresEqual(before, after)) changed.Add(nameof(StudentProfile.ExamScores));
        if (!before.Constraints.OrderBy(x => x).SequenceEqual(after.Constraints.OrderBy(x => x)))
            changed.Add(nameof(StudentProfile.Constraints));
        if (before.EducationStage != after.EducationStage) changed.Add(nameof(StudentProfile.EducationStage));
        if (before.FundingTrackPreference != after.FundingTrackPreference) changed.Add(nameof(StudentProfile.FundingTrackPreference));

        return changed;
    }

    private static bool ExamScoresEqual(StudentProfile before, StudentProfile after)
    {
        if (before.ExamScores.Count != after.ExamScores.Count) return false;
        var b = before.ExamScores.OrderBy(e => e.ExamType).Select(e => (e.ExamType, e.Score)).ToList();
        var a = after.ExamScores.OrderBy(e => e.ExamType).Select(e => (e.ExamType, e.Score)).ToList();
        return b.SequenceEqual(a);
    }

    public static string DescribeChange(string field, StudentProfile before, StudentProfile after) => field switch
    {
        nameof(StudentProfile.BudgetBand) =>
            $"Budget band changed from {before.BudgetBand} to {after.BudgetBand}",
        nameof(StudentProfile.Gpa) =>
            $"GPA changed from {before.Gpa?.ToString("0.00") ?? "unset"} to {after.Gpa?.ToString("0.00") ?? "unset"}",
        nameof(StudentProfile.TargetCountries) =>
            $"Target countries changed from [{string.Join(", ", before.TargetCountries)}] to [{string.Join(", ", after.TargetCountries)}]",
        nameof(StudentProfile.Interests) =>
            $"Interests changed from [{string.Join(", ", before.Interests)}] to [{string.Join(", ", after.Interests)}]",
        nameof(StudentProfile.TimelineMonthsToApplication) =>
            $"Application timeline changed from {before.TimelineMonthsToApplication} to {after.TimelineMonthsToApplication} months",
        nameof(StudentProfile.ExamScores) => "Exam scores were updated",
        nameof(StudentProfile.Constraints) => "Constraints were updated",
        nameof(StudentProfile.EducationStage) =>
            $"Education stage changed from {before.EducationStage?.ToString() ?? "unset"} to {after.EducationStage?.ToString() ?? "unset"}",
        nameof(StudentProfile.FundingTrackPreference) =>
            $"Funding track preference changed from {before.FundingTrackPreference} to {after.FundingTrackPreference}",
        _ => $"{field} changed"
    };
}
