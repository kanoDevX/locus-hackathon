"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { ScholarshipSearchResultDto } from "./types";

export interface ScholarshipSearchFilters {
  country?: string;
  minCoverage?: number;
  query?: string;
}

export function useScholarshipSearch(filters: ScholarshipSearchFilters) {
  return useQuery({
    queryKey: ["scholarships", filters],
    queryFn: () =>
      apiFetch<ScholarshipSearchResultDto[]>("/api/v1/scholarships/search", {
        query: { country: filters.country, minCoverage: filters.minCoverage, query: filters.query },
      }),
  });
}
