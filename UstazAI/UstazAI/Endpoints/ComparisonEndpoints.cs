using MediatR;
using UstazAI.Application.Comparison;
using UstazAI.Application.Common.Interfaces;

namespace UstazAI.Endpoints;

public sealed record CompareProgramsRequestDto(Guid ProfileId, List<int> ProgramIds);

public static class ComparisonEndpoints
{
    public static void MapComparisonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/comparison").WithTags("Comparison").RequireAuthorization();

        group.MapPost("/", async (CompareProgramsRequestDto req, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new CompareProgramsQuery(req.ProfileId, user.UserId!.Value, req.ProgramIds), ct)));
    }
}
