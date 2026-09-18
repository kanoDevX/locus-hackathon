"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type { DegreeLevel, ProgramSummaryDto } from "./types";

export interface ProgramSearchFilters {
  query?: string;
  country?: string;
  field?: string;
  degreeLevel?: DegreeLevel;
  maxTuition?: number;
  scholarshipOnly?: boolean;
}

export function useProgramSearch(filters: ProgramSearchFilters) {
  return useQuery({
    queryKey: ["programs", filters],
    queryFn: () =>
      apiFetch<ProgramSummaryDto[]>("/api/v1/programs/search", {
        query: {
          query: filters.query,
          country: filters.country,
          field: filters.field,
          degreeLevel: filters.degreeLevel,
          maxTuition: filters.maxTuition,
          scholarshipOnly: filters.scholarshipOnly,
        },
      }),
  });
}
