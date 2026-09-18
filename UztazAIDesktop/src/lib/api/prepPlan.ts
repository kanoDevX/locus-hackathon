"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { RoadmapTaskDto } from "./types";

export function usePrepPlan(profileId: string | null | undefined, programId: number | null | undefined) {
  return useQuery({
    queryKey: ["prepPlan", profileId, programId],
    queryFn: () => apiFetch<RoadmapTaskDto[]>(`/api/v1/profile/${profileId}/prep-plan/${programId}`),
    enabled: !!profileId && !!programId,
  });
}

export function useGeneratePrepPlan(profileId: string | null | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (programId: number) =>
      apiFetch<RoadmapTaskDto[]>(`/api/v1/profile/${profileId}/prep-plan/${programId}`, { method: "POST" }),
    onSuccess: (data, programId) => {
      queryClient.setQueryData(["prepPlan", profileId, programId], data);
      queryClient.invalidateQueries({ queryKey: ["roadmap", profileId] });
      queryClient.invalidateQueries({ queryKey: ["calendar", profileId] });
    },
  });
}
