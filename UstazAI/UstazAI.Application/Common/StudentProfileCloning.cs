using UstazAI.Domain.Entities;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Application.Common;

/// <summary>
/// A detached, deep-enough copy of a StudentProfile's own scalar/list fields — used everywhere a
/// handler needs a genuine "before" snapshot to diff against after mutating the tracked entity in
/// place (ProfileDiffCalculator) or to perturb hypothetically without touching the tracked entity
/// (WhatIfQuery's counterfactual simulation). Deliberately excludes the profile's child
/// collections (ExamRecords, Recommendations, RoadmapTasks, ...) — those are separate,
/// independently-versioned entities, not part of what this clone is for.
/// </summary>
public static class StudentProfileCloning
{
    public static StudentProfile Clone(this StudentProfile p) => new()
    {
        Id = p.Id,
        Version = p.Version,
        FullName = p.FullName,
        Grade = p.Grade,
        Age = p.Age,
        PreferredLanguage = p.PreferredLanguage,
        Interests = [.. p.Interests],
        Gpa = p.Gpa,
        ExamScores = [.. p.ExamScores.Select(e => new ExamScore
        {
            ExamType = e.ExamType, Score = e.Score, MaxScore = e.MaxScore, DateTaken = e.DateTaken
        })],
        TargetCountries = [.. p.TargetCountries],
        BudgetBand = p.BudgetBand,
        TimelineMonthsToApplication = p.TimelineMonthsToApplication,
        Constraints = [.. p.Constraints],
        PersonaTone = p.PersonaTone,
        EducationStage = p.EducationStage,
        FundingTrackPreference = p.FundingTrackPreference,
        CollegeBackground = p.CollegeBackground is { } cb
            ? new CollegeBackground
            {
                CollegeSpecialtyName = cb.CollegeSpecialtyName,
                DiplomaAverageScore = cb.DiplomaAverageScore,
                GraduationYear = cb.GraduationYear,
                TargetSpecialtyMatchesCollegeSpecialty = cb.TargetSpecialtyMatchesCollegeSpecialty
            }
            : null
    };
}
