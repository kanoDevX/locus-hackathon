"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Search as SearchIcon, MapPin } from "lucide-react";
import { useProgramSearch } from "@/lib/api/programs";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Skeleton } from "@/components/ui/skeleton";

export default function ProgramsPage() {
  const t = useTranslations("programs");
  const [query, setQuery] = useState("");
  const [scholarshipOnly, setScholarshipOnly] = useState(false);
  const { data, isLoading } = useProgramSearch({ query: query || undefined, scholarshipOnly: scholarshipOnly || undefined });

  return (
    <div className="mx-auto max-w-3xl">
      <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>

      <div className="mt-4 flex flex-wrap items-center gap-4">
        <div className="relative flex-1 min-w-56">
          <SearchIcon className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-[var(--neutral-400)]" />
          <Input value={query} onChange={(e) => setQuery(e.target.value)} placeholder={t("search")} className="pl-9" />
        </div>
        <label className="flex items-center gap-2 text-sm">
          <Switch checked={scholarshipOnly} onCheckedChange={setScholarshipOnly} />
          {t("scholarshipOnly")}
        </label>
      </div>

      <div className="mt-6 grid grid-cols-1 gap-3 sm:grid-cols-2">
        {isLoading && Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-24 w-full" />)}

        {data?.map((p) => (
          <Card key={p.programId}>
            <CardContent className="p-4">
              <p className="text-sm font-semibold">{p.programName}</p>
              <p className="text-xs text-[var(--neutral-500)]">{p.universityName}</p>
              <p className="mt-1 flex items-center gap-1 text-xs text-[var(--neutral-400)]">
                <MapPin className="size-3" /> {p.city}, {p.country}
              </p>
              <p className="mt-2 text-xs">
                ${p.tuitionPerYearUsd.toLocaleString()}/yr · {p.fieldOfStudy}
              </p>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
