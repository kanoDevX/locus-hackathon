"use client";

import { Command } from "cmdk";
import { useTranslations } from "next-intl";
import { useEffect } from "react";
import { useTheme } from "next-themes";
import {
  User, Stethoscope, Sparkles, Columns3, Map, Target, Star, CalendarDays, Award, Search, PenLine, Moon, Sun,
  ClipboardCheck, MessageCircle, LayoutDashboard, Activity,
} from "lucide-react";
import { useRouter } from "@/i18n/navigation";

const ITEMS = [
  { key: "home", href: "/journey/home", icon: LayoutDashboard },
  { key: "profile", href: "/journey/profile", icon: User },
  { key: "eligibilityResult", href: "/journey/eligibility-result", icon: ClipboardCheck },
  { key: "diagnostics", href: "/journey/diagnostics", icon: Stethoscope },
  { key: "recommendations", href: "/journey/recommendations", icon: Sparkles },
  { key: "comparison", href: "/journey/comparison", icon: Columns3 },
  { key: "roadmap", href: "/journey/roadmap", icon: Map },
  { key: "nextAction", href: "/journey/next-action", icon: Target },
  { key: "favorites", href: "/journey/favorites", icon: Star },
  { key: "calendar", href: "/journey/calendar", icon: CalendarDays },
  { key: "scholarships", href: "/journey/scholarships", icon: Award },
  { key: "programs", href: "/journey/programs", icon: Search },
  { key: "essays", href: "/journey/essays", icon: PenLine },
  { key: "chat", href: "/journey/chat", icon: MessageCircle },
  { key: "insights", href: "/journey/insights", icon: Activity },
] as const;

export function CommandPalette({ open, onOpenChange }: { open: boolean; onOpenChange: (open: boolean) => void }) {
  const t = useTranslations("nav");
  const tPalette = useTranslations("commandPalette");
  const router = useRouter();
  const { resolvedTheme, setTheme } = useTheme();

  useEffect(() => {
    function handler(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        onOpenChange(!open);
      }
      if (e.key === "Escape") onOpenChange(false);
    }
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [open, onOpenChange]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center bg-black/40 backdrop-blur-sm pt-[15vh]" onClick={() => onOpenChange(false)}>
      <Command
        onClick={(e) => e.stopPropagation()}
        className="w-full max-w-lg overflow-hidden rounded-[var(--radius-lg)] border border-[var(--border-subtle)] bg-[var(--surface)] shadow-[var(--shadow-overlay)]"
      >
        <Command.Input
          autoFocus
          placeholder={tPalette("placeholder")}
          className="w-full border-b border-[var(--border-subtle)] bg-transparent px-4 py-3.5 text-sm outline-none placeholder:text-[var(--neutral-400)]"
        />
        <Command.List className="max-h-80 overflow-y-auto p-2">
          <Command.Empty className="px-3 py-6 text-center text-sm text-[var(--neutral-400)]">
            {tPalette("noResults")}
          </Command.Empty>
          <Command.Group>
            {ITEMS.map((item) => (
              <Command.Item
                key={item.key}
                onSelect={() => {
                  router.push(item.href);
                  onOpenChange(false);
                }}
                className="flex cursor-pointer items-center gap-2.5 rounded-[var(--radius-sm)] px-3 py-2.5 text-sm text-[var(--foreground)] data-[selected=true]:bg-[var(--brand-50)] dark:data-[selected=true]:bg-[var(--brand-900)]"
              >
                <item.icon className="size-4 text-[var(--neutral-400)]" />
                {t(item.key)}
              </Command.Item>
            ))}
            <Command.Item
              onSelect={() => {
                setTheme(resolvedTheme === "dark" ? "light" : "dark");
                onOpenChange(false);
              }}
              className="flex cursor-pointer items-center gap-2.5 rounded-[var(--radius-sm)] px-3 py-2.5 text-sm text-[var(--foreground)] data-[selected=true]:bg-[var(--brand-50)] dark:data-[selected=true]:bg-[var(--brand-900)]"
            >
              {resolvedTheme === "dark" ? <Sun className="size-4 text-[var(--neutral-400)]" /> : <Moon className="size-4 text-[var(--neutral-400)]" />}
              Toggle theme
            </Command.Item>
          </Command.Group>
        </Command.List>
      </Command>
    </div>
  );
}
