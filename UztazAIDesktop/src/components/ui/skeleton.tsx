import { cn } from "@/lib/utils";

export function Skeleton({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("animate-pulse rounded-[var(--radius-md)] bg-[var(--neutral-200)] dark:bg-[var(--neutral-800)]", className)}
      {...props}
    />
  );
}
