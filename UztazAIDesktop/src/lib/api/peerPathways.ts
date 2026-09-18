"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { PeerPathwaysResultDto } from "./types";

export function usePeerPathways(profileId: string | null | undefined, programId: number | null) {
  return useQuery({
    queryKey: ["peerPathways", profileId, programId],
    queryFn: () =>
      apiFetch<PeerPathwaysResultDto>(`/api/v1/profile/${profileId}/peer-pathways`, { query: { programId: programId! } }),
    enabled: !!profileId && !!programId,
  });
}
