// Server-only: called from the "Entry" landing page (a Server Component), never from client code
// — unlike everything in api/client.ts, this fetch runs during SSR, has no auth concerns (the
// endpoint is AllowAnonymous), and doesn't need TanStack Query since it's fetched once per render
// rather than re-fetched/cached client-side.

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5087";

export interface ValuePropositionDto {
  locale: string;
  headline: string;
  body: string;
  expectedOutput: string[];
}

/** Returns null (never throws) on any failure — a public marketing page must render its static
 * fallback copy rather than hard-fail because the API was briefly unreachable. */
export async function getValueProposition(locale: string): Promise<ValuePropositionDto | null> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/v1/onboarding/value-proposition?locale=${encodeURIComponent(locale)}`, {
      cache: "no-store",
    });
    if (!response.ok) return null;
    const data = await response.json();
    if (!data?.headline || !data?.body) return null;
    return {
      locale: data.locale ?? locale,
      headline: data.headline,
      body: data.body,
      expectedOutput: Array.isArray(data.expectedOutput) ? data.expectedOutput : [],
    };
  } catch {
    return null;
  }
}
