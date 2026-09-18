"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { ChatMessageDto } from "./types";

export function useChatHistory(profileId: string | null | undefined) {
  return useQuery({
    queryKey: ["chatHistory", profileId],
    queryFn: () => apiFetch<ChatMessageDto[]>(`/api/v1/profile/${profileId}/chat/history`),
    enabled: !!profileId,
  });
}

export function useSendChatMessage(profileId: string | null | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (message: string) =>
      apiFetch<ChatMessageDto>(`/api/v1/profile/${profileId}/chat`, { method: "POST", body: { message } }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["chatHistory", profileId] });
    },
  });
}
