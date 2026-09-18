namespace UstazAI.Domain.Enums;

public enum Locale
{
    Ru,
    Kk,
    En
}

public enum BudgetBand
{
    Low,        // < $5,000/yr total cost tolerance
    Medium,     // $5,000 - $15,000/yr
    High,       // $15,000 - $30,000/yr
    VeryHigh    // > $30,000/yr
}

/// <summary>Affordability & Fairness Audit (§10.6) — a deterministic, honest disclosure of
/// whether a recommended program's real cost (after any scholarship) actually fits the
/// applicant's own stated budget band, computed the same way and from the same numbers as
/// HybridScoringEngine's FinancialFitScore, just expressed as an actionable tier instead of a
/// bare 0-100 score. Never AI-narrated or estimated — see
/// HybridScoringEngine.EvaluateAffordability, the single source of truth both this and
/// FinancialFitScore are derived from.</summary>
public enum AffordabilityTier
{
    /// <summary>Effective annual cost is within the budget band's own ceiling.</summary>
    Affordable,

    /// <summary>Effective annual cost exceeds the ceiling but by no more than 50% — realistically
    /// reachable with extra funding (grant, part-time work, family contribution), not a fantasy.</summary>
    Stretch,

    /// <summary>Effective annual cost exceeds the ceiling by more than 50% — recommending this
    /// without disclosing that would be exactly the kind of fabricated-certainty-adjacent
    /// omission this product's guardrails exist to prevent (§5.3): a program can score well
    /// academically and still be honestly out of reach financially.</summary>
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
    Unt,                 // Kazakhstan Unified National Test
    Ielts,
    Toefl,
    DuolingoEnglishTest,
    Sat,
    Igcse,
    Ent,                 // generic entrance/national exam placeholder
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

    /// <summary>A gap-analysis-driven subject prep task (§13): plugs into the same RoadmapTask
    /// DAG/status/progress machinery as every other node, distinguished by <c>Subject</c> and
    /// curated <c>Resources</c> rather than by a second, forked task table.</summary>
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

/// <summary>Curated resource link category (§13), used to pick an icon/label on the frontend
/// without parsing free text.</summary>
public enum ResourceType
{
    Video,
    Article,
    PracticeTest,
    Course
}

/// <summary>Who authored a chat turn — kept as an explicit enum (string-converted at rest, like
/// every other enum in this schema) rather than a bare bool, so a future third role (e.g. system
/// notices) doesn't require a breaking migration.</summary>
public enum ChatRole
{
    User,
    Assistant
}

/// <summary>Coarse, judge-legible rating for a campus city's public transit (§14) — deliberately
/// not a raw numeric score, since no seeded source publishes a comparable transit index across
/// every country in the catalog.</summary>
public enum PublicTransitQuality
{
    Poor,
    Fair,
    Good,
    Excellent
}

/// <summary>Which real-world admission path the applicant is on — drives which branch of the
/// intake wizard and which threshold table GrantEligibilityEngine applies (§12.1 of the exam
/// intake spec). A school student and a college student sit on structurally different paths in
/// Kazakhstan's actual admission system; this is a required, explicit choice, never inferred.</summary>
public enum EducationStage
{
    SchoolGrade9,
    SchoolGrade10,
    SchoolGrade11,
    CollegeStudent,
    CollegeGraduate
}

/// <summary>
/// The single Track discriminator carried on every ExamRecord. Deliberately unifies what the
/// domain spec calls EntTrack (school path) and CollegeAdmissionPath (college path) into one
/// enum rather than two, because GrantEligibilityEngine dispatches on exactly this value and a
/// single discriminator makes "which threshold table applies" a total function instead of a
/// two-enum cross-product some branches of which are meaningless (e.g. a school student can
/// never have a CollegeAdmissionPath). The five members are NOT interchangeable — collapsing
/// them back into a flat "exam type" erases the domain-accuracy this type exists to prove (see
/// GrantEligibilityEngine's explicit switch over every member).
/// </summary>
public enum AdmissionExamTrack
{
    /// <summary>School path: 3 mandatory subjects + 2 profile subjects, 120 questions, 4 hours,
    /// max 140 points.</summary>
    StandardEnt,

    /// <summary>School path: 3 mandatory subjects + a university-administered creative/practical
    /// exam instead of a profile pair (art/design/music/architecture/journalism/sport/select
    /// pedagogy &amp; medicine specialties).</summary>
    CreativeExam,

    /// <summary>College path, switching field entirely: sits the full exam, scored exactly like
    /// a school graduate (same threshold table as StandardEnt/CreativeExam).</summary>
    ChangingSpecialty,

    /// <summary>College path, staying in the same specialty, competing for a grant: the reduced
    /// exam — general-professional + specialized-discipline questions only, 60 questions,
    /// 120 minutes, max 70 points, against a much lower state threshold.</summary>
    ContinuingSpecialtyGrant,

    /// <summary>College path, staying in the same specialty, paid admission: no exam at all —
    /// direct admission via the university's own admission commission. Not score-based; carries
    /// no meaningful ExamRecord score and GrantEligibilityEngine short-circuits to a
    /// document-readiness verdict for it.</summary>
    ContinuingSpecialtyPaid,

    /// <summary>School path, but not sitting the ENT at all — applying to foreign universities
    /// only. Kazakhstan's three-tier ENT/grant system (§12) is inapplicable by definition, not
    /// merely unmet: CalculateEligibilityCommand returns no domestic verdicts for this track
    /// rather than evaluating a state/university threshold that was never meant to apply. Not
    /// score-based; carries no meaningful ExamRecord score. The student's actual foreign-program
    /// matches still come from Recommendations (country-agnostic: GPA, interests, budget, target
    /// countries, IELTS/TOEFL/SAT), which never depended on an ExamRecord at all.</summary>
    NotTakingEnt
}

/// <summary>Explicit, persisted user choice of which funding track to target. For
/// ContinuingSpecialtyPaid applicants this is effectively pre-determined — the intake handler
/// persists it as Paid rather than asking a redundant question.</summary>
public enum FundingTrackPreference
{
    GrantOnly,
    PaidOnly,
    Flexible
}
