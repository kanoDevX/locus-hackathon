"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { useRouter, Link } from "@/i18n/navigation";
import { toast } from "sonner";
import * as DropdownMenu from "@radix-ui/react-dropdown-menu";
import { ChevronDown, LogOut, RotateCcw, UserCog } from "lucide-react";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useLogout } from "@/lib/api/auth";
import { useDemoReset } from "@/lib/api/system";
import { LocaleSwitcher } from "./locale-switcher";
import { ThemeToggle } from "./theme-toggle";
import { Button } from "@/components/ui/button";
import { AccountSettingsDialog } from "@/components/journey/account-settings-dialog";

export function AppHeader() {
  const t = useTranslations("nav");
  const tCommon = useTranslations("common");
  const tReset = useTranslations("demoReset");
  const router = useRouter();
  const { displayName, role, clear } = useAuthStore();
  const logout = useLogout();
  const demoReset = useDemoReset();
  const [accountOpen, setAccountOpen] = useState(false);

  async function handleSignOut() {
    await logout.mutateAsync().catch(() => clear());
    router.push("/");
  }

  async function handleDemoReset() {
    try {
      await demoReset.mutateAsync();
      toast.success(tReset("confirm"));
      router.push("/journey/home");
    } catch {
      toast.error(tCommon("errorTitle"));
    }
  }

  return (
    <header className="sticky top-0 z-30 border-b border-[var(--border-subtle)] bg-[var(--background)]/90 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-3 px-4 py-3 sm:px-6">
        <Link href="/journey/home" className="text-sm font-semibold tracking-tight">
          {tCommon("appName")}
        </Link>

        <div className="flex items-center gap-1.5">
          <LocaleSwitcher />
          <ThemeToggle />

          <DropdownMenu.Root>
            <DropdownMenu.Trigger asChild>
              <Button variant="secondary" size="sm">
                {displayName ?? "…"}
                <ChevronDown className="size-3.5" />
              </Button>
            </DropdownMenu.Trigger>
            <DropdownMenu.Portal>
              <DropdownMenu.Content
                align="end"
                sideOffset={8}
                className="z-50 min-w-56 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface)] p-1 shadow-[var(--shadow-overlay)]"
              >
                <DropdownMenu.Item
                  onSelect={() => setAccountOpen(true)}
                  className="flex cursor-pointer items-center gap-2 rounded-[var(--radius-sm)] px-2.5 py-2 text-sm outline-none data-[highlighted]:bg-[var(--brand-50)] dark:data-[highlighted]:bg-[var(--brand-900)]"
                >
                  <UserCog className="size-4" />
                  {t("accountSettings")}
                </DropdownMenu.Item>
                {role === "Judge" && (
                  <DropdownMenu.Item
                    onSelect={handleDemoReset}
                    className="flex cursor-pointer items-center gap-2 rounded-[var(--radius-sm)] px-2.5 py-2 text-sm text-[var(--warning-500)] outline-none data-[highlighted]:bg-[var(--warning-100)]"
                  >
                    <RotateCcw className="size-4" />
                    {tReset("label")}
                  </DropdownMenu.Item>
                )}
                <DropdownMenu.Item
                  onSelect={handleSignOut}
                  className="flex cursor-pointer items-center gap-2 rounded-[var(--radius-sm)] px-2.5 py-2 text-sm outline-none data-[highlighted]:bg-[var(--brand-50)] dark:data-[highlighted]:bg-[var(--brand-900)]"
                >
                  <LogOut className="size-4" />
                  {t("signOut")}
                </DropdownMenu.Item>
              </DropdownMenu.Content>
            </DropdownMenu.Portal>
          </DropdownMenu.Root>
        </div>
      </div>

      <AccountSettingsDialog open={accountOpen} onOpenChange={setAccountOpen} />
    </header>
  );
}
