using UstazAI.Domain.Enums;

namespace UstazAI.Application.Dtos;

public sealed record SubjectScoreDto(string SubjectName, decimal Score, decimal MaxScore);

public sealed record CollegeBackgroundDto(
    string CollegeSpecialtyName, decimal? DiplomaAverageScore, int? GraduationYear,
    bool TargetSpecialtyMatchesCollegeSpecialty);

public sealed record ExamRecordDto(
    int ExamRecordId, AdmissionExamTrack Track, List<SubjectScoreDto> SubjectBreakdown,
    decimal? TotalScore, DateOnly? DateTaken, DataProvenanceDto Provenance);

public sealed record SupplementaryExamRecordDto(
    int Id, ExamType ExamType, decimal Score, decimal MaxScore, DateOnly? DateTaken, DataProvenanceDto Provenance);

public sealed record ExamIntakeResultDto(
    Guid ProfileId, int ProfileVersion, EducationStage EducationStage, FundingTrackPreference FundingTrackPreference,
    ExamRecordDto? LatestExamRecord, List<SupplementaryExamRecordDto> SupplementaryExamRecords,
    CollegeBackgroundDto? CollegeBackground, ProfileDiffDto? Diff);

public sealed record EligibilityResultDto(
    int Id, ProgramSummaryDto Program, AdmissionExamTrack Track, bool MeetsStateThreshold, bool MeetsUniversityThreshold,
    UncertaintyEstimateDto GrantCompetitiveness, bool PaidTrackEligible, bool IsDocumentOnlyVerdict, string Notes,
    DataProvenanceDto Provenance);
