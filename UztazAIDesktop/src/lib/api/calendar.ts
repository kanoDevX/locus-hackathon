"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "./client";
import { useAuthStore } from "@/lib/stores/auth-store";
import type { CalendarEntryDto } from "./types";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5299";

export function useCalendar(profileId: string | null | undefined) {
  return useQuery({
    queryKey: ["calendar", profileId],
    queryFn: () => apiFetch<CalendarEntryDto[]>(`/api/v1/profile/${profileId}/calendar`),
    enabled: !!profileId,
  });
}

export function useNotifications(profileId: string | null | undefined, withinDays = 14) {
  return useQuery({
    queryKey: ["notifications", profileId, withinDays],
    queryFn: () =>
      apiFetch<CalendarEntryDto[]>(`/api/v1/profile/${profileId}/notifications`, { query: { withinDays } }),
    enabled: !!profileId,
  });
}

/** Direct download link for the .ics export — the token must be attached, so this builds a
 * fetch-and-save flow rather than a plain <a href>. */
export async function downloadCalendarIcs(profileId: string) {
  const token = useAuthStore.getState().accessToken;
  const response = await fetch(`${API_BASE_URL}/api/v1/profile/${profileId}/calendar.ics`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = "ustazai-admission-route.ics";
  a.click();
  URL.revokeObjectURL(url);
}
