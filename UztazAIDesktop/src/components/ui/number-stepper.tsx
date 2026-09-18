"use client";

import * as React from "react";
import { Minus, Plus } from "lucide-react";
import { cn } from "@/lib/utils";

export const NumberStepper = React.forwardRef<
  HTMLInputElement,
  {
    value: number | undefined;
    onChange: (value: number | undefined) => void;
    onBlur?: () => void;
    min?: number;
    max?: number;
    step?: number;
    suffix?: string;
    placeholder?: string;
    className?: string;
    disabled?: boolean;
  }
>(({ value, onChange, onBlur, min = 0, max = 999, step = 1, suffix, placeholder, className, disabled }, ref) => {
  function clamp(v: number) {
    return Math.min(max, Math.max(min, v));
  }

  function roundToStep(v: number) {
    const decimals = (step.toString().split(".")[1] ?? "").length;
    return Number(v.toFixed(decimals));
  }

  function nudge(delta: number) {
    const base = value ?? min;
    onChange(clamp(roundToStep(base + delta)));
  }

  return (
    <div
      className={cn(
        "flex h-11 items-center rounded-[var(--radius-md)] border border-[var(--border-strong)] bg-[var(--surface)] pr-1 transition-colors duration-150 focus-within:ring-2 focus-within:ring-[var(--ring)]",
        disabled && "opacity-50",
        className
      )}
    >
      <button
        type="button"
        disabled={disabled || (value ?? min) <= min}
        onClick={() => nudge(-step)}
        aria-label="Decrease"
        className="flex h-full w-9 shrink-0 items-center justify-center rounded-l-[var(--radius-md)] text-[var(--neutral-500)] hover:bg-[var(--surface-raised)] disabled:pointer-events-none disabled:opacity-40"
      >
        <Minus className="size-3.5" />
      </button>
      <input
        ref={ref}
        type="number"
        inputMode="decimal"
        disabled={disabled}
        value={value ?? ""}
        placeholder={placeholder}
        onChange={(e) => {
          const raw = e.target.value;
          if (raw === "") {
            onChange(undefined);
            return;
          }
          const n = Number(raw);
          if (!Number.isNaN(n)) onChange(n);
        }}
        onBlur={() => {
          if (value !== undefined) onChange(clamp(value));
          onBlur?.();
        }}
        className="w-full min-w-0 flex-1 bg-transparent text-center text-sm tabular-nums text-[var(--foreground)] outline-none [appearance:textfield] placeholder:text-[var(--neutral-400)] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
      />
      {suffix && <span className="shrink-0 pr-1.5 text-xs text-[var(--neutral-400)]">{suffix}</span>}
      <button
        type="button"
        disabled={disabled || (value ?? min) >= max}
        onClick={() => nudge(step)}
        aria-label="Increase"
        className="flex h-full w-9 shrink-0 items-center justify-center rounded-r-[var(--radius-md)] text-[var(--neutral-500)] hover:bg-[var(--surface-raised)] disabled:pointer-events-none disabled:opacity-40"
      >
        <Plus className="size-3.5" />
      </button>
    </div>
  );
});
NumberStepper.displayName = "NumberStepper";
