using Microsoft.EntityFrameworkCore;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Infrastructure.Persistence;

public sealed class UstazDbContext(DbContextOptions<UstazDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<ProfileSnapshot> ProfileSnapshots => Set<ProfileSnapshot>();
    public DbSet<ProfileDiff> ProfileDiffs => Set<ProfileDiff>();
    public DbSet<Diagnostics> Diagnostics => Set<Diagnostics>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<RoadmapTask> RoadmapTasks => Set<RoadmapTask>();
    public DbSet<RoadmapTaskDependency> RoadmapTaskDependencies => Set<RoadmapTaskDependency>();
    public DbSet<FavoriteProgram> FavoritePrograms => Set<FavoriteProgram>();
    public DbSet<University> Universities => Set<University>();
    public DbSet<ProgramOffering> ProgramOfferings => Set<ProgramOffering>();
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<AdmitArchetype> AdmitArchetypes => Set<AdmitArchetype>();
    public DbSet<AiUsageLog> AiUsageLogs => Set<AiUsageLog>();
    public DbSet<AiDecisionLog> AiDecisionLogs => Set<AiDecisionLog>();
    public DbSet<ExamRecord> ExamRecords => Set<ExamRecord>();
    public DbSet<SupplementaryExamRecord> SupplementaryExamRecords => Set<SupplementaryExamRecord>();
    public DbSet<AdmissionThreshold> AdmissionThresholds => Set<AdmissionThreshold>();
    public DbSet<EligibilityResult> EligibilityResults => Set<EligibilityResult>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<StudyGuide> StudyGuides => Set<StudyGuide>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.HasMany(x => x.RefreshTokens).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.HasIndex(x => x.TokenHash).IsUnique();
        });

        b.Entity<StudentProfile>(e =>
        {
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Interests).HasJsonConversion();
            e.Property(x => x.TargetCountries).HasJsonConversion();
            e.Property(x => x.Constraints).HasJsonConversion();
            e.Property(x => x.ExamScores).HasJsonConversion();
            e.Property(x => x.Gpa).HasColumnType("decimal(4,2)");

            e.HasMany(x => x.Snapshots).WithOne().HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.DiagnosticsHistory).WithOne().HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Recommendations).WithOne().HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.RoadmapTasks).WithOne().HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Favorites).WithOne().HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.ExamRecords).WithOne().HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.SupplementaryExamRecords).WithOne().HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.EligibilityResults).WithOne().HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.ChatMessages).WithOne().HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.EducationStage).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.FundingTrackPreference).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CollegeBackground).HasNullableJsonConversion();
        });

        b.Entity<ExamRecord>(e =>
        {
            e.Property(x => x.Track).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.SubjectBreakdown).HasJsonConversion();
            e.Property(x => x.Provenance).HasJsonConversion();
            e.Property(x => x.TotalScore).HasColumnType("decimal(6,2)");
            e.HasIndex(x => new { x.StudentProfileId, x.ProfileVersion });
        });

        b.Entity<SupplementaryExamRecord>(e =>
        {
            e.Property(x => x.ExamType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Provenance).HasJsonConversion();
            e.Property(x => x.Score).HasColumnType("decimal(6,2)");
            e.Property(x => x.MaxScore).HasColumnType("decimal(6,2)");
        });

        b.Entity<AdmissionThreshold>(e =>
        {
            e.Property(x => x.Track).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Provenance).HasJsonConversion();
            e.Property(x => x.StateThreshold).HasColumnType("decimal(6,2)");
            e.Property(x => x.UniversityInternalThreshold).HasColumnType("decimal(6,2)");
            e.Property(x => x.HistoricalCutoffMin).HasColumnType("decimal(6,2)");
            e.Property(x => x.HistoricalCutoffMax).HasColumnType("decimal(6,2)");
            e.Property(x => x.HistoricalCutoffMedian).HasColumnType("decimal(6,2)");
            e.HasIndex(x => new { x.ProgramId, x.Track }).IsUnique();
            e.HasOne<ProgramOffering>().WithMany(x => x.AdmissionThresholds).HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<EligibilityResult>(e =>
        {
            e.Property(x => x.Track).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.GrantCompetitiveness).HasJsonConversion();
            e.Property(x => x.Provenance).HasJsonConversion();
            e.HasIndex(x => new { x.StudentProfileId, x.ProfileVersion });
            e.HasOne(x => x.Program).WithMany().HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<FavoriteProgram>(e =>
        {
            e.HasIndex(x => new { x.StudentProfileId, x.ProgramId }).IsUnique();
            e.HasOne<ProgramOffering>().WithMany().HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ProfileDiff>(e =>
        {
            e.Property(x => x.ChangedFields).HasJsonConversion();
            e.Property(x => x.LikelyAffectedStages).HasJsonConversion();
        });

        b.Entity<Diagnostics>(e =>
        {
            e.Property(x => x.Strengths).HasJsonConversion();
            e.Property(x => x.ConstraintsFound).HasJsonConversion();
        });

        b.Entity<University>(e =>
        {
            e.Property(x => x.Provenance).HasJsonConversion();
            e.Property(x => x.Coordinates).HasNullableJsonConversion();
            e.Property(x => x.Environment).HasNullableJsonConversion();
        });

        b.Entity<ProgramOffering>(e =>
        {
            e.Property(x => x.RequiredExams).HasJsonConversion();
            e.Property(x => x.Provenance).HasJsonConversion();
            e.Property(x => x.TuitionPerYearUsd).HasColumnType("decimal(10,2)");
            e.Property(x => x.LivingCostPerYearUsd).HasColumnType("decimal(10,2)");
            e.Property(x => x.MinGpa).HasColumnType("decimal(4,2)");
            e.Property(x => x.ScholarshipCoveragePercent).HasColumnType("decimal(5,2)");
            e.Property(x => x.TypicalAdmitRatePercent).HasColumnType("decimal(5,2)");
            e.Property(x => x.AverageStartingSalaryUsd).HasColumnType("decimal(10,2)");

            e.HasOne(x => x.University).WithMany(x => x.Programs).HasForeignKey(x => x.UniversityId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.AdmitArchetypes).WithOne().HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Scholarships).WithOne().HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Scholarship>(e =>
        {
            e.Property(x => x.Provenance).HasJsonConversion();
            e.Property(x => x.CoveragePercent).HasColumnType("decimal(5,2)");
        });

        b.Entity<AdmitArchetype>(e =>
        {
            e.Property(x => x.GpaMin).HasColumnType("decimal(4,2)");
            e.Property(x => x.GpaMax).HasColumnType("decimal(4,2)");
            e.Property(x => x.ExamScoreMin).HasColumnType("decimal(10,2)");
            e.Property(x => x.ExamScoreMax).HasColumnType("decimal(10,2)");
        });

        b.Entity<Recommendation>(e =>
        {
            e.Property(x => x.AdmissionProbability).HasJsonConversion();
            e.Property(x => x.Provenance).HasJsonConversion();
            e.HasOne(x => x.Program).WithMany().HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AffordabilityTier).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.AffordabilityEffectiveCostUsd).HasColumnType("decimal(10,2)");
            e.Property(x => x.AffordabilityBudgetCeilingUsd).HasColumnType("decimal(10,2)");
        });

        b.Entity<RoadmapTask>(e =>
        {
            e.HasOne<ProgramOffering>().WithMany().HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Resources).HasJsonConversion();
        });

        b.Entity<ChatMessage>(e =>
        {
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.StudentProfileId, x.CreatedAtUtc });
        });

        b.Entity<StudyGuide>(e =>
        {
            e.Property(x => x.Steps).HasJsonConversion();
            e.Property(x => x.Provenance).HasJsonConversion();
            e.HasIndex(x => x.RoadmapTaskId).IsUnique();
            e.HasOne<RoadmapTask>().WithMany().HasForeignKey(x => x.RoadmapTaskId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RoadmapTaskDependency>(e =>
        {
            e.HasIndex(x => new { x.TaskId, x.PrerequisiteTaskId }).IsUnique();
            e.HasOne(x => x.Task).WithMany(x => x.Prerequisites).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.PrerequisiteTask).WithMany().HasForeignKey(x => x.PrerequisiteTaskId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<AiDecisionLog>(e =>
        {
            e.HasIndex(x => x.StudentProfileId);
        });

        b.Entity<AiUsageLog>(e =>
        {
            e.HasIndex(x => x.CreatedAtUtc);
        });

        // Enums stored as their string names — far more debuggable in raw SQL than magic ints.
        b.Entity<StudentProfile>().Property(x => x.PreferredLanguage).HasConversion<string>().HasMaxLength(10);
        b.Entity<StudentProfile>().Property(x => x.BudgetBand).HasConversion<string>().HasMaxLength(20);
        b.Entity<StudentProfile>().Property(x => x.PersonaTone).HasConversion<string>().HasMaxLength(20);
        b.Entity<ProgramOffering>().Property(x => x.DegreeLevel).HasConversion<string>().HasMaxLength(20);
        b.Entity<RoadmapTask>().Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
        b.Entity<RoadmapTask>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Entity<AdmitArchetype>().Property(x => x.BudgetBand).HasConversion<string>().HasMaxLength(20);
        b.Entity<AdmitArchetype>().Property(x => x.Outcome).HasConversion<string>().HasMaxLength(20);
        b.Entity<User>().Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        b.Entity<AiDecisionLog>().Property(x => x.DecisionType).HasConversion<string>().HasMaxLength(30);
    }
}
