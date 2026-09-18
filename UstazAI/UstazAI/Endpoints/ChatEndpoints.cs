using MediatR;
using UstazAI.Application.Chat;
using UstazAI.Application.Common.Interfaces;

namespace UstazAI.Endpoints;

public sealed record ChatRequestDto(string Message);

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile/{profileId:guid}/chat").WithTags("Chat").RequireAuthorization();

        group.MapPost("/", async (Guid profileId, ChatRequestDto req, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new SendChatMessageCommand(profileId, user.UserId!.Value, req.Message), ct)))
            .RequireRateLimiting("ai");

        group.MapGet("/history", async (Guid profileId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetChatHistoryQuery(profileId, user.UserId!.Value), ct)));
    }
}
