"use client";

import { forwardRef, useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import { cn } from "@/lib/utils";

export interface PasswordFieldProps extends React.InputHTMLAttributes<HTMLInputElement> {
  showLabel?: string;
  hideLabel?: string;
}

/** A password <input> with a show/hide toggle — the universal modern-standard affordance so a
 * user can verify what they typed instead of blindly trusting masked dots (§ research: "Modern
 * Authentication" / form UX best practices). Toggle is type="button" so it never submits the
 * surrounding form, and the input keeps the correct `autocomplete` value passed in by the
 * caller (new-password vs current-password) rather than a blanket "off" that browsers ignore
 * anyway. */
export const PasswordField = forwardRef<HTMLInputElement, PasswordFieldProps>(
  ({ className, showLabel = "Show password", hideLabel = "Hide password", ...props }, ref) => {
    const [visible, setVisible] = useState(false);

    return (
      <div className="relative">
        <input
          ref={ref}
          type={visible ? "text" : "password"}
          className={cn(
            "flex h-11 w-full rounded-[var(--radius-md)] border border-[var(--border-strong)] bg-[var(--surface)] px-3.5 pr-11 text-sm text-[var(--foreground)] placeholder:text-[var(--neutral-400)] transition-colors duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring)] disabled:opacity-50",
            className
          )}
          {...props}
        />
        <button
          type="button"
          onClick={() => setVisible((v) => !v)}
          aria-label={visible ? hideLabel : showLabel}
          aria-pressed={visible}
          tabIndex={-1}
          className="absolute right-0 top-0 flex h-11 w-11 items-center justify-center text-[var(--neutral-400)] hover:text-[var(--foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ring)] rounded-[var(--radius-md)]"
        >
          {visible ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
        </button>
      </div>
    );
  }
);
PasswordField.displayName = "PasswordField";
