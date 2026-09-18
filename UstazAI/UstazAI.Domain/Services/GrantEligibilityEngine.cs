using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Domain.Services;

/// <summary>
/// Deterministic, framework-free eligibility engine modeling Kazakhstan's real three-tier
/// admission-scoring system (state threshold / university internal threshold / competitive grant
/// cutoff) plus the parallel, non-competitive paid track. Dispatches strictly on
/// <see cref="ExamRecord.Track"/> — this explicit switch is the core of what makes the engine
/// domain-accurate rather than a generic "score vs. cutoff" comparator, and is guarded by a unit
/// test per branch (see GrantEligibilityEngineTests). Never invents a confident-looking cutoff:
/// every number traces back to a seeded <see cref="AdmissionThreshold"/> row, and a missing
/// threshold produces an honestly-flagged low-confidence verdict rather than a fabricated one.
/// </summary>
public static class GrantEligibilityEngine
{
    public static EligibilityVerdict Evaluate(ExamRecord examRecord, AdmissionThreshold? threshold) =>
        examRecord.Track switch
        {
            AdmissionExamTrack.StandardEnt =>
                ScoreBasedVerdict(examRecord, threshold, "Standard ENT (school graduate track)"),

            AdmissionExamTrack.CreativeExam =>
                ScoreBasedVerdict(examRecord, threshold, "Creative exam track (school graduate)"),

            AdmissionExamTrack.ChangingSpecialty =>
                ScoreBasedVerdict(examRecord, threshold, "Changing specialty — scored exactly like a school graduate"),

            AdmissionExamTrack.ContinuingSpecialtyGrant =>
                ScoreBasedVerdict(examRecord, threshold, "Continuing specialty, reduced exam, grant track"),

            // No exam at all on this path — direct admission via the university's own commission.
            // Short-circuits before any threshold lookup; a score-based verdict would be
            // meaningless here and answering "true/false" on a state/university threshold that
            // doesn't apply to this path would misrepresent how this applicant is actually
            // admitted.
            AdmissionExamTrack.ContinuingSpecialtyPaid => DocumentOnlyVerdict(),

            _ => throw new ArgumentOutOfRangeException(nameof(examRecord), examRecord.Track, "Unhandled admission exam track.")
        };

    private static EligibilityVerdict ScoreBasedVerdict(ExamRecord examRecord, AdmissionThreshold? threshold, string trackLabel)
    {
        if (threshold is null)
        {
            return new EligibilityVerdict(
                MeetsStateThreshold: false,
                MeetsUniversityThreshold: false,
                GrantCompetitiveness: new UncertaintyEstimate
                {
                    PointEstimate = 0,
                    LowerBound = 0,
                    UpperBound = 100,
                    SampleSize = 0,
                    Basis = "No admission threshold data seeded for this program/track — cannot assess"
                },
                PaidTrackEligible: false,
                IsDocumentOnlyVerdict: false,
                Notes: $"{trackLabel}: no admission threshold data available for this program yet.");
        }

        var score = examRecord.TotalScore ?? 0;
        var meetsState = score >= threshold.StateThreshold;
        var meetsUniversity = score >= threshold.UniversityInternalThreshold;

        // Paid admission never competes against the grant cutoff — it only needs to clear the
        // program's own (lower) minimum. A student below the competitive cutoff can still enroll
        // on a paid basis if they clear this bar.
        var paidEligible = meetsUniversity;

        var competitiveness = EstimateGrantCompetitiveness(score, threshold);

        var notes = meetsState
            ? meetsUniversity
                ? $"{trackLabel}: meets both the state ({threshold.StateThreshold:0}) and this program's internal ({threshold.UniversityInternalThreshold:0}) thresholds."
                : $"{trackLabel}: meets the state threshold ({threshold.StateThreshold:0}) but not this program's higher internal minimum ({threshold.UniversityInternalThreshold:0}) — paid admission may still be out of reach too."
            : $"{trackLabel}: below the state threshold ({threshold.StateThreshold:0}) — not eligible to compete for a grant on this track.";

        return new EligibilityVerdict(meetsState, meetsUniversity, competitiveness, paidEligible, IsDocumentOnlyVerdict: false, notes);
    }

    /// <summary>Historical cutoff is modeled as a seeded range, never a single deterministic
    /// number (§2) — position within that range maps to a 10-90 point estimate, widened by
    /// sample size exactly like HybridScoringEngine's admission-probability estimate.</summary>
    private static UncertaintyEstimate EstimateGrantCompetitiveness(decimal score, AdmissionThreshold threshold)
    {
        if (threshold.HistoricalCutoffSampleSize <= 0)
        {
            return new UncertaintyEstimate
            {
                PointEstimate = 50,
                LowerBound = 20,
                UpperBound = 80,
                SampleSize = 0,
                Basis = "No historical cutoff data seeded for this program/track — wide, low-confidence estimate"
            };
        }

        double point;
        if (score >= threshold.HistoricalCutoffMax) point = 90;
        else if (score <= threshold.HistoricalCutoffMin) point = 10;
        else
        {
            var range = (double)(threshold.HistoricalCutoffMax - threshold.HistoricalCutoffMin);
            var position = range > 0 ? (double)(score - threshold.HistoricalCutoffMin) / range : 0.5;
            point = 10 + position * 80;
        }

        var margin = Math.Clamp(40 / Math.Sqrt(threshold.HistoricalCutoffSampleSize), 5, 25);
        return new UncertaintyEstimate
        {
            PointEstimate = Math.Round(point, 1),
            LowerBound = Math.Round(Math.Clamp(point - margin, 0, 100), 1),
            UpperBound = Math.Round(Math.Clamp(point + margin, 0, 100), 1),
            SampleSize = threshold.HistoricalCutoffSampleSize,
            Basis = $"historical grant cutoff range {threshold.HistoricalCutoffMin:0}-{threshold.HistoricalCutoffMax:0} " +
                    $"(median {threshold.HistoricalCutoffMedian:0}) over {threshold.HistoricalCutoffSampleSize} prior seeded cohorts"
        };
    }

    private static EligibilityVerdict DocumentOnlyVerdict() => new(
        // Deliberately true/true rather than false/false: these two flags don't apply to this
        // path at all, and defaulting them to "not blocked" avoids a caller that ignores
        // IsDocumentOnlyVerdict misreading this as a failed eligibility check.
        MeetsStateThreshold: true,
        MeetsUniversityThreshold: true,
        GrantCompetitiveness: new UncertaintyEstimate
        {
            PointEstimate = 100,
            LowerBound = 100,
            UpperBound = 100,
            SampleSize = 0,
            Basis = "Not applicable — this path is admitted directly by the university's admission commission, not evaluated against a grant cutoff."
        },
        PaidTrackEligible: true,
        IsDocumentOnlyVerdict: true,
        Notes: "No exam required on this path. Admission is direct via the university's admission commission, contingent on your college diploma and its own paperwork requirements — see your roadmap for the document checklist.");
}
