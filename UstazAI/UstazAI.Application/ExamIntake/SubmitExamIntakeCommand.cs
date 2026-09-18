using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Application.ExamIntake;

public sealed record SubjectScoreInput(string SubjectName, decimal Score, decimal MaxScore);
public sealed record SupplementaryExamInput(ExamType ExamType, decimal Score, decimal MaxScore, DateOnly? DateTaken);
public sealed record CollegeBackgroundInput(
    string CollegeSpecialtyName, decimal? DiplomaAverageScore, int? GraduationYear, bool TargetSpecialtyMatchesCollegeSpecialty);

public sealed record SubmitExamIntakeCommand(
    Guid ProfileId,
    Guid UserId,
    EducationStage EducationStage,
    AdmissionExamTrack Track,
    List<SubjectScoreInput> SubjectBreakdown,
    decimal? TotalScore,
    DateOnly? ExamDateTaken,
    CollegeBackgroundInput? CollegeBackground,
    List<SupplementaryExamInput> SupplementaryExams,
    FundingTrackPreference FundingTrackPreference) : IRequest<ExamIntakeResultDto>;

public sealed class SubmitExamIntakeValidator : AbstractValidator<SubmitExamIntakeCommand>
{
    private static readonly AdmissionExamTrack[] SchoolTracks =
        [AdmissionExamTrack.StandardEnt, AdmissionExamTrack.CreativeExam, AdmissionExamTrack.NotTakingEnt];
    private static readonly AdmissionExamTrack[] CollegeTracks =
        [AdmissionExamTrack.ChangingSpecialty, AdmissionExamTrack.ContinuingSpecialtyGrant, AdmissionExamTrack.ContinuingSpecialtyPaid, AdmissionExamTrack.NotTakingEnt];

    public SubmitExamIntakeValidator()
    {
        RuleFor(x => x.EducationStage).IsInEnum();
        RuleFor(x => x.Track).IsInEnum();
        RuleFor(x => x.FundingTrackPreference).IsInEnum();

        RuleForEach(x => x.SupplementaryExams).ChildRules(exam =>
        {
            exam.RuleFor(e => e.Score).GreaterThanOrEqualTo(0);
            exam.RuleFor(e => e.MaxScore).GreaterThan(0);
        });

        RuleFor(x => x).Custom((cmd, context) =>
        {
            var isCollegeStage = cmd.EducationStage is EducationStage.CollegeStudent or EducationStage.CollegeGraduate;

            if (isCollegeStage && !CollegeTracks.Contains(cmd.Track))
                context.AddFailure(nameof(SubmitExamIntakeCommand.Track), $"{cmd.Track} is not a valid track for a college-path applicant.");
            if (!isCollegeStage && !SchoolTracks.Contains(cmd.Track))
                context.AddFailure(nameof(SubmitExamIntakeCommand.Track), $"{cmd.Track} is not a valid track for a school applicant.");

            if (isCollegeStage && cmd.CollegeBackground is null && cmd.Track != AdmissionExamTrack.NotTakingEnt)
                context.AddFailure(nameof(SubmitExamIntakeCommand.CollegeBackground), "College background is required for college-path applicants.");
            if (!isCollegeStage && cmd.CollegeBackground is not null)
                context.AddFailure(nameof(SubmitExamIntakeCommand.CollegeBackground), "College background does not apply to school applicants.");

            if (cmd.CollegeBackground is { } cb)
            {
                if (cb.TargetSpecialtyMatchesCollegeSpecialty && cmd.Track == AdmissionExamTrack.ChangingSpecialty)
                    context.AddFailure(nameof(SubmitExamIntakeCommand.Track),
                        "ChangingSpecialty only applies when the target specialty does not match the college specialty.");
                if (!cb.TargetSpecialtyMatchesCollegeSpecialty && cmd.Track is AdmissionExamTrack.ContinuingSpecialtyGrant or AdmissionExamTrack.ContinuingSpecialtyPaid)
                    context.AddFailure(nameof(SubmitExamIntakeCommand.Track),
                        $"{cmd.Track} only applies when the target specialty matches the college specialty.");
            }

            if (cmd.Track is AdmissionExamTrack.ContinuingSpecialtyPaid or AdmissionExamTrack.NotTakingEnt)
            {
                if (cmd.TotalScore is not null)
                    context.AddFailure(nameof(SubmitExamIntakeCommand.TotalScore), $"{cmd.Track} sits no exam — omit TotalScore.");
                return;
            }

            var maxAllowed = cmd.Track == AdmissionExamTrack.ContinuingSpecialtyGrant ? 70m : 140m;
            if (cmd.TotalScore is null)
                context.AddFailure(nameof(SubmitExamIntakeCommand.TotalScore), "TotalScore is required for this track.");
            else if (cmd.TotalScore < 0 || cmd.TotalScore > maxAllowed)
                context.AddFailure(nameof(SubmitExamIntakeCommand.TotalScore), $"TotalScore must be between 0 and {maxAllowed} for {cmd.Track}.");

            foreach (var subject in cmd.SubjectBreakdown)
            {
                if (subject.Score < 5)
                    context.AddFailure(nameof(SubmitExamIntakeCommand.SubjectBreakdown), $"{subject.SubjectName}: minimum 5 points per discipline.");
                if (subject.Score > subject.MaxScore)
                    context.AddFailure(nameof(SubmitExamIntakeCommand.SubjectBreakdown), $"{subject.SubjectName}: score cannot exceed its max score.");
            }
        });
    }
}

public sealed class SubmitExamIntakeHandler(IAppDbContext db) : IRequestHandler<SubmitExamIntakeCommand, ExamIntakeResultDto>
{
    public async Task<ExamIntakeResultDto> Handle(SubmitExamIntakeCommand cmd, CancellationToken ct)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == cmd.ProfileId && p.UserId == cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"Profile {cmd.ProfileId} not found");

        var before = profile.Clone();

        db.ProfileSnapshots.Add(new ProfileSnapshot
        {
            StudentProfileId = profile.Id,
            Version = profile.Version,
            SerializedStateJson = System.Text.Json.JsonSerializer.Serialize(before.ToDto())
        });

        profile.EducationStage = cmd.EducationStage;
        profile.FundingTrackPreference = cmd.FundingTrackPreference;
        profile.CollegeBackground = cmd.CollegeBackground is { } cb
            ? new CollegeBackground
            {
                CollegeSpecialtyName = cb.CollegeSpecialtyName,
                DiplomaAverageScore = cb.DiplomaAverageScore,
                GraduationYear = cb.GraduationYear,
                TargetSpecialtyMatchesCollegeSpecialty = cb.TargetSpecialtyMatchesCollegeSpecialty
            }
            : null;
        profile.Version = before.Version + 1;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        var changedFields = ProfileDiffCalculator.DetectChangedFields(before, profile);
        var diff = BuildDiff(before, profile, changedFields);
        db.ProfileDiffs.Add(diff);

        var examRecord = new ExamRecord
        {
            StudentProfileId = profile.Id,
            ProfileVersion = profile.Version,
            Track = cmd.Track,
            SubjectBreakdown = [.. cmd.SubjectBreakdown.Select(s => new SubjectScore { SubjectName = s.SubjectName, Score = s.Score, MaxScore = s.MaxScore })],
            TotalScore = cmd.TotalScore,
            DateTaken = cmd.ExamDateTaken
        };
        db.ExamRecords.Add(examRecord);

        var supplementaryRecords = cmd.SupplementaryExams.Select(s => new SupplementaryExamRecord
        {
            StudentProfileId = profile.Id,
            ExamType = s.ExamType,
            Score = s.Score,
            MaxScore = s.MaxScore,
            DateTaken = s.DateTaken
        }).ToList();
        db.SupplementaryExamRecords.AddRange(supplementaryRecords);

        await db.SaveChangesAsync(ct);

        return new ExamIntakeResultDto(
            profile.Id, profile.Version, profile.EducationStage!.Value, profile.FundingTrackPreference,
            examRecord.ToDto(), [.. supplementaryRecords.Select(s => s.ToDto())],
            profile.CollegeBackground?.ToDto(), diff.ToDto());
    }

    private static ProfileDiff BuildDiff(StudentProfile before, StudentProfile after, List<string> changedFields)
    {
        var stages = new List<string> { "Diagnostics", "Recommendations", "Roadmap" };

        var summary = changedFields.Count == 0
            ? "No tracked fields changed."
            : string.Join(" ", changedFields.Select(f => ProfileDiffCalculator.DescribeChange(f, before, after)))
              + " A new exam record was recorded. Call POST /exam-intake/calculate to see your updated eligibility, " +
                $"then {string.Join(", ", stages)} to see how your route shifts as a result.";

        return new ProfileDiff
        {
            StudentProfileId = after.Id,
            FromVersion = before.Version,
            ToVersion = after.Version,
            ChangedFields = changedFields,
            ImpactSummary = summary,
            LikelyAffectedStages = stages
        };
    }
}
