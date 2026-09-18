"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { motion } from "framer-motion";
import {
  CheckCircle2, Circle, Lock, FileText, GraduationCap, Send, Users, Wallet, MessageSquare, BookOpen,
  Video, Newspaper, ClipboardCheck, BookMarked, ExternalLink,
} from "lucide-react";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { celebratoryPop } from "@/lib/motion";
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

export function RoadmapTaskCard({
  task,
  locked,
  blockingTitles,
  onToggleDone,
  isUpdating,
  onOpenDetail,
}: {
  task: RoadmapTaskDto;
  locked: boolean;
  blockingTitles: string[];
  onToggleDone: () => void;
  isUpdating: boolean;
  onOpenDetail: () => void;
}) {
  const t = useTranslations("roadmap");
  const Icon = CATEGORY_ICON[task.category];
  const isDone = task.status === "Done";

  const [now] = useState(() => Date.now());
  const daysUntilDue = task.dueDate ? Math.ceil((new Date(task.dueDate).getTime() - now) / (1000 * 60 * 60 * 24)) : null;

  return (
    <motion.div layout variants={isDone ? celebratoryPop : undefined} animate={isDone ? "visible" : undefined}>
      <Card
        role="button"
        tabIndex={0}
        onClick={onOpenDetail}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            onOpenDetail();
          }
        }}
        className={`cursor-pointer transition-colors duration-150 hover:bg-[var(--surface-raised)] ${locked ? "opacity-60" : ""}`}
      >
        <div className="flex flex-col gap-3 p-4 sm:flex-row sm:items-start">
          <div className="flex items-start gap-3">
            <span
              className={`mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full ${
                isDone
                  ? "bg-[var(--success-100)] text-[var(--success-500)]"
                  : locked
                    ? "bg-[var(--neutral-100)] text-[var(--neutral-400)] dark:bg-[var(--neutral-800)]"
                    : "bg-[var(--brand-100)] text-[var(--brand-700)] dark:bg-[var(--brand-900)] dark:text-[var(--brand-200)]"
              }`}
            >
              {locked ? <Lock className="size-4" /> : <Icon className="size-4" />}
            </span>

            <div className="min-w-0 flex-1">
              <p className={`text-sm font-semibold ${isDone ? "text-[var(--neutral-400)] line-through" : ""}`}>{task.title}</p>
              <p className="mt-0.5 text-xs text-[var(--neutral-500)]">{task.description}</p>

              <div className="mt-2 flex flex-wrap items-center gap-2">
                {task.category === "SubjectPrep" && task.subject && <Badge variant="brand">{task.subject}</Badge>}
                {task.dueDate && daysUntilDue !== null && (
                  <Badge variant={daysUntilDue < 0 ? "danger" : daysUntilDue <= 14 ? "warning" : "neutral"}>
                    {daysUntilDue < 0 ? t("overdue") : t("dueIn", { days: daysUntilDue })}
                  </Badge>
                )}
                {locked && blockingTitles.length > 0 && (
                  <span className="text-[11px] text-[var(--neutral-400)]">{t("locked")}: {blockingTitles.join(", ")}</span>
                )}
              </div>

              {task.category === "SubjectPrep" && task.resources.length > 0 && (
                <ul className="mt-2.5 flex flex-col gap-1.5">
                  {task.resources.map((resource) => {
                    const ResourceIcon = RESOURCE_ICON[resource.resourceType];
                    return (
                      <li key={resource.url}>
                        <a
                          href={resource.url}
                          target="_blank"
                          rel="noopener noreferrer"
                          onClick={(e) => e.stopPropagation()}
                          className="inline-flex items-center gap-1.5 text-xs text-[var(--brand-700)] hover:underline dark:text-[var(--brand-300)]"
                        >
                          <ResourceIcon className="size-3.5 shrink-0" />
                          <span className="truncate">{resource.title}</span>
                          <ExternalLink className="size-3 shrink-0 opacity-60" />
                        </a>
                      </li>
                    );
                  })}
                </ul>
              )}
            </div>
          </div>

          <Button
            variant={isDone ? "secondary" : "primary"}
            size="sm"
            disabled={locked || isUpdating}
            onClick={(e) => {
              e.stopPropagation();
              onToggleDone();
            }}
            className="w-full shrink-0 sm:w-auto"
          >
            {isDone ? <CheckCircle2 className="size-4" /> : <Circle className="size-4" />}
            {isDone ? t("reopen") : t("markDone")}
          </Button>
        </div>
      </Card>
    </motion.div>
  );
}
