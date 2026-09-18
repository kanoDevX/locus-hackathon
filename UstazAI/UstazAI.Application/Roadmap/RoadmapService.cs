using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Common.Services;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;

namespace UstazAI.Application.Roadmap;

public sealed class RoadmapService(IAppDbContext db, DecisionLedgerWriter ledger)
{
    public async Task<List<RoadmapTask>> GenerateAsync(StudentProfile profile, int programId, CancellationToken ct)
    {
        var program = await db.ProgramOfferings
            .Include(p => p.University)
            .FirstOrDefaultAsync(p => p.Id == programId, ct)
            ?? throw new KeyNotFoundException($"Program {programId} not found");

        var staleIds = await db.RoadmapTasks
            .Where(t => t.StudentProfileId == profile.Id && t.ProgramId == programId && t.Status == RoadmapTaskStatus.NotStarted)
            .Select(t => t.Id)
            .ToListAsync(ct);
        await RoadmapTaskCleanup.RemoveTasksAsync(db, staleIds, ct);
        await db.SaveChangesAsync(ct);

        var drafts = RoadmapPlanner.BuildRoadmap(profile, program);
        var keyToTask = new Dictionary<string, RoadmapTask>();

        foreach (var d in drafts)
        {
            var task = new RoadmapTask
            {
                StudentProfileId = profile.Id,
                ProgramId = programId,
                Title = d.Title,
                Description = d.Description,
                Category = d.Category,
                DueDate = d.DueDate,
                UrgencyScore = d.UrgencyScore,
                ImpactScore = d.ImpactScore,
                IsAiGenerated = false
            };
            keyToTask[d.Key] = task;
            db.RoadmapTasks.Add(task);
        }

        await db.SaveChangesAsync(ct);

        foreach (var d in drafts)
        {
            var task = keyToTask[d.Key];
            foreach (var prereqKey in d.PrerequisiteKeys)
            {
                db.RoadmapTaskDependencies.Add(new RoadmapTaskDependency
                {
                    TaskId = task.Id,
                    PrerequisiteTaskId = keyToTask[prereqKey].Id
                });
            }
        }

        await ledger.AppendAsync(profile.Id, AiDecisionType.Roadmap, new { programId, TaskCount = drafts.Count }, ct);
        await db.SaveChangesAsync(ct);

        return [.. keyToTask.Values];
    }
}
