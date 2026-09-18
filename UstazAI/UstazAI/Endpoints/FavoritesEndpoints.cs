using MediatR;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Favorites;

namespace UstazAI.Endpoints;

public sealed record AddFavoriteRequestDto(int ProgramId, string? Note);

public static class FavoritesEndpoints
{
    public static void MapFavoritesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile/{profileId:guid}/favorites").WithTags("Favorites").RequireAuthorization();

        group.MapGet("/", async (Guid profileId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetFavoritesQuery(profileId, user.UserId!.Value), ct)));

        group.MapPost("/", async (Guid profileId, AddFavoriteRequestDto req, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new AddFavoriteCommand(profileId, user.UserId!.Value, req.ProgramId, req.Note), ct)));

        group.MapDelete("/{programId:int}", async (Guid profileId, int programId, ICurrentUser user, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RemoveFavoriteCommand(profileId, user.UserId!.Value, programId), ct);
            return Results.NoContent();
        });
    }
}
