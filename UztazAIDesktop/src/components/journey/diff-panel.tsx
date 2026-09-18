"use client";

import { AnimatePresence, motion } from "framer-motion";
import { useTranslations } from "next-intl";
import { X, Sparkles } from "lucide-react";
import { useJourneyStore } from "@/lib/stores/journey-store";
import { Link } from "@/i18n/navigation";
import { slideInFromRight } from "@/lib/motion";

export function DiffPanel() {
  const t = useTranslations("diff");
  const { diffPanel, dismissDiffPanel } = useJourneyStore();

  return (
    <div className="pointer-events-none fixed inset-x-0 top-4 z-40 flex justify-center px-4 sm:justify-end sm:pr-8">
      <AnimatePresence>
        {diffPanel && (
          <motion.div
            variants={slideInFromRight}
            initial="hidden"
            animate="visible"
            exit="exit"
            className="pointer-events-auto w-full max-w-sm rounded-[var(--radius-lg)] border border-[var(--border-subtle)] bg-[var(--surface)] p-4 shadow-[var(--shadow-overlay)]"
          >
            <div className="flex items-start gap-3">
              <span className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-full bg-[var(--brand-100)] text-[var(--brand-700)] dark:bg-[var(--brand-900)] dark:text-[var(--brand-200)]">
                <Sparkles className="size-4" />
              </span>
              <div className="min-w-0 flex-1">
                <p className="text-sm font-semibold">
                  {diffPanel.kind === "profile" ? t("profileUpdated") : t("recommendationsChanged")}
                </p>

                {diffPanel.profileDiff && (
                  <p className="mt-1 text-xs leading-relaxed text-[var(--neutral-500)]">
                    {diffPanel.profileDiff.impactSummary}
                  </p>
                )}

                {diffPanel.recommendationDelta && (
                  <div className="mt-1.5 flex flex-wrap gap-1.5">
                    {diffPanel.recommendationDelta.addedProgramIds.length > 0 && (
                      <span className="rounded-full bg-[var(--success-100)] px-2 py-0.5 text-[11px] font-medium text-[var(--success-500)]">
                        {t("added", { count: diffPanel.recommendationDelta.addedProgramIds.length })}
                      </span>
                    )}
                    {diffPanel.recommendationDelta.removedProgramIds.length > 0 && (
                      <span className="rounded-full bg-[var(--danger-100)] px-2 py-0.5 text-[11px] font-medium text-[var(--danger-500)]">
                        {t("removed", { count: diffPanel.recommendationDelta.removedProgramIds.length })}
                      </span>
                    )}
                    {diffPanel.recommendationDelta.rankChangedProgramIds.length > 0 && (
                      <span className="rounded-full bg-[var(--info-100)] px-2 py-0.5 text-[11px] font-medium text-[var(--info-500)]">
                        {t("reranked", { count: diffPanel.recommendationDelta.rankChangedProgramIds.length })}
                      </span>
                    )}
                  </div>
                )}

                <div className="mt-3 flex items-center gap-3">
                  <Link
                    href="/journey/recommendations"
                    onClick={dismissDiffPanel}
                    className="text-xs font-medium text-[var(--primary)] hover:underline"
                  >
                    {t("viewChanges")}
                  </Link>
                  <button
                    onClick={dismissDiffPanel}
                    className="text-xs font-medium text-[var(--neutral-400)] hover:text-[var(--neutral-600)]"
                  >
                    {t("dismiss")}
                  </button>
                </div>
              </div>
              <button
                onClick={dismissDiffPanel}
                aria-label="Close"
                className="rounded-full p-1 text-[var(--neutral-400)] hover:bg-[var(--surface-raised)]"
              >
                <X className="size-3.5" />
              </button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
