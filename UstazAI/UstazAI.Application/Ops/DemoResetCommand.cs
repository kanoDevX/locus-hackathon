using MediatR;
using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Application.Common.Services;
using UstazAI.Application.Dtos;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Application.Ops;

public sealed record DemoResetCommand(Guid UserId) : IRequest<ProfileDto>;

public sealed class DemoResetHandler(IAppDbContext db) : IRequestHandler<DemoResetCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(DemoResetCommand cmd, CancellationToken ct)
    {
        var existingProfileIds = await db.StudentProfiles
            .Where(p => p.UserId == cmd.UserId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        foreach (var profileId in existingProfileIds)
        {
            var taskIds = await db.RoadmapTasks.Where(t => t.StudentProfileId == profileId).Select(t => t.Id).ToListAsync(ct);
            await RoadmapTaskCleanup.RemoveTasksAsync(db, taskIds, ct);

            db.Recommendations.RemoveRange(db.Recommendations.Where(r => r.StudentProfileId == profileId));
            db.Diagnostics.RemoveRange(db.Diagnostics.Where(d => d.StudentProfileId == profileId));
            db.FavoritePrograms.RemoveRange(db.FavoritePrograms.Where(f => f.StudentProfileId == profileId));
            db.ProfileSnapshots.RemoveRange(db.ProfileSnapshots.Where(s => s.StudentProfileId == profileId));
            db.ProfileDiffs.RemoveRange(db.ProfileDiffs.Where(d => d.StudentProfileId == profileId));
            db.AiDecisionLogs.RemoveRange(db.AiDecisionLogs.Where(l => l.StudentProfileId == profileId));
            db.ExamRecords.RemoveRange(db.ExamRecords.Where(r => r.StudentProfileId == profileId));
            db.SupplementaryExamRecords.RemoveRange(db.SupplementaryExamRecords.Where(r => r.StudentProfileId == profileId));
            db.EligibilityResults.RemoveRange(db.EligibilityResults.Where(r => r.StudentProfileId == profileId));
            db.ChatMessages.RemoveRange(db.ChatMessages.Where(m => m.StudentProfileId == profileId));
        }
        await db.SaveChangesAsync(ct);

        db.StudentProfiles.RemoveRange(db.StudentProfiles.Where(p => p.UserId == cmd.UserId));
        await db.SaveChangesAsync(ct);

        var demoProfile = new StudentProfile
        {
            Id = Guid.NewGuid(),
            UserId = cmd.UserId,
            Version = 1,
            FullName = "Aigerim Demo Student",
            Grade = 11,
            Age = 17,
            PreferredLanguage = Locale.Ru,
            Interests = ["Computer Science", "Robotics"],
            Gpa = 3.6m,
            ExamScores =
            [
                new ExamScore { ExamType = ExamType.Ielts, Score = 6.5m, MaxScore = 9m, DateTaken = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)) },
                new ExamScore { ExamType = ExamType.Sat, Score = 1300m, MaxScore = 1600m, DateTaken = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)) }
            ],
            TargetCountries = ["Kazakhstan", "South Korea", "Poland"],
            BudgetBand = BudgetBand.Medium,
            TimelineMonthsToApplication = 10,
            Constraints = ["Needs scholarship coverage above 50%"],
            EducationStage = EducationStage.SchoolGrade11,
            FundingTrackPreference = FundingTrackPreference.Flexible
        };

        db.StudentProfiles.Add(demoProfile);
        await db.SaveChangesAsync(ct);

        db.ExamRecords.Add(new ExamRecord
        {
            StudentProfileId = demoProfile.Id,
            ProfileVersion = demoProfile.Version,
            Track = AdmissionExamTrack.StandardEnt,
            SubjectBreakdown =
            [
                new SubjectScore { SubjectName = "Math Literacy", Score = 22, MaxScore = 25 },
                new SubjectScore { SubjectName = "Kazakh/Russian Language", Score = 20, MaxScore = 25 },
                new SubjectScore { SubjectName = "History of Kazakhstan", Score = 18, MaxScore = 25 },
                new SubjectScore { SubjectName = "Mathematics", Score = 26, MaxScore = 35 },
                new SubjectScore { SubjectName = "Physics", Score = 24, MaxScore = 35 }
            ],
            TotalScore = 110,
            DateTaken = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1))
        });
        await db.SaveChangesAsync(ct);

        return demoProfile.ToDto();
    }
}
