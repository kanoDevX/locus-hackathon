using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.WhatIf;

namespace UstazAI.Endpoints;

public sealed record WhatIfRequestDto(int ProgramId);

public static class WhatIfEndpoints
{
    public static void MapWhatIfEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/profile/{profileId:guid}/what-if", async (Guid profileId, WhatIfRequestDto req, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new WhatIfQuery(profileId, user.UserId!.Value, req.ProgramId), ct)))
            .WithTags("WhatIf").RequireAuthorization().RequireRateLimiting("ai");
    }
}
