"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type {
  AdmissionExamTrack,
  EducationStage,
  EligibilityResultDto,
  ExamIntakeResultDto,
  ExamType,
  FundingTrackPreference,
} from "./types";

export interface SubjectScoreInput {
  subjectName: string;
  score: number;
  maxScore: number;
}

export interface CollegeBackgroundInput {
  collegeSpecialtyName: string;
  diplomaAverageScore: number | null;
  graduationYear: number | null;
  targetSpecialtyMatchesCollegeSpecialty: boolean;
}

export interface SupplementaryExamInput {
  examType: ExamType;
  score: number;
  maxScore: number;
  dateTaken: string | null;
}

export interface ExamIntakeInput {
  educationStage: EducationStage;
  track: AdmissionExamTrack;
  subjectBreakdown: SubjectScoreInput[];
  totalScore: number | null;
  examDateTaken: string | null;
  collegeBackground: CollegeBackgroundInput | null;
  supplementaryExams: SupplementaryExamInput[];
  fundingTrackPreference: FundingTrackPreference;
}

export function useSubmitExamIntake(profileId: string | null | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ExamIntakeInput) =>
      apiFetch<ExamIntakeResultDto>(`/api/v1/profile/${profileId}/exam-intake`, { method: "POST", body: input }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["profile", profileId] });
      queryClient.invalidateQueries({ queryKey: ["recommendations", profileId] });
    },
  });
}

export function useCalculateEligibility(profileId: string | null | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => apiFetch<EligibilityResultDto[]>(`/api/v1/profile/${profileId}/exam-intake/calculate`, { method: "POST" }),
    onSuccess: (data) => {
      queryClient.setQueryData(["eligibilityResult", profileId], data);
    },
  });
}

export function useEligibilityResult(profileId: string | null | undefined) {
  return useQuery({
    queryKey: ["eligibilityResult", profileId],
    queryFn: () => apiFetch<EligibilityResultDto[]>(`/api/v1/profile/${profileId}/exam-intake/result`),
    enabled: !!profileId,
  });
}
