using UstazAI.Domain.Enums;

namespace UstazAI.Domain.ValueObjects;

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

public sealed class SubjectScore
{
    public string SubjectName { get; set; } = default!;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
}

public sealed class CollegeBackground
{
    public string CollegeSpecialtyName { get; set; } = default!;
    public decimal? DiplomaAverageScore { get; set; }
    public int? GraduationYear { get; set; }
    public bool TargetSpecialtyMatchesCollegeSpecialty { get; set; }
}

public sealed class ResourceLink
{
    public string Title { get; set; } = default!;
    public string Url { get; set; } = default!;
    public ResourceType ResourceType { get; set; }
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("UstazAI curated resource catalog");
}

public sealed class GeoCoordinates
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed class EnvironmentProfile
{
    public decimal CostOfLivingIndexUsdPerMonth { get; set; }
    public int SafetyIndex { get; set; }
    public string ClimateSummary { get; set; } = default!;
    public PublicTransitQuality PublicTransitQuality { get; set; }
    public decimal? InternationalStudentPercent { get; set; }
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("UstazAI seed dataset");
}

public sealed class StudyGuideStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int EstimatedMinutes { get; set; }
    public string VideoSearchQuery { get; set; } = default!;
}
