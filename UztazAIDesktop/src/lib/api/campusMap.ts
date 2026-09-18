"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { CampusMapDto } from "./types";

export function useCampusMap(profileId: string | null | undefined, programId: number | null | undefined) {
  return useQuery({
    queryKey: ["campusMap", profileId, programId],
    queryFn: () => apiFetch<CampusMapDto>(`/api/v1/profile/${profileId}/campus-map/${programId}`),
    enabled: !!profileId && !!programId,
  });
}
