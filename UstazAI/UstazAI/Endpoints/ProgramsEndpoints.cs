using MediatR;
using UstazAI.Application.Programs;
using UstazAI.Domain.Enums;

namespace UstazAI.Endpoints;

public static class ProgramsEndpoints
{
    public static void MapProgramsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/programs/search", async (
                string? query, string? country, string? field, DegreeLevel? degreeLevel,
                decimal? maxTuition, bool? scholarshipOnly, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new SearchProgramsQuery(query, country, field, degreeLevel, maxTuition, scholarshipOnly), ct)))
            .WithTags("Programs").RequireAuthorization();

        // Live, source-cited refresh of one catalog program (Gemini + Google Search) — saved on the
        // row, so later reads reuse it with no further AI call.
        app.MapPost("/api/v1/programs/{programId:int}/refresh-from-web", async (int programId, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new RefreshProgramFromWebCommand(programId), ct)))
            .WithTags("Programs").RequireAuthorization().RequireRateLimiting("ai");
        app.MapPost("/api/v1/programs/{programId:int}/refresh-thresholds-from-web", async (
                int programId, AdmissionExamTrack track, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new RefreshThresholdsFromWebCommand(programId, track), ct)))
            .WithTags("Programs").RequireAuthorization().RequireRateLimiting("ai");
    }
}
