using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;

namespace UstazAI.Domain.Services;

/// <summary>Draft roadmap node keyed by a local string so prerequisite edges (§4 stage 6, DAG)
/// can be wired up before real database ids exist.</summary>
public sealed record RoadmapTaskDraft(
    string Key,
    string Title,
    string Description,
    RoadmapTaskCategory Category,
    DateOnly? DueDate,
    int UrgencyScore,
    int ImpactScore,
    List<string> PrerequisiteKeys);

/// <summary>
/// Deterministic roadmap generator: deadlines are backward-calculated from the program's real
/// application deadline, and prerequisite edges form a genuine DAG rather than a flat checklist.
/// </summary>
public static class RoadmapPlanner
{
    public static List<RoadmapTaskDraft> BuildRoadmap(StudentProfile profile, ProgramOffering program)
    {
        var deadline = program.ApplicationDeadline;
        DateOnly Due(int daysBefore) => deadline.AddDays(-daysBefore);

        var drafts = new List<RoadmapTaskDraft>();
        var examKeys = new List<string>();

        foreach (var req in program.RequiredExams)
        {
            var have = profile.ExamScores.FirstOrDefault(e => e.ExamType == req.ExamType);
            var met = have is not null && have.Score >= req.MinScore;
            if (met) continue;

            var key = $"exam-{req.ExamType}";
            examKeys.Add(key);
            drafts.Add(new RoadmapTaskDraft(key, $"Prepare for {req.ExamType} (target {req.MinScore})",
                $"Reach at least {req.MinScore} on {req.ExamType}, required by {program.Name}.",
                RoadmapTaskCategory.Exam, Due(90), 90, 90, []));
        }

        const string docsKey = "documents";
        drafts.Add(new RoadmapTaskDraft(docsKey, "Collect application documents",
            "Transcript, recommendation letters, passport copy, motivation letter draft.",
            RoadmapTaskCategory.Document, Due(45), 70, 60, [.. examKeys]));

        var priorKeys = new List<string> { docsKey };

        if (program.ScholarshipAvailable)
        {
            const string scholarshipKey = "scholarship";
            drafts.Add(new RoadmapTaskDraft(scholarshipKey, "Apply for scholarship",
                $"{program.Name} offers up to {program.ScholarshipCoveragePercent}% coverage — submit alongside admission.",
                RoadmapTaskCategory.Financial, Due(30), 60, 75, [docsKey]));
            priorKeys.Add(scholarshipKey);
        }

        const string interviewKey = "interview";
        drafts.Add(new RoadmapTaskDraft(interviewKey, "Finalize essay and interview prep",
            "Review motivation essay; rehearse interview answers if the program requires one.",
            RoadmapTaskCategory.Interview, Due(14), 55, 50, [docsKey]));
        priorKeys.Add(interviewKey);

        const string submitKey = "submit";
        drafts.Add(new RoadmapTaskDraft(submitKey, $"Submit application to {program.Name}",
            $"Final submission. Hard deadline: {deadline:yyyy-MM-dd}.",
            RoadmapTaskCategory.Application, Due(0), 100, 100, priorKeys));

        return drafts;
    }
}
