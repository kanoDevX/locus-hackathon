"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { RoadmapTaskDto } from "./types";

export function useNextAction(profileId: string | null | undefined) {
  return useQuery({
    queryKey: ["nextAction", profileId],
    queryFn: async () => (await apiFetch<RoadmapTaskDto | undefined>(`/api/v1/profile/${profileId}/next-action`)) ?? null,
    enabled: !!profileId,
  });
}
