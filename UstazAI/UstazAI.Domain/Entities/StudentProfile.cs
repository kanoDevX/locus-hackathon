using UstazAI.Domain.Common;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Entities;

public sealed class StudentProfile : AuditableEntity<Guid>
{
    public Guid UserId { get; set; }
    public int Version { get; set; } = 1;

    public string FullName { get; set; } = default!;
    public int Grade { get; set; }
    public int Age { get; set; }
    public Locale PreferredLanguage { get; set; } = Locale.Ru;

    public List<string> Interests { get; set; } = [];
    public decimal? Gpa { get; set; }
    public List<ExamScore> ExamScores { get; set; } = [];
    public List<string> TargetCountries { get; set; } = [];
    public BudgetBand BudgetBand { get; set; } = BudgetBand.Medium;
    public int TimelineMonthsToApplication { get; set; }
    public List<string> Constraints { get; set; } = [];

    public PersonaTone PersonaTone { get; set; } = PersonaTone.Neutral;

    public Guid? OrganizationId { get; set; }

    public EducationStage? EducationStage { get; set; }

    public FundingTrackPreference FundingTrackPreference { get; set; } = FundingTrackPreference.Flexible;

    public CollegeBackground? CollegeBackground { get; set; }

    public List<ProfileSnapshot> Snapshots { get; set; } = [];
    public List<Diagnostics> DiagnosticsHistory { get; set; } = [];
    public List<Recommendation> Recommendations { get; set; } = [];
    public List<RoadmapTask> RoadmapTasks { get; set; } = [];
    public List<FavoriteProgram> Favorites { get; set; } = [];
    public List<ExamRecord> ExamRecords { get; set; } = [];
    public List<SupplementaryExamRecord> SupplementaryExamRecords { get; set; } = [];
    public List<EligibilityResult> EligibilityResults { get; set; } = [];
    public List<ChatMessage> ChatMessages { get; set; } = [];
}

public sealed class FavoriteProgram : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int ProgramId { get; set; }
    public string? Note { get; set; }
}

public sealed class ProfileSnapshot : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int Version { get; set; }
    public string SerializedStateJson { get; set; } = default!;
}

public sealed class ProfileDiff : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int FromVersion { get; set; }
    public int ToVersion { get; set; }
    public List<string> ChangedFields { get; set; } = [];
    public string ImpactSummary { get; set; } = default!;
    public List<string> LikelyAffectedStages { get; set; } = [];
}
