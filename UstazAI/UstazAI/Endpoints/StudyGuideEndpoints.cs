using MediatR;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.StudyGuide;

namespace UstazAI.Endpoints;

public sealed record GenerateStudyGuideRequestDto(bool ForceRegenerate = false);

public static class StudyGuideEndpoints
{
    public static void MapStudyGuideEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile/{profileId:guid}/roadmap/tasks/{taskId:int}/study-guide")
            .WithTags("StudyGuide").RequireAuthorization();

        group.MapPost("/", async (Guid profileId, int taskId, GenerateStudyGuideRequestDto? req, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GenerateStudyGuideCommand(profileId, user.UserId!.Value, taskId, req?.ForceRegenerate ?? false), ct)))
            .RequireRateLimiting("ai");

        group.MapGet("/", async (Guid profileId, int taskId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetStudyGuideQuery(profileId, user.UserId!.Value, taskId), ct)));
    }
}
