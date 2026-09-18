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

export function useInsights() {
  return useQuery({
    queryKey: ["insights"],
    queryFn: () => apiFetch<InsightsDto>("/api/v1/system/insights"),
    staleTime: 30_000,
  });
}
