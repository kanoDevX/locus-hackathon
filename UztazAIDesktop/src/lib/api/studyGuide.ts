"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { StudyGuideDto } from "./types";

export function useStudyGuide(profileId: string | null | undefined, taskId: number | null | undefined) {
  return useQuery({
    queryKey: ["studyGuide", profileId, taskId],
    // Empty body (no guide generated yet) → undefined from apiFetch, coalesced to null — a
    // legitimate "not generated yet" state, not an error (see profile.ts's useMyProfile for the
    // same pattern and why).
    queryFn: async () => (await apiFetch<StudyGuideDto | undefined>(`/api/v1/profile/${profileId}/roadmap/tasks/${taskId}/study-guide`)) ?? null,
    enabled: !!profileId && !!taskId,
  });
}

export function useGenerateStudyGuide(profileId: string | null | undefined, taskId: number | null | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (forceRegenerate: boolean) =>
      apiFetch<StudyGuideDto>(`/api/v1/profile/${profileId}/roadmap/tasks/${taskId}/study-guide`, {
        method: "POST",
        body: { forceRegenerate },
      }),
    onSuccess: (data) => {
      queryClient.setQueryData(["studyGuide", profileId, taskId], data);
    },
  });
}
