"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "./client";

export interface ModuleBreakdownDto {
  module: string;
  calls: number;
  successRatePercent: number;
  fallbackRatePercent: number;
}

export interface InsightsDto {
  totalAiCalls: number;
  successRatePercent: number;
  fallbackRatePercent: number;
  averageLatencyMs: number;
  p95LatencyMs: number;
  totalInputTokens: number;
  totalOutputTokens: number;
  byModule: ModuleBreakdownDto[];
}

/** GET /api/v1/system/insights — an "engineering-maturity" endpoint (per its own backend doc
 * comment) surfacing real Gemini call volume, latency and fallback rate straight from the
 * AiUsageLog table, so this can be shown to judges as evidence the product was engineered around
 * real cost/latency constraints rather than demoed once. Built server-side but had no frontend
 * page at all before this. */
export function useInsights() {
  return useQuery({
    queryKey: ["insights"],
    queryFn: () => apiFetch<InsightsDto>("/api/v1/system/insights"),
    staleTime: 30_000,
  });
}
