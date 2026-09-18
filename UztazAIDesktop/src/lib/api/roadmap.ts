"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { RoadmapTaskDto, RoadmapTaskStatus } from "./types";

export function useRoadmap(profileId: string | null | undefined) {
  return useQuery({
    queryKey: ["roadmap", profileId],
    queryFn: () => apiFetch<RoadmapTaskDto[]>(`/api/v1/profile/${profileId}/roadmap`),
    enabled: !!profileId,
  });
}

export function useGenerateRoadmap(profileId: string | null | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (programId: number) =>
      apiFetch<RoadmapTaskDto[]>(`/api/v1/profile/${profileId}/roadmap`, { method: "POST", body: { programId } }),
    onSuccess: (data) => {
      queryClient.setQueryData(["roadmap", profileId], data);
      queryClient.invalidateQueries({ queryKey: ["calendar", profileId] });
      queryClient.invalidateQueries({ queryKey: ["nextAction", profileId] });
    },
  });
}

export function useUpdateTaskProgress(profileId: string | null | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { taskId: number; status: RoadmapTaskStatus }) =>
      apiFetch<RoadmapTaskDto>(`/api/v1/roadmap/tasks/${input.taskId}/progress`, {
        method: "PATCH",
        body: { status: input.status },
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["roadmap", profileId] });
      queryClient.invalidateQueries({ queryKey: ["nextAction", profileId] });
      queryClient.invalidateQueries({ queryKey: ["calendar", profileId] });
    },
  });
}
