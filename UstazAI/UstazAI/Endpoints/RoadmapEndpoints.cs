using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Roadmap;

namespace UstazAI.Endpoints;

public sealed record GenerateRoadmapRequestDto(int ProgramId);

public static class RoadmapEndpoints
{
    public static void MapRoadmapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile/{profileId:guid}/roadmap").WithTags("Roadmap")
            .RequireAuthorization().RequireRateLimiting("ai");

        group.MapPost("/", async (Guid profileId, GenerateRoadmapRequestDto req, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GenerateRoadmapCommand(profileId, user.UserId!.Value, req.ProgramId), ct)));

        group.MapGet("/", async (Guid profileId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetRoadmapQuery(profileId, user.UserId!.Value), ct)));
    }
}
