"use client";

import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { UserRole } from "@/lib/api/types";

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  userId: string | null;
  email: string | null;
  displayName: string | null;
  role: UserRole | null;
  activeProfileId: string | null;
  hasHydrated: boolean;
  setHasHydrated: (value: boolean) => void;
  setSession: (session: {
    accessToken: string;
    refreshToken: string;
    userId: string;
    email: string;
    displayName: string;
    role: UserRole;
  }) => void;
  setActiveProfileId: (profileId: string | null) => void;
  updateAccountInfo: (info: { displayName: string; email: string }) => void;
  clear: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      userId: null,
      email: null,
      displayName: null,
      role: null,
      activeProfileId: null,
      hasHydrated: false,
      setHasHydrated: (value) => set({ hasHydrated: value }),
      setSession: (session) =>
        set({
          accessToken: session.accessToken,
          refreshToken: session.refreshToken,
          userId: session.userId,
          email: session.email,
          displayName: session.displayName,
          role: session.role,
        }),
      setActiveProfileId: (profileId) => set({ activeProfileId: profileId }),
      updateAccountInfo: (info) => set({ displayName: info.displayName, email: info.email }),
      clear: () =>
        set({
          accessToken: null,
          refreshToken: null,
          userId: null,
          email: null,
          displayName: null,
          role: null,
          activeProfileId: null,
        }),
    }),
    {
      name: "ustazai-auth",
      onRehydrateStorage: () => (state) => state?.setHasHydrated(true),
    }
  )
);
