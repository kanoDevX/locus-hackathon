using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Application.Profiles;

/// <summary>
/// Resolves the caller's own profile without the client needing to already know its id —
/// `GET /profile/{id}` alone leaves a returning user with no way to find their profile after a
/// fresh login on a new device/browser or after `activeProfileId` was cleared client-side (it's
/// stored only in localStorage, not derived from the session). One user has exactly one
/// StudentProfile in this product's model (see CreateOrUpdateProfileCommand's upsert-by-UserId
/// behavior when ProfileId is omitted), so "the first one for this UserId" is unambiguous.
/// </summary>
public sealed record GetMyProfileQuery(Guid UserId) : IRequest<ProfileDto?>;

public sealed class GetMyProfileHandler(IAppDbContext db) : IRequestHandler<GetMyProfileQuery, ProfileDto?>
{
    public async Task<ProfileDto?> Handle(GetMyProfileQuery query, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == query.UserId, ct);
        return profile?.ToDto();
    }
}
