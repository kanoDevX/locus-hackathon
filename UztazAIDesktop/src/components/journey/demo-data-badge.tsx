"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Info, ShieldCheck, Globe, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { Tooltip, TooltipContent, TooltipTrigger, TooltipProvider } from "@/components/ui/tooltip";
import { Badge } from "@/components/ui/badge";
import { ApiError } from "@/lib/api/client";
import type { DataProvenanceDto } from "@/lib/api/types";

export function DemoDataBadge({
  provenance,
  onVerify,
}: {
  provenance: DataProvenanceDto;
  onVerify?: () => Promise<unknown>;
}) {
  const t = useTranslations("common");
  const [verifying, setVerifying] = useState(false);

  async function verify() {
    if (!onVerify) return;
    setVerifying(true);
    try {
      await onVerify();
      toast.success(t("verifiedOnline"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("verifyFailed"));
    } finally {
      setVerifying(false);
    }
  }

  return (
    <span className="inline-flex flex-wrap items-center gap-1.5">
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Badge variant={provenance.isDemoData ? "info" : "success"} className="cursor-help gap-1">
              {provenance.isDemoData ? <Info className="size-3" /> : <ShieldCheck className="size-3" />}
              {provenance.isDemoData ? t("demoData") : t("verified")}
            </Badge>
          </TooltipTrigger>
          <TooltipContent className="max-w-xs">
            {provenance.isDemoData ? t("demoDataHint") : provenance.source}
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
      {provenance.isDemoData && onVerify && (
        <button
          type="button"
          onClick={verify}
          disabled={verifying}
          className="inline-flex items-center gap-1 rounded-full border border-[var(--border-subtle)] px-2.5 py-1 text-xs font-medium text-[var(--neutral-500)] hover:text-[var(--primary)] disabled:opacity-60"
        >
          {verifying ? <Loader2 className="size-3 animate-spin" /> : <Globe className="size-3" />}
          {verifying ? t("verifying") : t("verifyOnline")}
        </button>
      )}
    </span>
  );
}
