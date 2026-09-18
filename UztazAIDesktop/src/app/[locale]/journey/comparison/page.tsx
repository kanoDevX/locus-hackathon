"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useJourneyStore } from "@/lib/stores/journey-store";
import { useCompletePrograms } from "@/lib/api/comparison";
import { DemoDataBadge } from "@/components/journey/demo-data-badge";
import { refreshProgramFromWebOnly } from "@/lib/api/recommendations";
import { CampusMapCard } from "@/components/journey/campus-map-card";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";

export default function ComparisonPage() {
  const t = useTranslations("comparison");
  const tCommon = useTranslations("common");
  const profileId = useAuthStore((s) => s.activeProfileId);
  const selectedProgramIds = useJourneyStore((s) => s.selectedProgramIds);
  const compare = useCompletePrograms();

  useEffect(() => {
    if (profileId && selectedProgramIds.length >= 2) {
      compare.mutate({ profileId, programIds: selectedProgramIds });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [profileId, selectedProgramIds.join(",")]);

  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
      <p className="mt-1 text-sm text-[var(--neutral-500)]">{t("subtitle")}</p>

      {selectedProgramIds.length < 2 && (
        <Card className="mt-6">
          <CardContent className="p-8 text-center text-sm text-[var(--neutral-500)]">{t("pickPrompt")}</CardContent>
        </Card>
      )}

      {compare.isPending && (
        <div className="mt-6 flex gap-4">
          {Array.from({ length: 2 }).map((_, i) => (
            <Skeleton key={i} className="h-80 flex-1" />
          ))}
        </div>
      )}

      {compare.data && (
        <Tabs defaultValue="fit" className="mt-6">
          <TabsList>
            <TabsTrigger value="fit">{t("tabFit")}</TabsTrigger>
            <TabsTrigger value="map">{t("tabMap")}</TabsTrigger>
          </TabsList>

          <TabsContent value="fit">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[560px] border-separate border-spacing-0 overflow-hidden rounded-[var(--radius-lg)] border border-[var(--border-subtle)]">
                <thead>
                  <tr>
                    <th className="w-40 bg-[var(--surface-raised)] p-3 text-left text-xs font-medium text-[var(--neutral-400)]" />
                    {compare.data.map((row) => (
                      <th key={row.program.programId} className="border-l border-[var(--border-subtle)] bg-[var(--surface-raised)] p-3 text-left">
                        <p className="text-sm font-semibold">{row.program.programName}</p>
                        <p className="text-xs font-normal text-[var(--neutral-500)]">{row.program.universityName}</p>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="bg-[var(--surface)] text-sm">
                  <Row label={t("overall")}>{(row) => `${Math.round(row.overallScore)} / 100`}</Row>
                  <Row label={t("tuition")}>{(row) => `$${row.program.tuitionPerYearUsd.toLocaleString()} ${tCommon("usdPerYear")}`}</Row>
                  <Row label={t("living")}>{(row) => `$${row.program.livingCostPerYearUsd.toLocaleString()} ${tCommon("usdPerYear")}`}</Row>
                  <Row label={t("deadline")}>{(row) => new Date(row.program.applicationDeadline).toLocaleDateString()}</Row>
                  <Row label={t("scholarship")}>
                    {(row) => (row.program.scholarshipAvailable ? `${row.program.scholarshipCoveragePercent}%` : "—")}
                  </Row>
                  <Row label={t("admitRate")}>{(row) => `${row.program.typicalAdmitRatePercent}%`}</Row>
                  <tr>
                    <td className="border-t border-[var(--border-subtle)] p-3 text-xs font-medium text-[var(--neutral-400)]">Data</td>
                    {compare.data.map((row) => (
                      <td key={row.program.programId} className="border-l border-t border-[var(--border-subtle)] p-3">
                        <DemoDataBadge
                          provenance={row.provenance}
                          onVerify={async () => {
                            await refreshProgramFromWebOnly(row.program.programId);
                            if (profileId) await compare.mutateAsync({ profileId, programIds: selectedProgramIds });
                          }}
                        />
                      </td>
                    ))}
                  </tr>
                </tbody>
              </table>
            </div>
          </TabsContent>

          <TabsContent value="map">
            <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
              {compare.data.map((row) => (
                <CampusMapCard key={row.program.programId} campusMap={row.campusMap} />
              ))}
            </div>
          </TabsContent>
        </Tabs>
      )}
    </div>
  );

  function Row({ label, children }: { label: string; children: (row: NonNullable<typeof compare.data>[number]) => React.ReactNode }) {
    if (!compare.data) return null;
    return (
      <tr>
        <td className="border-t border-[var(--border-subtle)] p-3 text-xs font-medium text-[var(--neutral-400)]">{label}</td>
        {compare.data.map((row) => (
          <td key={row.program.programId} className="border-l border-t border-[var(--border-subtle)] p-3">
            {children(row)}
          </td>
        ))}
      </tr>
    );
  }
}
