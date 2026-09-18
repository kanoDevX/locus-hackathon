using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;

namespace UstazAI.Application.ExamIntake;

public sealed record GetEligibilityResultQuery(Guid ProfileId, Guid UserId) : IRequest<List<EligibilityResultDto>>;

public sealed class GetEligibilityResultHandler(IAppDbContext db) : IRequestHandler<GetEligibilityResultQuery, List<EligibilityResultDto>>
{
    public async Task<List<EligibilityResultDto>> Handle(GetEligibilityResultQuery query, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == query.ProfileId && p.UserId == query.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {query.ProfileId} not found");

        var results = await db.EligibilityResults
            .Include(r => r.Program).ThenInclude(p => p.University)
            .Where(r => r.StudentProfileId == profile.Id && r.ProfileVersion == profile.Version)
            .ToListAsync(ct);

        return [.. results
            .OrderByDescending(r => r.GrantCompetitiveness.PointEstimate)
            .Select(r => r.ToDto())];
    }
}
