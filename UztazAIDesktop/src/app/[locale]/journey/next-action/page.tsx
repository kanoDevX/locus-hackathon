"use client";

import { useTranslations } from "next-intl";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { Target, ArrowRight } from "lucide-react";
import { useRouter } from "@/i18n/navigation";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useNextAction } from "@/lib/api/nextAction";
import { useUpdateTaskProgress } from "@/lib/api/roadmap";
import { ApiError } from "@/lib/api/client";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { celebratoryPop } from "@/lib/motion";

export default function NextActionPage() {
  const t = useTranslations("nextAction");
  const router = useRouter();
  const profileId = useAuthStore((s) => s.activeProfileId);
  const { data: task, isLoading } = useNextAction(profileId);
  const updateProgress = useUpdateTaskProgress(profileId);

  async function markDone() {
    if (!task) return;
    try {
      await updateProgress.mutateAsync({ taskId: task.taskId, status: "Done" });
      toast.success("🎉");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "Could not update task");
    }
  }

  return (
    <div className="mx-auto flex max-w-lg flex-col items-center py-10 text-center">
      <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
      <p className="mt-1 text-sm text-[var(--neutral-500)]">{t("subtitle")}</p>

      <div className="mt-8 w-full">
        {isLoading && <Skeleton className="h-48 w-full" />}

        {!isLoading && !task && (
          <Card>
            <CardContent className="flex flex-col items-center gap-3 p-10">
              <p className="text-sm text-[var(--neutral-500)]">{t("empty")}</p>
              <Button variant="secondary" size="sm" onClick={() => router.push("/journey/roadmap")}>
                {t("goToRoadmap")} <ArrowRight className="size-3.5" />
              </Button>
            </CardContent>
          </Card>
        )}

        {task && (
          <motion.div variants={celebratoryPop} initial="hidden" animate="visible">
            <Card className="border-2 border-[var(--brand-300)]">
              <CardContent className="flex flex-col items-center gap-4 p-8">
                <span className="flex size-14 items-center justify-center rounded-full bg-[var(--brand-100)] text-[var(--brand-700)] dark:bg-[var(--brand-900)] dark:text-[var(--brand-200)]">
                  <Target className="size-6" />
                </span>
                <div>
                  <p className="text-lg font-semibold">{task.title}</p>
                  <p className="mt-1 text-sm text-[var(--neutral-500)]">{task.description}</p>
                </div>
                <Button onClick={markDone} disabled={updateProgress.isPending} size="lg">
                  Done
                </Button>
              </CardContent>
            </Card>
          </motion.div>
        )}
      </div>
    </div>
  );
}
