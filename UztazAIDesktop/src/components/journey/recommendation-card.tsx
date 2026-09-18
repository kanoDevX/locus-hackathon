"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { motion, AnimatePresence } from "framer-motion";
import { MapPin, ChevronDown, Star, Sparkles, Sliders, Users, Wallet } from "lucide-react";
import { toast } from "sonner";
import { Link } from "@/i18n/navigation";
import { Card, CardHeader, CardContent, CardFooter } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { FitScoreBar } from "./fit-score-bar";
import { UncertaintyBand } from "./uncertainty-band";
import { DemoDataBadge } from "./demo-data-badge";
import { diffHighlight } from "@/lib/motion";
import { useJourneyStore } from "@/lib/stores/journey-store";
import { useAddFavorite, useFavorites, useRemoveFavorite } from "@/lib/api/favorites";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useRefreshProgramFromWeb } from "@/lib/api/recommendations";
import { useProfile } from "@/lib/api/profile";
import type { AffordabilityTier, RecommendationDto } from "@/lib/api/types";

export function RecommendationCard({
  recommendation,
  selected,
  onToggleSelect,
}: {
  recommendation: RecommendationDto;
  selected: boolean;
  onToggleSelect: (programId: number) => void;
}) {
  const t = useTranslations("recommendations");
  const [showReasoning, setShowReasoning] = useState(false);
  const recentlyChanged = useJourneyStore((s) => s.recentlyChangedProgramIds);
  const profileId = useAuthStore((s) => s.activeProfileId);
  const { data: favorites } = useFavorites(profileId);
  const addFavorite = useAddFavorite(profileId);
  const removeFavorite = useRemoveFavorite(profileId);
  const refreshFromWeb = useRefreshProgramFromWeb(profileId);
  const { data: profile } = useProfile(profileId);
  const grantOnly = profile?.fundingTrackPreference === "GrantOnly";

  const isHighlighted = recentlyChanged.has(recommendation.program.programId);
  const isFavorite = favorites?.some((f) => f.program.programId === recommendation.program.programId);

  async function toggleFavorite() {
    try {
      if (isFavorite) {
        await removeFavorite.mutateAsync(recommendation.program.programId);
        toast.success(t("removedFromFavorites"));
      } else {
        await addFavorite.mutateAsync({ programId: recommendation.program.programId });
        toast.success(t("savedToFavorites"));
      }
    } catch {
      /* toast handled globally by react-query error boundary in a fuller build */
    }
  }

  return (
    <motion.div
      layout
      initial={isHighlighted ? "initial" : false}
      animate={isHighlighted ? "animate" : undefined}
      variants={diffHighlight}
      className="rounded-[var(--radius-lg)]"
    >
      <Card>
        <CardHeader className="gap-2">
          <div className="flex items-start justify-between gap-3">
            <div>
              <div className="flex items-center gap-2">
                <Badge variant="neutral">#{recommendation.rankPosition}</Badge>
                <h3 className="text-base font-semibold leading-tight">{recommendation.program.programName}</h3>
              </div>
              <p className="mt-1 text-sm text-[var(--neutral-500)]">{recommendation.program.universityName}</p>
              <p className="mt-0.5 flex items-center gap-1 text-xs text-[var(--neutral-400)]">
                <MapPin className="size-3" />
                {recommendation.program.city}, {recommendation.program.country}
              </p>
            </div>
            <Button
              variant="ghost"
              size="icon"
              aria-label="favorite"
              onClick={toggleFavorite}
              disabled={addFavorite.isPending || removeFavorite.isPending}
            >
              <Star className={isFavorite ? "size-4 fill-[var(--warning-500)] text-[var(--warning-500)]" : "size-4"} />
            </Button>
          </div>
          <div className="flex flex-wrap items-center gap-1.5">
            <DemoDataBadge provenance={recommendation.provenance} onVerify={() => refreshFromWeb.mutateAsync(recommendation.program.programId)} />
            <AffordabilityBadge
              tier={recommendation.affordabilityTier}
              effectiveCostUsd={recommendation.affordabilityEffectiveCostUsd}
              grantOnly={grantOnly}
            />
          </div>
        </CardHeader>

        <CardContent className="flex flex-col gap-5">
          <div>
            <p className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-[var(--brand-600)] dark:text-[var(--brand-300)]">
              {t("whyFits")}
            </p>
            <p className="text-sm leading-relaxed">{recommendation.narrativeSummary}</p>
          </div>

          <div className="flex flex-col gap-2">
            <FitScoreBar label={t("academic")} score={recommendation.academicFitScore} />
            <FitScoreBar label={t("financial")} score={recommendation.financialFitScore} />
            <FitScoreBar label={t("career")} score={recommendation.careerFitScore} />
            <FitScoreBar label={t("timeline")} score={recommendation.timelineFitScore} />
          </div>

          <UncertaintyBand estimate={recommendation.admissionProbability} />

          <button
            onClick={() => setShowReasoning((v) => !v)}
            className="flex items-center gap-1.5 text-xs font-medium text-[var(--primary)] hover:underline"
          >
            <Sparkles className="size-3.5" />
            {t("seeReasoning")}
            <ChevronDown className={`size-3.5 transition-transform duration-200 ${showReasoning ? "rotate-180" : ""}`} />
          </button>

          <AnimatePresence initial={false}>
            {showReasoning && (
              <motion.div
                initial={{ height: 0, opacity: 0 }}
                animate={{ height: "auto", opacity: 1 }}
                exit={{ height: 0, opacity: 0 }}
                transition={{ duration: 0.28, ease: [0.4, 0, 0.2, 1] }}
                className="overflow-hidden"
              >
                <div className="flex flex-col gap-3 rounded-[var(--radius-md)] bg-[var(--surface-raised)] p-3.5 text-xs leading-relaxed text-[var(--neutral-600)] dark:text-[var(--neutral-300)]">
                  <ReasoningLine label={t("academic")} text={recommendation.academicFitExplanation} />
                  <ReasoningLine label={t("financial")} text={recommendation.financialFitExplanation} />
                  <ReasoningLine label={t("career")} text={recommendation.careerFitExplanation} />
                  <ReasoningLine label={t("timeline")} text={recommendation.timelineFitExplanation} />
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </CardContent>

        <CardFooter className="flex-wrap items-center gap-3">
          <label className="flex items-center gap-2 text-xs text-[var(--neutral-500)]">
            <input
              type="checkbox"
              checked={selected}
              onChange={() => onToggleSelect(recommendation.program.programId)}
              className="size-4 rounded border-[var(--border-strong)] accent-[var(--primary)]"
            />
            {t("compareLabel")}
          </label>
          <Link
            href={`/journey/what-if?programId=${recommendation.program.programId}`}
            className="flex items-center gap-1 text-xs font-medium text-[var(--neutral-500)] hover:text-[var(--primary)]"
          >
            <Sliders className="size-3.5" /> {t("whatIf")}
          </Link>
          <Link
            href={`/journey/peer-pathways?programId=${recommendation.program.programId}`}
            className="flex items-center gap-1 text-xs font-medium text-[var(--neutral-500)] hover:text-[var(--primary)]"
          >
            <Users className="size-3.5" /> {t("peerPathways")}
          </Link>
        </CardFooter>
      </Card>
    </motion.div>
  );
}

function ReasoningLine({ label, text }: { label: string; text: string }) {
  return (
    <p>
      <span className="font-semibold text-[var(--foreground)]">{label}: </span>
      {text}
    </p>
  );
}

const AFFORDABILITY_VARIANT: Record<AffordabilityTier, "success" | "warning" | "danger"> = {
  Affordable: "success",
  Stretch: "warning",
  OverBudget: "danger",
};

/** Affordability & Fairness Audit (§10.6) — a program can score well academically and still be
 * honestly out of reach financially; this discloses that plainly rather than burying it in a
 * bare 0-100 "financial fit" number. Tier and cost come straight from
 * HybridScoringEngine.EvaluateAffordability, never AI-estimated. */
function AffordabilityBadge({ tier, effectiveCostUsd, grantOnly }: { tier: AffordabilityTier; effectiveCostUsd: number; grantOnly: boolean }) {
  const t = useTranslations("recommendations");
  return (
    <Badge variant={AFFORDABILITY_VARIANT[tier]} className="gap-1">
      <Wallet className="size-3" />
      {t(grantOnly ? `affordabilityTierGrant.${tier}` : `affordabilityTier.${tier}`)} · ${effectiveCostUsd.toLocaleString()}/{t("perYear")}
    </Badge>
  );
}
