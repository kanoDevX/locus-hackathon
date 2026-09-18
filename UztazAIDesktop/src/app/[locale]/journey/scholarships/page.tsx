"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Award } from "lucide-react";
import { useScholarshipSearch } from "@/lib/api/scholarships";
import { DemoDataBadge } from "@/components/journey/demo-data-badge";
import { Card, CardContent } from "@/components/ui/card";
import { Input, Label } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";

export default function ScholarshipsPage() {
  const t = useTranslations("scholarships");
  const [country, setCountry] = useState("");
  const [minCoverage, setMinCoverage] = useState<number | undefined>(undefined);
  const { data, isLoading } = useScholarshipSearch({ country: country || undefined, minCoverage });

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>

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
