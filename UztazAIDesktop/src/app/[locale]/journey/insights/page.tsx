"use client";

import { useTranslations } from "next-intl";
import { motion } from "framer-motion";
import { Activity, Gauge, Zap, Coins, CheckCircle2, AlertTriangle, BarChart3 } from "lucide-react";
import { useInsights } from "@/lib/api/insights";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { fadeInUp } from "@/lib/motion";

/**
 * Judge-facing "engineering maturity" dashboard (§2/§6) — surfaces real numbers pulled from the
 * AiUsageLog table (every Gemini call this environment has ever made: latency, success/fallback
 * rate, token spend) rather than asserting "the product is fast and reliable" with nothing behind
 * it. The backend endpoint (`GET /system/insights`) existed with exactly this purpose documented
 * in its own doc comment but had no frontend page at all until this one — the numbers were real
 * but literally unreachable by anyone without Swagger/curl.
 */
export default function InsightsPage() {
  const t = useTranslations("insights");
  const { data, isLoading } = useInsights();

  return (
    <div className="mx-auto max-w-4xl">
      <motion.div variants={fadeInUp} initial="hidden" animate="visible">
        <div className="flex items-center gap-2.5">
          <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-[var(--brand-100)] text-[var(--brand-700)] dark:bg-[var(--brand-900)] dark:text-[var(--brand-200)]">
            <Activity className="size-4" />
          </span>
          <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
        </div>
        <p className="mt-2 text-sm text-[var(--neutral-500)]">{t("subtitle")}</p>
      </motion.div>

      {isLoading && (
        <div className="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-24 w-full" />
          ))}
        </div>
      )}

      {!isLoading && data && data.totalAiCalls === 0 && (
        <Card className="mt-6">
          <CardContent className="p-8 text-center text-sm text-[var(--neutral-500)]">{t("empty")}</CardContent>
        </Card>
      )}

      {!isLoading && data && data.totalAiCalls > 0 && (
        <>
          <motion.div
            variants={fadeInUp}
            custom={1}
            initial="hidden"
            animate="visible"
            className="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-3"
          >
            <StatTile icon={Zap} label={t("totalCalls")} value={data.totalAiCalls.toLocaleString()} />
            <StatTile
              icon={CheckCircle2}
              label={t("successRate")}
              value={`${data.successRatePercent}%`}
              tone={data.successRatePercent >= 90 ? "success" : "warning"}
            />
            <StatTile
              icon={AlertTriangle}
              label={t("fallbackRate")}
              value={`${data.fallbackRatePercent}%`}
              tone={data.fallbackRatePercent <= 10 ? "success" : "warning"}
            />
            <StatTile icon={Gauge} label={t("avgLatency")} value={t("msValue", { ms: Math.round(data.averageLatencyMs) })} />
            <StatTile icon={Gauge} label={t("p95Latency")} value={t("msValue", { ms: Math.round(data.p95LatencyMs) })} />
            <StatTile
              icon={Coins}
              label={t("totalTokens")}
              value={(data.totalInputTokens + data.totalOutputTokens).toLocaleString()}
            />
          </motion.div>

          <motion.div variants={fadeInUp} custom={2} initial="hidden" animate="visible" className="mt-6">
            <Card>
              <CardHeader className="flex-row items-center gap-2.5">
                <BarChart3 className="size-4 text-[var(--neutral-400)]" />
                <CardTitle className="text-base">{t("byModuleTitle")}</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-col gap-2">
                {data.byModule
                  .sort((a, b) => b.calls - a.calls)
                  .map((m) => (
                    <div
                      key={m.module}
                      className="flex flex-wrap items-center justify-between gap-2 rounded-[var(--radius-md)] border border-[var(--border-subtle)] p-3"
                    >
                      <p className="text-sm font-medium">{m.module}</p>
                      <div className="flex items-center gap-2">
                        <Badge variant="neutral">{t("callsCount", { count: m.calls })}</Badge>
                        <Badge variant={m.successRatePercent >= 90 ? "success" : "warning"}>
                          {t("successRate")} {m.successRatePercent}%
                        </Badge>
                        {m.fallbackRatePercent > 0 && (
                          <Badge variant="info">
                            {t("fallbackRate")} {m.fallbackRatePercent}%
                          </Badge>
                        )}
                      </div>
                    </div>
                  ))}
              </CardContent>
            </Card>
          </motion.div>

          <p className="mt-4 text-xs text-[var(--neutral-400)]">{t("disclaimer")}</p>
        </>
      )}
    </div>
  );
}

function StatTile({
  icon: Icon,
  label,
  value,
  tone,
}: {
  icon: React.ElementType;
  label: string;
  value: string;
  tone?: "success" | "warning";
}) {
  const toneClass =
    tone === "success"
      ? "text-[var(--success-500)]"
      : tone === "warning"
        ? "text-[var(--warning-500)]"
        : "text-[var(--foreground)]";
  return (
    <Card>
      <CardContent className="flex flex-col gap-1.5 p-4">
        <div className="flex items-center gap-1.5 text-xs text-[var(--neutral-500)]">
          <Icon className="size-3.5" />
          {label}
        </div>
        <p className={`text-xl font-semibold tabular-nums ${toneClass}`}>{value}</p>
      </CardContent>
    </Card>
  );
}
