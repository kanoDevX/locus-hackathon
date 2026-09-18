"use client";

import { motion } from "framer-motion";
import { CheckCircle2, CircleDashed, LoaderCircle, XCircle } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { fadeIn, fadeInUp } from "@/lib/motion";
import { cn } from "@/lib/utils";

export type IntakeStageStatus = "pending" | "active" | "done" | "error";

export interface IntakeStage {
  key: string;
  label: string;
  status: IntakeStageStatus;
}

/**
 * Staged progress screen for the intake → eligibility flow (§12 Addendum). Each stage's status is
 * driven directly by a real TanStack Query mutation's own `status` — never a fixed-duration timer
 * standing in for real work, so what the student sees always matches what the backend is actually
 * doing (submit exam intake, then calculate eligibility against the seeded catalog).
 */
export function IntakeProgressScreen({ stages, title, subtitle }: { stages: IntakeStage[]; title: string; subtitle: string }) {
  return (
    <motion.div variants={fadeIn} initial="hidden" animate="visible" className="mx-auto max-w-md">
      <Card>
        <CardContent className="flex flex-col items-center gap-6 p-8 text-center">
          <div>
            <h1 className="text-lg font-semibold tracking-tight">{title}</h1>
            <p className="mt-1 text-sm text-[var(--neutral-500)]">{subtitle}</p>
          </div>

          <ul className="flex w-full flex-col gap-3">
            {stages.map((stage, i) => (
              <motion.li
                key={stage.key}
                custom={i}
                variants={fadeInUp}
                initial="hidden"
                animate="visible"
                className={cn(
                  "flex items-center gap-3 rounded-[var(--radius-md)] border p-3 text-left text-sm transition-colors duration-150",
                  stage.status === "done" && "border-[var(--success-100)] bg-[var(--success-100)]/40 text-[var(--foreground)]",
                  stage.status === "active" && "border-[var(--brand-200)] bg-[var(--brand-50)] text-[var(--foreground)] dark:border-[var(--brand-800)] dark:bg-[var(--brand-900)]",
                  stage.status === "pending" && "border-[var(--border-subtle)] text-[var(--neutral-400)]",
                  stage.status === "error" && "border-[var(--danger-100)] bg-[var(--danger-100)]/40 text-[var(--foreground)]"
                )}
              >
                {stage.status === "done" && <CheckCircle2 className="size-4.5 shrink-0 text-[var(--success-500)]" />}
                {stage.status === "active" && <LoaderCircle className="size-4.5 shrink-0 animate-spin text-[var(--brand-600)] dark:text-[var(--brand-300)]" />}
                {stage.status === "pending" && <CircleDashed className="size-4.5 shrink-0" />}
                {stage.status === "error" && <XCircle className="size-4.5 shrink-0 text-[var(--danger-500)]" />}
                <span className={stage.status === "done" ? "font-medium" : undefined}>{stage.label}</span>
              </motion.li>
            ))}
          </ul>
        </CardContent>
      </Card>
    </motion.div>
  );
}
