"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { useRouter } from "@/i18n/navigation";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useCalculateEligibility, useEligibilityResult } from "@/lib/api/examIntake";
import { ApiError } from "@/lib/api/client";
import { EligibilityResultCard } from "@/components/journey/eligibility-result-card";
import { IntakeProgressScreen, type IntakeStage } from "@/components/journey/intake-progress-screen";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { fadeInUp } from "@/lib/motion";

/**
 * §12 result screen: the exam-intake wizard hands off here immediately after submitting, this
 * page triggers the (deterministic, synchronous) eligibility calculation, shows the real staged
 * progress while it runs, then reveals the dual grant/paid verdict per program. From here the
 * student continues into the existing journey (Diagnostics is the next mandatory stage) rather
 * than a separate "home dashboard" screen this codebase doesn't otherwise have.
 */
export default function EligibilityResultPage() {
  const t = useTranslations("eligibility");
  const router = useRouter();
  const profileId = useAuthStore((s) => s.activeProfileId);
  const { data: existingResults } = useEligibilityResult(profileId);
  const calculate = useCalculateEligibility(profileId);

  const results = existingResults && existingResults.length > 0 ? existingResults : (calculate.data ?? existingResults);

  useEffect(() => {
    if (profileId && !results && calculate.status === "idle") {
      calculate.mutate();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [profileId, results]);

  const stages: IntakeStage[] = [
    { key: "intake", label: t("stageIntakeSaved"), status: "done" },
    {
      key: "calculate",
      label: t("stageCalculating"),
      status: calculate.status === "success" || results ? "done" : calculate.status === "error" ? "error" : "active",
    },
  ];

  const showProgress = !results && calculate.status !== "error";

  return (
    <div className="mx-auto max-w-2xl">
      {showProgress && <IntakeProgressScreen stages={stages} title={t("progressTitle")} subtitle={t("progressSubtitle")} />}

      {calculate.status === "error" && !results && (
        <Card className="mt-6">
          <CardContent className="flex flex-col items-center gap-3 p-8 text-center">
            <p className="text-sm text-[var(--danger-500)]">
              {calculate.error instanceof ApiError ? calculate.error.message : t("calculateFailed")}
            </p>
            <Button size="sm" onClick={() => calculate.mutate()}>
              {t("retry")}
            </Button>
          </CardContent>
        </Card>
      )}

      {results && results.length > 0 && (
        <motion.div variants={fadeInUp} initial="hidden" animate="visible">
          <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
          <p className="mt-1 text-sm text-[var(--neutral-500)]">{t("subtitle")}</p>

          <div className="mt-6 flex flex-col gap-4">
            {results.map((result) => (
              <EligibilityResultCard key={result.id} result={result} />
            ))}
          </div>

          <div className="mt-6 flex justify-end">
            <Button
              onClick={() => {
                toast.success(t("continueToast"));
                router.push("/journey/home");
              }}
            >
              {t("continue")}
            </Button>
          </div>
        </motion.div>
      )}

      {results && results.length === 0 && (
        <Card className="mt-6">
          <CardContent className="p-8 text-center text-sm text-[var(--neutral-500)]">{t("noResults")}</CardContent>
        </Card>
      )}
    </div>
  );
}
