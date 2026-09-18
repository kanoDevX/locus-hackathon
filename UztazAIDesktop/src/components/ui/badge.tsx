import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium leading-none",
  {
    variants: {
      variant: {
        neutral: "bg-[var(--neutral-100)] text-[var(--neutral-700)] dark:bg-[var(--neutral-800)] dark:text-[var(--neutral-200)]",
        brand: "bg-[var(--brand-100)] text-[var(--brand-800)] dark:bg-[var(--brand-900)] dark:text-[var(--brand-200)]",
        success: "bg-[var(--success-100)] text-[var(--success-500)]",
        warning: "bg-[var(--warning-100)] text-[var(--warning-500)]",
        info: "bg-[var(--info-100)] text-[var(--info-500)]",
        danger: "bg-[var(--danger-100)] text-[var(--danger-500)]",
      },
    },
    defaultVariants: { variant: "neutral" },
  }
);

export interface BadgeProps extends React.HTMLAttributes<HTMLSpanElement>, VariantProps<typeof badgeVariants> {}

export function Badge({ className, variant, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ variant, className }))} {...props} />;
}
