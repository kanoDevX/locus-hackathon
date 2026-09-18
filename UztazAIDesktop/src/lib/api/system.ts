"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import { useAuthStore } from "@/lib/stores/auth-store";
import type { ProfileDto } from "./types";

/** Judge Sandbox Mode — resets the calling account's demo profile to a known scripted state.
 * Only meaningful for the seeded Judge role account; the UI hides this outside that role. */
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
