using MediatR;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;
using UstazAI.Application.Profiles;
using UstazAI.Domain.Enums;

namespace UstazAI.Endpoints;

public sealed record ProfileRequestDto(
    Guid? ProfileId, string FullName, int Grade, int Age, Locale PreferredLanguage,
    List<string> Interests, decimal? Gpa, List<ExamScoreDto> ExamScores, List<string> TargetCountries,
    BudgetBand BudgetBand, int TimelineMonthsToApplication, List<string> Constraints);

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile").WithTags("Profile").RequireAuthorization();

        group.MapPost("/", async (ProfileRequestDto req, ICurrentUser user, ISender sender, CancellationToken ct) =>
        {
            var cmd = new CreateOrUpdateProfileCommand(
                user.UserId!.Value, req.ProfileId, req.FullName, req.Grade, req.Age, req.PreferredLanguage,
                req.Interests, req.Gpa, req.ExamScores, req.TargetCountries, req.BudgetBand,
                req.TimelineMonthsToApplication, req.Constraints);
            return Results.Ok(await sender.Send(cmd, ct));
        });

        group.MapGet("/mine", async (ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetMyProfileQuery(user.UserId!.Value), ct)));

        group.MapGet("/{profileId:guid}", async (Guid profileId, ICurrentUser user, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProfileQuery(profileId, user.UserId!.Value), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }
}
