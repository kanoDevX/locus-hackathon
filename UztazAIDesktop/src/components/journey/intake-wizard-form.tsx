"use client";

import { useEffect, useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import type { z } from "zod";
import { motion, AnimatePresence } from "framer-motion";
import { Plus, Trash2, School, GraduationCap, Award, Check, Repeat, ArrowRightLeft, Coins, Landmark, Shuffle, FileCheck2, PlaneTakeoff } from "lucide-react";
import { toast } from "sonner";
import { useRouter } from "@/i18n/navigation";
import {
  intakeWizardSchema,
  WIZARD_STEP_FIELDS,
  WIZARD_TOTAL_STEPS,
  WIZARD_DRAFT_STORAGE_KEY,
  type IntakeWizardFormValues,
} from "@/lib/validation/intake-wizard-schema";
import { isCollegeStage as isCollegeStageOf, TRACK_MAX_SCORE, supplementaryExamTypes } from "@/lib/validation/exam-intake-schema";
import type { EducationStage, FundingTrackPreference, ProfileDto } from "@/lib/api/types";
import { useSaveProfile, useProfile } from "@/lib/api/profile";
import { useSubmitExamIntake } from "@/lib/api/examIntake";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useJourneyStore } from "@/lib/stores/journey-store";
import { ApiError } from "@/lib/api/client";
import { Button } from "@/components/ui/button";
import { Input, Label } from "@/components/ui/input";
import { NumberStepper } from "@/components/ui/number-stepper";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { TagInput } from "@/components/journey/tag-input";
import { fadeInUp } from "@/lib/motion";
import { cn } from "@/lib/utils";

const EDUCATION_STAGE_ICON: Record<EducationStage, React.ElementType> = {
  SchoolGrade9: School,
  SchoolGrade10: School,
  SchoolGrade11: School,
  CollegeStudent: GraduationCap,
  CollegeGraduate: Award,
};

const FUNDING_ICON: Record<FundingTrackPreference, React.ElementType> = {
  GrantOnly: Landmark,
  PaidOnly: Coins,
  Flexible: Shuffle,
};

const DEFAULT_VALUES: IntakeWizardFormValues = {
  fullName: "",
  grade: 11,
  age: 17,
  preferredLanguage: "Ru",
  interests: [],
  gpa: undefined,
  examScores: [],
  targetCountries: [],
  budgetBand: "Medium",
  timelineMonthsToApplication: 10,
  constraints: [],
  educationStage: "SchoolGrade11",
  track: "StandardEnt",
  subjectBreakdown: [],
  totalScore: undefined,
  examDateTaken: null,
  collegeBackground: null,
  supplementaryExams: [],
  fundingTrackPreference: "Flexible",
};

/**
 * `track` has no field of its own on `ProfileDto` — it only ever gets set by the wizard's own
 * interactive handlers (`onSelectStage`, `onSelectSpecialtyContinuity`, ...), never by loading an
 * existing profile. A retaking college-stage student whose `educationStage`/`collegeBackground`
 * already came back from the server therefore started with `track` stuck at
 * `DEFAULT_VALUES.track` ("StandardEnt") — a school track invalid for a college stage — unless
 * they happened to re-click every derivation step in order. That produced a cross-field
 * validation error on `track` when advancing past the path step, and because `track` is chosen
 * via `ChoiceCard` (which never renders a field error, unlike `Field`-wrapped inputs), "Next"
 * would just silently do nothing. Deriving `track` the same way the interactive handlers would
 * have, right when the profile loads, keeps the form internally consistent from the start.
 */
function deriveTrackFromProfile(profile: ProfileDto): IntakeWizardFormValues["track"] {
  const stage = profile.educationStage ?? "SchoolGrade11";
  if (!isCollegeStageOf(stage)) return "StandardEnt";
  if (!profile.collegeBackground?.targetSpecialtyMatchesCollegeSpecialty) return "ChangingSpecialty";
  return profile.fundingTrackPreference === "PaidOnly" ? "ContinuingSpecialtyPaid" : "ContinuingSpecialtyGrant";
}

/**
 * Placement & Eligibility Intake Wizard (§12 Addendum 2) — the product's literal front door.
 * Replaces the old flat Profile step: collects the same base profile fields (still needed by
 * HybridScoringEngine/Diagnostics downstream) PLUS the branching exam-intake fields, in one
 * wizard, then submits both `POST /profile` and `POST /exam-intake` before handing off to the
 * staged eligibility-calculation screen. The step indicator itself lives in the focused
 * `OnboardingHeader` (journey/layout.tsx), driven by `journeyStore.wizardProgress` set below —
 * this component owns only the form.
 */
export function IntakeWizardForm() {
  const t = useTranslations("intake");
  const tCommon = useTranslations("common");
  // Zod messages in exam-intake-schema.ts are stable keys (e.g. "totalScoreRequired"), not raw
  // English prose — translate here before ever handing one to a <Field error=...>. A user
  // reported seeing "A total score is required for this track." verbatim in a Russian UI before
  // this existed; keeping the keys and this one translation point makes that class of bug
  // impossible to reintroduce by accident.
  function fieldError(message: string | undefined): string | undefined {
    return message ? t(`errors.${message}` as Parameters<typeof t>[0]) : undefined;
  }
  const router = useRouter();
  const activeProfileId = useAuthStore((s) => s.activeProfileId);
  const { data: existingProfile } = useProfile(activeProfileId);
  const saveProfile = useSaveProfile();
  const submitExamIntake = useSubmitExamIntake(activeProfileId);
  const showDiffPanel = useJourneyStore((s) => s.showDiffPanel);
  const setWizardProgress = useJourneyStore((s) => s.setWizardProgress);

  const [step, setStep] = useState(0);

  const form = useForm<z.input<typeof intakeWizardSchema>, unknown, IntakeWizardFormValues>({
    resolver: zodResolver(intakeWizardSchema),
    defaultValues: DEFAULT_VALUES,
    mode: "onBlur",
  });

  useEffect(() => {
    if (existingProfile) {
      form.reset({
        ...DEFAULT_VALUES,
        fullName: existingProfile.fullName,
        grade: existingProfile.grade,
        age: existingProfile.age,
        preferredLanguage: existingProfile.preferredLanguage,
        interests: existingProfile.interests,
        gpa: existingProfile.gpa ?? undefined,
        targetCountries: existingProfile.targetCountries,
        budgetBand: existingProfile.budgetBand,
        timelineMonthsToApplication: existingProfile.timelineMonthsToApplication,
        constraints: existingProfile.constraints,
        educationStage: existingProfile.educationStage ?? "SchoolGrade11",
        track: deriveTrackFromProfile(existingProfile),
        fundingTrackPreference: existingProfile.fundingTrackPreference,
        collegeBackground: existingProfile.collegeBackground,
      });
      return;
    }
    const raw = typeof window !== "undefined" ? window.localStorage.getItem(WIZARD_DRAFT_STORAGE_KEY) : null;
    if (raw) {
      try {
        form.reset(JSON.parse(raw));
      } catch {
        /* ignore corrupt draft */
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [existingProfile]);

  useEffect(() => {
    const sub = form.watch((values) => {
      window.localStorage.setItem(WIZARD_DRAFT_STORAGE_KEY, JSON.stringify(values));
    });
    return () => sub.unsubscribe();
  }, [form]);

  // `goNext` validates by calling `form.trigger()` directly, never `form.handleSubmit()` — this
  // is a multi-step wizard, not a single submit. React Hook Form's default reValidateMode
  // ("onChange", meant to clear a field's error as soon as it's fixed) is gated on
  // `formState.isSubmitted`, which only `handleSubmit` ever sets — so it never fires here, and an
  // error `trigger()` surfaced stays on screen verbatim even after the user corrects the value. A
  // user reported exactly this: entered a valid total score, the "score is required" message
  // never went away until they clicked Next again. Once a step has been validated at least once
  // (formState.errors is non-empty), re-run that step's validation on every further edit to its
  // own fields so a fixed field's error clears immediately instead of looking stuck.
  useEffect(() => {
    const sub = form.watch((_values, { name }) => {
      if (!name || Object.keys(form.formState.errors).length === 0) return;
      const stepFields = WIZARD_STEP_FIELDS[step];
      if (stepFields.some((f) => name === f || name.startsWith(`${f}.`))) {
        form.trigger(stepFields);
      }
    });
    return () => sub.unsubscribe();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [step]);

  // Drives the top-of-viewport "Step X of Y" indicator in OnboardingHeader — cleared on unmount
  // so it never lingers once the wizard is no longer mounted (e.g. after a successful submit).
  useEffect(() => {
    setWizardProgress({ step, totalSteps: WIZARD_TOTAL_STEPS });
    return () => setWizardProgress(null);
  }, [step, setWizardProgress]);

  const educationStage = form.watch("educationStage");
  const track = form.watch("track");
  const collegeBackground = form.watch("collegeBackground");
  const subjectBreakdown = form.watch("subjectBreakdown");
  const supplementaryExams = form.watch("supplementaryExams");
  const isCollege = isCollegeStageOf(educationStage);
  // Both tracks have no Kazakhstan exam score to collect — ContinuingSpecialtyPaid (direct
  // admission by documents) and NotTakingEnt (applying abroad only, so the ENT/grant system is
  // inapplicable by definition, not merely unmet). They still get their own distinct explainer
  // text on the scores step since the reason is different for each.
  const isScoreless = track === "ContinuingSpecialtyPaid" || track === "NotTakingEnt";
  const isNotTakingEnt = track === "NotTakingEnt";
  const continuingChoiceMade = collegeBackground ? collegeBackground.targetSpecialtyMatchesCollegeSpecialty : null;

  const DEFAULT_COLLEGE_BACKGROUND = {
    collegeSpecialtyName: "",
    diplomaAverageScore: undefined,
    graduationYear: new Date().getFullYear(),
    targetSpecialtyMatchesCollegeSpecialty: false,
  } as const;

  function selectNotTakingEnt() {
    // Deliberately does NOT touch collegeBackground. An earlier version nulled it out here (it's
    // KZ-college-specific data that no longer applies once you're opting out of that system) —
    // but for a college-stage student that also hid the *entire* specialty-continuity section
    // (gated on `collegeBackground` being non-null), including the "same"/"different specialty"
    // cards needed to switch back — a real bug: a user picked this and then had no way to undo
    // it. Leaving collegeBackground as-is keeps every option visible and clickable at once;
    // `isNotTakingEnt` alone (not collegeBackground's presence) now drives what's required.
    form.setValue("track", "NotTakingEnt");
    form.setValue("fundingTrackPreference", "Flexible");
  }

  function onSelectStage(stage: EducationStage) {
    const nowCollege = isCollegeStageOf(stage);
    form.setValue("educationStage", stage);
    form.setValue("track", nowCollege ? "ChangingSpecialty" : "StandardEnt");
    if (nowCollege) {
      form.setValue(
        "collegeBackground",
        form.getValues("collegeBackground") ?? {
          collegeSpecialtyName: "",
          diplomaAverageScore: undefined,
          graduationYear: new Date().getFullYear(),
          targetSpecialtyMatchesCollegeSpecialty: false,
        }
      );
      form.setValue("grade", 12);
    } else {
      form.setValue("collegeBackground", null);
      form.setValue("grade", stage === "SchoolGrade9" ? 9 : stage === "SchoolGrade10" ? 10 : 11);
    }
  }

  // Two plain-language questions replace what used to be a track dropdown kept manually in sync
  // with a separate "matches" switch — the two controls could silently disagree with each other.
  // Now the track is always *derived* from the answers, never picked independently of them.
  function onSelectSpecialtyContinuity(matches: boolean) {
    // A student can reach here after having picked "Not taking the ENT" first, which clears
    // collegeBackground entirely (selectNotTakingEnt above) — re-initialize it rather than
    // setting a nested path on null, same lazy-init onSelectStage already does.
    if (!form.getValues("collegeBackground")) {
      form.setValue("collegeBackground", { ...DEFAULT_COLLEGE_BACKGROUND, targetSpecialtyMatchesCollegeSpecialty: matches });
    } else {
      form.setValue("collegeBackground.targetSpecialtyMatchesCollegeSpecialty", matches);
    }
    if (!matches) {
      form.setValue("track", "ChangingSpecialty");
    } else {
      form.setValue("track", "ContinuingSpecialtyGrant");
      form.setValue("fundingTrackPreference", "Flexible");
    }
  }

  function onSelectContinuingFunding(wantsGrant: boolean) {
    const nextTrack = wantsGrant ? "ContinuingSpecialtyGrant" : "ContinuingSpecialtyPaid";
    form.setValue("track", nextTrack);
    if (!wantsGrant) form.setValue("fundingTrackPreference", "PaidOnly");
  }

  async function goNext() {
    const valid = await form.trigger(WIZARD_STEP_FIELDS[step]);
    if (!valid) {
      // Step 0 (education stage) and step 2 (track / funding / college background) are all
      // chosen via ChoiceCard, which — unlike a Field-wrapped input — never renders its own error
      // message. Without this, a validation failure there left "Next" looking like it silently
      // did nothing (see deriveTrackFromProfile's doc comment for the bug this most often masked).
      if (step === 0 || step === 2) toast.error(t("stepValidationError"));
      return;
    }
    if (step < WIZARD_TOTAL_STEPS - 1) setStep((s) => s + 1);
    else await onSubmit();
  }

  function goBack() {
    setStep((s) => Math.max(0, s - 1));
  }

  async function onSubmit() {
    const values = intakeWizardSchema.parse(form.getValues());
    try {
      const profileResult = await saveProfile.mutateAsync({
        profileId: activeProfileId,
        fullName: values.fullName,
        grade: values.grade,
        age: values.age,
        preferredLanguage: values.preferredLanguage,
        interests: values.interests,
        gpa: values.gpa ?? null,
        examScores: values.supplementaryExams.map((e) => ({ examType: e.examType, score: e.score, maxScore: e.maxScore, dateTaken: e.dateTaken ?? null })),
        targetCountries: values.targetCountries,
        budgetBand: values.budgetBand,
        timelineMonthsToApplication: values.timelineMonthsToApplication,
        constraints: values.constraints,
      });
      if (profileResult.diff) showDiffPanel({ kind: "profile", profileDiff: profileResult.diff });

      await submitExamIntake.mutateAsync({
        educationStage: values.educationStage,
        track: values.track,
        subjectBreakdown: values.subjectBreakdown,
        totalScore: values.track === "ContinuingSpecialtyPaid" ? null : (values.totalScore ?? null),
        examDateTaken: values.examDateTaken ?? null,
        collegeBackground: values.collegeBackground
          ? {
              collegeSpecialtyName: values.collegeBackground.collegeSpecialtyName,
              diplomaAverageScore: values.collegeBackground.diplomaAverageScore ?? null,
              graduationYear: values.collegeBackground.graduationYear ?? null,
              targetSpecialtyMatchesCollegeSpecialty: values.collegeBackground.targetSpecialtyMatchesCollegeSpecialty,
            }
          : null,
        supplementaryExams: values.supplementaryExams.map((e) => ({
          examType: e.examType,
          score: e.score,
          maxScore: e.maxScore,
          dateTaken: e.dateTaken ?? null,
        })),
        fundingTrackPreference: values.fundingTrackPreference,
      });

      window.localStorage.removeItem(WIZARD_DRAFT_STORAGE_KEY);
      router.push("/journey/eligibility-result");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("submitFailed"));
    }
  }

  const isSubmitting = saveProfile.isPending || submitExamIntake.isPending;

  return (
    <div className="mx-auto max-w-xl">
      <Card>
        <AnimatePresence mode="wait">
          <motion.div key={step} variants={fadeInUp} initial="hidden" animate="visible">
            {step === 0 && (
              <>
                <CardHeader>
                  <CardTitle>{t("stepStage")}</CardTitle>
                  <CardDescription>{t("stepStageHint")}</CardDescription>
                </CardHeader>
                <CardContent className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  {(["SchoolGrade9", "SchoolGrade10", "SchoolGrade11", "CollegeStudent", "CollegeGraduate"] as EducationStage[]).map((stage) => (
                    <ChoiceCard
                      key={stage}
                      icon={EDUCATION_STAGE_ICON[stage]}
                      title={t(`stage.${stage}`)}
                      hint={t(`stage.${stage}Hint`)}
                      selected={educationStage === stage}
                      onClick={() => onSelectStage(stage)}
                    />
                  ))}
                </CardContent>
              </>
            )}

            {step === 1 && (
              <>
                <CardHeader>
                  <CardTitle>{t("stepBasics")}</CardTitle>
                  <CardDescription>{t("stepBasicsHint")}</CardDescription>
                </CardHeader>
                <CardContent className="flex flex-col gap-4">
                  <Field label={t("fullName")} error={form.formState.errors.fullName?.message}>
                    <Input {...form.register("fullName")} placeholder={t("fullNamePlaceholder")} autoFocus />
                  </Field>
                  <div className="grid grid-cols-2 gap-4">
                    {!isCollege && (
                      <Field label={t("grade")} error={form.formState.errors.grade?.message}>
                        <Controller
                          control={form.control}
                          name="grade"
                          render={({ field }) => (
                            <NumberStepper value={field.value as number | undefined} onChange={(v) => field.onChange(v)} min={9} max={11} />
                          )}
                        />
                      </Field>
                    )}
                    <Field label={t("age")} error={form.formState.errors.age?.message}>
                      <Controller
                        control={form.control}
                        name="age"
                        render={({ field }) => (
                          <NumberStepper value={field.value as number | undefined} onChange={(v) => field.onChange(v)} min={10} max={30} />
                        )}
                      />
                    </Field>
                  </div>
                  <Field label={t("language")}>
                    <Controller
                      control={form.control}
                      name="preferredLanguage"
                      render={({ field }) => (
                        <Select value={field.value} onValueChange={field.onChange}>
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="Ru">Русский</SelectItem>
                            <SelectItem value="Kk">Қазақша</SelectItem>
                            <SelectItem value="En">English</SelectItem>
                          </SelectContent>
                        </Select>
                      )}
                    />
                  </Field>
                </CardContent>
              </>
            )}

            {step === 2 && (
              <>
                <CardHeader>
                  <CardTitle>{t("stepPath")}</CardTitle>
                  <CardDescription>{t("stepPathHint")}</CardDescription>
                </CardHeader>
                <CardContent className="flex flex-col gap-5">
                  {!isCollege && (
                    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                      <ChoiceCard
                        icon={School}
                        title={t("trackLabel.StandardEnt")}
                        hint={t("trackHint.StandardEnt")}
                        selected={track === "StandardEnt"}
                        onClick={() => form.setValue("track", "StandardEnt")}
                      />
                      <ChoiceCard
                        icon={Award}
                        title={t("trackLabel.CreativeExam")}
                        hint={t("trackHint.CreativeExam")}
                        selected={track === "CreativeExam"}
                        onClick={() => form.setValue("track", "CreativeExam")}
                      />
                      <ChoiceCard
                        icon={PlaneTakeoff}
                        title={t("trackLabel.NotTakingEnt")}
                        hint={t("trackHint.NotTakingEnt")}
                        selected={isNotTakingEnt}
                        onClick={selectNotTakingEnt}
                      />
                    </div>
                  )}

                  {isCollege && collegeBackground && (
                    <>
                      {!isNotTakingEnt && (
                        <div className="flex flex-col gap-3">
                          <Field label={t("collegeSpecialtyName")} error={fieldError(form.formState.errors.collegeBackground?.collegeSpecialtyName?.message)}>
                            <Input {...form.register("collegeBackground.collegeSpecialtyName")} placeholder={t("collegeSpecialtyPlaceholder")} autoFocus />
                          </Field>
                          <div className="grid grid-cols-2 gap-4">
                            <Field label={t("diplomaAverageScore")}>
                              <Controller
                                control={form.control}
                                name="collegeBackground.diplomaAverageScore"
                                render={({ field }) => (
                                  <NumberStepper
                                    value={(field.value as number | null | undefined) ?? undefined}
                                    onChange={field.onChange}
                                    min={0}
                                    max={5}
                                    step={0.01}
                                  />
                                )}
                              />
                            </Field>
                            <Field label={t("graduationYear")}>
                              <Controller
                                control={form.control}
                                name="collegeBackground.graduationYear"
                                render={({ field }) => (
                                  <NumberStepper
                                    value={(field.value as number | null | undefined) ?? undefined}
                                    onChange={field.onChange}
                                    min={2000}
                                    max={2100}
                                  />
                                )}
                              />
                            </Field>
                          </div>
                        </div>
                      )}

                      <div>
                        <p className="mb-2 text-sm font-medium">{t("specialtyContinuityQuestion")}</p>
                        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                          <ChoiceCard
                            icon={Repeat}
                            title={t("specialtyContinuitySame")}
                            hint={t("specialtyContinuitySameHint")}
                            selected={!isNotTakingEnt && continuingChoiceMade === true}
                            onClick={() => onSelectSpecialtyContinuity(true)}
                          />
                          <ChoiceCard
                            icon={ArrowRightLeft}
                            title={t("specialtyContinuityDifferent")}
                            hint={t("specialtyContinuityDifferentHint")}
                            selected={!isNotTakingEnt && continuingChoiceMade === false}
                            onClick={() => onSelectSpecialtyContinuity(false)}
                          />
                          {/* A student can also opt entirely out of Kazakhstan's college-continuation
                              system — living right alongside "same"/"different" rather than behind a
                              separate toggle keeps every option one click away from every other one.
                              Nulling collegeBackground here to hide this whole section (the earlier
                              approach) trapped a student who picked it: with the section gone, there
                              was no way back to "same"/"different" either — a real bug reported by a
                              user who clicked it and then "couldn't undo". */}
                          <ChoiceCard
                            icon={PlaneTakeoff}
                            title={t("trackLabel.NotTakingEnt")}
                            hint={t("trackHint.NotTakingEntCollege")}
                            selected={isNotTakingEnt}
                            onClick={selectNotTakingEnt}
                          />
                        </div>
                      </div>

                      {!isNotTakingEnt && continuingChoiceMade === true && (
                        <div>
                          <p className="mb-2 text-sm font-medium">{t("continuingFundingQuestion")}</p>
                          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                            <ChoiceCard
                              icon={Landmark}
                              title={t("trackLabel.ContinuingSpecialtyGrant")}
                              hint={t("trackHint.ContinuingSpecialtyGrant")}
                              selected={track === "ContinuingSpecialtyGrant"}
                              onClick={() => onSelectContinuingFunding(true)}
                            />
                            <ChoiceCard
                              icon={FileCheck2}
                              title={t("trackLabel.ContinuingSpecialtyPaid")}
                              hint={t("trackHint.ContinuingSpecialtyPaid")}
                              selected={track === "ContinuingSpecialtyPaid"}
                              onClick={() => onSelectContinuingFunding(false)}
                            />
                          </div>
                        </div>
                      )}
                    </>
                  )}

                  {!isScoreless && (
                    <div>
                      <p className="mb-2 text-sm font-medium">{t("fundingTrackPreference")}</p>
                      <div className="grid grid-cols-3 gap-2">
                        {(["GrantOnly", "PaidOnly", "Flexible"] as FundingTrackPreference[]).map((f) => (
                          <ChoiceCard
                            key={f}
                            compact
                            icon={FUNDING_ICON[f]}
                            title={t(`funding.${f}`)}
                            selected={form.watch("fundingTrackPreference") === f}
                            onClick={() => form.setValue("fundingTrackPreference", f)}
                          />
                        ))}
                      </div>
                    </div>
                  )}
                  {track === "ContinuingSpecialtyPaid" && (
                    <p className="rounded-[var(--radius-md)] border border-[var(--info-100)] bg-[var(--info-100)]/40 p-3 text-xs text-[var(--neutral-500)]">
                      {t("fundingForcedPaidHint")}
                    </p>
                  )}
                  {track === "NotTakingEnt" && (
                    <p className="rounded-[var(--radius-md)] border border-[var(--info-100)] bg-[var(--info-100)]/40 p-3 text-xs text-[var(--neutral-500)]">
                      {t("fundingNotApplicableHint")}
                    </p>
                  )}
                </CardContent>
              </>
            )}

            {step === 3 && (
              <>
                <CardHeader>
                  <CardTitle>{t("stepScores")}</CardTitle>
                  <CardDescription>
                    {track === "ContinuingSpecialtyPaid"
                      ? t("stepScoresDocumentOnlyHint")
                      : track === "NotTakingEnt"
                        ? t("stepScoresNotTakingEntHint")
                        : t("stepScoresHint")}
                  </CardDescription>
                </CardHeader>
                <CardContent className="flex flex-col gap-4">
                  {/* An applicant — school or college stage — can reach this step already on a
                      scored track (StandardEnt/ChangingSpecialty/etc. are the defaults, and the
                      "Not taking the ENT" choice on the previous step is easy to miss) and then
                      get stuck on a required-score field with no visible way out — a real report
                      from a user who asked "isn't this about the ENT? then why is there no button
                      here [to say I'm not taking it]" — and who hit it again on the *college*
                      path specifically, which this button didn't originally cover. */}
                  {!isScoreless && (
                    <button
                      type="button"
                      onClick={() => {
                        selectNotTakingEnt();
                        form.setValue("totalScore", undefined);
                      }}
                      className="flex items-center gap-1.5 self-start text-xs font-medium text-[var(--brand-600)] hover:underline dark:text-[var(--brand-300)]"
                    >
                      <PlaneTakeoff className="size-3.5" />
                      {t("notTakingEntEscapeHatch")}
                    </button>
                  )}
                  {track === "ContinuingSpecialtyPaid" && (
                    <div className="flex items-start gap-2.5 rounded-[var(--radius-md)] border border-[var(--info-100)] bg-[var(--info-100)]/40 p-4 text-sm">
                      <FileCheck2 className="mt-0.5 size-4 shrink-0 text-[var(--info-500)]" />
                      {t("documentOnlyExplainer")}
                    </div>
                  )}
                  {track === "NotTakingEnt" && (
                    <div className="flex items-start gap-2.5 rounded-[var(--radius-md)] border border-[var(--info-100)] bg-[var(--info-100)]/40 p-4 text-sm">
                      <PlaneTakeoff className="mt-0.5 size-4 shrink-0 text-[var(--info-500)]" />
                      {t("notTakingEntExplainer")}
                    </div>
                  )}
                  {!isScoreless && (
                    <>
                      <Field
                        label={t("totalScore")}
                        hint={t("totalScoreHint", { max: TRACK_MAX_SCORE[track as keyof typeof TRACK_MAX_SCORE] ?? 140 })}
                        error={fieldError(form.formState.errors.totalScore?.message)}
                      >
                        <Controller
                          control={form.control}
                          name="totalScore"
                          render={({ field }) => (
                            <NumberStepper
                              value={(field.value as number | null | undefined) ?? undefined}
                              onChange={field.onChange}
                              min={0}
                              max={TRACK_MAX_SCORE[track as keyof typeof TRACK_MAX_SCORE] ?? 140}
                              step={0.5}
                            />
                          )}
                        />
                      </Field>

                      <div className="flex flex-col gap-3">
                        <Label>{t("subjectBreakdownLabel")}</Label>
                        {subjectBreakdown.map((_, index) => (
                          <div key={index} className="flex flex-col gap-2 rounded-[var(--radius-md)] border border-[var(--border-subtle)] p-3 sm:flex-row sm:items-end">
                            <Field
                              label={t("subjectName")}
                              className="flex-1"
                              error={fieldError(form.formState.errors.subjectBreakdown?.[index]?.subjectName?.message)}
                            >
                              <Input {...form.register(`subjectBreakdown.${index}.subjectName`)} placeholder={t("subjectNamePlaceholder")} />
                            </Field>
                            <Field
                              label={t("subjectScore")}
                              className="w-full sm:w-32"
                              error={fieldError(form.formState.errors.subjectBreakdown?.[index]?.score?.message)}
                            >
                              <Controller
                                control={form.control}
                                name={`subjectBreakdown.${index}.score`}
                                render={({ field }) => (
                                  <NumberStepper value={field.value as number | undefined} onChange={(v) => field.onChange(v ?? 0)} min={0} max={100} />
                                )}
                              />
                            </Field>
                            <Field label={t("subjectMax")} className="w-full sm:w-32">
                              <Controller
                                control={form.control}
                                name={`subjectBreakdown.${index}.maxScore`}
                                render={({ field }) => (
                                  <NumberStepper value={field.value as number | undefined} onChange={(v) => field.onChange(v ?? 1)} min={1} max={100} />
                                )}
                              />
                            </Field>
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon"
                              aria-label={t("removeSubject")}
                              onClick={() => form.setValue("subjectBreakdown", subjectBreakdown.filter((_, i) => i !== index))}
                            >
                              <Trash2 className="size-4 text-[var(--danger-500)]" />
                            </Button>
                          </div>
                        ))}
                        <Button
                          type="button"
                          variant="secondary"
                          size="sm"
                          onClick={() => form.setValue("subjectBreakdown", [...subjectBreakdown, { subjectName: "", score: 0, maxScore: 35 }])}
                        >
                          <Plus className="size-3.5" />
                          {t("addSubject")}
                        </Button>
                      </div>
                    </>
                  )}
                </CardContent>
              </>
            )}

            {step === 4 && (
              <>
                <CardHeader>
                  <CardTitle>{t("stepGoals")}</CardTitle>
                  <CardDescription>{t("stepGoalsHint")}</CardDescription>
                </CardHeader>
                <CardContent className="flex flex-col gap-4">
                  <Field label={t("interests")} error={form.formState.errors.interests?.message}>
                    <Controller
                      control={form.control}
                      name="interests"
                      render={({ field }) => <TagInput value={field.value} onChange={field.onChange} placeholder={t("interestsPlaceholder")} />}
                    />
                  </Field>
                  <Field label={t("targetCountries")} error={form.formState.errors.targetCountries?.message}>
                    <Controller
                      control={form.control}
                      name="targetCountries"
                      render={({ field }) => <TagInput value={field.value} onChange={field.onChange} placeholder={t("targetCountriesPlaceholder")} />}
                    />
                  </Field>

                  <div className="flex flex-col gap-3">
                    <Label>{t("supplementaryExams")}</Label>
                    {supplementaryExams.map((_, index) => (
                      <div key={index} className="flex flex-col gap-2 rounded-[var(--radius-md)] border border-[var(--border-subtle)] p-3 sm:flex-row sm:items-end">
                        <Field label={t("examType")} className="flex-1">
                          <Controller
                            control={form.control}
                            name={`supplementaryExams.${index}.examType`}
                            render={({ field }) => (
                              <Select value={field.value} onValueChange={field.onChange}>
                                <SelectTrigger>
                                  <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                  {supplementaryExamTypes.map((et) => (
                                    <SelectItem key={et} value={et}>
                                      {et}
                                    </SelectItem>
                                  ))}
                                </SelectContent>
                              </Select>
                            )}
                          />
                        </Field>
                        <Field label={t("examScore")} className="w-full sm:w-32">
                          <Controller
                            control={form.control}
                            name={`supplementaryExams.${index}.score`}
                            render={({ field }) => (
                              <NumberStepper value={field.value as number | undefined} onChange={(v) => field.onChange(v ?? 0)} min={0} max={1600} step={0.5} />
                            )}
                          />
                        </Field>
                        <Field label={t("examMax")} className="w-full sm:w-32">
                          <Controller
                            control={form.control}
                            name={`supplementaryExams.${index}.maxScore`}
                            render={({ field }) => (
                              <NumberStepper value={field.value as number | undefined} onChange={(v) => field.onChange(v ?? 1)} min={1} max={1600} step={0.5} />
                            )}
                          />
                        </Field>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          aria-label={t("removeExam")}
                          onClick={() => form.setValue("supplementaryExams", supplementaryExams.filter((_, i) => i !== index))}
                        >
                          <Trash2 className="size-4 text-[var(--danger-500)]" />
                        </Button>
                      </div>
                    ))}
                    <Button
                      type="button"
                      variant="secondary"
                      size="sm"
                      onClick={() =>
                        form.setValue("supplementaryExams", [...supplementaryExams, { examType: "Ielts", score: 0, maxScore: 9, dateTaken: null }])
                      }
                    >
                      <Plus className="size-3.5" />
                      {t("addExam")}
                    </Button>
                  </div>
                </CardContent>
              </>
            )}

            {step === 5 && (
              <>
                <CardHeader>
                  <CardTitle>{t("stepConstraints")}</CardTitle>
                  <CardDescription>{t("stepConstraintsHint")}</CardDescription>
                </CardHeader>
                <CardContent className="flex flex-col gap-4">
                  <Field label={t("gpa")} hint={t("gpaHint")} error={form.formState.errors.gpa?.message}>
                    <Controller
                      control={form.control}
                      name="gpa"
                      render={({ field }) => (
                        <NumberStepper value={(field.value as number | null | undefined) ?? undefined} onChange={field.onChange} min={0} max={4} step={0.01} />
                      )}
                    />
                  </Field>
                  {form.watch("fundingTrackPreference") === "GrantOnly" ? (
                    <p className="rounded-[var(--radius-md)] border border-[var(--info-100)] bg-[var(--info-100)]/40 p-3 text-xs text-[var(--neutral-500)]">
                      {t("budgetGrantOnlyNote")}
                    </p>
                  ) : (
                  <Field label={t("budgetBand")} hint={t("budgetHint")}>
                    <Controller
                      control={form.control}
                      name="budgetBand"
                      render={({ field }) => (
                        <Select value={field.value} onValueChange={field.onChange}>
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="Low">{t("budgetLow")}</SelectItem>
                            <SelectItem value="Medium">{t("budgetMedium")}</SelectItem>
                            <SelectItem value="High">{t("budgetHigh")}</SelectItem>
                            <SelectItem value="VeryHigh">{t("budgetVeryHigh")}</SelectItem>
                          </SelectContent>
                        </Select>
                      )}
                    />
                  </Field>
                  )}
                  <Field label={t("timeline")} hint={t("timelineHint")}>
                    <Controller
                      control={form.control}
                      name="timelineMonthsToApplication"
                      render={({ field }) => (
                        <NumberStepper
                          value={field.value as number | undefined}
                          onChange={(v) => field.onChange(v ?? 0)}
                          min={0}
                          max={60}
                          suffix={tCommon("months")}
                        />
                      )}
                    />
                  </Field>
                  <Field label={t("constraints")} hint={t("constraintsHint")}>
                    <Controller
                      control={form.control}
                      name="constraints"
                      render={({ field }) => <TagInput value={field.value} onChange={field.onChange} placeholder={t("constraintsPlaceholder")} />}
                    />
                  </Field>
                </CardContent>
              </>
            )}
          </motion.div>
        </AnimatePresence>

        <div className="flex items-center justify-between gap-3 border-t border-[var(--border-subtle)] p-5">
          <Button type="button" variant="ghost" onClick={goBack} disabled={step === 0}>
            {tCommon("back")}
          </Button>
          <p className="hidden text-xs text-[var(--neutral-400)] sm:block">{t("saved")}</p>
          <Button type="button" onClick={goNext} disabled={isSubmitting}>
            {step === WIZARD_TOTAL_STEPS - 1 ? t("submit") : tCommon("next")}
          </Button>
        </div>
      </Card>
    </div>
  );
}

function ChoiceCard({
  icon: Icon,
  title,
  hint,
  selected,
  onClick,
  compact,
}: {
  icon: React.ElementType;
  title: string;
  hint?: string;
  selected: boolean;
  onClick: () => void;
  compact?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "flex flex-col items-start gap-2 rounded-[var(--radius-md)] border p-4 text-left transition-colors duration-150",
        compact && "items-center gap-1.5 p-3 text-center",
        selected ? "border-[var(--primary)] bg-[var(--brand-50)] dark:bg-[var(--brand-900)]" : "border-[var(--border-subtle)] hover:bg-[var(--surface-raised)]"
      )}
    >
      <div className={cn("flex w-full items-center justify-between", compact && "justify-center")}>
        <Icon className="size-5 text-[var(--brand-600)] dark:text-[var(--brand-300)]" />
        {selected && !compact && <Check className="size-4 text-[var(--primary)]" />}
      </div>
      <p className={cn("text-sm font-semibold", compact && "text-xs")}>{title}</p>
      {hint && !compact && <p className="text-xs text-[var(--neutral-500)]">{hint}</p>}
    </button>
  );
}

function Field({
  label,
  hint,
  error,
  className,
  children,
}: {
  label: string;
  hint?: string;
  error?: string;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div className={cn("flex flex-col gap-1.5", className)}>
      <Label>{label}</Label>
      {children}
      {hint && !error && <p className="text-xs text-[var(--neutral-400)]">{hint}</p>}
      {error && <p className="text-xs text-[var(--danger-500)]">{error}</p>}
    </div>
  );
}
