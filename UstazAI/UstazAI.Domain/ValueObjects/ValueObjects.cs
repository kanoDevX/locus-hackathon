using UstazAI.Domain.Enums;

namespace UstazAI.Domain.ValueObjects;

/// <summary>
/// Attached to every factual/AI-derived claim so the API never asserts a fact without
/// saying where it came from or flagging it as illustrative demo data.
/// </summary>
public sealed class DataProvenance
{
    public string Source { get; set; } = "UstazAI demo dataset";
    public bool IsDemoData { get; set; } = true;
    public DateTime LastVerifiedUtc { get; set; } = DateTime.UtcNow;

    public static DataProvenance Demo(string source) => new()
    {
        Source = source,
        IsDemoData = true,
        LastVerifiedUtc = DateTime.UtcNow
    };

    public static DataProvenance Verified(string source, DateTime verifiedUtc) => new()
    {
        Source = source,
        IsDemoData = false,
        LastVerifiedUtc = verifiedUtc
    };
}

public sealed class ExamScore
{
    public ExamType ExamType { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public DateOnly? DateTaken { get; set; }
}

/// <summary>
/// Never a bare percentage — carries a confidence interval and sample size so the product
/// cannot present fabricated certainty (see Guardrail pipeline).
/// </summary>
public sealed class UncertaintyEstimate
{
    public double PointEstimate { get; set; }
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public int SampleSize { get; set; }
    public string Basis { get; set; } = "similar seeded archetype profiles";
}

public sealed class ExamRequirement
{
    public ExamType ExamType { get; set; }
    public decimal MinScore { get; set; }
}

/// <summary>One subject's score within an ExamRecord (e.g. "Math Literacy": 68). Kazakhstan's
/// exam tracks are scored per-subject with a "not less than 5 points per discipline" eligibility
/// rule, so the breakdown — not just the total — is load-bearing for GapAnalysisEngine later and
/// for explaining exactly which subject is the binding constraint.</summary>
public sealed class SubjectScore
{
    public string SubjectName { get; set; } = default!;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
}

/// <summary>Attached to StudentProfile only when EducationStage is CollegeStudent/CollegeGraduate.
/// Modeled as a JSON-embedded value object (like ExamScores above) rather than a separate table —
/// it has no independent identity or history requirement beyond what StudentProfile.Version
/// already provides, consistent with how the rest of the profile's attached data is stored.</summary>
public sealed class CollegeBackground
{
    public string CollegeSpecialtyName { get; set; } = default!;
    public decimal? DiplomaAverageScore { get; set; }
    public int? GraduationYear { get; set; }
    public bool TargetSpecialtyMatchesCollegeSpecialty { get; set; }
}

/// <summary>A single curated study resource attached to a SubjectPrep RoadmapTask (§13). Sourced
/// from <c>ResourceLinkCatalog</c> — a fixed, code-reviewed lookup, never AI-generated, so a link
/// can never be hallucinated into a task.</summary>
public sealed class ResourceLink
{
    public string Title { get; set; } = default!;
    public string Url { get; set; } = default!;
    public ResourceType ResourceType { get; set; }
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("UstazAI curated resource catalog");
}

/// <summary>Attached to University, not per-program (§14) — every program at the same campus
/// shares one physical location, so duplicating coordinates per ProgramOffering would just be
/// redundant data that could drift out of sync with itself.</summary>
public sealed class GeoCoordinates
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

/// <summary>Campus-city environment stats (§14), attached to University for the same reason as
/// GeoCoordinates above. Illustrative/demo figures like the rest of the seed catalog — see
/// DataProvenance and the README's disclosure section.</summary>
public sealed class EnvironmentProfile
{
    public decimal CostOfLivingIndexUsdPerMonth { get; set; }
    public int SafetyIndex { get; set; }
    public string ClimateSummary { get; set; } = default!;
    public PublicTransitQuality PublicTransitQuality { get; set; }
    public decimal? InternationalStudentPercent { get; set; }
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("UstazAI seed dataset");
}

/// <summary>One ordered step of a StudyGuide (§ "how do I actually learn this subject"). Carries
/// a `VideoSearchQuery` rather than a specific video — this product never fabricates a YouTube
/// video's title, id or thumbnail (no video-metadata API is wired up), so the honest thing a
/// step can point at is a real, functional YouTube *search* for that exact query, built
/// client-side/DTO-side from this string. See IMapProvider for the same discipline applied to
/// map links.</summary>
public sealed class StudyGuideStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int EstimatedMinutes { get; set; }
    public string VideoSearchQuery { get; set; } = default!;
}
