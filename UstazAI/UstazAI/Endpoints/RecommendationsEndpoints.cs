using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Recommendations;

namespace UstazAI.Endpoints;

public static class RecommendationsEndpoints
{
    public static void MapRecommendationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile/{profileId:guid}/recommendations").WithTags("Recommendations")
            .RequireAuthorization().RequireRateLimiting("ai");

        group.MapPost("/", async (Guid profileId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GenerateRecommendationsCommand(profileId, user.UserId!.Value), ct)));

        group.MapGet("/", async (Guid profileId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetLatestRecommendationsQuery(profileId, user.UserId!.Value), ct)));
    }
}
