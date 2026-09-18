"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { ArrowLeft } from "lucide-react";
import { useRouter, usePathname, Link } from "@/i18n/navigation";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useProfile, useMyProfile } from "@/lib/api/profile";
import { AppHeader } from "@/components/layout/app-header";
import { OnboardingHeader } from "@/components/layout/onboarding-header";
import { CommandPalette } from "@/components/layout/command-palette";
import { CommandPaletteFab } from "@/components/layout/command-palette-fab";
import { ErrorBoundary } from "@/components/layout/error-boundary";
import { DiffPanel } from "@/components/journey/diff-panel";

function isIntakeComplete(profile: { educationStage: string | null } | undefined, activeProfileId: string | null | undefined) {
  return !!activeProfileId && !!profile && profile.educationStage !== null;
}

export default function JourneyLayout({ children }: { children: React.ReactNode }) {
  const t = useTranslations("common");
  const router = useRouter();
  const pathname = usePathname();
  const { accessToken, hasHydrated, activeProfileId, setActiveProfileId } = useAuthStore();
  const [paletteOpen, setPaletteOpen] = useState(false);

  const shouldResolveMyProfile = hasHydrated && !!accessToken && !activeProfileId;
  const myProfile = useMyProfile(shouldResolveMyProfile);
  useEffect(() => {
    if (myProfile.data) setActiveProfileId(myProfile.data.id);
  }, [myProfile.data, setActiveProfileId]);

  const { data: profile, isFetched: profileFetched } = useProfile(activeProfileId);

  useEffect(() => {
    if (hasHydrated && !accessToken) router.replace("/login");
  }, [hasHydrated, accessToken, router]);

  const isIntakeRoute = pathname.startsWith("/journey/profile");
  const knowsIntakeStatus = activeProfileId ? profileFetched : myProfile.isFetched && myProfile.data === null;
  const intakeComplete = isIntakeComplete(profile, activeProfileId);

  useEffect(() => {
    if (knowsIntakeStatus && !intakeComplete && !isIntakeRoute) {
      router.replace("/journey/profile");
    }
  }, [knowsIntakeStatus, intakeComplete, isIntakeRoute, router]);

  if (!hasHydrated || !accessToken) {
    return <div className="flex min-h-screen items-center justify-center text-sm text-[var(--neutral-400)]">…</div>;
  }

  if (isIntakeRoute) {
    return (
      <div className="flex min-h-screen flex-col">
        <OnboardingHeader />
        <main className="mx-auto w-full max-w-2xl flex-1 px-4 py-8 sm:px-6">
          <ErrorBoundary>{children}</ErrorBoundary>
        </main>
      </div>
    );
  }

  if (!knowsIntakeStatus || !intakeComplete) {
    return <div className="flex min-h-screen items-center justify-center text-sm text-[var(--neutral-400)]">…</div>;
  }

  const showBackToHomeLink = pathname !== "/journey/home" && !pathname.startsWith("/journey/roadmap/study");

  return (
    <div className="flex min-h-screen flex-col">
      <AppHeader />
      <DiffPanel />
      <CommandPalette open={paletteOpen} onOpenChange={setPaletteOpen} />
      <CommandPaletteFab onClick={() => setPaletteOpen(true)} />
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-6">
        {showBackToHomeLink && (
          <Link
            href="/journey/home"
            className="mb-4 inline-flex items-center gap-1.5 text-sm text-[var(--neutral-500)] transition-colors duration-150 hover:text-[var(--foreground)]"
          >
            <ArrowLeft className="size-3.5" />
            {t("backToHome")}
          </Link>
        )}
        <ErrorBoundary>{children}</ErrorBoundary>
      </main>
    </div>
  );
}
