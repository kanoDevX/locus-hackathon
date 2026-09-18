import { CheckCircle2 } from "lucide-react";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { Button } from "@/components/ui/button";
import { EntryPreviewCard } from "@/components/journey/entry-preview-card";
import { LocaleSwitcher } from "@/components/layout/locale-switcher";
import { ThemeToggle } from "@/components/layout/theme-toggle";
import { getValueProposition } from "@/lib/api/onboarding";

export default async function EntryPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("entry");
  const common = await getTranslations("common");
  // The case brief's own journey table (§4) names this the "Entry" stage with its own endpoint —
  // fetched server-side (this is already an async Server Component) rather than hardcoded, so the
  // landing page reflects the same source of truth Judge Sandbox / API docs do. Falls back to the
  // static translation strings below if the API is unreachable — a public marketing page must
  // never hard-fail because a backend call didn't come back.
  const valueProp = await getValueProposition(locale);

  return (
    <main className="flex min-h-screen flex-col">
      <header className="flex items-center justify-between px-6 py-5 sm:px-10">
        <span className="text-base font-semibold tracking-tight">{common("appName")}</span>
        <div className="flex items-center gap-2">
          <LocaleSwitcher />
          <ThemeToggle />
        </div>
      </header>

      <section className="mx-auto flex w-full max-w-5xl flex-1 flex-col items-center justify-center gap-10 px-6 py-12 sm:flex-row sm:gap-16">
        <div className="max-w-xl text-center sm:text-left">
          <p className="text-sm font-medium text-[var(--brand-600)] dark:text-[var(--brand-300)]">{t("eyebrow")}</p>
          <h1 className="mt-3 text-3xl font-semibold leading-tight tracking-tight sm:text-4xl">{valueProp?.headline || t("headline")}</h1>
          <p className="mt-4 text-base leading-relaxed text-[var(--neutral-500)]">{valueProp?.body || t("body")}</p>

          {valueProp && valueProp.expectedOutput.length > 0 && (
            <ul className="mt-5 flex flex-col gap-2 text-left">
              {valueProp.expectedOutput.map((line) => (
                <li key={line} className="flex items-start gap-2 text-sm text-[var(--neutral-600)] dark:text-[var(--neutral-300)]">
                  <CheckCircle2 className="mt-0.5 size-4 shrink-0 text-[var(--success-500)]" />
                  {line}
                </li>
              ))}
            </ul>
          )}

          <div className="mt-8 flex flex-col items-center gap-3 sm:flex-row sm:items-start">
            <Button asChild size="lg">
              <Link href="/register">{t("cta")}</Link>
            </Button>
            <Button asChild variant="ghost" size="lg">
              <Link href="/login">{t("ctaLogin")}</Link>
            </Button>
          </div>
        </div>

        <div className="w-full max-w-sm">
          <p className="mb-2 text-center text-xs font-medium uppercase tracking-wide text-[var(--neutral-400)] sm:text-left">
            {t("previewLabel")}
          </p>
          <EntryPreviewCard />
        </div>
      </section>
    </main>
  );
}
