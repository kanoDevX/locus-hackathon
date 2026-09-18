"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { Wallet, Globe, Loader2 } from "lucide-react";
import { useRouter } from "@/i18n/navigation";
import { useProfile } from "@/lib/api/profile";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useJourneyStore } from "@/lib/stores/journey-store";
import { useGenerateRecommendations, useLatestRecommendations, useRefreshAllProgramsFromWeb } from "@/lib/api/recommendations";
import { ApiError } from "@/lib/api/client";
import { RecommendationCard } from "@/components/journey/recommendation-card";
import { Button } from "@/components/ui/button";
import { Card, CardHeader } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { fadeInUp } from "@/lib/motion";

export default function RecommendationsPage() {
  const t = useTranslations("recommendations");
  const router = useRouter();
  const profileId = useAuthStore((s) => s.activeProfileId);
  const { data: latest, isLoading } = useLatestRecommendations(profileId);
  const { data: profile } = useProfile(profileId);
  const grantOnly = profile?.fundingTrackPreference === "GrantOnly";
  const generate = useGenerateRecommendations(profileId);
  const refreshAll = useRefreshAllProgramsFromWeb(profileId);
  const { selectedProgramIds, toggleSelectedProgram } = useJourneyStore();

  const recommendations = latest && latest.length > 0 ? latest : (generate.data?.recommendations ?? latest);

  const affordabilityCounts = recommendations?.reduce(
    (acc, r) => {
      if (r.affordabilityTier === "Affordable") acc.affordable++;
      else if (r.affordabilityTier === "Stretch") acc.stretch++;
      else acc.overBudget++;
      return acc;
    },
    { affordable: 0, stretch: 0, overBudget: 0 }
  );

  useEffect(() => {
    if (profileId && !isLoading && (!latest || latest.length === 0) && !generate.isPending && !generate.data) {
      generate.mutate();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [profileId, isLoading, latest]);

  async function handleRegenerate() {
    try {
      await generate.mutateAsync();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "Could not generate recommendations");
    }
  }

  async function handleRefreshAll() {
    if (!recommendations?.length) return;
    const result = await refreshAll.mutateAsync(recommendations.map((r) => r.program.programId));
    if (result.succeeded === result.total) toast.success(t("webRefreshedAll", { count: result.succeeded }));
    else if (result.succeeded > 0) toast.warning(t("webRefreshedSome", { done: result.succeeded, total: result.total }));
    else toast.error(result.lastError ?? t("webRefreshFailed"));
  }

  return (
    <div>
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
          <p className="mt-1 text-sm text-[var(--neutral-500)]">{t("subtitle")}</p>
        </div>
        <div className="flex items-center gap-2">
          {selectedProgramIds.length >= 2 && (
            <Button size="sm" onClick={() => router.push("/journey/comparison")}>
              {t("compareSelected", { count: selectedProgramIds.length })}
            </Button>
          )}
          <Button variant="secondary" size="sm" onClick={handleRefreshAll} disabled={refreshAll.isPending || !recommendations?.length}>
            {refreshAll.isPending ? <Loader2 className="size-3.5 animate-spin" /> : <Globe className="size-3.5" />}
            {refreshAll.isPending ? t("webRefreshing") : t("refreshAllFromWeb")}
          </Button>
          <Button variant="secondary" size="sm" onClick={handleRegenerate} disabled={generate.isPending}>
            {t("regenerate")}
          </Button>
        </div>
      </div>

      {affordabilityCounts && recommendations && recommendations.length > 0 && (
        <div className="mt-4 flex items-start gap-2.5 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface)] p-3.5 text-sm">
          <Wallet className="mt-0.5 size-4 shrink-0 text-[var(--neutral-400)]" />
          <p className="text-[var(--neutral-600)] dark:text-[var(--neutral-300)]">
            {t(grantOnly ? "affordabilitySummaryGrant" : "affordabilitySummary", {
              affordable: affordabilityCounts.affordable,
              total: recommendations.length,
              stretch: affordabilityCounts.stretch,
              overBudget: affordabilityCounts.overBudget,
            })}
          </p>
        </div>
      )}

      <div className="mt-6 grid grid-cols-1 gap-4 lg:grid-cols-2 xl:grid-cols-3">
        {(isLoading || (generate.isPending && !recommendations)) &&
          Array.from({ length: 3 }).map((_, i) => <RecommendationSkeleton key={i} />)}

        {recommendations?.map((rec, i) => (
          <motion.div key={rec.recommendationId} variants={fadeInUp} custom={i} initial="hidden" animate="visible">
            <RecommendationCard
              recommendation={rec}
              selected={selectedProgramIds.includes(rec.program.programId)}
              onToggleSelect={toggleSelectedProgram}
            />
          </motion.div>
        ))}
      </div>
    </div>
  );
}

function RecommendationSkeleton() {
  return (
    <Card>
      <CardHeader>
        <Skeleton className="h-4 w-16" />
        <Skeleton className="h-5 w-48" />
        <Skeleton className="h-3 w-32" />
      </CardHeader>
      <div className="flex flex-col gap-2 p-5 pt-0">
        <Skeleton className="h-16 w-full" />
        <Skeleton className="h-2 w-full" />
        <Skeleton className="h-2 w-full" />
        <Skeleton className="h-2 w-full" />
      </div>
    </Card>
  );
}
