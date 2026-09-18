"use client";

import { create } from "zustand";
import type { ProfileDiffDto, RecommendationDeltaDto } from "@/lib/api/types";

export type PersonaTone = "Neutral" | "Reassuring" | "Direct" | "Energetic";

interface DiffPanelContent {
  kind: "profile" | "recommendations";
  profileDiff?: ProfileDiffDto;
  recommendationDelta?: RecommendationDeltaDto;
}

interface WizardProgress {
  step: number;
  totalSteps: number;
}

interface JourneyState {
  recentlyChangedProgramIds: Set<number>;
  diffPanel: DiffPanelContent | null;
  personaTone: PersonaTone;
  selectedProgramIds: number[];
  wizardProgress: WizardProgress | null;
  showDiffPanel: (content: DiffPanelContent) => void;
  dismissDiffPanel: () => void;
  setPersonaTone: (tone: PersonaTone) => void;
  toggleSelectedProgram: (programId: number) => void;
  setWizardProgress: (progress: WizardProgress | null) => void;
}

export const useJourneyStore = create<JourneyState>((set) => ({
  recentlyChangedProgramIds: new Set(),
  diffPanel: null,
  personaTone: "Neutral",
  selectedProgramIds: [],
  wizardProgress: null,
  toggleSelectedProgram: (programId) =>
    set((state) => ({
      selectedProgramIds: state.selectedProgramIds.includes(programId)
        ? state.selectedProgramIds.filter((id) => id !== programId)
        : [...state.selectedProgramIds, programId].slice(-5),
    })),
  showDiffPanel: (content) => {
    const changed = new Set<number>([
      ...(content.recommendationDelta?.addedProgramIds ?? []),
      ...(content.recommendationDelta?.rankChangedProgramIds ?? []),
    ]);
    set({ diffPanel: content, recentlyChangedProgramIds: changed });
    if (typeof window !== "undefined") {
      window.setTimeout(() => set({ recentlyChangedProgramIds: new Set() }), 2000);
    }
  },
  dismissDiffPanel: () => set({ diffPanel: null }),
  setPersonaTone: (tone) => set({ personaTone: tone }),
  setWizardProgress: (progress) => set({ wizardProgress: progress }),
}));
