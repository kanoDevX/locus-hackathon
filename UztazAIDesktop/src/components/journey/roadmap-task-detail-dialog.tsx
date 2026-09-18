"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import {
  CheckCircle2, Circle, Lock, FileText, GraduationCap, Send, Users, Wallet, MessageSquare, BookOpen,
  Video, Newspaper, ClipboardCheck, BookMarked, ExternalLink, Gauge, Zap, Link2, Sparkles,
} from "lucide-react";
import { useRouter } from "@/i18n/navigation";
import { Dialog, DialogContent, DialogTitle, DialogDescription } from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DemoDataBadge } from "@/components/journey/demo-data-badge";
import type { RoadmapTaskDto, RoadmapTaskCategory, ResourceType } from "@/lib/api/types";

const CATEGORY_ICON: Record<RoadmapTaskCategory, React.ElementType> = {
  Exam: GraduationCap,
  Document: FileText,
  Application: Send,
  Activity: Users,
  Financial: Wallet,
  Interview: MessageSquare,
  SubjectPrep: BookOpen,
};

const RESOURCE_ICON: Record<ResourceType, React.ElementType> = {
  Video: Video,
  Article: Newspaper,
  PracticeTest: ClipboardCheck,
  Course: BookMarked,
};

/**
 * Full-detail view for one roadmap task (§ UX request: "detailed roadmap you can click into, with
 * full info" — the pattern every task-tracking app from Linear to Trello uses: a compact card in
 * the list, a complete record behind a click). Reuses the same category icon map and status
 * language as RoadmapTaskCard rather than re-deriving any of it.
 */
export function RoadmapTaskDetailDialog({
  task,
  open,
  onOpenChange,
  locked,
  blockingTasks,
  onToggleDone,
  isUpdating,
}: {
  task: RoadmapTaskDto | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  locked: boolean;
  blockingTasks: RoadmapTaskDto[];
  onToggleDone: () => void;
  isUpdating: boolean;
}) {
  const t = useTranslations("roadmap");
  const router = useRouter();
  // useState's lazy initializer is the React-sanctioned place to read an impure value like
  // Date.now() exactly once, keeping the render body itself pure across re-renders (same
  // pattern as RoadmapTaskCard).
  const [now] = useState(() => Date.now());

  if (!task) return null;

  const Icon = CATEGORY_ICON[task.category];
  const isDone = task.status === "Done";
  const daysUntilDue = task.dueDate ? Math.ceil((new Date(task.dueDate).getTime() - now) / (1000 * 60 * 60 * 24)) : null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <div className="flex items-start gap-3">
          <span
            className={`mt-0.5 flex size-10 shrink-0 items-center justify-center rounded-full ${
              isDone
                ? "bg-[var(--success-100)] text-[var(--success-500)]"
                : locked
                  ? "bg-[var(--neutral-100)] text-[var(--neutral-400)] dark:bg-[var(--neutral-800)]"
                  : "bg-[var(--brand-100)] text-[var(--brand-700)] dark:bg-[var(--brand-900)] dark:text-[var(--brand-200)]"
            }`}
          >
            {locked ? <Lock className="size-5" /> : <Icon className="size-5" />}
          </span>
          <div className="min-w-0 flex-1">
            <DialogTitle>{task.title}</DialogTitle>
            <DialogDescription>{task.description}</DialogDescription>
          </div>
        </div>

        <div className="mt-4 flex flex-wrap items-center gap-2">
          <Badge variant={isDone ? "success" : "neutral"}>{t(`status.${task.status}`)}</Badge>
          {task.category === "SubjectPrep" && task.subject && <Badge variant="brand">{task.subject}</Badge>}
          {task.dueDate && daysUntilDue !== null && (
            <Badge variant={daysUntilDue < 0 ? "danger" : daysUntilDue <= 14 ? "warning" : "neutral"}>
              {daysUntilDue < 0 ? t("overdue") : t("dueIn", { days: daysUntilDue })}
            </Badge>
          )}
        </div>

        
          <Button
            className="mt-4 w-full"
            onClick={() => {
              onOpenChange(false);
              router.push(`/journey/roadmap/study/${task.taskId}`);
            }}
          >
            <Sparkles className="size-4" />
            {t("learnThis")}
          </Button>
        

        <div className="mt-4 grid grid-cols-2 gap-3">
          <ScoreMeter icon={Zap} label={t("urgencyLabel")} value={task.urgencyScore} />
          <ScoreMeter icon={Gauge} label={t("impactLabel")} value={task.impactScore} />
        </div>

        {task.dueDate && (
          <p className="mt-3 text-xs text-[var(--neutral-500)]">
            {t("dueDateLabel")}: <span className="font-medium text-[var(--foreground)]">{new Date(task.dueDate).toLocaleDateString()}</span>
          </p>
        )}

        {blockingTasks.length > 0 && (
          <div className="mt-4">
            <p className="mb-1.5 flex items-center gap-1.5 text-xs font-semibold text-[var(--neutral-400)]">
              <Link2 className="size-3.5" />
              {t("prerequisitesLabel")}
            </p>
            <ul className="flex flex-col gap-1">
              {blockingTasks.map((p) => (
                <li key={p.taskId} className="flex items-center gap-1.5 text-xs">
                  {p.status === "Done" ? (
                    <CheckCircle2 className="size-3.5 text-[var(--success-500)]" />
                  ) : (
                    <Circle className="size-3.5 text-[var(--neutral-400)]" />
                  )}
                  <span className={p.status === "Done" ? "text-[var(--neutral-400)] line-through" : ""}>{p.title}</span>
                </li>
              ))}
            </ul>
          </div>
        )}

        {task.category === "SubjectPrep" && task.resources.length > 0 && (
          <div className="mt-4">
            <p className="mb-1.5 text-xs font-semibold text-[var(--neutral-400)]">{t("resourcesLabel")}</p>
            <ul className="flex flex-col gap-2">
              {task.resources.map((resource) => {
                const ResourceIcon = RESOURCE_ICON[resource.resourceType];
                return (
                  <li key={resource.url} className="flex items-center justify-between gap-2 rounded-[var(--radius-md)] border border-[var(--border-subtle)] p-2.5">
                    <a
                      href={resource.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex min-w-0 items-center gap-2 text-sm text-[var(--brand-700)] hover:underline dark:text-[var(--brand-300)]"
                    >
                      <ResourceIcon className="size-4 shrink-0" />
                      <span className="truncate">{resource.title}</span>
                      <ExternalLink className="size-3 shrink-0 opacity-60" />
                    </a>
                    <DemoDataBadge provenance={resource.provenance} />
                  </li>
                );
              })}
            </ul>
          </div>
        )}

        <Button variant={isDone ? "secondary" : "primary"} disabled={locked || isUpdating} onClick={onToggleDone} className="mt-5 w-full">
          {isDone ? <CheckCircle2 className="size-4" /> : <Circle className="size-4" />}
          {isDone ? t("reopen") : t("markDone")}
        </Button>
      </DialogContent>
    </Dialog>
  );
}

function ScoreMeter({ icon: Icon, label, value }: { icon: React.ElementType; label: string; value: number }) {
  return (
    <div className="flex flex-col gap-1 rounded-[var(--radius-md)] border border-[var(--border-subtle)] p-2.5">
      <span className="flex items-center gap-1.5 text-[11px] font-medium text-[var(--neutral-400)]">
        <Icon className="size-3.5" />
        {label}
      </span>
      <div className="h-1.5 w-full rounded-full bg-[var(--neutral-200)] dark:bg-[var(--neutral-800)]">
        <div className="h-full rounded-full bg-[var(--brand-500)]" style={{ width: `${value}%` }} />
      </div>
      <span className="text-xs font-semibold tabular-nums">{value}/100</span>
    </div>
  );
}
