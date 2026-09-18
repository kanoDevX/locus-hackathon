"use client";

import { use, useEffect } from "react";
import { useTranslations } from "next-intl";
import { motion } from "framer-motion";
import { ArrowLeft, BookOpen, Clock, ExternalLink, RotateCcw, Sparkles, AlertTriangle, SquarePlay } from "lucide-react";
import { useRouter } from "@/i18n/navigation";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useRoadmap } from "@/lib/api/roadmap";
import { useStudyGuide, useGenerateStudyGuide } from "@/lib/api/studyGuide";
import { ApiError } from "@/lib/api/client";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { DemoDataBadge } from "@/components/journey/demo-data-badge";
import { fadeInUp, fadeIn } from "@/lib/motion";

/**
 * Full-screen "how do I actually learn this" study guide (§ UX request) — a dedicated route
 * (not a dialog) reached from a SubjectPrep roadmap task's "Learn this" button. Generated once
 * and persisted server-side, so navigating back here later re-reads the same plan instead of
 * generating a new one on every visit. Each step links to a real YouTube *search*, never a
 * specific invented video (see StudyGuideStepDto's own doc comment for why).
 */
export default function StudyGuidePage({ params }: { params: Promise<{ taskId: string }> }) {
  const { taskId: taskIdParam } = use(params);
  const taskId = Number(taskIdParam);
  const t = useTranslations("studyGuide");
  const router = useRouter();
  const profileId = useAuthStore((s) => s.activeProfileId);

  const { data: tasks } = useRoadmap(profileId);
  const task = tasks?.find((x) => x.taskId === taskId) ?? null;

  const { data: guide, isLoading: guideLoading, isFetched: guideFetched } = useStudyGuide(profileId, taskId);
  const generate = useGenerateStudyGuide(profileId, taskId);

  const resolvedGuide = generate.data ?? guide;

  useEffect(() => {
    if (guideFetched && !guide && !generate.isPending && !generate.data) {
      generate.mutate(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [guideFetched, guide]);

  async function handleRegenerate() {
    try {
      await generate.mutateAsync(true);
      toast.success(t("regenerated"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("generateFailed"));
    }
  }

  const isGenerating = generate.isPending || guideLoading || !guideFetched;
  const subjectSearchUrl = (task?.subject ?? task?.title)
    ? `https://www.youtube.com/results?search_query=${encodeURIComponent(task.subject ?? task.title)}`
    : null;

  return (
    <div className="mx-auto max-w-3xl">
      <Button variant="ghost" size="sm" onClick={() => router.push("/journey/roadmap")} className="mb-4">
        <ArrowLeft className="size-3.5" />
        {t("backToRoadmap")}
      </Button>

      <motion.div variants={fadeInUp} initial="hidden" animate="visible">
        <div className="flex items-start gap-3">
          <span className="flex size-11 shrink-0 items-center justify-center rounded-full bg-[var(--brand-100)] text-[var(--brand-700)] dark:bg-[var(--brand-900)] dark:text-[var(--brand-200)]">
            <BookOpen className="size-5" />
          </span>
          <div className="min-w-0 flex-1">
            <p className="text-xs font-medium uppercase tracking-wide text-[var(--neutral-400)]">{t("eyebrow")}</p>
            <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">{task?.subject ?? task?.title ?? t("titleFallback")}</h1>
            {task && <p className="mt-1 text-sm text-[var(--neutral-500)]">{task.description}</p>}
          </div>
        </div>

        {subjectSearchUrl && (
          <a
            href={subjectSearchUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="mt-4 inline-flex items-center gap-2 rounded-[var(--radius-md)] border border-[var(--border-strong)] bg-[var(--surface-raised)] px-3.5 py-2 text-sm font-medium hover:bg-[var(--brand-50)] dark:hover:bg-[var(--brand-900)]"
          >
            <SquarePlay className="size-4 text-[var(--danger-500)]" />
            {t("browseSubjectOnYouTube", { subject: task?.subject ?? task?.title ?? "" })}
            <ExternalLink className="size-3.5 opacity-60" />
          </a>
        )}
      </motion.div>

      <div className="mt-8">
        {isGenerating && <GeneratingState />}

        {!isGenerating && resolvedGuide && (
          <motion.div variants={fadeIn} initial="hidden" animate="visible" className="flex flex-col gap-5">
            <div className="flex flex-wrap items-center justify-between gap-3 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-raised)] p-3">
              <div className="flex items-center gap-2">
                <Badge variant={resolvedGuide.fallbackUsed ? "warning" : "brand"} className="gap-1.5">
                  <Sparkles className="size-3" />
                  {resolvedGuide.fallbackUsed ? t("fallbackBadge") : t("aiGeneratedBadge")}
                </Badge>
                <DemoDataBadge provenance={resolvedGuide.provenance} />
              </div>
              <Button variant="secondary" size="sm" onClick={handleRegenerate} disabled={generate.isPending}>
                <RotateCcw className="size-3.5" />
                {t("regenerate")}
              </Button>
            </div>

            {resolvedGuide.fallbackUsed && (
              <div className="flex items-start gap-2.5 rounded-[var(--radius-md)] border border-[var(--warning-100)] bg-[var(--warning-100)]/40 p-3 text-xs text-[var(--neutral-600)] dark:text-[var(--neutral-300)]">
                <AlertTriangle className="mt-0.5 size-3.5 shrink-0 text-[var(--warning-500)]" />
                {t("fallbackExplainer")}
              </div>
            )}

            <ol className="relative flex flex-col gap-4 border-l-2 border-[var(--border-subtle)] pl-6">
              {resolvedGuide.steps.map((step, i) => (
                <motion.li key={step.stepNumber} variants={fadeInUp} custom={i} initial="hidden" animate="visible" className="relative">
                  <span className="absolute -left-[calc(1.5rem+9px)] top-1 flex size-6 items-center justify-center rounded-full bg-[var(--primary)] text-xs font-semibold text-[var(--primary-foreground)]">
                    {step.stepNumber}
                  </span>
                  <Card>
                    <CardContent className="flex flex-col gap-2 p-4">
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <p className="text-sm font-semibold">{step.title}</p>
                        <Badge variant="neutral" className="gap-1">
                          <Clock className="size-3" />
                          {t("estimatedMinutes", { minutes: step.estimatedMinutes })}
                        </Badge>
                      </div>
                      <p className="text-sm text-[var(--neutral-500)]">{step.description}</p>
                      <a
                        href={step.youTubeSearchUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="mt-1 inline-flex w-fit items-center gap-1.5 text-xs text-[var(--brand-700)] hover:underline dark:text-[var(--brand-300)]"
                      >
                        <SquarePlay className="size-3.5 text-[var(--danger-500)]" />
                        {t("searchYouTubeFor", { query: step.videoSearchQuery })}
                        <ExternalLink className="size-3 opacity-60" />
                      </a>
                    </CardContent>
                  </Card>
                </motion.li>
              ))}
            </ol>
          </motion.div>
        )}
      </div>
    </div>
  );
}

function GeneratingState() {
  const t = useTranslations("studyGuide");
  return (
    <motion.div variants={fadeIn} initial="hidden" animate="visible" className="flex flex-col items-center gap-3 rounded-[var(--radius-lg)] border border-[var(--border-subtle)] bg-[var(--surface)] p-10 text-center">
      <Sparkles className="size-6 animate-pulse text-[var(--brand-500)]" />
      <p className="text-sm font-medium">{t("generatingTitle")}</p>
      <p className="text-xs text-[var(--neutral-500)]">{t("generatingSubtitle")}</p>
    </motion.div>
  );
}
