using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Enums;

namespace UstazAI.Application.NextAction;

public sealed record UpdateTaskProgressCommand(int TaskId, Guid UserId, RoadmapTaskStatus Status) : IRequest<RoadmapTaskDto>;

public sealed class UpdateTaskProgressHandler(IAppDbContext db) : IRequestHandler<UpdateTaskProgressCommand, RoadmapTaskDto>
{
    public async Task<RoadmapTaskDto> Handle(UpdateTaskProgressCommand cmd, CancellationToken ct)
    {
        var task = await db.RoadmapTasks
            .Include(t => t.Prerequisites)
            .FirstOrDefaultAsync(t => t.Id == cmd.TaskId, ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found");

        var ownsProfile = await db.StudentProfiles.AnyAsync(p => p.Id == task.StudentProfileId && p.UserId == cmd.UserId, ct);
        if (!ownsProfile)
            throw new KeyNotFoundException($"Task {cmd.TaskId} not found");

        task.Status = cmd.Status;
        task.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return task.ToDto();
    }
}
