"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import { useRouter, Link } from "@/i18n/navigation";
import { toast } from "sonner";
import { useLogin } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";
import { loginSchema, type LoginFormValues } from "@/lib/validation/auth-schema";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { PasswordField } from "@/components/journey/password-field";
import { FormField } from "@/components/journey/form-field";

export default function LoginPage() {
  const t = useTranslations("auth");
  const tValidation = useTranslations("validation");
  const router = useRouter();
  const login = useLogin();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    mode: "onBlur",
  });

  async function onSubmit(values: LoginFormValues) {
    try {
      await login.mutateAsync(values);
      // The journey layout guard bounces an account with incomplete intake back to the wizard
      // automatically, so this can always target the hub — no need to duplicate that check here.
      router.push("/journey/home");
    } catch (err) {
      // Deliberately generic — the backend never reveals whether the email or the password was
      // wrong, so this UI doesn't either (avoids account enumeration).
      toast.error(err instanceof ApiError ? err.message : t("loginFailed"));
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center px-6 py-12">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle>{t("loginTitle")}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
            <FormField id="email" label={t("email")} error={errors.email && tValidation(errors.email.message as never)}>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                autoFocus
                aria-invalid={!!errors.email}
                aria-describedby={errors.email ? "email-error" : undefined}
                {...register("email")}
              />
            </FormField>

            <FormField
              id="password"
              label={t("password")}
              error={errors.password && tValidation(errors.password.message as never)}
            >
              <PasswordField
                id="password"
                autoComplete="current-password"
                showLabel={t("showPassword")}
                hideLabel={t("hidePassword")}
                aria-invalid={!!errors.password}
                aria-describedby={errors.password ? "password-error" : undefined}
                {...register("password")}
              />
            </FormField>

            <Button type="submit" disabled={login.isPending} className="mt-2">
              {login.isPending ? t("signingIn") : t("loginCta")}
            </Button>
            <p className="text-center text-sm text-[var(--neutral-500)]">
              <Link href="/register" className="text-[var(--primary)] hover:underline">
                {t("switchToRegister")}
              </Link>
            </p>
            <p className="text-center text-xs text-[var(--neutral-400)]">{t("judgeHint")}</p>
          </form>
        </CardContent>
      </Card>
    </main>
  );
}
