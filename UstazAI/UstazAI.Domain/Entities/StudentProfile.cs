using UstazAI.Domain.Common;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Entities;

/// <summary>
/// Aggregate root of the whole journey. Every mutation bumps <see cref="Version"/> and is
/// expected to be captured as a <see cref="ProfileSnapshot"/> so the Diff Engine can explain
/// what changed and which downstream stages are affected.
/// </summary>
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

    /// <summary>Free-text signal used by the persona classifier (see §5.5); latest inferred tone
    /// is cached here so it stays consistent across the whole journey, not per-message.</summary>
    public PersonaTone PersonaTone { get; set; } = PersonaTone.Neutral;

    /// <summary>Nullable, unused today — reserved for a future school/NGO "Institution Mode"
    /// tenant so the schema does not need to change later.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Set once via the exam-intake wizard (§12.1) — the single most important branch
    /// point in the product: which real-world admission path the applicant is on. Null until the
    /// applicant has completed the intake; existing users who signed up before this feature
    /// existed are treated as "not yet taken the intake" rather than defaulted into a guess.</summary>
    public EducationStage? EducationStage { get; set; }

    public FundingTrackPreference FundingTrackPreference { get; set; } = FundingTrackPreference.Flexible;

    /// <summary>Present only when EducationStage is CollegeStudent/CollegeGraduate.</summary>
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

/// <summary>A program the student has bookmarked for later comparison — "favorites" is one of
/// the case brief's own suggested baseline features.</summary>
public sealed class FavoriteProgram : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int ProgramId { get; set; }
    public string? Note { get; set; }
}

/// <summary>Immutable point-in-time capture of a profile, used purely for diffing.</summary>
public sealed class ProfileSnapshot : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int Version { get; set; }
    public string SerializedStateJson { get; set; } = default!;
}

/// <summary>Structured "what changed and why" record produced by the Diff Engine on every
/// profile update (see §5.2). Deliberately stays scoped to field-level change detection — the
/// downstream ranking/roadmap impact is computed by the Recommendations and Roadmap endpoints
/// themselves (each a distinct, independently callable journey stage per §4), which report
/// their own before/after delta rather than the profile mutation reaching into their domain.</summary>
public sealed class ProfileDiff : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int FromVersion { get; set; }
    public int ToVersion { get; set; }
    public List<string> ChangedFields { get; set; } = [];
    public string ImpactSummary { get; set; } = default!;
    public List<string> LikelyAffectedStages { get; set; } = [];
}
