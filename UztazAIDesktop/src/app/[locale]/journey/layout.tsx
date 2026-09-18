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

/**
 * A brand-new (or mid-retake) account with no completed Placement & Eligibility Intake must not
 * be able to reach any other feature — the wizard is the product's literal front door and every
 * other screen assumes it already ran (EducationStage/Track/exam data feed the eligibility
 * calculation, diagnostics and recommendations). `educationStage` is null on the backend until
 * `SubmitExamIntakeCommand` runs once, so it's the single source of truth here — never a
 * client-only flag that could drift from what's actually persisted.
 */
function isIntakeComplete(profile: { educationStage: string | null } | undefined, activeProfileId: string | null | undefined) {
  return !!activeProfileId && !!profile && profile.educationStage !== null;
}

export default function JourneyLayout({ children }: { children: React.ReactNode }) {
  const t = useTranslations("common");
  const router = useRouter();
  const pathname = usePathname();
  const { accessToken, hasHydrated, activeProfileId, setActiveProfileId } = useAuthStore();
  const [paletteOpen, setPaletteOpen] = useState(false);

  // `activeProfileId` lives only in this browser's localStorage — a fresh login (new device, a
  // different browser, a cleared cache) can leave it empty even though the account already has a
  // profile server-side. Resolve it via /profile/mine whenever it's missing, so a returning user
  // never gets silently routed back into the wizard and made to create a second profile.
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
  // Wait for the profile fetch to actually resolve — either through useProfile once
  // activeProfileId is known, or through the /mine lookup confirming there's genuinely no
  // profile yet — before concluding intake is incomplete. Otherwise a returning user would flash
  // through the wizard for a frame on every load while a query is still in flight.
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

  // Focus mode: the intake wizard (first-run or a later retake) never shares the screen with the
  // rest of the app's navigation — no command palette, no journey stepper, no secondary-feature
  // links. This is deliberate, not a bug: a multi-step required form is easiest to finish when
  // there's nowhere else to click.
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

  // Every non-Home screen used to carry the full journey stepper as an implicit "you are here,
  // here's how to get back" affordance. Now that Home is a real dashboard and the stepper is
  // gone (§ UX request), the only way back was the header logo — easy to miss. A single link at
  // the top of every subpage's content restores that convenience without reintroducing the
  // stepper. The study-guide route keeps its own, more specific "Back to roadmap" link instead.
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
