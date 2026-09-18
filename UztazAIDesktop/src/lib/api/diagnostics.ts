"use client";

import { useLocale } from "next-intl";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { DiagnosticsDto } from "./types";

export function useGenerateDiagnostics(profileId: string | null | undefined) {
  const queryClient = useQueryClient();
  const locale = useLocale();
  return useMutation({
    mutationFn: () => apiFetch<DiagnosticsDto>(`/api/v1/profile/${profileId}/diagnostics`, { method: "POST" }),
    onSuccess: (data) => queryClient.setQueryData(["diagnostics", profileId, locale], data),
  });
}
