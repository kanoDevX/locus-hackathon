using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;

namespace UstazAI.Application.NextAction;

public sealed record NextActionQuery(Guid ProfileId, Guid UserId) : IRequest<RoadmapTaskDto?>;

public sealed class NextActionHandler(IAppDbContext db) : IRequestHandler<NextActionQuery, RoadmapTaskDto?>
{
    public async Task<RoadmapTaskDto?> Handle(NextActionQuery query, CancellationToken ct)
    {
        var ownsProfile = await db.StudentProfiles.AnyAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct);
        if (!ownsProfile) throw new KeyNotFoundException($"Profile {query.ProfileId} not found");

        var tasks = await db.RoadmapTasks
            .Include(t => t.Prerequisites)
            .Where(t => t.StudentProfileId == query.ProfileId && t.Status != RoadmapTaskStatus.Done)
            .ToListAsync(ct);

        var doneIds = (await db.RoadmapTasks
                .Where(t => t.StudentProfileId == query.ProfileId && t.Status == RoadmapTaskStatus.Done)
                .Select(t => t.Id)
                .ToListAsync(ct))
            .ToHashSet();

        var actionable = tasks.Where(t => t.Prerequisites.All(p => doneIds.Contains(p.PrerequisiteTaskId))).ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        RoadmapTask? best = null;
        double bestScore = double.MinValue;

        foreach (var t in actionable)
        {
            var daysUntilDue = t.DueDate is { } due ? Math.Max(0, due.DayNumber - today.DayNumber) : 90;
            var urgencyMultiplier = daysUntilDue switch
            {
                <= 7 => 2.0,
                <= 30 => 1.5,
                <= 60 => 1.1,
                _ => 1.0
            };
            var priority = t.UrgencyScore * urgencyMultiplier * t.ImpactScore;
            if (priority > bestScore)
            {
                bestScore = priority;
                best = t;
            }
        }

        return best?.ToDto();
    }
}
