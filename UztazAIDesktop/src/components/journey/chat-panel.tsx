"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { motion } from "framer-motion";
import { Send, Sparkles, User as UserIcon, AlertTriangle } from "lucide-react";
import { toast } from "sonner";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useChatHistory, useSendChatMessage } from "@/lib/api/chat";
import { ApiError } from "@/lib/api/client";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { fadeInUp } from "@/lib/motion";
import { cn } from "@/lib/utils";

/**
 * Result-aware AI chat (§13). Every reply is grounded server-side in the caller's own
 * already-computed eligibility/diagnostics data — this panel is just a thin message list + input,
 * it never has its own client-side "context" to manage. See src/lib/api/chat.ts for why this is a
 * plain request/response mutation rather than a streaming hook (the backend returns one complete
 * JSON reply per call, matching the rest of the product's AI-backed endpoints).
 */
export function ChatPanel() {
  const t = useTranslations("chat");
  const profileId = useAuthStore((s) => s.activeProfileId);
  const { data: history, isLoading } = useChatHistory(profileId);
  const sendMessage = useSendChatMessage(profileId);
  const [draft, setDraft] = useState("");
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [history, sendMessage.isPending]);

  async function handleSend() {
    const message = draft.trim();
    if (!message || sendMessage.isPending) return;
    setDraft("");
    try {
      await sendMessage.mutateAsync(message);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("sendFailed"));
    }
  }

  return (
    <Card className="flex h-[560px] flex-col">
      <div ref={scrollRef} className="flex-1 overflow-y-auto p-4">
        {isLoading && (
          <div className="flex flex-col gap-3">
            <Skeleton className="h-12 w-2/3" />
            <Skeleton className="ml-auto h-10 w-1/2" />
          </div>
        )}

        {!isLoading && (!history || history.length === 0) && (
          <div className="flex h-full flex-col items-center justify-center gap-2 text-center text-sm text-[var(--neutral-400)]">
            <Sparkles className="size-6" />
            <p>{t("emptyState")}</p>
          </div>
        )}

        <div className="flex flex-col gap-3">
          {history?.map((message) => (
            <motion.div
              key={message.id}
              variants={fadeInUp}
              initial="hidden"
              animate="visible"
              className={cn("flex items-start gap-2.5", message.role === "User" && "flex-row-reverse")}
            >
              <span
                className={cn(
                  "mt-0.5 flex size-7 shrink-0 items-center justify-center rounded-full",
                  message.role === "User"
                    ? "bg-[var(--neutral-100)] text-[var(--neutral-500)] dark:bg-[var(--neutral-800)]"
                    : "bg-[var(--brand-100)] text-[var(--brand-700)] dark:bg-[var(--brand-900)] dark:text-[var(--brand-200)]"
                )}
              >
                {message.role === "User" ? <UserIcon className="size-3.5" /> : <Sparkles className="size-3.5" />}
              </span>
              <div
                className={cn(
                  "max-w-[80%] rounded-[var(--radius-md)] px-3.5 py-2.5 text-sm",
                  message.role === "User" ? "bg-[var(--neutral-100)] dark:bg-[var(--neutral-800)]" : "bg-[var(--brand-50)] dark:bg-[var(--brand-900)]/60"
                )}
              >
                <p className="whitespace-pre-wrap">{message.content}</p>
                {message.fallbackUsed && (
                  <p className="mt-1.5 flex items-center gap-1 text-[11px] text-[var(--warning-500)]">
                    <AlertTriangle className="size-3" />
                    {t("fallbackNotice")}
                  </p>
                )}
              </div>
            </motion.div>
          ))}
          {sendMessage.isPending && (
            <div className="flex items-start gap-2.5">
              <span className="mt-0.5 flex size-7 shrink-0 items-center justify-center rounded-full bg-[var(--brand-100)] text-[var(--brand-700)] dark:bg-[var(--brand-900)] dark:text-[var(--brand-200)]">
                <Sparkles className="size-3.5" />
              </span>
              <Skeleton className="h-9 w-40 rounded-[var(--radius-md)]" />
            </div>
          )}
        </div>
      </div>

      <div className="flex items-center gap-2 border-t border-[var(--border-subtle)] p-3">
        <input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter" && !e.shiftKey) {
              e.preventDefault();
              handleSend();
            }
          }}
          placeholder={t("placeholder")}
          maxLength={2000}
          className="h-11 flex-1 rounded-[var(--radius-md)] border border-[var(--border-strong)] bg-[var(--surface)] px-3.5 text-sm text-[var(--foreground)] outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring)]"
        />
        <Button size="icon" onClick={handleSend} disabled={!draft.trim() || sendMessage.isPending} aria-label={t("send")}>
          <Send className="size-4" />
        </Button>
      </div>
    </Card>
  );
}
