"use client";

import { useEffect } from "react";
import { useLocale, useTranslations } from "next-intl";
import { motion } from "framer-motion";
import { useQueryClient } from "@tanstack/react-query";
import { CheckCircle2, AlertCircle, Target, Gauge } from "lucide-react";
import { toast } from "sonner";
import { useRouter } from "@/i18n/navigation";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useGenerateDiagnostics } from "@/lib/api/diagnostics";
import { ApiError } from "@/lib/api/client";
import type { DiagnosticsDto } from "@/lib/api/types";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { fadeInUp } from "@/lib/motion";

export default function DiagnosticsPage() {
  const t = useTranslations("diagnostics");
  const router = useRouter();
  const locale = useLocale();
  const profileId = useAuthStore((s) => s.activeProfileId);
  const queryClient = useQueryClient();
  const generate = useGenerateDiagnostics(profileId);

  const cached = queryClient.getQueryData<DiagnosticsDto>(["diagnostics", profileId, locale]);
  const diagnostics = generate.data ?? cached;

  useEffect(() => {
    if (!diagnostics && profileId && !generate.isPending) {
      generate.mutate();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [profileId, locale]);

  async function handleRegenerate() {
    try {
      await generate.mutateAsync();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "Could not run diagnostics");
    }
  }

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
      <p className="mt-1 text-sm text-[var(--neutral-500)]">{t("subtitle")}</p>

      <div className="mt-6">
        {generate.isPending && !diagnostics && <DiagnosticsSkeleton />}

        {diagnostics && (
          <motion.div variants={fadeInUp} initial="hidden" animate="visible">
            <Card>
              <CardHeader className="flex-row items-center justify-between">
                <div>
                  <CardTitle className="flex items-center gap-2">
                    <Target className="size-4 text-[var(--brand-600)] dark:text-[var(--brand-300)]" />
                    {t("goal")}
                  </CardTitle>
                  <CardDescription className="mt-1 text-[var(--foreground)]">{diagnostics.inferredGoal}</CardDescription>
                </div>
                <Badge variant={diagnostics.fallbackUsed ? "warning" : "brand"}>
                  {diagnostics.fallbackUsed ? t("fallback") : t("aiNarrated")}
                </Badge>
              </CardHeader>
              <CardContent className="flex flex-col gap-5">
                <div className="flex items-center gap-2 text-sm text-[var(--neutral-500)]">
                  <Gauge className="size-4" />
                  {t("confidence")}: {Math.round(diagnostics.confidenceScore * 100)}%
                </div>

                <div>
                  <p className="mb-2 text-sm font-medium">{t("strengths")}</p>
                  <div className="flex flex-wrap gap-2">
                    {diagnostics.strengths.map((s, i) => (
                      <Badge key={i} variant="success" className="gap-1.5">
                        <CheckCircle2 className="size-3.5" />
                        {s}
                      </Badge>
                    ))}
                  </div>
                </div>

                <div>
                  <p className="mb-2 text-sm font-medium">{t("constraints")}</p>
                  <div className="flex flex-wrap gap-2">
                    {diagnostics.constraintsFound.map((c, i) => (
                      <Badge key={i} variant="warning" className="gap-1.5">
                        <AlertCircle className="size-3.5" />
                        {c}
                      </Badge>
                    ))}
                  </div>
                </div>
              </CardContent>
            </Card>

            <div className="mt-5 flex items-center justify-between">
              <Button variant="secondary" size="sm" onClick={handleRegenerate} disabled={generate.isPending}>
                {t("regenerate")}
              </Button>
              <Button onClick={() => router.push("/journey/recommendations")}>{t("continueTo")}</Button>
            </div>
          </motion.div>
        )}
      </div>
    </div>
  );
}

function DiagnosticsSkeleton() {
  return (
    <Card>
      <CardHeader>
        <Skeleton className="h-4 w-40" />
        <Skeleton className="h-5 w-64" />
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <Skeleton className="h-4 w-24" />
        <div className="flex gap-2">
          <Skeleton className="h-6 w-28 rounded-full" />
          <Skeleton className="h-6 w-24 rounded-full" />
        </div>
        <Skeleton className="h-4 w-24" />
        <div className="flex gap-2">
          <Skeleton className="h-6 w-32 rounded-full" />
        </div>
      </CardContent>
    </Card>
  );
}
