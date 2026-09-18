using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;

namespace UstazAI.Application.Common.Services;

/// <summary>Deletes roadmap tasks together with any dependency edges that reference them (either
/// as the task or as a prerequisite). RoadmapTaskDependency has two FKs into the same
/// RoadmapTask table, which SQL Server refuses to cascade-delete automatically (multiple cascade
/// paths) — so both FKs are configured NoAction and cleanup is always done explicitly here.</summary>
public static class RoadmapTaskCleanup
{
    public static async Task RemoveTasksAsync(IAppDbContext db, IEnumerable<int> taskIds, CancellationToken ct)
    {
        var ids = taskIds.ToList();
        if (ids.Count == 0) return;

        var deps = await db.RoadmapTaskDependencies
            .Where(d => ids.Contains(d.TaskId) || ids.Contains(d.PrerequisiteTaskId))
            .ToListAsync(ct);
        db.RoadmapTaskDependencies.RemoveRange(deps);

        var tasks = await db.RoadmapTasks.Where(t => ids.Contains(t.Id)).ToListAsync(ct);
        db.RoadmapTasks.RemoveRange(tasks);
    }
}
