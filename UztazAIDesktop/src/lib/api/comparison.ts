"use client";

import { useMutation } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { ProgramComparisonRowDto } from "./types";

export function useCompletePrograms() {
  return useMutation({
    mutationFn: (input: { profileId: string; programIds: number[] }) =>
      apiFetch<ProgramComparisonRowDto[]>("/api/v1/comparison", { method: "POST", body: input }),
  });
}
