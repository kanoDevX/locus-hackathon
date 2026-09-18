"use client";

import { useTranslations } from "next-intl";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useJourneyStore } from "@/lib/stores/journey-store";
import { useLogout } from "@/lib/api/auth";
import { useRouter } from "@/i18n/navigation";
import { ThemeToggle } from "./theme-toggle";
import { LocaleSwitcher } from "./locale-switcher";
import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import { LogOut } from "lucide-react";

export function OnboardingHeader() {
  const tCommon = useTranslations("common");
  const tNav = useTranslations("nav");
  const tIntake = useTranslations("intake");
  const router = useRouter();
  const { clear } = useAuthStore();
  const logout = useLogout();
  const wizardProgress = useJourneyStore((s) => s.wizardProgress);

  async function handleSignOut() {
    await logout.mutateAsync().catch(() => clear());
    router.push("/");
  }

  const percent = wizardProgress ? ((wizardProgress.step + 1) / wizardProgress.totalSteps) * 100 : 0;

  return (
    <header className="sticky top-0 z-30 border-b border-[var(--border-subtle)] bg-[var(--background)]/90 backdrop-blur">
      <div className="mx-auto flex max-w-2xl items-center justify-between gap-4 px-4 py-3 sm:px-6">
        <span className="text-sm font-semibold tracking-tight">{tCommon("appName")}</span>

        {wizardProgress && (
          <div className="flex min-w-0 flex-1 flex-col items-center gap-1.5">
            <p className="text-xs font-medium text-[var(--neutral-400)]">
              {tIntake("stepOf", { current: wizardProgress.step + 1, total: wizardProgress.totalSteps })}
            </p>
            <Progress value={percent} className="w-full max-w-xs" />
          </div>
        )}

        <div className="flex items-center gap-1.5">
          <LocaleSwitcher />
          <ThemeToggle />
          <Button variant="ghost" size="sm" onClick={handleSignOut} aria-label={tNav("signOut")}>
            <LogOut className="size-4" />
          </Button>
        </div>
      </div>
    </header>
  );
}
