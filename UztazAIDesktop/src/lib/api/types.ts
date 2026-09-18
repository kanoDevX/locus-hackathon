/** Mirrors the DTOs exposed by the UstazAI ASP.NET Core backend (see ../../backend/UstazAI). */

export type Locale = "Ru" | "Kk" | "En";
export type BudgetBand = "Low" | "Medium" | "High" | "VeryHigh";
export type DegreeLevel = "Foundation" | "Bachelor" | "Master";
export type ExamType = "Unt" | "Ielts" | "Toefl" | "DuolingoEnglishTest" | "Sat" | "Igcse" | "Ent" | "Other";
export type RoadmapTaskCategory = "Exam" | "Document" | "Application" | "Activity" | "Financial" | "Interview" | "SubjectPrep";
export type RoadmapTaskStatus = "NotStarted" | "InProgress" | "Blocked" | "Done";
export type UserRole = "Student" | "Judge" | "Admin";
export type AdmitOutcome = "Admitted" | "Waitlisted" | "Rejected";

// --- §12 Placement & Eligibility Intake ------------------------------------------------------

export type EducationStage = "SchoolGrade9" | "SchoolGrade10" | "SchoolGrade11" | "CollegeStudent" | "CollegeGraduate";
export type AdmissionExamTrack =
  | "StandardEnt"
  | "CreativeExam"
  | "ChangingSpecialty"
  | "ContinuingSpecialtyGrant"
  | "ContinuingSpecialtyPaid"
  | "NotTakingEnt";
export type FundingTrackPreference = "GrantOnly" | "PaidOnly" | "Flexible";
export type ResourceType = "Video" | "Article" | "PracticeTest" | "Course";
export type ChatRole = "User" | "Assistant";
export type PublicTransitQuality = "Poor" | "Fair" | "Good" | "Excellent";

export interface SubjectScoreDto {
  subjectName: string;
  score: number;
  maxScore: number;
}

export interface CollegeBackgroundDto {
  collegeSpecialtyName: string;
  diplomaAverageScore: number | null;
  graduationYear: number | null;
  targetSpecialtyMatchesCollegeSpecialty: boolean;
}

export interface ExamRecordDto {
  id: number;
  track: AdmissionExamTrack;
  subjectBreakdown: SubjectScoreDto[];
  totalScore: number | null;
  dateTaken: string | null;
  provenance: DataProvenanceDto;
}

export interface SupplementaryExamRecordDto {
  id: number;
  examType: ExamType;
  score: number;
  maxScore: number;
  dateTaken: string | null;
  provenance: DataProvenanceDto;
}

export interface ExamIntakeResultDto {
  profileId: string;
  profileVersion: number;
  educationStage: EducationStage;
  fundingTrackPreference: FundingTrackPreference;
  latestExamRecord: ExamRecordDto | null;
  supplementaryExamRecords: SupplementaryExamRecordDto[];
  collegeBackground: CollegeBackgroundDto | null;
  diff: ProfileDiffDto | null;
}

export interface EligibilityResultDto {
  id: number;
  program: ProgramSummaryDto;
  track: AdmissionExamTrack;
  meetsStateThreshold: boolean;
  meetsUniversityThreshold: boolean;
  grantCompetitiveness: UncertaintyEstimateDto;
  paidTrackEligible: boolean;
  isDocumentOnlyVerdict: boolean;
  notes: string;
  provenance: DataProvenanceDto;
}

// --- §13 Result-Aware Chat + Gap-to-Course Prep ----------------------------------------------

export interface ResourceLinkDto {
  title: string;
  url: string;
  resourceType: ResourceType;
  provenance: DataProvenanceDto;
}

export interface ChatMessageDto {
  id: number;
  role: ChatRole;
  content: string;
  isAiGenerated: boolean;
  fallbackUsed: boolean;
  createdAtUtc: string;
}

// --- §14 University Map & Environment Intelligence -------------------------------------------

export interface GeoCoordinatesDto {
  latitude: number;
  longitude: number;
}

export interface EnvironmentProfileDto {
  costOfLivingIndexUsdPerMonth: number;
  safetyIndex: number;
  climateSummary: string;
  publicTransitQuality: PublicTransitQuality;
  internationalStudentPercent: number | null;
  provenance: DataProvenanceDto;
}

export interface MapLinkDto {
  provider: string;
  embedUrl: string;
  attributionText: string;
}

export interface CampusMapDto {
  programId: number;
  programName: string;
  universityName: string;
  city: string;
  country: string;
  coordinates: GeoCoordinatesDto | null;
  environment: EnvironmentProfileDto | null;
  mapLink: MapLinkDto | null;
}

export interface AuthResult {
  userId: string;
  email: string;
  displayName: string;
  role: UserRole;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
}

export interface AccountDto {
  userId: string;
  email: string;
  displayName: string;
  role: UserRole;
}

export interface ExamScoreDto {
  examType: ExamType;
  score: number;
  maxScore: number;
  dateTaken: string | null;
}

export interface ProfileDto {
  id: string;
  version: number;
  fullName: string;
  grade: number;
  age: number;
  preferredLanguage: Locale;
  interests: string[];
  gpa: number | null;
  examScores: ExamScoreDto[];
  targetCountries: string[];
  budgetBand: BudgetBand;
  timelineMonthsToApplication: number;
  constraints: string[];
  educationStage: EducationStage | null;
  fundingTrackPreference: FundingTrackPreference;
  collegeBackground: CollegeBackgroundDto | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface ProfileDiffDto {
  fromVersion: number;
  toVersion: number;
  changedFields: string[];
  impactSummary: string;
  likelyAffectedStages: string[];
}

export interface CreateOrUpdateProfileResult {
  profile: ProfileDto;
  diff: ProfileDiffDto | null;
}

export interface DiagnosticsDto {
  diagnosticsId: number;
  profileVersion: number;
  strengths: string[];
  constraintsFound: string[];
  inferredGoal: string;
  confidenceScore: number;
  isAiGenerated: boolean;
  fallbackUsed: boolean;
}

export interface DataProvenanceDto {
  source: string;
  isDemoData: boolean;
  lastVerifiedUtc: string;
}

export interface UncertaintyEstimateDto {
  estimate: number;
  lowerBound: number;
  upperBound: number;
  sampleSize: number;
  basis: string;
}

export interface ProgramSummaryDto {
  programId: number;
  programName: string;
  universityName: string;
  country: string;
  city: string;
  fieldOfStudy: string;
  degreeLevel: DegreeLevel;
  languageOfInstruction: string;
  tuitionPerYearUsd: number;
  livingCostPerYearUsd: number;
  applicationDeadline: string;
  scholarshipAvailable: boolean;
  scholarshipCoveragePercent: number;
  typicalAdmitRatePercent: number;
  averageStartingSalaryUsd: number | null;
}

export type AffordabilityTier = "Affordable" | "Stretch" | "OverBudget";

export interface RecommendationDto {
  recommendationId: number;
  program: ProgramSummaryDto;
  rankPosition: number;
  overallScore: number;
  academicFitScore: number;
  financialFitScore: number;
  careerFitScore: number;
  timelineFitScore: number;
  academicFitExplanation: string;
  financialFitExplanation: string;
  careerFitExplanation: string;
  timelineFitExplanation: string;
  narrativeSummary: string;
  admissionProbability: UncertaintyEstimateDto;
  provenance: DataProvenanceDto;
  isAiNarrated: boolean;
  fallbackUsed: boolean;
  affordabilityTier: AffordabilityTier;
  affordabilityEffectiveCostUsd: number;
  affordabilityBudgetCeilingUsd: number;
}

export interface RecommendationDeltaDto {
  hasPreviousBatch: boolean;
  previousProfileVersion: number | null;
  addedProgramIds: number[];
  removedProgramIds: number[];
  rankChangedProgramIds: number[];
}

export interface AffordabilitySummaryDto {
  affordableCount: number;
  stretchCount: number;
  overBudgetCount: number;
}

export interface GenerateRecommendationsResult {
  recommendations: RecommendationDto[];
  delta: RecommendationDeltaDto;
  affordability: AffordabilitySummaryDto;
}

export interface ProgramComparisonRowDto {
  program: ProgramSummaryDto;
  overallScore: number;
  academicFitScore: number;
  financialFitScore: number;
  careerFitScore: number;
  timelineFitScore: number;
  admissionProbability: UncertaintyEstimateDto;
  provenance: DataProvenanceDto;
  campusMap: CampusMapDto;
}

export interface RoadmapTaskDto {
  taskId: number;
  title: string;
  description: string;
  category: RoadmapTaskCategory;
  dueDate: string | null;
  status: RoadmapTaskStatus;
  urgencyScore: number;
  impactScore: number;
  prerequisiteTaskIds: number[];
  /** Only set for category === "SubjectPrep" (§13). */
  subject: string | null;
  resources: ResourceLinkDto[];
  programId: number | null;
}

export interface WhatIfOptionDto {
  variable: string;
  description: string;
  baselineOverallScore: number;
  simulatedOverallScore: number;
  overallScoreDelta: number;
  baselineAdmissionProbability: number;
  simulatedAdmissionProbability: number;
  admissionProbabilityDelta: number;
}

export interface PeerArchetypeGroupDto {
  archetypeLabel: string;
  outcome: AdmitOutcome;
  count: number;
  typicalTimelineMonths: number;
  commonBlocker: string | null;
}

export interface PeerPathwaysResultDto {
  sampleSize: number;
  groups: PeerArchetypeGroupDto[];
  disclaimer: string;
}

export interface FavoriteDto {
  favoriteId: number;
  program: ProgramSummaryDto;
  note: string | null;
  createdAtUtc: string;
}

export interface CalendarEntryDto {
  type: "Task" | "ApplicationDeadline";
  title: string;
  date: string;
  category: string | null;
  relatedProgramId: number | null;
  relatedTaskId: number | null;
}

export interface ScholarshipSearchResultDto {
  scholarshipId: number;
  name: string;
  coveragePercent: number;
  eligibilityCriteria: string;
  deadlineDate: string | null;
  program: ProgramSummaryDto | null;
  provenance: DataProvenanceDto;
}

export interface EssayReviewDto {
  strengths: string[];
  suggestedImprovements: string[];
  clarityFeedback: string;
  structureFeedback: string;
  isAiGenerated: boolean;
  fallbackUsed: boolean;
}

export interface InsightsDto {
  totalAiCalls: number;
  successRatePercent: number;
  fallbackRatePercent: number;
  averageLatencyMs: number;
  p95LatencyMs: number;
  totalInputTokens: number;
  totalOutputTokens: number;
  byModule: { module: string; calls: number; successRatePercent: number; fallbackRatePercent: number }[];
}

// --- Study guide (full-screen "how do I learn this", one per SubjectPrep roadmap task) --------

export interface StudyGuideStepDto {
  stepNumber: number;
  title: string;
  description: string;
  estimatedMinutes: number;
  videoSearchQuery: string;
  /** A real, functional YouTube *search* link — never a specific video. No video-metadata API is
   * wired up, so a specific title/id/thumbnail would have to be invented; this product doesn't
   * fabricate facts. */
  youTubeSearchUrl: string;
}

export interface StudyGuideDto {
  id: number;
  roadmapTaskId: number;
  subject: string;
  steps: StudyGuideStepDto[];
  isAiGenerated: boolean;
  fallbackUsed: boolean;
  provenance: DataProvenanceDto;
  createdAtUtc: string;
}
