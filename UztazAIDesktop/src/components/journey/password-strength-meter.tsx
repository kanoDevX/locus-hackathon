"use client";

import { useTranslations } from "next-intl";
import { passwordStrength } from "@/lib/validation/auth-schema";

const COLORS = [
  "bg-[var(--danger-500)]",
  "bg-[var(--warning-500)]",
  "bg-[var(--warning-500)]",
  "bg-[var(--success-500)]",
  "bg-[var(--success-500)]",
];

export function PasswordStrengthMeter({ password }: { password: string }) {
  const t = useTranslations("auth");
  if (!password) return null;

  const { score, label } = passwordStrength(password);

  return (
    <div className="mt-1.5">
      <div className="flex gap-1">
        {Array.from({ length: 4 }).map((_, i) => (
          <div
            key={i}
            className={`h-1 flex-1 rounded-full transition-colors duration-150 ${
              i < score ? COLORS[score] : "bg-[var(--neutral-200)] dark:bg-[var(--neutral-800)]"
            }`}
          />
        ))}
      </div>
      <p className="mt-1 text-[11px] text-[var(--neutral-400)]">{t(`passwordStrength.${label}` as never)}</p>
    </div>
  );
}
