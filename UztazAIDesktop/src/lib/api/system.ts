"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import { useAuthStore } from "@/lib/stores/auth-store";
import type { ProfileDto } from "./types";

export function useDemoReset() {
  const queryClient = useQueryClient();
  const setActiveProfileId = useAuthStore((s) => s.setActiveProfileId);

  return useMutation({
    mutationFn: () => apiFetch<ProfileDto>("/api/v1/system/demo-reset", { method: "POST" }),
    onSuccess: (data) => {
      setActiveProfileId(data.id);
      queryClient.clear();
      queryClient.setQueryData(["profile", data.id], data);
    },
  });
}
