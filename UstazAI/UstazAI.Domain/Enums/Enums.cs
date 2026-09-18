namespace UstazAI.Domain.Enums;

public enum Locale
{
    Ru,
    Kk,
    En
}

public enum BudgetBand
{
    Low,
    Medium,
    High,
    VeryHigh
}

public enum AffordabilityTier
{
    Affordable,

    Stretch,

    OverBudget
}

public enum DegreeLevel
{
    Foundation,
    Bachelor,
    Master
}

public enum ExamType
{
    Unt,
    Ielts,
    Toefl,
    DuolingoEnglishTest,
    Sat,
    Igcse,
    Ent,
    Other
}

public enum RoadmapTaskCategory
{
    Exam,
    Document,
    Application,
    Activity,
    Financial,
    Interview,

    SubjectPrep
}

public enum RoadmapTaskStatus
{
    NotStarted,
    InProgress,
    Blocked,
    Done
}

public enum PersonaTone
{
    Neutral,
    Reassuring,
    Direct,
    Energetic
}

public enum AdmitOutcome
{
    Admitted,
    Waitlisted,
    Rejected
}

public enum UserRole
{
    Student,
    Judge,
    Admin
}

public enum AiDecisionType
{
    Diagnostics,
    Recommendation,
    Roadmap,
    WhatIf,
    PersonaClassification,
    EssayReview,
    EligibilityCheck,
    Chat,
    PrepPlan,
    StudyGuide
}

public enum ResourceType
{
    Video,
    Article,
    PracticeTest,
    Course
}

public enum ChatRole
{
    User,
    Assistant
}

public enum PublicTransitQuality
{
    Poor,
    Fair,
    Good,
    Excellent
}

public enum EducationStage
{
    SchoolGrade9,
    SchoolGrade10,
    SchoolGrade11,
    CollegeStudent,
    CollegeGraduate
}

public enum AdmissionExamTrack
{
    StandardEnt,

    CreativeExam,

    ChangingSpecialty,

    ContinuingSpecialtyGrant,

    ContinuingSpecialtyPaid,

    NotTakingEnt
}

public enum FundingTrackPreference
{
    GrantOnly,
    PaidOnly,
    Flexible
}
