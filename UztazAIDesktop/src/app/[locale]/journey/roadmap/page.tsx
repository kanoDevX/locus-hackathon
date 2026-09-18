"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { toast } from "sonner";
import { BookOpen } from "lucide-react";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useGenerateRoadmap, useRoadmap, useUpdateTaskProgress } from "@/lib/api/roadmap";
import { useLatestRecommendations } from "@/lib/api/recommendations";
import { useGeneratePrepPlan } from "@/lib/api/prepPlan";
import { ApiError } from "@/lib/api/client";
import { RoadmapTaskCard } from "@/components/journey/roadmap-task-card";
import { RoadmapTaskDetailDialog } from "@/components/journey/roadmap-task-detail-dialog";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

export default function RoadmapPage() {
  const t = useTranslations("roadmap");
  const profileId = useAuthStore((s) => s.activeProfileId);
  const { data: tasks, isLoading } = useRoadmap(profileId);
  const { data: recommendations } = useLatestRecommendations(profileId);
  const generateRoadmap = useGenerateRoadmap(profileId);
  const updateProgress = useUpdateTaskProgress(profileId);
  const generatePrepPlan = useGeneratePrepPlan(profileId);
  const [selectedTaskId, setSelectedTaskId] = useState<number | null>(null);

  // Every roadmap task carries its ProgramId — the prep-plan trigger targets whichever program
  // the current roadmap was actually built for (§13), not a separately re-picked one.
  const roadmapProgramId = tasks?.find((t) => t.programId != null)?.programId ?? null;

  async function buildFor(programId: number) {
    try {
      await generateRoadmap.mutateAsync(programId);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "Could not build roadmap");
    }
  }

  async function buildPrepPlan() {
    if (!roadmapProgramId) return;
    try {
      const created = await generatePrepPlan.mutateAsync(roadmapProgramId);
      // The endpoint returns 200 with an empty array (not an error) when there's nothing to
      // build — e.g. a document-only admission track with no exam scores, or every subject
      // already clearing its threshold. Silence there reads as "broken", so it needs its own
      // message rather than falling through to the generic success/nothing-happened case.
      if (created.length === 0) toast.info(t("prepPlanEmpty"));
      else toast.success(t("prepPlanSuccess", { count: created.length }));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("prepPlanFailed"));
    }
  }

  async function toggleTask(taskId: number, currentlyDone: boolean) {
    try {
      await updateProgress.mutateAsync({ taskId, status: currentlyDone ? "NotStarted" : "Done" });
      if (!currentlyDone) toast.success(t("celebration"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "Could not update task");
    }
  }

  const doneIds = new Set((tasks ?? []).filter((t) => t.status === "Done").map((t) => t.taskId));
  const taskById = new Map((tasks ?? []).map((t) => [t.taskId, t]));
  const sorted = [...(tasks ?? [])].sort((a, b) => {
    if (!a.dueDate) return 1;
    if (!b.dueDate) return -1;
    return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime();
  });

  const selectedTask = selectedTaskId !== null ? (taskById.get(selectedTaskId) ?? null) : null;
  const selectedTaskLocked = selectedTask ? selectedTask.prerequisiteTaskIds.some((id) => !doneIds.has(id)) : false;
  const selectedTaskBlockers = selectedTask
    ? selectedTask.prerequisiteTaskIds.map((id) => taskById.get(id)).filter((t): t is NonNullable<typeof t> => !!t)
    : [];

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
      <p className="mt-1 text-sm text-[var(--neutral-500)]">{t("subtitle")}</p>

      {isLoading && (
        <div className="mt-6 flex flex-col gap-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-20 w-full" />
          ))}
        </div>
      )}

      {!isLoading && (!tasks || tasks.length === 0) && (
        <Card className="mt-6">
          <CardHeader>
            <CardTitle className="text-sm">{t("pickProgram")}</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-2">
            {recommendations?.map((rec) => (
              <div key={rec.recommendationId} className="flex items-center justify-between rounded-[var(--radius-md)] border border-[var(--border-subtle)] p-3">
                <div>
                  <p className="text-sm font-medium">{rec.program.programName}</p>
                  <p className="text-xs text-[var(--neutral-500)]">{rec.program.universityName}</p>
                </div>
                <Button size="sm" onClick={() => buildFor(rec.program.programId)} disabled={generateRoadmap.isPending}>
                  {t("generate")}
                </Button>
              </div>
            ))}
          </CardContent>
        </Card>
      )}

      {!isLoading && tasks && tasks.length > 0 && (
        <div className="mt-4 flex flex-wrap items-center justify-between gap-2 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface)] p-3">
          <p className="text-xs text-[var(--neutral-500)]">{t("buildPrepPlanHint")}</p>
          <Button variant="secondary" size="sm" onClick={buildPrepPlan} disabled={!roadmapProgramId || generatePrepPlan.isPending}>
            <BookOpen className="size-3.5" />
            {t("buildPrepPlan")}
          </Button>
        </div>
      )}

      {!isLoading && tasks && tasks.length > 0 && (
        <div className="mt-3 flex flex-col gap-3">
          {sorted.map((task) => {
            const locked = task.prerequisiteTaskIds.some((id) => !doneIds.has(id));
            const blockingTitles = task.prerequisiteTaskIds.filter((id) => !doneIds.has(id)).map((id) => taskById.get(id)?.title ?? "");
            return (
              <RoadmapTaskCard
                key={task.taskId}
                task={task}
                locked={locked}
                blockingTitles={blockingTitles}
                isUpdating={updateProgress.isPending}
                onToggleDone={() => toggleTask(task.taskId, task.status === "Done")}
                onOpenDetail={() => setSelectedTaskId(task.taskId)}
              />
            );
          })}
        </div>
      )}

      <RoadmapTaskDetailDialog
        task={selectedTask}
        open={selectedTaskId !== null}
        onOpenChange={(open) => !open && setSelectedTaskId(null)}
        locked={selectedTaskLocked}
        blockingTasks={selectedTaskBlockers}
        isUpdating={updateProgress.isPending}
        onToggleDone={() => selectedTask && toggleTask(selectedTask.taskId, selectedTask.status === "Done")}
      />
    </div>
  );
}
