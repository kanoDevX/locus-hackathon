import { cn } from "@/lib/utils";

function colorFor(score: number) {
  if (score >= 70) return "bg-[var(--success-500)]";
  if (score >= 45) return "bg-[var(--warning-500)]";
  return "bg-[var(--danger-500)]";
}

export function FitScoreBar({ label, score }: { label: string; score: number }) {
  return (
    <div className="flex items-center gap-2.5">
      <span className="w-28 shrink-0 text-xs text-[var(--neutral-500)]">{label}</span>
      <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-[var(--neutral-200)] dark:bg-[var(--neutral-800)]">
        <div
          className={cn("h-full rounded-full transition-all duration-500 ease-[var(--ease-emphasized)]", colorFor(score))}
          style={{ width: `${Math.min(100, Math.max(0, score))}%` }}
        />
      </div>
      <span className="w-9 shrink-0 text-right text-xs font-medium tabular-nums">{Math.round(score)}</span>
    </div>
  );
}
