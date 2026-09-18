# UstazAI — Web Frontend

**LOCUS Startup Hackathon 2026 — Case #2 "Personal Admission Route"**

The frontend for [UstazAI](../UstazAI), consuming the ASP.NET Core / .NET 10 backend. One
continuous, own-brand journey — Entry → Profile → Diagnostics → Recommendations → Comparison →
Roadmap → Next Action — with a persistent progress rail, an explicit "what changed and why" diff
experience, and honest, non-fabricated uncertainty everywhere a number is shown.

## Design system

- **Brand**: "Wayfinder Teal" (`--brand-*`, HSL-neighborhood of `#1f9179`) on warm-neutral
  (stone, not clinical gray) surfaces — deliberately not the generic SaaS blue/violet or a
  pastel-cartoon EdTech palette. One primary hue + a 4-color semantic set (success/fit,
  warning/risk, info/demo-data, danger/blocker), defined once as CSS custom properties in
  [`globals.css`](src/app/[locale]/globals.css) and re-mapped for dark mode via a `data-theme`
  attribute (not just `prefers-color-scheme`, so a manual toggle always wins).
- **Type**: one variable family, **Inter**, loaded with `latin` + `cyrillic` + `cyrillic-ext`
  subsets — verified to render Kazakh-specific Cyrillic characters (Ә ғ қ ң ө ұ ү һ і) correctly,
  not assumed.
- **Motion**: one shared easing/duration token set in [`lib/motion.ts`](src/lib/motion.ts)
  (150ms micro / 320ms transition / 520ms celebratory), used for the survey step transitions, the
  Diff Panel slide-in, the roadmap-task celebration pop, and the recommendation-card
  diff-highlight pulse — motion always signals a state change, never decoration-only.
- **Elevation**: 3 levels only (base / raised card / overlay), via `--shadow-raised` /
  `--shadow-overlay`.
- **Components**: Radix UI primitives, fully re-skinned against the tokens above in
  [`components/ui`](src/components/ui) — not default shadcn styling.
- **Dark mode**: `next-themes` with `attribute="data-theme"`; Tailwind's `dark:` variant is
  re-pointed at that same attribute via `@custom-variant dark` in `globals.css` so the manual
  toggle and every `dark:` utility class agree (this was a real bug caught during testing — see
  Known Issues Found & Fixed below).

## Tech stack

- **Next.js 16** (App Router, Turbopack) + **React 19** + **TypeScript strict**. (The original
  brief named Next.js 15; 16 was current stable by build time and is the natural successor —
  same App Router paradigm, no behavioral compromise, so we used latest stable rather than
  pinning to an older major version.)
- **Tailwind CSS 4**, custom `@theme` token layer, no unmodified default shadcn look
- **Radix UI** primitives (dialog, dropdown, select, tabs, tooltip, progress, switch, separator)
- **Framer Motion** for all orchestrated transitions
- **React Hook Form + Zod** (`@hookform/resolvers`) for the multi-step survey, with per-step field
  validation gating (`STEP_FIELDS`) and autosave-and-resume to `localStorage`
- **TanStack Query** for every backend call — caching, mutation-driven cache updates, no manual
  `fetch`-in-`useEffect` data fetching anywhere
- **Zustand** for auth session (persisted) and journey UI state (diff panel content, recently
  "changed" program ids for the highlight pulse, comparison selection)
- **next-intl** — `ru` (default) / `kk` / `en`, URL-prefixed (`/ru/...`), full message catalogs
  for all three in [`messages/`](messages)
- **lucide-react** icons (one family, one stroke weight), **recharts**-free — the fit-score bars
  and uncertainty band are hand-built SVG-free progress primitives, simpler and more accessible
  than pulling in a charting library for two visualization types
- **cmdk** for the ⌘K command palette

## Running it

```bash
npm install
cp .env.local.example .env.local   # or just create one — see below
npm run dev
```

`.env.local`:

```
NEXT_PUBLIC_API_BASE_URL=http://localhost:5087
```

That's the backend's default `http` launch profile port (`UstazAI/Properties/launchSettings.json`
— matches what `dotnet run` or your IDE's Run button use out of the box). Point it at wherever
the [backend](../UstazAI) actually ends up running instead if that differs (CORS is already open
there for local dev). Then register a new account or sign in with the backend's seeded judge account
(`judge@ustazai.demo` / `JudgePass123!`) and use "Reset demo" from the account menu for a clean,
scripted starting profile.

```bash
npm run build   # production build — verified clean, 0 errors
npm run lint    # 0 errors, 1 informational (react-hook-form/React-Compiler) warning
npx tsc --noEmit
```

## The diff experience (§5) — how it's actually wired

The backend does not run a SignalR hub in this build (see backend README). Instead, every
profile-update and recommendation-regenerate call returns its diff/delta **synchronously** in the
HTTP response. [`DiffPanel`](src/components/journey/diff-panel.tsx) reads that from a Zustand
store ([`journey-store.ts`](src/lib/stores/journey-store.ts)) the moment the mutation resolves —
an animated, dismissible panel naming exactly what changed
("Budget lowered → 2 dropped, 1 new"), and the affected `RecommendationCard`s get a brief
colored-outline pulse ([`diffHighlight`](src/lib/motion.ts)) instead of silently re-rendering.
This is arguably easier for a judge to verify live (no websocket client needed, works the same
over a flaky connection) than a push-based version would have been.

## What's genuinely built vs. deliberately out of scope

Built and manually + automatically verified end-to-end against the real backend: the full
mandatory journey, autosave-and-resume, the diff panel, favorites, deadline calendar + `.ics`
export, scholarship search, program catalog search, essay feedback (with its own fallback state
when Gemini is unavailable), the what-if simulator, peer pathways, the judge demo-reset control,
dark/light theme, `ru`/`kk`/`en` locales, and a 375px mobile viewport pass.

### Addendum 2 — Placement & Eligibility Intake, Chat, Roadmap, Map (§12–§14)

Also built and verified live against the real backend (not just type-checked):

- **Placement & Eligibility Intake Wizard** ([intake-wizard-form.tsx](src/components/journey/intake-wizard-form.tsx)):
  replaces the old flat Profile step at the same `/journey/profile` route. A 6-step wizard —
  education stage (5 tappable cards), basics, admission track (dynamically filtered by stage via
  `tracksForStage()`), exam score + subject breakdown (track-specific max-score bounds), goals +
  supplementary exams, budget/timeline — validated by [intake-wizard-schema.ts](src/lib/validation/intake-wizard-schema.ts),
  which composes the existing `profileSchema` with `exam-intake-schema.ts`'s cross-field rules
  (school-stage-vs-track validity, college-background requiredness, target-specialty-match-vs-track
  consistency, the paid-track no-score rule, min-5-points-per-discipline) rather than duplicating
  them — the same rules the backend's `SubmitExamIntakeValidator` enforces. Autosave-and-resume
  via `localStorage`, same pattern as the original Profile step.
- **Staged progress screen** ([intake-progress-screen.tsx](src/components/journey/intake-progress-screen.tsx)):
  each stage's status is driven by a real TanStack Query mutation's own `status` (submit intake →
  calculate eligibility), never a fixed-duration fake timer.
- **Eligibility result screen** (`/journey/eligibility-result`): dual grant/paid verdict columns
  per domestic program, reusing the existing `UncertaintyBand` component (extended with optional
  `label`/`sampleSizeLabel` props) for `GrantCompetitiveness` rather than a bare percentage.
- **Result-aware chat** (`/journey/chat`): a plain request/response `useMutation`, not a streaming
  hook — the backend's chat endpoint returns one complete JSON reply per call, matching every
  other AI-backed endpoint in this product (no SSE anywhere in this backend, see above). Verified
  live: a real Gemini-narrated reply, correctly grounded in the caller's own eligibility data and
  correctly localized to the profile's language.
- **Extended roadmap** ([roadmap-task-card.tsx](src/components/journey/roadmap-task-card.tsx)):
  a new `SubjectPrep` category renders a subject badge and curated, clickable resource links
  inline in the existing task card — no separate component fork, no separate list. A "Strengthen
  weak subjects" button on the Roadmap page triggers `GeneratePrepPlanCommand` for whichever
  program the current roadmap was built for (via the now-exposed `RoadmapTaskDto.programId`).
- **University Map & Environment Intelligence** ([campus-map-card.tsx](src/components/journey/campus-map-card.tsx)):
  folded into the Comparison screen as a second tab (`Tabs`, previously unused in this codebase)
  rather than a disconnected page — `ProgramComparisonRowDto.campusMap` is already embedded in the
  existing `/api/v1/comparison` response, so no extra round-trip is needed. An unmapped university
  shows an explicit "not yet available" state, never a fabricated coordinate.

Not built, because the backend doesn't expose them this round (see backend README's own "Known
limitations"), and shipping UI for a capability that doesn't exist server-side would be exactly
the "decorative-only AI theatrics" anti-pattern the case brief warns against:

- **Streaming token-by-token AI reveal** — the backend returns complete JSON, not an SSE stream;
  recommendation text fades/slides in on arrival instead of pretending to stream it. The new §13
  chat panel follows the same rule.
- **Multi-agent reasoning trace (advocate/risk/synthesis)** — "See the reasoning" instead
  surfaces the real four structured sub-explanations (academic/financial/career/timeline) the
  backend actually computes and returns — honest, not simulated.
- **Document OCR intake, parent/mentor share view, real 2GIS/MapLibre interactive map embed** —
  the map view links out to 2GIS/OpenStreetMap rather than embedding an interactive map widget,
  matching the backend's `IMapProvider` being a key-free URL builder rather than a full mapping
  SDK integration this round.
- **Storybook and a full Playwright/axe-core CI pipeline** — the core journey and every new
  feature (including the full §12 wizard→eligibility→roadmap→chat→comparison flow) were instead
  verified with an interactive browser pass against the live backend, plus the backend's own
  57-test automated suite (unit + integration) that exercises the same API contract this frontend
  calls; a dedicated Playwright suite is a natural next addition but wasn't built this round.

## Known issues found & fixed during testing

Interactive testing against the live backend surfaced three real bugs, fixed in place (not
worked around):

1. **Backend enum serialization** — the API was returning/expecting enums as raw numbers
   (`0`, `1`, `2`) over JSON by default, while this frontend's TypeScript types (correctly) use
   readable string literals (`"Medium"`, `"Ru"`). Fixed backend-side with a global
   `JsonStringEnumConverter`, which also makes the Scalar/OpenAPI docs far more legible for
   judges reading them directly.
2. **Roadmap task card mobile overlap** — the mark-done button overlapped the task title on a
   375px viewport. Fixed by stacking the card's content and action vertically below `sm:`.
3. **Dark mode / Tailwind `dark:` desync** — `next-themes`' manual toggle sets a `data-theme`
   attribute, but Tailwind's `dark:` utility variant defaulted to the OS-level
   `prefers-color-scheme` media query instead, so several elements (progress-bar tracks) ignored
   a manual light/dark switch. Fixed with a `@custom-variant dark` pointing both systems at the
   same attribute.
