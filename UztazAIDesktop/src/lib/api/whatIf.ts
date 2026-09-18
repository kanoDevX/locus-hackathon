"use client";

import { useMutation } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { WhatIfOptionDto } from "./types";

export function useWhatIf(profileId: string | null | undefined) {
  return useMutation({
    mutationFn: (programId: number) =>
      apiFetch<WhatIfOptionDto[]>(`/api/v1/profile/${profileId}/what-if`, { method: "POST", body: { programId } }),
  });
}
