using MediatR;
using UstazAI.Application.CampusMap;
using UstazAI.Application.Common.Interfaces;

namespace UstazAI.Endpoints;

/// <summary>University map & environment intelligence (§14). Also folded directly into
/// POST /api/v1/comparison (§4 stage 5) — see ProgramComparisonRowDto.CampusMap — rather than
/// existing only as a disconnected standalone screen.</summary>
public static class CampusMapEndpoints
{
    public static void MapCampusMapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile/{profileId:guid}/campus-map/{programId:int}").WithTags("CampusMap").RequireAuthorization();

        group.MapGet("/", async (Guid profileId, int programId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetCampusMapQuery(profileId, user.UserId!.Value, programId), ct)));
    }
}
