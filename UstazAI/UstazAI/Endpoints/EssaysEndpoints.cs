using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Essays;

namespace UstazAI.Endpoints;

public sealed record ReviewEssayRequestDto(string EssayText, string? Prompt);

public static class EssaysEndpoints
{
    public static void MapEssaysEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/profile/{profileId:guid}/essays/review", async (
                Guid profileId, ReviewEssayRequestDto req, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ReviewEssayCommand(profileId, user.UserId!.Value, req.EssayText, req.Prompt), ct)))
            .WithTags("Essays").RequireAuthorization().RequireRateLimiting("ai");
    }
}
