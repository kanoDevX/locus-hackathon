"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { FavoriteDto } from "./types";

export function useFavorites(profileId: string | null | undefined) {
  return useQuery({
    queryKey: ["favorites", profileId],
    queryFn: () => apiFetch<FavoriteDto[]>(`/api/v1/profile/${profileId}/favorites`),
    enabled: !!profileId,
  });
}

export function useAddFavorite(profileId: string | null | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { programId: number; note?: string }) =>
      apiFetch<FavoriteDto>(`/api/v1/profile/${profileId}/favorites`, { method: "POST", body: input }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["favorites", profileId] }),
  });
}

export function useRemoveFavorite(profileId: string | null | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (programId: number) =>
      apiFetch<void>(`/api/v1/profile/${profileId}/favorites/${programId}`, { method: "DELETE" }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["favorites", profileId] }),
  });
}
