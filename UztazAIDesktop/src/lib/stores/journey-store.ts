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
  /** Program ids that just changed in a recommendation delta — read by RecommendationCard to
   * apply a brief highlight pulse instead of a silent re-render (§5 diff experience). */
  recentlyChangedProgramIds: Set<number>;
  diffPanel: DiffPanelContent | null;
  personaTone: PersonaTone;
  selectedProgramIds: number[];
  /** Current step of whichever multi-step form is active, read by the focused onboarding header
   * (journey/layout.tsx) so the "Step X of Y" indicator lives in one place at the top of the
   * viewport instead of duplicated inside each wizard. Null when no wizard is mounted. */
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
