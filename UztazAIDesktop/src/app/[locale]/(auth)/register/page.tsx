"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import { useRouter, Link } from "@/i18n/navigation";
import { toast } from "sonner";
import { useRegister } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";
import { registerSchema, type RegisterFormValues } from "@/lib/validation/auth-schema";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { PasswordField } from "@/components/journey/password-field";
import { PasswordStrengthMeter } from "@/components/journey/password-strength-meter";
import { FormField } from "@/components/journey/form-field";

export default function RegisterPage() {
  const t = useTranslations("auth");
  const tValidation = useTranslations("validation");
  const router = useRouter();
  const register = useRegister();

  const {
    register: field,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    mode: "onBlur",
  });

  const password = watch("password") ?? "";

  async function onSubmit(values: RegisterFormValues) {
    try {
      await register.mutateAsync(values);
      router.push("/journey/profile");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("registerFailed"));
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center px-6 py-12">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle>{t("registerTitle")}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
            <FormField
              id="name"
              label={t("displayName")}
              error={errors.displayName && tValidation(errors.displayName.message as never)}
            >
              <Input
                id="name"
                autoComplete="name"
                autoFocus
                aria-invalid={!!errors.displayName}
                aria-describedby={errors.displayName ? "name-error" : undefined}
                {...field("displayName")}
              />
            </FormField>

            <FormField id="email" label={t("email")} error={errors.email && tValidation(errors.email.message as never)}>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                aria-invalid={!!errors.email}
                aria-describedby={errors.email ? "email-error" : undefined}
                {...field("email")}
              />
            </FormField>

            <FormField
              id="password"
              label={t("password")}
              error={errors.password && tValidation(errors.password.message as never)}
            >
              <PasswordField
                id="password"
                autoComplete="new-password"
                showLabel={t("showPassword")}
                hideLabel={t("hidePassword")}
                aria-invalid={!!errors.password}
                aria-describedby={errors.password ? "password-error" : "password-hint"}
                {...field("password")}
              />
              {!errors.password && <p id="password-hint" className="text-xs text-[var(--neutral-400)]">{t("passwordHint")}</p>}
              <PasswordStrengthMeter password={password} />
            </FormField>

            <FormField
              id="confirmPassword"
              label={t("confirmPassword")}
              error={errors.confirmPassword && tValidation(errors.confirmPassword.message as never)}
            >
              <PasswordField
                id="confirmPassword"
                autoComplete="new-password"
                showLabel={t("showPassword")}
                hideLabel={t("hidePassword")}
                aria-invalid={!!errors.confirmPassword}
                aria-describedby={errors.confirmPassword ? "confirmPassword-error" : undefined}
                {...field("confirmPassword")}
              />
            </FormField>

            <Button type="submit" disabled={register.isPending} className="mt-2">
              {register.isPending ? t("creatingAccount") : t("registerCta")}
            </Button>
            <p className="text-center text-sm text-[var(--neutral-500)]">
              <Link href="/login" className="text-[var(--primary)] hover:underline">
                {t("switchToLogin")}
              </Link>
            </p>
          </form>
        </CardContent>
      </Card>
    </main>
  );
}
