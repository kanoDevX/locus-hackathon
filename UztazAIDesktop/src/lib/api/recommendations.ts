"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import { useJourneyStore } from "@/lib/stores/journey-store";
import type { GenerateRecommendationsResult, RecommendationDto } from "./types";

export function useLatestRecommendations(profileId: string | null | undefined) {
  return useQuery({
    queryKey: ["recommendations", profileId],
    queryFn: () => apiFetch<RecommendationDto[]>(`/api/v1/profile/${profileId}/recommendations`),
    enabled: !!profileId,
  });
}

export function useGenerateRecommendations(profileId: string | null | undefined) {
  const queryClient = useQueryClient();
  const showDiffPanel = useJourneyStore((s) => s.showDiffPanel);

  return useMutation({
    mutationFn: () =>
      apiFetch<GenerateRecommendationsResult>(`/api/v1/profile/${profileId}/recommendations`, { method: "POST" }),
    onSuccess: (data) => {
      queryClient.setQueryData(["recommendations", profileId], data.recommendations);
      if (data.delta.hasPreviousBatch) {
        showDiffPanel({ kind: "recommendations", recommendationDelta: data.delta });
      }
    },
  });
}

export interface RefreshProgramFromWebResult {
  updatedFields: string[];
  sourceUrls: string[];
}

export function useRefreshProgramFromWeb(profileId: string | null | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (programId: number) => {
      const result = await apiFetch<RefreshProgramFromWebResult>(`/api/v1/programs/${programId}/refresh-from-web`, { method: "POST" });
      const regenerated = await apiFetch<GenerateRecommendationsResult>(`/api/v1/profile/${profileId}/recommendations`, { method: "POST" });
      queryClient.setQueryData(["recommendations", profileId], regenerated.recommendations);
      return result;
    },
  });
}

export function useRefreshAllProgramsFromWeb(profileId: string | null | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (programIds: number[]) => {
      let succeeded = 0;
      let lastError: string | null = null;
      for (const id of programIds) {
        try {
          await apiFetch<RefreshProgramFromWebResult>(`/api/v1/programs/${id}/refresh-from-web`, { method: "POST" });
          succeeded++;
        } catch (err) {
          lastError = err instanceof Error ? err.message : "failed";
        }
      }
      if (succeeded > 0) {
        const regenerated = await apiFetch<GenerateRecommendationsResult>(`/api/v1/profile/${profileId}/recommendations`, { method: "POST" });
        queryClient.setQueryData(["recommendations", profileId], regenerated.recommendations);
      }
      return { succeeded, total: programIds.length, lastError };
    },
  });
}

export function refreshThresholdsFromWeb(programId: number, track: string) {
  return apiFetch<{ updatedFields: string[]; sourceNames: string[] }>(
    `/api/v1/programs/${programId}/refresh-thresholds-from-web`,
    { method: "POST", query: { track } }
  );
}

export function refreshProgramFromWebOnly(programId: number) {
  return apiFetch<RefreshProgramFromWebResult>(`/api/v1/programs/${programId}/refresh-from-web`, { method: "POST" });
}
