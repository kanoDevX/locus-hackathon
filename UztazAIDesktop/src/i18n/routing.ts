import { defineRouting } from "next-intl/routing";

/** Locale strategy: explicit URL prefix (/ru, /kk, /en) chosen over silent Accept-Language
 * detection alone — a judge switching languages mid-demo needs a visible, deterministic control,
 * and a prefixed URL is also directly shareable/bookmarkable. Russian is the default since it's
 * the most common shared language for the target Kazakhstani persona; Kazakh and English are
 * one tap away in the header switcher. */
export const routing = defineRouting({
  locales: ["ru", "kk", "en"],
  defaultLocale: "ru",
  localePrefix: "always",
});

export type AppLocale = (typeof routing.locales)[number];
