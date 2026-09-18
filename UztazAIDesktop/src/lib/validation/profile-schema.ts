import { z } from "zod";

export const examScoreSchema = z.object({
  examType: z.enum(["Unt", "Ielts", "Toefl", "DuolingoEnglishTest", "Sat", "Igcse", "Ent", "Other"]),
  score: z.coerce.number().min(0).max(5000),
  maxScore: z.coerce.number().min(1).max(5000),
});

export const profileSchema = z.object({
  fullName: z.string().min(1, "Required").max(200),
  grade: z.coerce.number().int().min(1).max(12),
  age: z.coerce.number().int().min(10).max(25),
  preferredLanguage: z.enum(["Ru", "Kk", "En"]),
  interests: z.array(z.string()).min(1, "Add at least one interest"),
  gpa: z.coerce.number().min(0).max(4).optional().nullable(),
  examScores: z.array(examScoreSchema),
  targetCountries: z.array(z.string()).min(1, "Add at least one country"),
  budgetBand: z.enum(["Low", "Medium", "High", "VeryHigh"]),
  timelineMonthsToApplication: z.coerce.number().int().min(0).max(60),
  constraints: z.array(z.string()),
});

export type ProfileFormValues = z.infer<typeof profileSchema>;

export const STEP_FIELDS: Record<number, (keyof ProfileFormValues)[]> = {
  0: ["fullName", "grade", "age", "preferredLanguage"],
  1: ["gpa", "examScores"],
  2: ["interests", "targetCountries"],
  3: ["budgetBand", "timelineMonthsToApplication", "constraints"],
};

export const DRAFT_STORAGE_KEY = "ustazai-profile-draft";
