"use client";

import { useTranslations } from "next-intl";
import { Search } from "lucide-react";
import { Tooltip, TooltipContent, TooltipTrigger, TooltipProvider } from "@/components/ui/tooltip";

export function CommandPaletteFab({ onClick }: { onClick: () => void }) {
  const t = useTranslations("commandPalette");

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <button
            type="button"
            onClick={onClick}
            aria-label={t("fabLabel")}
            className="fixed bottom-5 right-5 z-40 flex size-12 items-center justify-center rounded-full bg-[var(--primary)] text-[var(--primary-foreground)] shadow-[var(--shadow-overlay)] transition-transform duration-150 hover:scale-105 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--background)]"
          >
            <Search className="size-5" />
          </button>
        </TooltipTrigger>
        <TooltipContent side="left">{t("fabHint")}</TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}
