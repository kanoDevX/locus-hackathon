using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Diagnostics;

namespace UstazAI.Endpoints;

public static class DiagnosticsEndpoints
{
    public static void MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile/{profileId:guid}/diagnostics").WithTags("Diagnostics")
            .RequireAuthorization().RequireRateLimiting("ai");

        group.MapPost("/", async (Guid profileId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GenerateDiagnosticsCommand(profileId, user.UserId!.Value), ct)));
    }
}
