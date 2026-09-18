using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Application.StudyGuide;

public sealed record GetStudyGuideQuery(Guid ProfileId, Guid UserId, int RoadmapTaskId) : IRequest<StudyGuideDto?>;

public sealed class GetStudyGuideHandler(IAppDbContext db) : IRequestHandler<GetStudyGuideQuery, StudyGuideDto?>
{
    public async Task<StudyGuideDto?> Handle(GetStudyGuideQuery query, CancellationToken ct)
    {
        var ownsTask = await db.RoadmapTasks
            .AnyAsync(t => t.Id == query.RoadmapTaskId && t.StudentProfileId == query.ProfileId, ct);
        var ownsProfile = await db.StudentProfiles.AnyAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct);
        if (!ownsProfile || !ownsTask) throw new KeyNotFoundException($"Roadmap task {query.RoadmapTaskId} not found");

        var guide = await db.StudyGuides.FirstOrDefaultAsync(g => g.RoadmapTaskId == query.RoadmapTaskId, ct);
        return guide?.ToDto();
    }
}
