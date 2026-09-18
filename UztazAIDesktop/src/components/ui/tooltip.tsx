"use client";

import * as TooltipPrimitive from "@radix-ui/react-tooltip";
import { cn } from "@/lib/utils";

export const TooltipProvider = TooltipPrimitive.Provider;
export const Tooltip = TooltipPrimitive.Root;
export const TooltipTrigger = TooltipPrimitive.Trigger;

export function TooltipContent({ className, sideOffset = 6, ...props }: React.ComponentProps<typeof TooltipPrimitive.Content>) {
  return (
    <TooltipPrimitive.Portal>
      <TooltipPrimitive.Content
        sideOffset={sideOffset}
        className={cn(
          "z-50 rounded-[var(--radius-sm)] bg-[var(--neutral-900)] px-2.5 py-1.5 text-xs text-white shadow-[var(--shadow-raised)] dark:bg-[var(--neutral-100)] dark:text-[var(--neutral-900)]",
          className
        )}
        {...props}
      />
    </TooltipPrimitive.Portal>
  );
}
