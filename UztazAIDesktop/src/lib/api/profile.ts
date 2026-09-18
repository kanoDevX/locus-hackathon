"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import { useAuthStore } from "@/lib/stores/auth-store";
import type { CreateOrUpdateProfileResult, ExamScoreDto, ProfileDto, BudgetBand, Locale } from "./types";

export interface ProfileFormInput {
  profileId?: string | null;
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
}

export function useProfile(profileId: string | null | undefined) {
  return useQuery({
    queryKey: ["profile", profileId],
    queryFn: () => apiFetch<ProfileDto>(`/api/v1/profile/${profileId}`),
    enabled: !!profileId,
  });
}

export function useMyProfile(enabled: boolean) {
  return useQuery({
    queryKey: ["profile", "mine"],
    queryFn: async () => (await apiFetch<ProfileDto | undefined>("/api/v1/profile/mine")) ?? null,
    enabled,
    staleTime: 60_000,
  });
}

export function useSaveProfile() {
  const queryClient = useQueryClient();
  const setActiveProfileId = useAuthStore((s) => s.setActiveProfileId);

  return useMutation({
    mutationFn: (input: ProfileFormInput) =>
      apiFetch<CreateOrUpdateProfileResult>("/api/v1/profile", { method: "POST", body: input }),
    onSuccess: (data) => {
      setActiveProfileId(data.profile.id);
      queryClient.setQueryData(["profile", data.profile.id], data.profile);
      queryClient.invalidateQueries({ queryKey: ["recommendations"] });
      queryClient.invalidateQueries({ queryKey: ["roadmap"] });
      queryClient.invalidateQueries({ queryKey: ["calendar"] });
    },
  });
}
