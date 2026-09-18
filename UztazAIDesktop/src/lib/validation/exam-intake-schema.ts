import { z } from "zod";

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
const SCHOOL_TRACKS = new Set<string>(["StandardEnt", "CreativeExam", "NotTakingEnt"]);
const COLLEGE_TRACKS = new Set<string>(["ChangingSpecialty", "ContinuingSpecialtyGrant", "ContinuingSpecialtyPaid", "NotTakingEnt"]);

const SCORELESS_TRACKS = new Set<string>(["ContinuingSpecialtyPaid", "NotTakingEnt"]);

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

export const INTAKE_STEP_FIELDS: Record<number, (keyof ExamIntakeFormValues)[]> = {
  0: ["educationStage"],
  1: ["track", "fundingTrackPreference", "collegeBackground"],
  2: ["subjectBreakdown", "totalScore", "examDateTaken"],
  3: ["supplementaryExams"],
};

export const INTAKE_DRAFT_STORAGE_KEY = "ustazai-exam-intake-draft";

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
