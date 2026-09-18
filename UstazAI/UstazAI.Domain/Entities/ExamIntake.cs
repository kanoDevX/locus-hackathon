using UstazAI.Domain.Common;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Entities;

public sealed class ExamRecord : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int ProfileVersion { get; set; }

    public AdmissionExamTrack Track { get; set; }
    public List<SubjectScore> SubjectBreakdown { get; set; } = [];

    public decimal? TotalScore { get; set; }
    public DateOnly? DateTaken { get; set; }
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("Self-reported by applicant");
}

public sealed class SupplementaryExamRecord : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public ExamType ExamType { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public DateOnly? DateTaken { get; set; }
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("Self-reported by applicant");
}

public sealed class AdmissionThreshold : AuditableEntity<int>
{
    public int ProgramId { get; set; }
    public AdmissionExamTrack Track { get; set; }

    public decimal StateThreshold { get; set; }

    public decimal UniversityInternalThreshold { get; set; }

    public decimal HistoricalCutoffMin { get; set; }
    public decimal HistoricalCutoffMax { get; set; }
    public decimal HistoricalCutoffMedian { get; set; }
    public int HistoricalCutoffSampleSize { get; set; }

    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("UstazAI illustrative seed dataset");
}

public sealed class EligibilityResult : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int ProfileVersion { get; set; }
    public int ProgramId { get; set; }
    public ProgramOffering Program { get; set; } = default!;

    public AdmissionExamTrack Track { get; set; }
    public bool MeetsStateThreshold { get; set; }
    public bool MeetsUniversityThreshold { get; set; }
    public UncertaintyEstimate GrantCompetitiveness { get; set; } = new();
    public bool PaidTrackEligible { get; set; }
    public bool IsDocumentOnlyVerdict { get; set; }
    public string Notes { get; set; } = default!;
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("UstazAI hybrid eligibility engine");
}
