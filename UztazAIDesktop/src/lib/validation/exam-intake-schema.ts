import { z } from "zod";

/**
 * Placement & Eligibility Intake Wizard (§12) — the literal front door of the product. Mirrors
 * the backend's `SubmitExamIntakeValidator` cross-field rules exactly (education stage ↔ track
 * validity, college background requiredness, target-specialty-match ↔ track consistency, the
 * paid-track no-score rule, and the "minimum 5 points per discipline" rule) so a student never
 * reaches the server only to be rejected for a branch they could have been steered away from in
 * the UI. If the backend rule ever changes, this file must change with it — the two are not
 * independently maintained sources of truth, just two enforcement points of the same one.
 */

export const educationStages = ["SchoolGrade9", "SchoolGrade10", "SchoolGrade11", "CollegeStudent", "CollegeGraduate"] as const;
export const admissionExamTracks = [
  "StandardEnt",
  "CreativeExam",
  "ChangingSpecialty",
  "ContinuingSpecialtyGrant",
  "ContinuingSpecialtyPaid",
  "NotTakingEnt",
] as const;
export const fundingTrackPreferences = ["GrantOnly", "PaidOnly", "Flexible"] as const;
export const supplementaryExamTypes = ["Ielts", "Toefl", "DuolingoEnglishTest", "Sat"] as const;

const SCHOOL_STAGES = new Set<string>(["SchoolGrade9", "SchoolGrade10", "SchoolGrade11"]);
const COLLEGE_STAGES = new Set<string>(["CollegeStudent", "CollegeGraduate"]);
// NotTakingEnt opts out of Kazakhstan's admission system entirely — it's compatible with either
// stage (a school *or* college student can be applying abroad only), so it's a member of both
// sets rather than tied to one.
const SCHOOL_TRACKS = new Set<string>(["StandardEnt", "CreativeExam", "NotTakingEnt"]);
const COLLEGE_TRACKS = new Set<string>(["ChangingSpecialty", "ContinuingSpecialtyGrant", "ContinuingSpecialtyPaid", "NotTakingEnt"]);

/** Tracks with no Kazakhstan exam score at all — ContinuingSpecialtyPaid (direct admission by
 * documents via the university's own commission) and NotTakingEnt (applying abroad only, so the
 * ENT/grant system is inapplicable by definition, not merely unmet). */
const SCORELESS_TRACKS = new Set<string>(["ContinuingSpecialtyPaid", "NotTakingEnt"]);

/** Max total score per scored track — the scoreless tracks are deliberately absent. */
export const TRACK_MAX_SCORE: Record<Exclude<(typeof admissionExamTracks)[number], "ContinuingSpecialtyPaid" | "NotTakingEnt">, number> = {
  StandardEnt: 140,
  CreativeExam: 140,
  ChangingSpecialty: 140,
  ContinuingSpecialtyGrant: 70,
};

export const subjectScoreSchema = z.object({
  subjectName: z.string().min(1, "required"),
  score: z.coerce.number().min(0),
  maxScore: z.coerce.number().min(1),
});

// collegeSpecialtyName's requiredness is NOT enforced here — it depends on `track` (not required
// when NotTakingEnt, since that field is hidden and irrelevant on that path), so it's checked in
// validateExamIntakeCrossFields instead, alongside every other track-dependent rule.
export const collegeBackgroundSchema = z.object({
  collegeSpecialtyName: z.string(),
  diplomaAverageScore: z.coerce.number().min(0).max(5).optional().nullable(),
  graduationYear: z.coerce.number().int().min(2000).max(2100).optional().nullable(),
  targetSpecialtyMatchesCollegeSpecialty: z.boolean(),
});

export const supplementaryExamSchema = z.object({
  examType: z.enum(supplementaryExamTypes),
  score: z.coerce.number().min(0),
  maxScore: z.coerce.number().min(1),
  dateTaken: z.string().optional().nullable(),
});

export const examIntakeObjectSchema = z.object({
  educationStage: z.enum(educationStages),
  track: z.enum(admissionExamTracks),
  subjectBreakdown: z.array(subjectScoreSchema),
  totalScore: z.coerce.number().optional().nullable(),
  examDateTaken: z.string().optional().nullable(),
  collegeBackground: collegeBackgroundSchema.optional().nullable(),
  supplementaryExams: z.array(supplementaryExamSchema),
  fundingTrackPreference: z.enum(fundingTrackPreferences),
});

type ExamIntakeCrossFieldShape = z.infer<typeof examIntakeObjectSchema>;

/**
 * The branching cross-field rules, factored out so both the standalone exam-intake schema and
 * the wizard's combined (profile + intake) schema apply the exact same logic — never two forks
 * of the same rule set drifting apart.
 *
 * Every `message` here is a stable, short key (not English prose) — the wizard translates it via
 * `t(`errors.${key}`)` before showing it (see `fieldError` in intake-wizard-form.tsx). Zod's
 * `.message` has to be a plain string, so a raw English sentence here would otherwise leak
 * straight into a Russian/Kazakh UI verbatim, as it did before this was a key (a real bug a user
 * hit and reported: "A total score is required for this track." shown untranslated).
 */
export function validateExamIntakeCrossFields(val: ExamIntakeCrossFieldShape, ctx: z.RefinementCtx) {
  const isSchoolStage = SCHOOL_STAGES.has(val.educationStage);
  const isCollegeStage = COLLEGE_STAGES.has(val.educationStage);
  const isSchoolTrack = SCHOOL_TRACKS.has(val.track);
  const isCollegeTrack = COLLEGE_TRACKS.has(val.track);
  const isNotTakingEnt = val.track === "NotTakingEnt";

  if (isSchoolStage && !isSchoolTrack) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["track"], message: "schoolStageInvalidTrack" });
  }
  if (isCollegeStage && !isCollegeTrack) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["track"], message: "collegeStageInvalidTrack" });
  }

  if (isCollegeStage && !val.collegeBackground && !isNotTakingEnt) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["collegeBackground"], message: "collegeBackgroundRequired" });
  }
  if (isSchoolStage && val.collegeBackground) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["collegeBackground"], message: "collegeBackgroundNotApplicable" });
  }

  if (val.collegeBackground) {
    // The specialty name field is hidden (not requested at all) once NotTakingEnt is chosen — see
    // its doc comment on collegeBackgroundSchema for why this can't live in the base schema.
    if (isCollegeStage && !isNotTakingEnt && val.collegeBackground.collegeSpecialtyName.trim() === "") {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["collegeBackground", "collegeSpecialtyName"], message: "required" });
    }

    const matches = val.collegeBackground.targetSpecialtyMatchesCollegeSpecialty;
    if (val.track === "ChangingSpecialty" && matches) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["track"], message: "changingSpecialtyMismatch" });
    }
    if ((val.track === "ContinuingSpecialtyGrant" || val.track === "ContinuingSpecialtyPaid") && !matches) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["track"], message: "continuingSpecialtyMismatch" });
    }
  }

  if (SCORELESS_TRACKS.has(val.track)) {
    if (val.totalScore !== null && val.totalScore !== undefined) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["totalScore"], message: "scorelessTrackHasScore" });
    }
    return;
  }

  const bounds = TRACK_MAX_SCORE[val.track as keyof typeof TRACK_MAX_SCORE];
  if (val.totalScore === null || val.totalScore === undefined) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["totalScore"], message: "totalScoreRequired" });
  } else if (val.totalScore < 0 || val.totalScore > bounds) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["totalScore"], message: "totalScoreOutOfRange" });
  }

  val.subjectBreakdown.forEach((s, i) => {
    if (s.score < 5) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["subjectBreakdown", i, "score"], message: "subjectMinScore" });
    }
  });
}

export const examIntakeSchema = examIntakeObjectSchema.superRefine(validateExamIntakeCrossFields);

export type ExamIntakeFormValues = z.infer<typeof examIntakeSchema>;

/** Which top-level fields a wizard step must validate before advancing — same pattern as
 * `STEP_FIELDS` in profile-schema.ts. Cross-field branching rules (superRefine above) are
 * re-checked on every `trigger()` call regardless of which single field name is listed, since
 * Zod re-runs the whole schema; listing the step's own fields is enough to surface the right
 * per-field error messages inline. */
export const INTAKE_STEP_FIELDS: Record<number, (keyof ExamIntakeFormValues)[]> = {
  0: ["educationStage"],
  1: ["track", "fundingTrackPreference", "collegeBackground"],
  2: ["subjectBreakdown", "totalScore", "examDateTaken"],
  3: ["supplementaryExams"],
};

export const INTAKE_DRAFT_STORAGE_KEY = "ustazai-exam-intake-draft";

/** Which tracks are selectable for a given education stage — drives the wizard's dynamic reveal
 * of valid CollegeAdmissionPath/EntTrack options (Addendum 2 §"dynamic reveal"). */
export function tracksForStage(stage: (typeof educationStages)[number]): (typeof admissionExamTracks)[number][] {
  return SCHOOL_STAGES.has(stage)
    ? ["StandardEnt", "CreativeExam", "NotTakingEnt"]
    : ["ChangingSpecialty", "ContinuingSpecialtyGrant", "ContinuingSpecialtyPaid", "NotTakingEnt"];
}

export function isCollegeStage(stage: (typeof educationStages)[number]): boolean {
  return COLLEGE_STAGES.has(stage);
}

export function isScoredTrack(track: (typeof admissionExamTracks)[number]): boolean {
  return !SCORELESS_TRACKS.has(track);
}
