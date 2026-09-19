"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
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

export interface DiscoveredScholarship {
  name: string;
  coverage: string;
  eligibility: string;
  deadline: string;
}

export interface ScholarshipDiscovery {
  universityName: string;
  country: string;
  scholarships: DiscoveredScholarship[];
  sources: { title: string; url: string }[];
  retrievedAtUtc: string;
}

export function useDiscoverScholarships() {
  return useMutation({
    mutationFn: (input: { universityName: string; country: string }) =>
      apiFetch<ScholarshipDiscovery>("/api/v1/scholarships/discover", { method: "POST", body: input }),
  });
}
