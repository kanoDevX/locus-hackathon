using FluentAssertions;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;

namespace UstazAI.Tests;

public class GrantEligibilityEngineTests
{
    private static ExamRecord MakeRecord(AdmissionExamTrack track, decimal? totalScore) => new()
    {
        StudentProfileId = Guid.NewGuid(),
        ProfileVersion = 1,
        Track = track,
        TotalScore = totalScore,
        SubjectBreakdown = []
    };

    private static AdmissionThreshold MakeThreshold(
        AdmissionExamTrack track, decimal state, decimal university,
        decimal cutoffMin, decimal cutoffMax, decimal cutoffMedian, int sampleSize) => new()
    {
        ProgramId = 1,
        Track = track,
        StateThreshold = state,
        UniversityInternalThreshold = university,
        HistoricalCutoffMin = cutoffMin,
        HistoricalCutoffMax = cutoffMax,
        HistoricalCutoffMedian = cutoffMedian,
        HistoricalCutoffSampleSize = sampleSize
    };

    // --- StandardEnt -------------------------------------------------------------------------

    [Fact]
    public void StandardEnt_ScoreAboveBothThresholds_IsEligibleForStateAndUniversity()
    {
        var record = MakeRecord(AdmissionExamTrack.StandardEnt, totalScore: 95);
        var threshold = MakeThreshold(AdmissionExamTrack.StandardEnt, state: 50, university: 80, cutoffMin: 85, cutoffMax: 110, cutoffMedian: 95, sampleSize: 20);

        var verdict = GrantEligibilityEngine.Evaluate(record, threshold);

        verdict.MeetsStateThreshold.Should().BeTrue();
        verdict.MeetsUniversityThreshold.Should().BeTrue();
        verdict.PaidTrackEligible.Should().BeTrue();
        verdict.IsDocumentOnlyVerdict.Should().BeFalse();
    }

    [Fact]
    public void StandardEnt_ScoreBelowStateThreshold_IsIneligibleEverywhere()
    {
        var record = MakeRecord(AdmissionExamTrack.StandardEnt, totalScore: 40);
        var threshold = MakeThreshold(AdmissionExamTrack.StandardEnt, state: 50, university: 80, cutoffMin: 85, cutoffMax: 110, cutoffMedian: 95, sampleSize: 20);

        var verdict = GrantEligibilityEngine.Evaluate(record, threshold);

        verdict.MeetsStateThreshold.Should().BeFalse();
        verdict.MeetsUniversityThreshold.Should().BeFalse();
        verdict.PaidTrackEligible.Should().BeFalse();
    }

    [Fact]
    public void StandardEnt_MeetsStateButNotUniversityMinimum_IsGrantIneligibleButStateEligible()
    {
        var record = MakeRecord(AdmissionExamTrack.StandardEnt, totalScore: 60);
        var threshold = MakeThreshold(AdmissionExamTrack.StandardEnt, state: 50, university: 80, cutoffMin: 85, cutoffMax: 110, cutoffMedian: 95, sampleSize: 20);

        var verdict = GrantEligibilityEngine.Evaluate(record, threshold);

        verdict.MeetsStateThreshold.Should().BeTrue();
        verdict.MeetsUniversityThreshold.Should().BeFalse();
        verdict.PaidTrackEligible.Should().BeFalse();
    }

    // --- CreativeExam and ChangingSpecialty share StandardEnt's threshold table ---------------

    [Fact]
    public void CreativeExam_UsesSameThresholdLogicAsStandardEnt()
    {
        var record = MakeRecord(AdmissionExamTrack.CreativeExam, totalScore: 95);
        var threshold = MakeThreshold(AdmissionExamTrack.CreativeExam, state: 50, university: 80, cutoffMin: 85, cutoffMax: 110, cutoffMedian: 95, sampleSize: 20);

        var verdict = GrantEligibilityEngine.Evaluate(record, threshold);

        verdict.MeetsStateThreshold.Should().BeTrue();
        verdict.MeetsUniversityThreshold.Should().BeTrue();
        verdict.IsDocumentOnlyVerdict.Should().BeFalse();
    }

    [Fact]
    public void ChangingSpecialty_IsScoredExactlyLikeASchoolGraduate()
    {
        var record = MakeRecord(AdmissionExamTrack.ChangingSpecialty, totalScore: 45);
        var threshold = MakeThreshold(AdmissionExamTrack.ChangingSpecialty, state: 50, university: 80, cutoffMin: 85, cutoffMax: 110, cutoffMedian: 95, sampleSize: 20);

        var verdict = GrantEligibilityEngine.Evaluate(record, threshold);

        // Below the (school-graduate-scale, 140-point) state threshold — same rule as StandardEnt.
        verdict.MeetsStateThreshold.Should().BeFalse();
        verdict.IsDocumentOnlyVerdict.Should().BeFalse();
    }

    // --- ContinuingSpecialtyGrant: reduced 70-point scale, lower thresholds -------------------

    [Fact]
    public void ContinuingSpecialtyGrant_UsesTheReducedThresholdTable_NotTheSchoolGraduateOne()
    {
        // A score of 30 would fail the 50-point school-graduate state threshold but clears the
        // reduced track's much lower 25-point bar — proves the engine looks up the *matching*
        // threshold row rather than reusing school-graduate numbers.
        var record = MakeRecord(AdmissionExamTrack.ContinuingSpecialtyGrant, totalScore: 30);
        var threshold = MakeThreshold(AdmissionExamTrack.ContinuingSpecialtyGrant, state: 25, university: 40, cutoffMin: 45, cutoffMax: 65, cutoffMedian: 55, sampleSize: 12);

        var verdict = GrantEligibilityEngine.Evaluate(record, threshold);

        verdict.MeetsStateThreshold.Should().BeTrue();
        verdict.IsDocumentOnlyVerdict.Should().BeFalse();
    }

    [Fact]
    public void ContinuingSpecialtyGrant_BelowReducedStateThreshold_IsIneligible()
    {
        var record = MakeRecord(AdmissionExamTrack.ContinuingSpecialtyGrant, totalScore: 15);
        var threshold = MakeThreshold(AdmissionExamTrack.ContinuingSpecialtyGrant, state: 25, university: 40, cutoffMin: 45, cutoffMax: 65, cutoffMedian: 55, sampleSize: 12);

        var verdict = GrantEligibilityEngine.Evaluate(record, threshold);

        verdict.MeetsStateThreshold.Should().BeFalse();
    }

    // --- ContinuingSpecialtyPaid: no exam, short-circuits to a document-only verdict ----------

    [Fact]
    public void ContinuingSpecialtyPaid_ShortCircuitsToDocumentOnlyVerdict_RegardlessOfThreshold()
    {
        var record = MakeRecord(AdmissionExamTrack.ContinuingSpecialtyPaid, totalScore: null);
        // Even if a threshold row happens to exist for this track, the paid-direct path must
        // never evaluate a score against it.
        var threshold = MakeThreshold(AdmissionExamTrack.ContinuingSpecialtyPaid, state: 999, university: 999, cutoffMin: 999, cutoffMax: 999, cutoffMedian: 999, sampleSize: 5);

        var verdict = GrantEligibilityEngine.Evaluate(record, threshold);

        verdict.IsDocumentOnlyVerdict.Should().BeTrue();
        verdict.PaidTrackEligible.Should().BeTrue();
        verdict.Notes.Should().Contain("admission commission");
    }

    [Fact]
    public void ContinuingSpecialtyPaid_WorksEvenWithNoThresholdRowAtAll()
    {
        var record = MakeRecord(AdmissionExamTrack.ContinuingSpecialtyPaid, totalScore: null);

        var verdict = GrantEligibilityEngine.Evaluate(record, threshold: null);

        verdict.IsDocumentOnlyVerdict.Should().BeTrue();
    }

    // --- Cross-cutting behavior ----------------------------------------------------------------

    [Fact]
    public void MissingThresholdData_ForAScoreBasedTrack_ReturnsHonestlyFlaggedUnknownVerdict_NeverAFabricatedPass()
    {
        var record = MakeRecord(AdmissionExamTrack.StandardEnt, totalScore: 120);

        var verdict = GrantEligibilityEngine.Evaluate(record, threshold: null);

        verdict.MeetsStateThreshold.Should().BeFalse();
        verdict.MeetsUniversityThreshold.Should().BeFalse();
        verdict.GrantCompetitiveness.SampleSize.Should().Be(0);
        verdict.GrantCompetitiveness.Basis.Should().Contain("No admission threshold data");
    }

    [Fact]
    public void GrantCompetitiveness_IsAlwaysAnIntervalNeverABarePercentage()
    {
        var record = MakeRecord(AdmissionExamTrack.StandardEnt, totalScore: 95);
        var threshold = MakeThreshold(AdmissionExamTrack.StandardEnt, state: 50, university: 80, cutoffMin: 85, cutoffMax: 110, cutoffMedian: 95, sampleSize: 20);

        var verdict = GrantEligibilityEngine.Evaluate(record, threshold);

        verdict.GrantCompetitiveness.LowerBound.Should().BeLessThanOrEqualTo(verdict.GrantCompetitiveness.PointEstimate);
        verdict.GrantCompetitiveness.UpperBound.Should().BeGreaterThanOrEqualTo(verdict.GrantCompetitiveness.PointEstimate);
        verdict.GrantCompetitiveness.Basis.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PaidTrackEligibility_NeverRequiresClearingTheCompetitiveGrantCutoff()
    {
        // Score clears the university minimum but sits well below the historical grant cutoff
        // range — paid admission must still be reported eligible.
        var record = MakeRecord(AdmissionExamTrack.StandardEnt, totalScore: 82);
        var threshold = MakeThreshold(AdmissionExamTrack.StandardEnt, state: 50, university: 80, cutoffMin: 100, cutoffMax: 130, cutoffMedian: 115, sampleSize: 20);

        var verdict = GrantEligibilityEngine.Evaluate(record, threshold);

        verdict.MeetsUniversityThreshold.Should().BeTrue();
        verdict.PaidTrackEligible.Should().BeTrue();
        verdict.GrantCompetitiveness.PointEstimate.Should().BeLessThan(50); // still weak for the grant itself
    }
}
