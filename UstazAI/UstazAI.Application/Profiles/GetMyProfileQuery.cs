using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Application.Profiles;

public sealed record GetMyProfileQuery(Guid UserId) : IRequest<ProfileDto?>;

public sealed class GetMyProfileHandler(IAppDbContext db) : IRequestHandler<GetMyProfileQuery, ProfileDto?>
{
    public async Task<ProfileDto?> Handle(GetMyProfileQuery query, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == query.UserId, ct);
        return profile?.ToDto();
    }
}
