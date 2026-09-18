"use client";

import { useTranslations } from "next-intl";
import { motion } from "framer-motion";
import {
  Stethoscope, Sparkles, Columns3, Map, Star, CalendarDays, Award, Search, PenLine, MessageCircle,
  ArrowRight, RotateCcw, Target, CheckCircle2, Activity,
} from "lucide-react";
import { Link, useRouter } from "@/i18n/navigation";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useProfile } from "@/lib/api/profile";
import { useEligibilityResult } from "@/lib/api/examIntake";
import { useNextAction } from "@/lib/api/nextAction";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { UncertaintyBand } from "@/components/journey/uncertainty-band";
import { fadeInUp } from "@/lib/motion";

const FEATURES = [
  { href: "/journey/diagnostics", icon: Stethoscope, key: "diagnostics" },
  { href: "/journey/recommendations", icon: Sparkles, key: "recommendations" },
  { href: "/journey/roadmap", icon: Map, key: "roadmap" },
  { href: "/journey/comparison", icon: Columns3, key: "comparison" },
  { href: "/journey/chat", icon: MessageCircle, key: "chat" },
  { href: "/journey/favorites", icon: Star, key: "favorites" },
  { href: "/journey/calendar", icon: CalendarDays, key: "calendar" },
  { href: "/journey/scholarships", icon: Award, key: "scholarships" },
  { href: "/journey/programs", icon: Search, key: "programs" },
  { href: "/journey/essays", icon: PenLine, key: "essays" },
  { href: "/journey/insights", icon: Activity, key: "insights" },
] as const;

export default function HomePage() {
  const t = useTranslations("home");
  const tNav = useTranslations("nav");
  const router = useRouter();
  const profileId = useAuthStore((s) => s.activeProfileId);
  const { data: profile } = useProfile(profileId);
  const { data: eligibility, isLoading: eligibilityLoading } = useEligibilityResult(profileId);
  const { data: nextAction, isLoading: nextActionLoading } = useNextAction(profileId);

  const topResult = eligibility && eligibility.length > 0
    ? [...eligibility].sort((a, b) => b.grantCompetitiveness.estimate - a.grantCompetitiveness.estimate)[0]
    : null;

  return (
    <div className="mx-auto flex max-w-5xl flex-col gap-8">
      <motion.div variants={fadeInUp} initial="hidden" animate="visible">
        <p className="text-sm font-medium text-[var(--brand-600)] dark:text-[var(--brand-300)]">{t("eyebrow")}</p>
        <h1 className="mt-1 text-2xl font-semibold tracking-tight sm:text-3xl">{t("greeting", { name: profile?.fullName?.split(" ")[0] ?? "" })}</h1>
        <p className="mt-1 text-sm text-[var(--neutral-500)]">{t("subtitle")}</p>
      </motion.div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <motion.div variants={fadeInUp} custom={1} initial="hidden" animate="visible">
          <Card className="h-full">
            <CardHeader className="flex-row items-center gap-2.5">
              <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-[var(--brand-100)] text-[var(--brand-700)] dark:bg-[var(--brand-900)] dark:text-[var(--brand-200)]">
                <Target className="size-4" />
              </span>
              <CardTitle className="text-base">{t("nextActionTitle")}</CardTitle>
            </CardHeader>
            <CardContent>
              {nextActionLoading && <Skeleton className="h-16 w-full" />}
              {!nextActionLoading && !nextAction && (
                <p className="text-sm text-[var(--neutral-500)]">{t("nextActionEmpty")}</p>
              )}
              {nextAction && (
                <div className="flex flex-col gap-2">
                  <p className="text-sm font-semibold">{nextAction.title}</p>
                  <p className="text-xs text-[var(--neutral-500)]">{nextAction.description}</p>
                  <Button size="sm" className="mt-1 w-fit" onClick={() => router.push("/journey/roadmap")}>
                    {t("nextActionCta")}
                    <ArrowRight className="size-3.5" />
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>
        </motion.div>

        <motion.div variants={fadeInUp} custom={2} initial="hidden" animate="visible">
          <Card className="h-full">
            <CardHeader className="flex-row items-center gap-2.5">
              <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-[var(--success-100)] text-[var(--success-500)]">
                <CheckCircle2 className="size-4" />
              </span>
              <CardTitle className="text-base">{t("eligibilityTitle")}</CardTitle>
            </CardHeader>
            <CardContent>
              {eligibilityLoading && <Skeleton className="h-16 w-full" />}
              {!eligibilityLoading && !topResult && <p className="text-sm text-[var(--neutral-500)]">{t("eligibilityEmpty")}</p>}
              {topResult && (
                <div className="flex flex-col gap-2">
                  <p className="text-sm font-semibold">
                    {topResult.program.programName} <span className="font-normal text-[var(--neutral-500)]">· {topResult.program.universityName}</span>
                  </p>
                  {topResult.isDocumentOnlyVerdict ? (
                    <Badge variant="info" className="w-fit">{t("documentOnlyBadge")}</Badge>
                  ) : (
                    <UncertaintyBand estimate={topResult.grantCompetitiveness} label={t("grantCompetitivenessLabel")} />
                  )}
                  <Button size="sm" variant="secondary" className="mt-1 w-fit" onClick={() => router.push("/journey/eligibility-result")}>
                    {t("eligibilityCta")}
                    <ArrowRight className="size-3.5" />
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>
        </motion.div>
      </div>

      <motion.div variants={fadeInUp} custom={3} initial="hidden" animate="visible">
        <Card className="border-dashed">
          <CardContent className="flex flex-wrap items-center justify-between gap-4 py-4">
            <div className="flex items-center gap-3">
              <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-[var(--neutral-100)] text-[var(--neutral-500)] dark:bg-[var(--neutral-800)]">
                <RotateCcw className="size-4" />
              </span>
              <div>
                <p className="text-sm font-semibold">{t("retakeTitle")}</p>
                <p className="mt-0.5 text-xs text-[var(--neutral-500)]">{t("retakeDescription")}</p>
              </div>
            </div>
            <Button variant="secondary" size="sm" onClick={() => router.push("/journey/profile")}>
              <RotateCcw className="size-3.5" />
              {t("retakeCta")}
            </Button>
          </CardContent>
        </Card>
      </motion.div>

      <div>
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-[var(--neutral-400)]">{t("exploreTitle")}</h2>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
          {FEATURES.map((feature, i) => (
            <motion.div key={feature.href} variants={fadeInUp} custom={i * 0.3} initial="hidden" animate="visible">
              <Link
                href={feature.href}
                className="flex h-full flex-col items-start gap-2 rounded-[var(--radius-lg)] border border-[var(--border-subtle)] bg-[var(--surface)] p-4 shadow-[var(--shadow-raised)] transition-colors duration-150 hover:bg-[var(--surface-raised)]"
              >
                <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-[var(--brand-100)] text-[var(--brand-700)] dark:bg-[var(--brand-900)] dark:text-[var(--brand-200)]">
                  <feature.icon className="size-4" />
                </span>
                <div>
                  <p className="text-sm font-semibold">{tNav(feature.key)}</p>
                  <p className="mt-0.5 text-xs text-[var(--neutral-500)]">{t(`featureHint.${feature.key}`)}</p>
                </div>
              </Link>
            </motion.div>
          ))}
        </div>
      </div>
    </div>
  );
}
