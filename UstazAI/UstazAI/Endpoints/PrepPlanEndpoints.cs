using MediatR;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.PrepPlan;

namespace UstazAI.Endpoints;

public static class PrepPlanEndpoints
{
    public static void MapPrepPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile/{profileId:guid}/prep-plan/{programId:int}").WithTags("PrepPlan").RequireAuthorization();

        group.MapPost("/", async (Guid profileId, int programId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GeneratePrepPlanCommand(profileId, user.UserId!.Value, programId), ct)));

        group.MapGet("/", async (Guid profileId, int programId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetPrepPlanQuery(profileId, user.UserId!.Value, programId), ct)));
    }
}
