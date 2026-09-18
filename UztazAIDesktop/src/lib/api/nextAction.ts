"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { RoadmapTaskDto } from "./types";

export function useNextAction(profileId: string | null | undefined) {
  return useQuery({
    queryKey: ["nextAction", profileId],
    // The backend returns 200 with a genuinely empty body (→ undefined from apiFetch) when
    // there's no next action yet — a legitimate state, not a fetch failure — but TanStack Query
    // forbids a query function from resolving to undefined, so it's coalesced to null here.
    queryFn: async () => (await apiFetch<RoadmapTaskDto | undefined>(`/api/v1/profile/${profileId}/next-action`)) ?? null,
    enabled: !!profileId,
  });
}
