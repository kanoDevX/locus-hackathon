import { z } from "zod";
import { profileSchema } from "./profile-schema";
import {
  examIntakeObjectSchema,
  validateExamIntakeCrossFields,
  educationStages,
  admissionExamTracks,
} from "./exam-intake-schema";

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
