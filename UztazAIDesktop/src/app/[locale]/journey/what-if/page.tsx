"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { useSearchParams } from "next/navigation";
import { TrendingUp, TrendingDown } from "lucide-react";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useWhatIf } from "@/lib/api/whatIf";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export default function WhatIfPage() {
  const t = useTranslations("whatIf");
  const profileId = useAuthStore((s) => s.activeProfileId);
  const programId = Number(useSearchParams().get("programId") ?? 0);
  const whatIf = useWhatIf(profileId);

  useEffect(() => {
    if (profileId && programId) whatIf.mutate(programId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [profileId, programId]);

  return (
    <div className="mx-auto max-w-xl">
      <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
      <p className="mt-1 text-sm text-[var(--neutral-500)]">{t("subtitle")}</p>

      <div className="mt-6 flex flex-col gap-3">
        {whatIf.isPending && Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}

        {whatIf.data
          ?.slice()
          .sort((a, b) => b.admissionProbabilityDelta - a.admissionProbabilityDelta)
          .map((option, i) => (
            <Card key={i}>
              <CardHeader className="flex-row items-center justify-between">
                <CardTitle className="text-sm">{option.description}</CardTitle>
                <span
                  className={`flex items-center gap-1 text-sm font-semibold ${
                    option.admissionProbabilityDelta >= 0 ? "text-[var(--success-500)]" : "text-[var(--danger-500)]"
                  }`}
                >
                  {option.admissionProbabilityDelta >= 0 ? <TrendingUp className="size-4" /> : <TrendingDown className="size-4" />}
                  {option.admissionProbabilityDelta > 0 ? "+" : ""}
                  {option.admissionProbabilityDelta.toFixed(1)} pp
                </span>
              </CardHeader>
              <CardContent className="text-xs text-[var(--neutral-500)]">
                Overall fit: {option.baselineOverallScore.toFixed(0)} → {option.simulatedOverallScore.toFixed(0)}
              </CardContent>
            </Card>
          ))}
      </div>
    </div>
  );
}
