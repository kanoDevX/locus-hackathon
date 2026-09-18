using UstazAI.Domain.Common;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Entities;

public sealed class University : AuditableEntity<int>
{
    public string Name { get; set; } = default!;
    public string Country { get; set; } = default!;
    public string City { get; set; } = default!;
    public string? WebsiteUrl { get; set; }
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("UstazAI seed dataset");

    public GeoCoordinates? Coordinates { get; set; }
    public EnvironmentProfile? Environment { get; set; }

    public List<ProgramOffering> Programs { get; set; } = [];
}

public sealed class ProgramOffering : AuditableEntity<int>
{
    public int UniversityId { get; set; }
    public University University { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string FieldOfStudy { get; set; } = default!;
    public DegreeLevel DegreeLevel { get; set; }
    public string LanguageOfInstruction { get; set; } = "English";

    public decimal TuitionPerYearUsd { get; set; }
    public decimal LivingCostPerYearUsd { get; set; }
    public DateOnly ApplicationDeadline { get; set; }

    public decimal? MinGpa { get; set; }
    public List<ExamRequirement> RequiredExams { get; set; } = [];

    public bool ScholarshipAvailable { get; set; }
    public decimal ScholarshipCoveragePercent { get; set; }

    public decimal TypicalAdmitRatePercent { get; set; }
    public decimal? AverageStartingSalaryUsd { get; set; }

    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("UstazAI seed dataset");

    public List<AdmitArchetype> AdmitArchetypes { get; set; } = [];
    public List<Scholarship> Scholarships { get; set; } = [];

    public List<AdmissionThreshold> AdmissionThresholds { get; set; } = [];
}

public sealed class Scholarship : AuditableEntity<int>
{
    public int? ProgramId { get; set; }
    public string Name { get; set; } = default!;
    public decimal CoveragePercent { get; set; }
    public string EligibilityCriteria { get; set; } = default!;
    public DateOnly? DeadlineDate { get; set; }
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("UstazAI seed dataset");
}

public sealed class AdmitArchetype : AuditableEntity<int>
{
    public int ProgramId { get; set; }
    public string ArchetypeLabel { get; set; } = default!;
    public decimal GpaMin { get; set; }
    public decimal GpaMax { get; set; }
    public decimal ExamScoreMin { get; set; }
    public decimal ExamScoreMax { get; set; }
    public BudgetBand BudgetBand { get; set; }
    public AdmitOutcome Outcome { get; set; }
    public int TimelineMonths { get; set; }
    public string? CommonBlocker { get; set; }
    public int Weight { get; set; } = 1;
}
