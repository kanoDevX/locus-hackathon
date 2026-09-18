using FluentAssertions;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;
using UstazAI.Domain.Services;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Tests;

public class GapAnalysisEngineTests
{
    private static ExamRecord MakeRecord(decimal? totalScore, params (string Name, decimal Score, decimal Max)[] subjects) => new()
    {
        StudentProfileId = Guid.NewGuid(),
        ProfileVersion = 1,
        Track = AdmissionExamTrack.StandardEnt,
        TotalScore = totalScore,
        SubjectBreakdown = [.. subjects.Select(s => new SubjectScore { SubjectName = s.Name, Score = s.Score, MaxScore = s.Max })]
    };

    private static AdmissionThreshold MakeThreshold(decimal university) => new()
    {
        ProgramId = 1,
        Track = AdmissionExamTrack.StandardEnt,
        StateThreshold = 50,
        UniversityInternalThreshold = university,
        HistoricalCutoffMin = university + 10,
        HistoricalCutoffMax = university + 30,
        HistoricalCutoffMedian = university + 20,
        HistoricalCutoffSampleSize = 15
    };

    [Fact]
    public void RanksSubjectsByHeadroomDescending_WeakestRelativeSubjectFirst()
    {
        var record = MakeRecord(90, ("Mathematics", 20, 35), ("Physics", 30, 35));
        var threshold = MakeThreshold(university: 120);

        var gaps = GapAnalysisEngine.AnalyzeGaps(record, threshold);

        gaps.Should().HaveCount(2);
        gaps[0].SubjectName.Should().Be("Mathematics");
        gaps[0].PriorityRank.Should().Be(1);
        gaps[1].SubjectName.Should().Be("Physics");
        gaps[1].PriorityRank.Should().Be(2);
    }

    [Fact]
    public void AlreadyMeetsUniversityThreshold_ReturnsNoGaps()
    {
        var record = MakeRecord(130, ("Mathematics", 30, 35), ("Physics", 30, 35));
        var threshold = MakeThreshold(university: 100);

        var gaps = GapAnalysisEngine.AnalyzeGaps(record, threshold);

        gaps.Should().BeEmpty();
    }

    [Fact]
    public void NoThresholdOnFile_StillRanksGapsAgainstTheStudentsOwnHeadroom()
    {
        var record = MakeRecord(90, ("Mathematics", 20, 35));

        var gaps = GapAnalysisEngine.AnalyzeGaps(record, threshold: null);

        gaps.Should().ContainSingle();
        gaps[0].HeadroomPoints.Should().Be(15);
    }

    [Fact]
    public void DocumentOnlyTrack_WithNoSubjectBreakdown_ReturnsNoGaps()
    {
        var record = MakeRecord(totalScore: null);

        var gaps = GapAnalysisEngine.AnalyzeGaps(record, threshold: null);

        gaps.Should().BeEmpty();
    }

    [Fact]
    public void PerfectSubjectScore_IsExcludedFromGaps_NoHeadroomLeft()
    {
        var record = MakeRecord(90, ("Mathematics", 35, 35), ("Physics", 20, 35));
        var threshold = MakeThreshold(university: 120);

        var gaps = GapAnalysisEngine.AnalyzeGaps(record, threshold);

        gaps.Should().ContainSingle();
        gaps[0].SubjectName.Should().Be("Physics");
    }
}
