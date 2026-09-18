using MediatR;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.NextAction;
using UstazAI.Domain.Enums;

namespace UstazAI.Endpoints;

public sealed record UpdateTaskProgressRequestDto(RoadmapTaskStatus Status);

public static class NextActionEndpoints
{
    public static void MapNextActionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/profile/{profileId:guid}/next-action", async (Guid profileId, ICurrentUser user, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new NextActionQuery(profileId, user.UserId!.Value), ct);
            return result is null ? Results.NoContent() : Results.Ok(result);
        }).WithTags("NextAction").RequireAuthorization();

        app.MapPatch("/api/v1/roadmap/tasks/{taskId:int}/progress", async (int taskId, UpdateTaskProgressRequestDto req, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new UpdateTaskProgressCommand(taskId, user.UserId!.Value, req.Status), ct)))
            .WithTags("NextAction").RequireAuthorization();
    }
}
