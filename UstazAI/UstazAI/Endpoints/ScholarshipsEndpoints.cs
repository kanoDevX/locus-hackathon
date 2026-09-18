using MediatR;
using UstazAI.Application.Scholarships;

namespace UstazAI.Endpoints;

public static class ScholarshipsEndpoints
{
    public static void MapScholarshipsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/scholarships/search", async (string? country, decimal? minCoverage, string? query, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new SearchScholarshipsQuery(country, minCoverage, query), ct)))
            .WithTags("Scholarships").RequireAuthorization();
    }
}
