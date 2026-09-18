"use client";

import { useMutation } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { EssayReviewDto } from "./types";

export function useReviewEssay(profileId: string | null | undefined) {
  return useMutation({
    mutationFn: (input: { essayText: string; prompt?: string }) =>
      apiFetch<EssayReviewDto>(`/api/v1/profile/${profileId}/essays/review`, { method: "POST", body: input }),
  });
}
