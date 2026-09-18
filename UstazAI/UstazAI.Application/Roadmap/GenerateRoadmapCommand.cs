using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Application.Roadmap;

public sealed record GenerateRoadmapCommand(Guid ProfileId, Guid UserId, int ProgramId) : IRequest<List<RoadmapTaskDto>>;

public sealed class GenerateRoadmapHandler(IAppDbContext db, RoadmapService roadmapService)
    : IRequestHandler<GenerateRoadmapCommand, List<RoadmapTaskDto>>
{
    public async Task<List<RoadmapTaskDto>> Handle(GenerateRoadmapCommand cmd, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == cmd.ProfileId && p.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {cmd.ProfileId} not found");

        var tasks = await roadmapService.GenerateAsync(profile, cmd.ProgramId, ct);

        var ids = tasks.Select(t => t.Id).ToList();
        var withDeps = await db.RoadmapTasks
            .Include(t => t.Prerequisites)
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(ct);

        return [.. withDeps.Select(t => t.ToDto())];
    }
}

public sealed record GetRoadmapQuery(Guid ProfileId, Guid UserId) : IRequest<List<RoadmapTaskDto>>;

public sealed class GetRoadmapHandler(IAppDbContext db) : IRequestHandler<GetRoadmapQuery, List<RoadmapTaskDto>>
{
    public async Task<List<RoadmapTaskDto>> Handle(GetRoadmapQuery query, CancellationToken ct)
    {
        var ownsProfile = await db.StudentProfiles.AnyAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct);
        if (!ownsProfile) throw new KeyNotFoundException($"Profile {query.ProfileId} not found");

        var tasks = await db.RoadmapTasks
            .Include(t => t.Prerequisites)
            .Where(t => t.StudentProfileId == query.ProfileId)
            .OrderBy(t => t.DueDate)
            .ToListAsync(ct);

        return [.. tasks.Select(t => t.ToDto())];
    }
}
