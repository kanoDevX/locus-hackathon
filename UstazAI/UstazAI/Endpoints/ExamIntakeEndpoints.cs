using MediatR;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.ExamIntake;
using UstazAI.Domain.Enums;

namespace UstazAI.Endpoints;

public sealed record ExamIntakeRequestDto(
    EducationStage EducationStage,
    AdmissionExamTrack Track,
    List<SubjectScoreInput> SubjectBreakdown,
    decimal? TotalScore,
    DateOnly? ExamDateTaken,
    CollegeBackgroundInput? CollegeBackground,
    List<SupplementaryExamInput> SupplementaryExams,
    FundingTrackPreference FundingTrackPreference);

public static class ExamIntakeEndpoints
{
    public static void MapExamIntakeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile/{profileId:guid}/exam-intake").WithTags("ExamIntake").RequireAuthorization();

        group.MapPost("/", async (Guid profileId, ExamIntakeRequestDto req, ICurrentUser user, ISender sender, CancellationToken ct) =>
        {
            var cmd = new SubmitExamIntakeCommand(
                profileId, user.UserId!.Value, req.EducationStage, req.Track, req.SubjectBreakdown, req.TotalScore,
                req.ExamDateTaken, req.CollegeBackground, req.SupplementaryExams, req.FundingTrackPreference);
            return Results.Ok(await sender.Send(cmd, ct));
        });

        group.MapPost("/calculate", async (Guid profileId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new CalculateEligibilityCommand(profileId, user.UserId!.Value), ct)));

        group.MapGet("/result", async (Guid profileId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetEligibilityResultQuery(profileId, user.UserId!.Value), ct)));
    }
}
