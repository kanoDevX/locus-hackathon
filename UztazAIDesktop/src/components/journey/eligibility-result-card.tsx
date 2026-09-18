"use client";

import { useTranslations } from "next-intl";
import { CheckCircle2, XCircle, FileCheck2 } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { DemoDataBadge } from "@/components/journey/demo-data-badge";
import { UncertaintyBand } from "@/components/journey/uncertainty-band";
import { refreshThresholdsFromWeb } from "@/lib/api/recommendations";
import { useCalculateEligibility } from "@/lib/api/examIntake";
import { useAuthStore } from "@/lib/stores/auth-store";
import type { EligibilityResultDto } from "@/lib/api/types";

export function EligibilityResultCard({ result }: { result: EligibilityResultDto }) {
  const t = useTranslations("eligibility");
  const profileId = useAuthStore((s) => s.activeProfileId);
  const calculate = useCalculateEligibility(profileId);

  const verify = result.isDocumentOnlyVerdict
    ? undefined
    : async () => {
        await refreshThresholdsFromWeb(result.program.programId, result.track);
        await calculate.mutateAsync();
      };

  return (
    <Card>
      <CardHeader className="flex-row items-start justify-between gap-3">
        <div>
          <CardTitle className="text-base">{result.program.programName}</CardTitle>
          <CardDescription>
            {result.program.universityName} · {result.program.city}, {result.program.country}
          </CardDescription>
        </div>
        <DemoDataBadge provenance={result.provenance} onVerify={verify} />
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {result.isDocumentOnlyVerdict ? (
          <div className="flex items-start gap-2.5 rounded-[var(--radius-md)] border border-[var(--info-100)] bg-[var(--info-100)]/40 p-3">
            <FileCheck2 className="mt-0.5 size-4 shrink-0 text-[var(--info-500)]" />
            <div>
              <p className="text-sm font-medium">{t("documentOnly")}</p>
              <p className="mt-0.5 text-xs text-[var(--neutral-500)]">{result.notes}</p>
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="flex flex-col gap-2 rounded-[var(--radius-md)] border border-[var(--border-subtle)] p-3">
              <p className="text-xs font-semibold uppercase tracking-wide text-[var(--neutral-400)]">{t("grantColumn")}</p>
              <VerdictLine ok={result.meetsStateThreshold} label={t("meetsStateThreshold")} />
              <VerdictLine ok={result.meetsUniversityThreshold} label={t("meetsUniversityThreshold")} />
              <UncertaintyBand
                estimate={result.grantCompetitiveness}
                label={t("grantCompetitiveness")}
                sampleSizeLabel={t("basedOnAdmits", { count: result.grantCompetitiveness.sampleSize })}
              />
            </div>
            <div className="flex flex-col gap-2 rounded-[var(--radius-md)] border border-[var(--border-subtle)] p-3">
              <p className="text-xs font-semibold uppercase tracking-wide text-[var(--neutral-400)]">{t("paidColumn")}</p>
              <VerdictLine ok={result.paidTrackEligible} label={t("paidTrackEligible")} />
              {result.notes && <p className="mt-1 text-xs text-[var(--neutral-500)]">{result.notes}</p>}
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function VerdictLine({ ok, label }: { ok: boolean; label: string }) {
  return (
    <div className="flex items-center gap-2 text-sm">
      {ok ? <CheckCircle2 className="size-4 shrink-0 text-[var(--success-500)]" /> : <XCircle className="size-4 shrink-0 text-[var(--danger-500)]" />}
      <span>{label}</span>
    </div>
  );
}
