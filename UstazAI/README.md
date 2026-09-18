# UstazAI — Personal Admission Route

**LOCUS Startup Hackathon 2026 — Case #2 "Personal Admission Route" (LOCUSCASE2)**

UstazAI turns a Kazakhstani 11th-grader's profile and goals into a clear, explainable,
ever-changing admission route — not a static university list, and not a raw AI chatbot. A
deterministic statistical model scores and ranks programs; Gemini only ever narrates and
explains already-computed numbers, inside a structured product flow with code-level guardrails
around it.

Committed persona: **11th-grade Kazakhstani student exploring bachelor's programs at home and
abroad.**

## The journey (§4 of the case brief → API surface)

| Stage | Endpoint |
|---|---|
| Entry | `GET /api/v1/onboarding/value-proposition?locale=ru\|kk\|en` |
| Placement & Eligibility Intake (§12) | `POST /api/v1/profile/{id}/exam-intake`, `POST .../exam-intake/calculate`, `GET .../exam-intake/result` |
| Profile | `POST /api/v1/profile`, `GET /api/v1/profile/{id}` |
| Result-Aware Chat + Gap-to-Course Prep (§13) | `POST /api/v1/profile/{id}/chat`, `GET .../chat/history`, `POST /api/v1/profile/{id}/prep-plan/{programId}`, `GET .../prep-plan/{programId}` |
| Diagnostics | `POST /api/v1/profile/{id}/diagnostics` |
| Recommendations | `POST /api/v1/profile/{id}/recommendations`, `GET .../recommendations` |
| Comparison (+ University Map/Environment, §14) | `POST /api/v1/comparison`, `GET /api/v1/profile/{id}/campus-map/{programId}` |
| Roadmap | `POST /api/v1/profile/{id}/roadmap`, `GET .../roadmap` |
| Next Action | `GET /api/v1/profile/{id}/next-action`, `PATCH /api/v1/roadmap/tasks/{taskId}/progress` |

Plus: `POST /api/v1/profile/{id}/what-if` (counterfactual simulator), `GET
/api/v1/profile/{id}/peer-pathways` (anonymized peer outcome distribution), `POST
/api/v1/system/demo-reset` (judge sandbox), `GET /api/v1/system/insights` (AI cost/latency
telemetry), `GET /api/v1/system/health` / `GET /health`, and `/api/v1/auth/*`
(register/login/refresh/logout/logout-all).

Every one of these is its own MediatR command/query with its own handler — not a single
"GetEverything" endpoint — matching the case's journey-and-architecture criterion.

### Baseline features from the case brief's own suggestions

Researched what comparable EdTech/admissions platforms treat as table-stakes (deadline
tracking, favorites, scholarship matching, essay feedback that edits rather than ghost-writes —
see sources at the bottom) and added the ones the case brief explicitly names but the first pass
hadn't built yet:

| Feature | Endpoint |
|---|---|
| Favorites / shortlist | `GET\|POST /api/v1/profile/{id}/favorites`, `DELETE .../favorites/{programId}` |
| Deadline calendar | `GET /api/v1/profile/{id}/calendar` |
| Reminders (upcoming deadlines) | `GET /api/v1/profile/{id}/notifications?withinDays=14` |
| Downloadable calendar file | `GET /api/v1/profile/{id}/calendar.ics` (import into any calendar app) |
| Scholarship matching (standalone) | `GET /api/v1/scholarships/search?country=&minCoverage=&query=` |
| Catalog browse/search | `GET /api/v1/programs/search?query=&country=&field=&degreeLevel=&maxTuition=&scholarshipOnly=` |
| Essay help | `POST /api/v1/profile/{id}/essays/review` — feedback only, structurally incapable of returning a rewritten draft (admissions offices screen for AI-written essays) |

Plus senior-API-completeness items that were gaps in the first pass: refresh-token
revocation/logout (`/auth/logout`, `/auth/logout-all` — tokens are hashed at rest, never stored
raw), a standard `Microsoft.Extensions.Diagnostics.HealthChecks` `/health` endpoint alongside the
product's own richer one, and `IMemoryCache` on the catalog search hot path (§2's "IMemoryCache
for hot recommendation results" requirement, previously unimplemented).

## Why this isn't "just an AI chatbot"

1. **Hybrid Scoring Engine** ([HybridScoringEngine.cs](UstazAI.Domain/Services/HybridScoringEngine.cs)):
   a deterministic, framework-free scorer computes academic/financial/career/timeline fit and an
   admission-probability estimate from the seeded catalog + historical archetype data. Gemini is
   only ever asked to *narrate* these already-computed numbers — it cannot change a score.
2. **Uncertainty-Quantified Scoring**: every probability is `{estimate, lowerBound, upperBound,
   sampleSize, basis}`, never a bare percentage — directly answering the case's ban on
   fabricated certainty.
3. **Explainable Diff Engine** ([ProfileDiffCalculator.cs](UstazAI.Domain/Services/ProfileDiffCalculator.cs)):
   every profile update returns exactly which fields changed and which downstream stages are
   likely affected. The hard "changing one answer visibly changes recommendations" requirement is
   demonstrated concretely: `POST /recommendations` returns a `delta` (added/removed/rank-changed
   program ids) versus the previous batch every time it's called — see the integration test.
4. **Guardrail + Data Provenance** ([GuardrailRules.cs](UstazAI.Domain/Services/GuardrailRules.cs)):
   a code-level rule set rejects/rewrites guarantee-of-admission language as a last-resort safety
   net (`GuardrailBehavior` MediatR pipeline step), and every factual claim carries a
   `DataProvenance {source, isDemoData, lastVerified}` object.
5. **Immutable Explainability Ledger** ([DecisionLedger.cs](UstazAI.Domain/Services/DecisionLedger.cs)):
   every scoring/AI decision is appended to a hash-chained `AiDecisionLog` table — each row hashes
   its payload together with the previous row's hash, so the chain can be walked to prove nothing
   was altered after the fact.
6. **Counterfactual What-If Simulator**: `POST /profile/{id}/what-if` perturbs one realistic
   variable at a time (GPA, exam score, budget band, timeline) against the same deterministic
   scorer and ranks which change would move the needle most — pure computation, no extra AI cost.
7. **Peer Pathways**: instead of a single chance number, `GET /peer-pathways` returns the
   distribution of similar seeded archetype outcomes (admitted/waitlisted/rejected, typical
   timeline, common blocker).
8. **Judge Sandbox Mode**: `POST /system/demo-reset` (Judge role only) resets the caller's demo
   profile to a known, scripted state so a live 5-minute demo never depends on leftover state.
9. **Graceful AI degradation**: every Gemini call is wrapped in Polly retry/timeout/circuit-breaker
   policies (`Microsoft.Extensions.Http.Resilience`); if Gemini is slow, rate-limited, or
   unreachable, the Application layer falls back to a deterministic explanation instead of failing
   the request (`fallbackUsed: true` on the response).
10. **Domain-accurate Grant Eligibility Engine** ([GrantEligibilityEngine.cs](UstazAI.Domain/Services/GrantEligibilityEngine.cs), §12):
    models Kazakhstan's real three-tier admission scoring system — state threshold (пороговый
    балл), university internal threshold (внутренний балл, only ever raised above the state
    floor), and a historical competitive cutoff *range* (проходной балл), not a single point — and
    dispatches strictly across the 5 real admission tracks a Kazakhstani applicant can actually be
    on: school-graduate ENT (`StandardEnt`), creative-exam programs (`CreativeExam`), a college
    graduate changing specialty (`ChangingSpecialty`, scored like a school graduate),
    continuing-specialty on a grant (`ContinuingSpecialtyGrant`, a reduced 70-point exam against
    its own threshold table), and continuing-specialty paid (`ContinuingSpecialtyPaid`, no exam —
    direct commission admission by documents). This is deliberately *not* flattened into a generic
    "exam type" enum, because the eligibility rules genuinely differ per track — collapsing them
    would silently produce wrong verdicts for college applicants. Every verdict carries a
    `GrantCompetitiveness` uncertainty interval (never a bare percentage) and an honest "unknown"
    result rather than a fabricated pass when a program has no threshold data on file yet. Covered
    by 12 unit tests exercising all 5 branches (`GrantEligibilityEngineTests`) plus end-to-end
    integration tests through the real intake → calculate → result flow
    (`ExamIntakeIntegrationTests`).
11. **Result-Aware AI Chat + deterministic Gap Analysis** ([GapAnalysisEngine.cs](UstazAI.Domain/Services/GapAnalysisEngine.cs), §13):
    the chat assistant is never a general-purpose open chatbot — every reply is grounded in a
    scoped JSON context built server-side from the caller's own already-computed eligibility
    verdicts and diagnostics, and the system prompt explicitly forbids citing any number that
    isn't already in that context. `GapAnalysisEngine` itself is pure computation (no AI call): it
    ranks the applicant's own exam subjects by unclaimed headroom (`MaxScore - Score`) against the
    program's university threshold, and `GeneratePrepPlanCommand` turns the top-ranked gaps into
    `SubjectPrep` `RoadmapTask` nodes — reusing the existing roadmap DAG/status machinery rather
    than forking a second task table — each carrying curated `ResourceLink`s from a fixed,
    code-reviewed `ResourceLinkCatalog` (never AI-generated, so a link a student is told to click
    can never be hallucinated).
12. **University Map & Environment Intelligence** ([IMapProvider.cs](UstazAI.Application/Common/Interfaces/IMapProvider.cs), §14):
    a swappable `IMapProvider` port (2GIS for Kazakhstan, OpenStreetMap fallback everywhere else,
    both usable with zero API keys) turns a university's coordinates into a real, clickable map
    link. Coordinates and `EnvironmentProfile` (cost of living, safety, climate, transit quality,
    international-student share) live on `University`, not per-program, so campuses sharing one
    location never duplicate data. Folded directly into the existing Comparison endpoint's
    response (`ProgramComparisonRowDto.CampusMap`) rather than shipped as a disconnected map
    screen — a judge comparing programs sees location/environment data in the same response, per
    the spec's explicit integration instruction. A university with no seeded map data returns
    honest nulls, never a fabricated (0,0) coordinate.

## Tech stack

- **.NET 10 / C# 13**, minimal APIs, native `Microsoft.AspNetCore.OpenApi` + Scalar UI
- **Clean Architecture**: `UstazAI.Domain` (framework-free) → `UstazAI.Application` (MediatR
  CQRS, FluentValidation, ports) → `UstazAI.Infrastructure` (EF Core, Gemini client, JWT/BCrypt)
  → `UstazAI` (API composition root)
- **SQL Server** (local SQL Server Express instance for dev, SQL Server 2022 container in Docker
  Compose) via **EF Core 10**, code-first migrations, JSON columns for value objects (interests,
  exam scores, provenance, uncertainty estimates)
- **Gemini** (`gemini-3.5-flash`) via a hand-rolled REST client behind
  `IAiReasoningService` (Application port) / `GeminiReasoningService` (Infrastructure), using
  Gemini's controlled generation (`responseSchema`) — structured JSON only, never regex-parsed
  free text. Built to compose with `Microsoft.Extensions.AI.Abstractions`' `IChatClient` shape but
  implemented directly against the REST API for reliability (no dependency on an unofficial
  community connector package).
- **MediatR** for CQRS + pipeline behaviors (validation, logging, AI-guardrail sanitization)
- **FluentValidation**, **BCrypt.Net-Next**, **System.IdentityModel.Tokens.Jwt** (custom JWT
  issuance — not full ASP.NET Core Identity, see Known Limitations)
- **Serilog** (console + rolling file sink)
- **xUnit + FluentAssertions + NSubstitute + WebApplicationFactory**

## Running it

### Option A — local (SQL Server Express + `dotnet run`)

By default `appsettings.json` points at a local named SQL Server Express instance:
`Server=localhost\SQLEXPRESS;Database=UztazAIDb;...`. Adjust the `ConnectionStrings:Default`
value (or override it via `dotnet user-secrets` / an environment variable) if your instance name
or edition differs.

```bash
cd UstazAI
dotnet user-secrets set "Jwt:SigningKey" "<a long random string>"
dotnet user-secrets set "Gemini:ApiKey" "<your Gemini API key>"   # optional — app degrades gracefully without it
dotnet run
```

Then open `https://localhost:<port>/scalar/v1` for interactive API docs, or `GET
/api/v1/system/health`.

### Option B — Docker Compose (judge fallback, no local SQL Server needed)

```bash
cp .env.example .env   # fill in JWT_SIGNING_KEY
docker compose up --build
```

API is then reachable at `http://localhost:8080`.

### Judge / demo account

Seeded automatically on first run:

- **Email:** `judge@ustazai.demo`
- **Password:** `JudgePass123!`
- **Role:** `Judge` — the only role allowed to call `POST /api/v1/system/demo-reset`

### Suggested test script

1. `POST /auth/login` with the judge account.
2. `POST /system/demo-reset` → get a fresh demo `StudentProfile`.
3. `POST /profile/{id}/diagnostics`, `POST /profile/{id}/recommendations` (≥3 ranked results,
   each with a structured 4-factor "why it fits" + an uncertainty-quantified admission
   probability).
4. `POST /comparison` with two of the returned program ids.
5. `POST /profile/{id}/roadmap` with the top recommendation's program id (DAG of tasks with
   backward-calculated deadlines).
6. `GET /profile/{id}/next-action` → exactly one task.
7. `POST /profile` again with the **same `profileId`** but a different `budgetBand` → inspect
   `diff.changedFields` / `diff.likelyAffectedStages`.
8. `POST /profile/{id}/recommendations` again → `delta.addedProgramIds` /
   `removedProgramIds` show the visible, traceable change.

This exact flow is also encoded as an automated integration test:
[JourneyIntegrationTests.cs](UstazAI.Tests/JourneyIntegrationTests.cs).

## Data sources & disclosure

The seed catalog (`UstazAI.Infrastructure/Persistence/Seed/CatalogSeedData.cs`) lists ~23 real
universities across Kazakhstan, Russia, South Korea, Turkey, Poland, the Czech Republic, Germany,
the UK, the US, the UAE, Malaysia, Singapore and Canada, with ~31 programs. **Tuition, deadlines,
admit rates, scholarship coverage and starting salaries are approximate/illustrative figures for
demo purposes**, not scraped or verified live data — every row carries `DataProvenance.IsDemoData
= true`. Application deadlines are generated relative to "today" so the demo stays realistic
whenever a judge runs it. Historical admit archetypes (`AdmitArchetype`) are synthetic, seeded
per-program, and clearly not real applicant records.

The same seed data now also includes `AdmissionThreshold` rows (§12) for all 6 domestic Kazakhstan
programs — a state threshold, a university internal threshold, and a historical cutoff
min/max/median/sample-size per admission track (`StandardEnt` and `ContinuingSpecialtyGrant`).
These are illustrative figures, not scraped from a real university admissions office, and carry
the same `DataProvenance.IsDemoData = true` marker as the rest of the catalog. The demo profile's
seeded exam score (110/140) is deliberately chosen to clear some programs' cutoffs comfortably
while sitting in the competitive gray zone for the most selective one (Nazarbayev University CS),
so a live eligibility-calculation demo has something interesting to show.

`GeoCoordinates` and `EnvironmentProfile` (§14) are seeded for 8 of the 23 universities — the 3
Kazakhstan ones plus Seoul National University, Technical University of Munich, University of
Manchester, University of Toronto and Warsaw University of Technology. Coordinates are real
public city/campus-level locations; cost-of-living, safety, climate and transit figures are
illustrative estimates (`DataProvenance.IsDemoData = true`), not pulled from a live cost-of-living
API. `ResourceLinkCatalog` (§13) points at real, stable top-level domains (Khan Academy, the
Kazakhstan National Testing Center, IELTS/TOEFL/College Board/Duolingo's own sites) rather than
deep-linking to a specific article or video that could go stale — see the class's own doc comment
for the full disclosure.

## What's pre-built vs. built for this hackathon

Per the case's audit rule: the ASP.NET Core Web API template, EF Core/Serilog/MediatR/
FluentValidation package choices, and standard minimal-API/DI wiring patterns are conventional
framework usage, not a copied product template. All domain logic (hybrid scorer, diff engine,
guardrails, decision ledger, roadmap planner, what-if simulator, peer pathways, seed catalog,
Gemini prompt library and client) was written for this submission — see commit history.

## Known limitations / explicitly out of scope for this pass

To keep the mandatory core journey (§4) fully working and demo-ready rather than spreading effort
across every §5/§10 feature, the following were deliberately **not** built this round:

- **No SignalR push** — the "visibly changes" requirement is satisfied synchronously via the
  `diff`/`delta` fields returned directly from the mutation/recommendation calls (arguably easier
  for judges to test via Swagger/curl than a websocket demo would be).
- **No Hangfire background jobs** — recommendation/roadmap generation runs synchronously
  in-request; fast enough at this catalog size that a queue wasn't needed for the demo.
- **`CalculateEligibilityCommand` (§12) runs synchronously, not as the async job with SignalR
  progress the spec sketched** — same reasoning as the "no SignalR push" decision above: it's fast
  enough in-request against the current 6-program domestic catalog, and a synchronous
  `POST /exam-intake/calculate` returning the full verdict list is easier for judges to exercise
  via Swagger/curl than a websocket progress stream would be.
- **No full ASP.NET Core Identity** — a lightweight custom `User`/`RefreshToken` + BCrypt + JWT
  implementation instead, to keep auth simple and auditable.
- **Multi-agent "Admissions Committee" reasoning trace (§10.1), multimodal document OCR intake
  (§10.2), Gemini native function-calling (§10.5), fairness/affordability audit (§10.6),
  streaming SSE explanations (§10.7), and shareable parent/passport export (§10.8)** were not
  implemented — each is a substantial standalone feature and, per the case's own risk guidance, a
  half-built flagship feature during a live defense is worse than a rock-solid core journey.
- **Multi-tenant "Institution Mode" (§10.10)** — intentionally scoped to a schema-only reservation:
  `StudentProfile.OrganizationId` and `User.OrganizationId` are nullable, unused columns so the
  architecture can support it later without a breaking migration.
- `Microsoft.Extensions.AI.Abstractions` is referenced for interface-shape alignment, but the
  Gemini client talks to the REST API directly rather than through a published `IChatClient`
  connector package, for build reliability.
- **`GeneratePrepPlanCommand` (§13) runs synchronously, not as a background job** — same reasoning
  as the other two synchronous-by-design decisions above: it's pure deterministic computation over
  a small dataset, fast enough in-request.
- **`IMapProvider` (§14) is a deterministic, key-free URL builder, not a real geocoding/POI
  integration** — both 2GIS and OpenStreetMap support plain coordinate deep links with no API key,
  which is enough to make the map feature genuinely functional for a demo, but it is not the same
  as a real keyed 2GIS API integration (place search, richer POI data). The port is designed so a
  real implementation could swap in later without touching any Application handler.
- **Campus map data (§14) is seeded for a representative subset of universities, not the whole
  catalog** — all 3 Kazakhstan universities plus 5 international ones spanning East Asia, Europe
  and North America (see "Data sources & disclosure"), enough to demo the feature and the
  Comparison-screen integration across regions without hand-authoring coordinates/environment
  stats for all 23 seeded universities. A university without seeded map data returns honest nulls.

## Tests

```bash
dotnet test
```

57 tests, unit + integration:

- `HybridScoringEngineTests`, `ProfileDiffCalculatorTests`, `GuardrailRulesTests`, `DecisionLedgerTests` — core engine unit tests.
- `GrantEligibilityEngineTests` (§12) — all 5 admission-track branches: above/below/partial threshold, the reduced continuing-specialty table, the document-only paid path, missing-threshold honesty, uncertainty-is-always-an-interval.
- `GapAnalysisEngineTests` (§13) — headroom ranking, already-meets-threshold short-circuit, no-threshold-on-file, document-only track, zero-headroom exclusion.
- `DeterministicMapProviderTests` (§14) — 2GIS for Kazakhstan, OpenStreetMap fallback elsewhere.
- `JourneyIntegrationTests` (WebApplicationFactory, full journey + the profile-change → recommendation-delta hard requirement).
- `NewFeaturesIntegrationTests` (favorites, calendar/ICS export, scholarship search, essay review fallback, logout/token revocation).
- `ExamIntakeIntegrationTests` (§12) — the full intake → calculate → result flow for both the school/StandardEnt path and the college/ContinuingSpecialtyPaid path, plus track/education-stage mismatch validation and the calculate-without-intake conflict.
- `ClusterBIntegrationTests` (§13) — prep-plan generation/regeneration and chat send/history, both exercising the deterministic-fallback path (no Gemini key configured for the test host).
- `CampusMapIntegrationTests` (§14) — mapped vs. unmapped universities, and the Comparison-endpoint integration.

Integration tests need a reachable SQL Server instance (`UstazApiFactory` points at a dedicated
`UztazAIDb_IntegrationTests` database on `localhost\SQLEXPRESS` and a throwaway JWT key,
independent of your dev user-secrets).

## Sources consulted for this round's feature additions

- [Top AI & EdTech Tools for College Admissions in 2026 — Empowerly](https://empowerly.com/applications/top-ai-edtech-admissions-tools/)
- [Use of AI for College Admissions in 2026 — MyStrivePath](https://www.mystrivepath.com/ai-in-college-admissions-2026-guide)
- [Best AI College Counselor in 2026 — FindMyOrbit](https://www.findmyorbit.com/blog/best-ai-college-admissions-tools-2026)
- [Health Checks in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0)
- [How to Implement Refresh Tokens and Token Revocation in ASP.NET Core — antondevtips](https://antondevtips.com/blog/how-to-implement-refresh-tokens-and-token-revocation-in-aspnetcore)
