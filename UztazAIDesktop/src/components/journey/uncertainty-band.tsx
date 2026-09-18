import { useTranslations } from "next-intl";
import type { UncertaintyEstimateDto } from "@/lib/api/types";

/** Never a bare "87% chance!" badge — always the range, sample size and basis (§ anti-pattern
 * "fake precision UI"). Reused as-is by the eligibility result screen (§12) for
 * GrantCompetitiveness — pass `label`/`sampleSizeLabel` to relabel it for a non-recommendation
 * context instead of forking a second band component. */
export function UncertaintyBand({
  estimate,
  label,
  sampleSizeLabel,
}: {
  estimate: UncertaintyEstimateDto;
  label?: string;
  sampleSizeLabel?: string;
}) {
  const t = useTranslations("recommendations");

  return (
    <div>
      <div className="flex items-baseline justify-between">
        <span className="text-xs font-medium text-[var(--neutral-500)]">{label ?? t("admissionChance")}</span>
        <span className="text-sm font-semibold tabular-nums">
          {Math.round(estimate.lowerBound)}–{Math.round(estimate.upperBound)}%
        </span>
      </div>
      <div className="relative mt-1.5 h-1.5 rounded-full bg-[var(--neutral-200)] dark:bg-[var(--neutral-800)]">
        <div
          className="absolute h-full rounded-full bg-[var(--info-500)]/40"
          style={{ left: `${estimate.lowerBound}%`, width: `${Math.max(2, estimate.upperBound - estimate.lowerBound)}%` }}
        />
        <div
          className="absolute top-1/2 size-2.5 -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-[var(--surface)] bg-[var(--info-500)]"
          style={{ left: `${estimate.estimate}%` }}
        />
      </div>
      <p className="mt-1 text-[11px] text-[var(--neutral-400)]">{sampleSizeLabel ?? t("sampleSize", { count: estimate.sampleSize })}</p>
    </div>
  );
}
