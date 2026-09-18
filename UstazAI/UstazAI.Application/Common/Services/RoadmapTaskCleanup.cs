using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;

namespace UstazAI.Application.Common.Services;

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
