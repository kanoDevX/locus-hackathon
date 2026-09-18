using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Application.Recommendations;

public sealed record GetLatestRecommendationsQuery(Guid ProfileId, Guid UserId) : IRequest<List<RecommendationDto>>;

public sealed class GetLatestRecommendationsHandler(IAppDbContext db)
    : IRequestHandler<GetLatestRecommendationsQuery, List<RecommendationDto>>
{
    public async Task<List<RecommendationDto>> Handle(GetLatestRecommendationsQuery query, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {query.ProfileId} not found");

        var latest = await db.Recommendations
            .Include(r => r.Program).ThenInclude(p => p.University)
            .Where(r => r.StudentProfileId == profile.Id && r.ProfileVersion == profile.Version)
            .OrderBy(r => r.RankPosition)
            .ToListAsync(ct);

        return [.. latest.Select(r => r.ToDto())];
    }
}
