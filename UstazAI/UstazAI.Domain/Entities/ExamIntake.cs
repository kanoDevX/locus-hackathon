using UstazAI.Domain.Common;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Entities;

/// <summary>
/// One domestic exam attempt (ENT-family), versioned so re-takes are tracked rather than
/// overwritten — a new row per submission, kept alongside StudentProfile.Version like
/// Diagnostics/Recommendation already are. <see cref="Track"/> is the single discriminator
/// GrantEligibilityEngine dispatches on; never interpret TotalScore/SubjectBreakdown against the
/// wrong track's threshold table (§12.1/§12.2).
/// </summary>
public sealed class ExamRecord : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public int ProfileVersion { get; set; }

    public AdmissionExamTrack Track { get; set; }
    public List<SubjectScore> SubjectBreakdown { get; set; } = [];

    /// <summary>Null for ContinuingSpecialtyPaid — that path sits no exam at all, so a row is
    /// still created (Track is always resolvable from "the latest ExamRecord") but carries no
    /// score to avoid a misleading "scored zero" reading.</summary>
    public decimal? TotalScore { get; set; }
    public DateOnly? DateTaken { get; set; }
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("Self-reported by applicant");
}

/// <summary>International/supplementary exam score (SAT/IELTS/TOEFL/Duolingo) — reuses the
/// existing ExamType enum rather than introducing a parallel one, since ExamType already covers
/// this subset. Never substitutes for the domestic Track logic above; only extends eligibility
/// for programs abroad or with an English-proficiency requirement.</summary>
public sealed class SupplementaryExamRecord : AuditableEntity<int>
{
    public Guid StudentProfileId { get; set; }
    public ExamType ExamType { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public DateOnly? DateTaken { get; set; }
    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("Self-reported by applicant");
}

/// <summary>
/// The concrete data structure behind the case's three-tier threshold model (§2): a program has
/// a different threshold row per <see cref="Track"/> it accepts applicants under (its
/// StandardEnt bar differs from its ContinuingSpecialtyGrant bar). ContinuingSpecialtyPaid
/// intentionally carries no meaningful state/cutoff figures — paid admission only ever checks
/// <see cref="UniversityInternalThreshold"/> as a nominal minimum, never the grant cutoff.
/// </summary>
public sealed class AdmissionThreshold : AuditableEntity<int>
{
    public int ProgramId { get; set; }
    public AdmissionExamTrack Track { get; set; }

    /// <summary>Ministry-set minimum just to be eligible to compete for a grant at all.</summary>
    public decimal StateThreshold { get; set; }

    /// <summary>The program's own minimum — may only be raised above, never lowered below,
    /// StateThreshold.</summary>
    public decimal UniversityInternalThreshold { get; set; }

    /// <summary>Known only retroactively in reality — modeled here as a seeded historical range,
    /// never a single deterministic cutoff.</summary>
    public decimal HistoricalCutoffMin { get; set; }
    public decimal HistoricalCutoffMax { get; set; }
    public decimal HistoricalCutoffMedian { get; set; }
    public int HistoricalCutoffSampleSize { get; set; }

    public DataProvenance Provenance { get; set; } = DataProvenance.Demo("UstazAI illustrative seed dataset");
}

/// <summary>
/// One persisted eligibility verdict for one (profile version, program) pair — mirrors how
/// Recommendation is one row per (profile version, program). <see cref="IsDocumentOnlyVerdict"/>
/// marks the ContinuingSpecialtyPaid branch, where there is no score-based verdict at all.
/// </summary>
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
