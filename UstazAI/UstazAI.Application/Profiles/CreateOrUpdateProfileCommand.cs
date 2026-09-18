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

namespace UstazAI.Application.Profiles;

public sealed record CreateOrUpdateProfileCommand(
    Guid UserId,
    Guid? ProfileId,
    string FullName,
    int Grade,
    int Age,
    Locale PreferredLanguage,
    List<string> Interests,
    decimal? Gpa,
    List<ExamScoreDto> ExamScores,
    List<string> TargetCountries,
    BudgetBand BudgetBand,
    int TimelineMonthsToApplication,
    List<string> Constraints) : IRequest<CreateOrUpdateProfileResult>;

public sealed record CreateOrUpdateProfileResult(ProfileDto Profile, ProfileDiffDto? Diff);

public sealed class CreateOrUpdateProfileValidator : AbstractValidator<CreateOrUpdateProfileCommand>
{
    public CreateOrUpdateProfileValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Grade).InclusiveBetween(1, 12);
        RuleFor(x => x.Age).InclusiveBetween(10, 25);
        RuleFor(x => x.Gpa).InclusiveBetween(0, 4.0m).When(x => x.Gpa.HasValue);
        RuleFor(x => x.TimelineMonthsToApplication).InclusiveBetween(0, 60);
        RuleForEach(x => x.ExamScores).ChildRules(score =>
        {
            score.RuleFor(s => s.Score).GreaterThanOrEqualTo(0);
            score.RuleFor(s => s.MaxScore).GreaterThan(0);
        });
    }
}

public sealed class CreateOrUpdateProfileHandler(IAppDbContext db)
    : IRequestHandler<CreateOrUpdateProfileCommand, CreateOrUpdateProfileResult>
{
    public async Task<CreateOrUpdateProfileResult> Handle(CreateOrUpdateProfileCommand cmd, CancellationToken ct)
    {
        // A caller-supplied ProfileId that doesn't resolve to one of THIS user's profiles (a stale
        // client-side id from before a demo reset, a typo, or a foreign id) must fail loudly, not
        // silently fall through to "create a new one" — that would leave the user with two
        // StudentProfile rows for one account (the model assumes exactly one, see
        // GetMyProfileQuery), with exam records/roadmap/favorites entered under the first one now
        // invisible whichever row a later request happens to resolve to. Every other handler in
        // this codebase throws KeyNotFoundException for an unowned/missing id; this one now does
        // too, distinguishing that from the genuine "first-time create" case (ProfileId omitted).
        if (cmd.ProfileId is { } suppliedId)
        {
            var owned = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == suppliedId && p.UserId == cmd.UserId, ct)
                ?? throw new KeyNotFoundException($"Profile {suppliedId} not found");
            return await UpdateAsync(cmd, owned, ct);
        }

        var profile = new StudentProfile
        {
            Id = Guid.NewGuid(),
            UserId = cmd.UserId,
            Version = 1
        };
        Apply(cmd, profile);
        db.StudentProfiles.Add(profile);
        await db.SaveChangesAsync(ct);

        return new CreateOrUpdateProfileResult(profile.ToDto(), null);
    }

    private async Task<CreateOrUpdateProfileResult> UpdateAsync(CreateOrUpdateProfileCommand cmd, StudentProfile existing, CancellationToken ct)
    {
        var before = existing.Clone();

        db.ProfileSnapshots.Add(new ProfileSnapshot
        {
            StudentProfileId = existing.Id,
            Version = existing.Version,
            SerializedStateJson = System.Text.Json.JsonSerializer.Serialize(before.ToDto())
        });

        Apply(cmd, existing);
        existing.Version = before.Version + 1;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        var changedFields = ProfileDiffCalculator.DetectChangedFields(before, existing);
        var diff = BuildDiff(before, existing, changedFields);
        db.ProfileDiffs.Add(diff);

        await db.SaveChangesAsync(ct);

        return new CreateOrUpdateProfileResult(existing.ToDto(), diff.ToDto());
    }

    private static void Apply(CreateOrUpdateProfileCommand cmd, StudentProfile profile)
    {
        profile.FullName = cmd.FullName;
        profile.Grade = cmd.Grade;
        profile.Age = cmd.Age;
        profile.PreferredLanguage = cmd.PreferredLanguage;
        profile.Interests = [.. cmd.Interests];
        profile.Gpa = cmd.Gpa;
        profile.ExamScores = [.. cmd.ExamScores.Select(e => new ExamScore
        {
            ExamType = e.ExamType, Score = e.Score, MaxScore = e.MaxScore, DateTaken = e.DateTaken
        })];
        profile.TargetCountries = [.. cmd.TargetCountries];
        profile.BudgetBand = cmd.BudgetBand;
        profile.TimelineMonthsToApplication = cmd.TimelineMonthsToApplication;
        profile.Constraints = [.. cmd.Constraints];
    }

    private static ProfileDiff BuildDiff(StudentProfile before, StudentProfile after, List<string> changedFields)
    {
        var affectsRecommendations = new[]
        {
            nameof(StudentProfile.BudgetBand), nameof(StudentProfile.Gpa), nameof(StudentProfile.ExamScores),
            nameof(StudentProfile.Interests), nameof(StudentProfile.TargetCountries),
            nameof(StudentProfile.EducationStage), nameof(StudentProfile.FundingTrackPreference)
        };
        var affectsRoadmap = new[]
        {
            nameof(StudentProfile.TimelineMonthsToApplication), nameof(StudentProfile.TargetCountries), nameof(StudentProfile.ExamScores)
        };

        var stages = new List<string>();
        if (changedFields.Intersect(affectsRecommendations).Any()) stages.Add("Recommendations");
        if (changedFields.Intersect(affectsRoadmap).Any()) stages.Add("Roadmap");

        var summary = changedFields.Count == 0
            ? "No tracked fields changed."
            : string.Join(" ", changedFields.Select(f => ProfileDiffCalculator.DescribeChange(f, before, after)))
              + (stages.Count > 0
                  ? $" Call POST /recommendations and/or POST /roadmap to see how {string.Join(" and ", stages)} shift as a result."
                  : "");

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
