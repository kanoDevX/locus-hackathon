using MediatR;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.PeerPathways;

namespace UstazAI.Endpoints;

public static class PeerPathwaysEndpoints
{
    public static void MapPeerPathwaysEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/profile/{profileId:guid}/peer-pathways", async (Guid profileId, int programId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new PeerPathwaysQuery(profileId, user.UserId!.Value, programId), ct)))
            .WithTags("PeerPathways").RequireAuthorization();
    }
}
