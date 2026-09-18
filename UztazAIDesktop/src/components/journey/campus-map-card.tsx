"use client";

import { useTranslations } from "next-intl";
import { ExternalLink, MapPin, DollarSign, ShieldCheck, ThermometerSun, Bus, Users2, MapPinOff } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import dynamic from "next/dynamic";
import { DemoDataBadge } from "@/components/journey/demo-data-badge";
import type { CampusMapDto } from "@/lib/api/types";

// Leaflet touches `window`, so it must only load in the browser.
const UniversityMap = dynamic(() => import("@/components/journey/university-map").then((m) => m.UniversityMap), {
  ssr: false,
  loading: () => <div className="h-64 w-full animate-pulse rounded-[var(--radius-md)] bg-[var(--surface-raised)]" />,
});

const TRANSIT_VARIANT = {
  Poor: "danger",
  Fair: "warning",
  Good: "info",
  Excellent: "success",
} as const;

/** University Map & Environment Intelligence (§14). Folded into the Comparison screen rather
 * than a disconnected map page — see ComparisonPage's "Map & Environment" tab. A university with
 * no seeded map data shows an honest "not mapped yet" state, never a fabricated location. */
export function CampusMapCard({ campusMap }: { campusMap: CampusMapDto }) {
  const t = useTranslations("campusMap");

  const isMapped = campusMap.coordinates && campusMap.environment && campusMap.mapLink;

  return (
    <Card>
      <CardHeader className="flex-row items-start justify-between gap-3">
        <div>
          <CardTitle className="text-base">{campusMap.programName}</CardTitle>
          <p className="text-sm text-[var(--neutral-500)]">
            {campusMap.universityName} · {campusMap.city}, {campusMap.country}
          </p>
        </div>
        {campusMap.environment && <DemoDataBadge provenance={campusMap.environment.provenance} />}
      </CardHeader>
      <CardContent>
        {!isMapped && (
          <div className="flex items-center gap-2.5 rounded-[var(--radius-md)] border border-dashed border-[var(--border-strong)] p-4 text-sm text-[var(--neutral-400)]">
            <MapPinOff className="size-4 shrink-0" />
            {t("notMapped")}
          </div>
        )}

        {isMapped && campusMap.coordinates && campusMap.environment && campusMap.mapLink && (
          <div className="flex flex-col gap-4">
            <UniversityMap lat={campusMap.coordinates.latitude} lon={campusMap.coordinates.longitude} name={campusMap.universityName} city={`${campusMap.city}, ${campusMap.country}`} />
            <a
              href={campusMap.mapLink.embedUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex w-fit items-center gap-2 rounded-[var(--radius-md)] border border-[var(--border-strong)] bg-[var(--surface-raised)] px-3.5 py-2 text-sm font-medium hover:bg-[var(--brand-50)] dark:hover:bg-[var(--brand-900)]"
            >
              <MapPin className="size-4 text-[var(--brand-600)] dark:text-[var(--brand-300)]" />
              {t("openIn", { provider: campusMap.mapLink.provider })}
              <ExternalLink className="size-3.5 opacity-60" />
            </a>

            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
              <StatTile
                icon={DollarSign}
                label={t("costOfLiving")}
                value={`$${Math.round(campusMap.environment.costOfLivingIndexUsdPerMonth).toLocaleString()}/mo`}
              />
              <StatTile icon={ShieldCheck} label={t("safety")} value={`${campusMap.environment.safetyIndex}/100`} />
              <StatTile icon={ThermometerSun} label={t("climate")} value={campusMap.environment.climateSummary} />
              <StatTile
                icon={Bus}
                label={t("transit")}
                value={<Badge variant={TRANSIT_VARIANT[campusMap.environment.publicTransitQuality]}>{campusMap.environment.publicTransitQuality}</Badge>}
              />
              {campusMap.environment.internationalStudentPercent !== null && (
                <StatTile
                  icon={Users2}
                  label={t("internationalStudents")}
                  value={`${campusMap.environment.internationalStudentPercent}%`}
                />
              )}
            </div>

            <p className="text-[11px] text-[var(--neutral-400)]">{t("attribution", { attribution: campusMap.mapLink.attributionText })}</p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function StatTile({ icon: Icon, label, value }: { icon: React.ElementType; label: string; value: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1 rounded-[var(--radius-md)] border border-[var(--border-subtle)] p-3">
      <span className="flex items-center gap-1.5 text-[11px] font-medium text-[var(--neutral-400)]">
        <Icon className="size-3.5" />
        {label}
      </span>
      <span className="text-sm font-semibold">{value}</span>
    </div>
  );
}

