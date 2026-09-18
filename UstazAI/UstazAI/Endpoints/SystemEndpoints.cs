using MediatR;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Ops;
using UstazAI.Infrastructure.Persistence;

namespace UstazAI.Endpoints;

public static class SystemEndpoints
{
    public static void MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/system").WithTags("System");

        group.MapGet("/health", async (UstazDbContext db, CancellationToken ct) =>
        {
            var canConnect = await db.Database.CanConnectAsync(ct);
            return Results.Ok(new { status = canConnect ? "healthy" : "degraded", database = canConnect, timestampUtc = DateTime.UtcNow });
        }).AllowAnonymous();

        group.MapGet("/insights", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetInsightsQuery(), ct)))
            .RequireAuthorization();

        // Judge Sandbox Mode (§5.8) — guarded to the Judge role so a demo can always be reset
        // to a known scripted starting state.
        group.MapPost("/demo-reset", async (ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new DemoResetCommand(user.UserId!.Value), ct)))
            .RequireAuthorization(p => p.RequireRole("Judge"));
    }
}
