"use client";

import { useTranslations } from "next-intl";
import { ChatPanel } from "@/components/journey/chat-panel";

export default function ChatPage() {
  const t = useTranslations("chat");

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
      <p className="mt-1 text-sm text-[var(--neutral-500)]">{t("subtitle")}</p>
      <div className="mt-6">
        <ChatPanel />
      </div>
    </div>
  );
}
