import { z } from "zod";
import { profileSchema } from "./profile-schema";
import {
  examIntakeObjectSchema,
  validateExamIntakeCrossFields,
  educationStages,
  admissionExamTracks,
} from "./exam-intake-schema";

/**
 * The wizard's own top-level schema (§12 Addendum 2) — a superset of the existing profile
 * schema (still needed: HybridScoringEngine/Diagnostics operate on the base StudentProfile
 * fields) plus the exam-intake fields, so one wizard collects everything a first-time student
 * needs instead of two disconnected forms. Reuses `profileSchema`'s field defs and
 * `validateExamIntakeCrossFields` directly rather than re-declaring either rule set.
 */
export const intakeWizardSchema = z
  .object({
    ...profileSchema.shape,
    ...examIntakeObjectSchema.shape,
  })
  .superRefine(validateExamIntakeCrossFields);

export type IntakeWizardFormValues = z.infer<typeof intakeWizardSchema>;

export { educationStages, admissionExamTracks };

export const WIZARD_STEP_FIELDS: Record<number, (keyof IntakeWizardFormValues)[]> = {
  0: ["educationStage"],
  1: ["fullName", "grade", "age", "preferredLanguage"],
  2: ["track", "fundingTrackPreference", "collegeBackground"],
  3: ["subjectBreakdown", "totalScore"],
  4: ["interests", "targetCountries", "supplementaryExams"],
  5: ["gpa", "budgetBand", "timelineMonthsToApplication", "constraints"],
};

export const WIZARD_TOTAL_STEPS = 6;
export const WIZARD_DRAFT_STORAGE_KEY = "ustazai-intake-wizard-draft";
