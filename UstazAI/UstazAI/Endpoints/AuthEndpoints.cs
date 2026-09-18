using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using UstazAI.Application.Auth;
using UstazAI.Application.Common.Interfaces;

namespace UstazAI.Endpoints;

public sealed record UpdateAccountRequestDto(string DisplayName, string Email);
public sealed record ChangePasswordRequestDto(string CurrentPassword, string NewPassword);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterCommand cmd, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(cmd, ct)))
            .RequireRateLimiting("auth");

        group.MapPost("/login", async (LoginCommand cmd, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(cmd, ct)))
            .RequireRateLimiting("auth");

        group.MapPost("/refresh", async (RefreshTokenCommand cmd, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(cmd, ct)))
            .RequireRateLimiting("auth");

        group.MapPost("/logout", async (LogoutCommand cmd, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(cmd, ct);
            return Results.NoContent();
        });

        group.MapPost("/logout-all", async (ICurrentUser user, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new LogoutAllCommand(user.UserId!.Value), ct);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPut("/account", async (UpdateAccountRequestDto req, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new UpdateAccountCommand(user.UserId!.Value, req.DisplayName, req.Email), ct)))
            .RequireAuthorization();

        group.MapPost("/change-password", async (ChangePasswordRequestDto req, ICurrentUser user, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ChangePasswordCommand(user.UserId!.Value, req.CurrentPassword, req.NewPassword), ct);
            return Results.NoContent();
        }).RequireAuthorization();
    }
}
