"use client";

import { useTranslations } from "next-intl";
import { Download, FileText, GraduationCap, Send, Wallet, MessageSquare, CalendarClock } from "lucide-react";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useCalendar, downloadCalendarIcs } from "@/lib/api/calendar";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import type { CalendarEntryDto } from "@/lib/api/types";

const ICON_BY_CATEGORY: Record<string, React.ElementType> = {
  Exam: GraduationCap,
  Document: FileText,
  Application: Send,
  Financial: Wallet,
  Interview: MessageSquare,
  Deadline: CalendarClock,
};

export default function CalendarPage() {
  const t = useTranslations("calendar");
  const profileId = useAuthStore((s) => s.activeProfileId);
  const { data: entries, isLoading } = useCalendar(profileId);

  return (
    <div className="mx-auto max-w-2xl">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
        <Button variant="secondary" size="sm" onClick={() => profileId && downloadCalendarIcs(profileId)}>
          <Download className="size-3.5" />
          {t("downloadIcs")}
        </Button>
      </div>

      <div className="mt-6 flex flex-col gap-3">
        {isLoading && Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-16 w-full" />)}

        {!isLoading && entries?.length === 0 && (
          <Card>
            <CardContent className="p-8 text-center text-sm text-[var(--neutral-500)]">{t("empty")}</CardContent>
          </Card>
        )}

        {entries?.map((entry: CalendarEntryDto, i) => {
          const Icon = (entry.category && ICON_BY_CATEGORY[entry.category]) || CalendarClock;
          return (
            <Card key={i}>
              <CardContent className="flex items-center gap-3 p-4">
                <span className="flex size-9 items-center justify-center rounded-full bg-[var(--brand-100)] text-[var(--brand-700)] dark:bg-[var(--brand-900)] dark:text-[var(--brand-200)]">
                  <Icon className="size-4" />
                </span>
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-medium">{entry.title}</p>
                  <p className="text-xs text-[var(--neutral-500)]">{new Date(entry.date).toLocaleDateString()}</p>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
