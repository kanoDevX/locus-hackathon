using Microsoft.EntityFrameworkCore;
using UstazAI.Domain.Entities;

namespace UstazAI.Application.Common.Interfaces;

/// <summary>
/// Application-layer view of the persistence store. Kept deliberately thin (EF Core's DbSet
/// directly, no extra repository ceremony) — this is a hackathon-pragmatic CQRS choice, not an
/// oversight: Infrastructure's DbContext implements this so Application never references
/// Microsoft.EntityFrameworkCore.SqlServer or connection strings.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<StudentProfile> StudentProfiles { get; }
    DbSet<ProfileSnapshot> ProfileSnapshots { get; }
    DbSet<ProfileDiff> ProfileDiffs { get; }
    DbSet<Domain.Entities.Diagnostics> Diagnostics { get; }
    DbSet<Recommendation> Recommendations { get; }
    DbSet<RoadmapTask> RoadmapTasks { get; }
    DbSet<RoadmapTaskDependency> RoadmapTaskDependencies { get; }
    DbSet<FavoriteProgram> FavoritePrograms { get; }
    DbSet<University> Universities { get; }
    DbSet<ProgramOffering> ProgramOfferings { get; }
    DbSet<Scholarship> Scholarships { get; }
    DbSet<AdmitArchetype> AdmitArchetypes { get; }
    DbSet<AiUsageLog> AiUsageLogs { get; }
    DbSet<AiDecisionLog> AiDecisionLogs { get; }
    DbSet<ExamRecord> ExamRecords { get; }
    DbSet<SupplementaryExamRecord> SupplementaryExamRecords { get; }
    DbSet<AdmissionThreshold> AdmissionThresholds { get; }
    DbSet<EligibilityResult> EligibilityResults { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<Domain.Entities.StudyGuide> StudyGuides { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
