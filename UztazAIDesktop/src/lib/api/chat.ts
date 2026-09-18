"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { ChatMessageDto } from "./types";

/**
 * The backend's chat endpoint (§13) returns one complete JSON reply per call, not a
 * text/event-stream — same "complete JSON, not a fake stream" decision already made for
 * recommendations (see README). There is no streaming hook to reuse because the product has
 * never had one; this follows the same TanStack Query mutation shape as every other AI-backed
 * call (essays, diagnostics) rather than introducing SSE machinery the backend doesn't support.
 */
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
