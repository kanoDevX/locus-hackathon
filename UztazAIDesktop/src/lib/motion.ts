
export const EASE_STANDARD = [0.4, 0, 0.2, 1] as const;
export const EASE_EMPHASIZED = [0.2, 0, 0, 1] as const;

export const DURATION_MICRO = 0.15;
export const DURATION_TRANSITION = 0.32;
export const DURATION_CELEBRATORY = 0.52;

export const fadeInUp = {
  hidden: { opacity: 0, y: 8 },
  visible: (i = 0) => ({
    opacity: 1,
    y: 0,
    transition: { duration: DURATION_TRANSITION, ease: EASE_STANDARD, delay: i * 0.05 },
  }),
};

export const fadeIn = {
  hidden: { opacity: 0 },
  visible: { opacity: 1, transition: { duration: DURATION_TRANSITION, ease: EASE_STANDARD } },
};

export const scaleIn = {
  hidden: { opacity: 0, scale: 0.96 },
  visible: { opacity: 1, scale: 1, transition: { duration: DURATION_TRANSITION, ease: EASE_EMPHASIZED } },
};

export const slideInFromRight = {
  hidden: { opacity: 0, x: 24 },
  visible: { opacity: 1, x: 0, transition: { duration: DURATION_TRANSITION, ease: EASE_EMPHASIZED } },
  exit: { opacity: 0, x: 24, transition: { duration: DURATION_MICRO, ease: EASE_STANDARD } },
};

export const celebratoryPop = {
  hidden: { opacity: 0, scale: 0.8 },
  visible: {
    opacity: 1,
    scale: 1,
    transition: { duration: DURATION_CELEBRATORY, ease: EASE_EMPHASIZED, type: "spring" as const, bounce: 0.4 },
  },
};

export const diffHighlight = {
  initial: { boxShadow: "0 0 0 0px var(--brand-400)" },
  animate: {
    boxShadow: [
      "0 0 0 3px var(--brand-400)",
      "0 0 0 3px var(--brand-400)",
      "0 0 0 0px var(--brand-400)",
    ],
    transition: { duration: 1.6, times: [0, 0.6, 1], ease: EASE_STANDARD },
  },
};
