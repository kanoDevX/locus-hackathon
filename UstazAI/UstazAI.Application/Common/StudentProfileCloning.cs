using UstazAI.Domain.Entities;
using UstazAI.Domain.ValueObjects;

namespace UstazAI.Application.Common;

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
