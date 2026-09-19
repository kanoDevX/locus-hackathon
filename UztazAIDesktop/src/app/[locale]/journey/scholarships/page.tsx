"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Award, Globe, Loader2, ExternalLink, Sparkles } from "lucide-react";
import { toast } from "sonner";
import { useScholarshipSearch, useDiscoverScholarships } from "@/lib/api/scholarships";
import { ApiError } from "@/lib/api/client";
import { DemoDataBadge } from "@/components/journey/demo-data-badge";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input, Label } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";

export default function ScholarshipsPage() {
  const t = useTranslations("scholarships");
  const [country, setCountry] = useState("");
  const [minCoverage, setMinCoverage] = useState<number | undefined>(undefined);
  const { data, isLoading } = useScholarshipSearch({ country: country || undefined, minCoverage });
  const [university, setUniversity] = useState("");
  const [uniCountry, setUniCountry] = useState("");
  const discover = useDiscoverScholarships();

  async function runDiscovery() {
    if (!university.trim() || !uniCountry.trim()) return;
    try {
      await discover.mutateAsync({ universityName: university.trim(), country: uniCountry.trim() });
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("discoverFailed"));
    }
  }

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>

      <Card className="mt-4">
        <CardContent className="flex flex-col gap-3 p-4">
          <div className="flex items-start gap-2.5">
            <Sparkles className="mt-0.5 size-4 shrink-0 text-[var(--brand-600)]" />
            <div>
              <p className="text-sm font-semibold">{t("discoverTitle")}</p>
              <p className="text-xs text-[var(--neutral-500)]">{t("discoverHint")}</p>
            </div>
          </div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div className="flex flex-col gap-1.5">
              <Label>{t("university")}</Label>
              <Input value={university} onChange={(e) => setUniversity(e.target.value)} placeholder="Nazarbayev University" />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label>{t("country")}</Label>
              <Input value={uniCountry} onChange={(e) => setUniCountry(e.target.value)} placeholder="Kazakhstan" />
            </div>
          </div>
          <Button type="button" onClick={runDiscovery} disabled={discover.isPending || !university.trim() || !uniCountry.trim()} className="w-fit">
            {discover.isPending ? <Loader2 className="size-4 animate-spin" /> : <Globe className="size-4" />}
            {discover.isPending ? t("discovering") : t("discover")}
          </Button>

          {discover.data && (
            <div className="mt-1 flex flex-col gap-3">
              <p className="text-xs text-[var(--neutral-500)]">{t("discoverDisclaimer")}</p>
              {discover.data.scholarships.length === 0 && (
                <p className="text-sm text-[var(--neutral-500)]">{t("discoverNone")}</p>
              )}
              {discover.data.scholarships.map((sc, i) => (
                <div key={i} className="rounded-[var(--radius-md)] border border-[var(--border-subtle)] p-3">
                  <p className="text-sm font-semibold">{sc.name}</p>
                  {sc.coverage && <p className="mt-1 text-sm font-medium text-[var(--success-500)]">{sc.coverage}</p>}
                  {sc.eligibility && <p className="mt-1 text-xs text-[var(--neutral-500)]">{sc.eligibility}</p>}
                  {sc.deadline && (
                    <p className="mt-1 text-xs text-[var(--neutral-500)]">
                      {t("deadline")}: {sc.deadline}
                    </p>
                  )}
                </div>
              ))}
              <div>
                <p className="mb-1 text-xs font-medium text-[var(--neutral-500)]">{t("sources")}</p>
                <ul className="flex flex-col gap-1">
                  {discover.data.sources.map((src) => (
                    <li key={src.url}>
                      <a
                        href={src.url}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-flex items-center gap-1 text-xs text-[var(--brand-600)] hover:underline"
                      >
                        {src.title || src.url}
                        <ExternalLink className="size-3" />
                      </a>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <div className="mt-4 grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label>{t("country")}</Label>
          <Input value={country} onChange={(e) => setCountry(e.target.value)} placeholder="Kazakhstan" />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>{t("minCoverage")}</Label>
          <Input
            type="number"
            min={0}
            max={100}
            value={minCoverage ?? ""}
            onChange={(e) => setMinCoverage(e.target.value ? Number(e.target.value) : undefined)}
          />
        </div>
      </div>

      <div className="mt-6 flex flex-col gap-3">
        {isLoading && Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}

        {data?.map((s) => (
          <Card key={s.scholarshipId}>
            <CardContent className="flex items-start justify-between gap-3 p-4">
              <div className="flex items-start gap-3">
                <span className="mt-0.5 flex size-9 items-center justify-center rounded-full bg-[var(--warning-100)] text-[var(--warning-500)]">
                  <Award className="size-4" />
                </span>
                <div>
                  <p className="text-sm font-semibold">{s.name}</p>
                  <p className="text-xs text-[var(--neutral-500)]">{s.program?.programName ?? "General scholarship"}</p>
                  <p className="mt-1 text-xs text-[var(--neutral-500)]">{s.eligibilityCriteria}</p>
                  <p className="mt-1 text-sm font-medium text-[var(--success-500)]">{s.coveragePercent}% coverage</p>
                </div>
              </div>
              <DemoDataBadge provenance={s.provenance} />
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
