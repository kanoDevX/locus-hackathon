"use client";

import { useTranslations } from "next-intl";
import { useSearchParams } from "next/navigation";
import { Users } from "lucide-react";
import { useAuthStore } from "@/lib/stores/auth-store";
import { usePeerPathways } from "@/lib/api/peerPathways";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";

const OUTCOME_VARIANT = { Admitted: "success", Waitlisted: "warning", Rejected: "danger" } as const;

export default function PeerPathwaysPage() {
  const t = useTranslations("peerPathways");
  const profileId = useAuthStore((s) => s.activeProfileId);
  const programId = Number(useSearchParams().get("programId") ?? 0);
  const { data, isLoading } = usePeerPathways(profileId, programId || null);

  return (
    <div className="mx-auto max-w-xl">
      <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
      <p className="mt-1 text-xs text-[var(--neutral-400)]">{data?.disclaimer ?? t("disclaimer")}</p>

      <div className="mt-6 flex flex-col gap-3">
        {isLoading && Array.from({ length: 2 }).map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}

        {data?.groups.map((group, i) => (
          <Card key={i}>
            <CardContent className="flex items-center justify-between gap-3 p-4">
              <div className="flex items-center gap-3">
                <span className="flex size-9 items-center justify-center rounded-full bg-[var(--info-100)] text-[var(--info-500)]">
                  <Users className="size-4" />
                </span>
                <div>
                  <p className="text-sm font-medium">{group.archetypeLabel}</p>
                  <p className="text-xs text-[var(--neutral-500)]">
                    ~{group.typicalTimelineMonths} months · {group.count} similar profiles
                  </p>
                  {group.commonBlocker && <p className="text-xs text-[var(--neutral-400)]">Blocker: {group.commonBlocker}</p>}
                </div>
              </div>
              <Badge variant={OUTCOME_VARIANT[group.outcome]}>{group.outcome}</Badge>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
