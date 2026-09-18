using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Application.CampusMap;

public sealed record GetCampusMapQuery(Guid ProfileId, Guid UserId, int ProgramId) : IRequest<CampusMapDto>;

public sealed class GetCampusMapHandler(IAppDbContext db, IMapProvider mapProvider)
    : IRequestHandler<GetCampusMapQuery, CampusMapDto>
{
    public async Task<CampusMapDto> Handle(GetCampusMapQuery query, CancellationToken ct)
    {
        var ownsProfile = await db.StudentProfiles.AnyAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct);
        if (!ownsProfile) throw new KeyNotFoundException($"Profile {query.ProfileId} not found");

        var program = await db.ProgramOfferings
            .Include(p => p.University)
            .FirstOrDefaultAsync(p => p.Id == query.ProgramId, ct)
            ?? throw new KeyNotFoundException($"Program {query.ProgramId} not found");

        return CampusMapBuilder.Build(program, mapProvider);
    }
}
