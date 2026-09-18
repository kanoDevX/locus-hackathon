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

/** Resolves the signed-in user's own profile without needing `activeProfileId` cached
 * client-side first — `activeProfileId` lives only in localStorage (see auth-store.ts), so a
 * fresh login on a different device/browser, or a cleared cache, leaves it empty even for a
 * returning user who already completed intake. Without this, the app would send them straight
 * back into the wizard, which would then create a *second* profile server-side (the same bug a
 * missing "get my own record" lookup causes in any per-user-resource API). Backend returns 200
 * with an empty body (→ undefined, coerced to null here) rather than 404 when there's genuinely
 * no profile yet — that's an expected first-time state, not an error. */
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
