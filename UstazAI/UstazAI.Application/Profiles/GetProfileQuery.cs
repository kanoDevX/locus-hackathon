using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Application.Profiles;

public sealed record GetProfileQuery(Guid ProfileId, Guid UserId) : IRequest<ProfileDto?>;

public sealed class GetProfileHandler(IAppDbContext db) : IRequestHandler<GetProfileQuery, ProfileDto?>
{
    public async Task<ProfileDto?> Handle(GetProfileQuery query, CancellationToken ct)
    {
        var profile = await db.StudentProfiles
            .FirstOrDefaultAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct);
        return profile?.ToDto();
    }
}
