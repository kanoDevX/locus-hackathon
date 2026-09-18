"use client";

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import { toast } from "sonner";
import { useRouter } from "@/i18n/navigation";
import { accountInfoSchema, changePasswordSchema, type AccountInfoFormValues, type ChangePasswordFormValues } from "@/lib/validation/account-schema";
import { useUpdateAccount, useChangePassword } from "@/lib/api/auth";
import { useAuthStore } from "@/lib/stores/auth-store";
import { ApiError } from "@/lib/api/client";
import { Dialog, DialogContent, DialogTitle, DialogDescription } from "@/components/ui/dialog";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { PasswordField } from "@/components/journey/password-field";
import { FormField } from "@/components/journey/form-field";
import { passwordStrength } from "@/lib/validation/auth-schema";

export function AccountSettingsDialog({ open, onOpenChange }: { open: boolean; onOpenChange: (open: boolean) => void }) {
  const t = useTranslations("account");
  const tValidation = useTranslations("validation");
  const tAuth = useTranslations("auth");
  const router = useRouter();
  const { displayName, email, clear } = useAuthStore();
  const updateAccount = useUpdateAccount();
  const changePassword = useChangePassword();

  const infoForm = useForm<AccountInfoFormValues>({
    resolver: zodResolver(accountInfoSchema),
    values: { displayName: displayName ?? "", email: email ?? "" },
  });

  const passwordForm = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: "", newPassword: "", confirmNewPassword: "" },
  });

  useEffect(() => {
    if (open) passwordForm.reset({ currentPassword: "", newPassword: "", confirmNewPassword: "" });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  const newPassword = passwordForm.watch("newPassword");
  const strength = newPassword ? passwordStrength(newPassword) : null;

  async function onSaveInfo(values: AccountInfoFormValues) {
    try {
      await updateAccount.mutateAsync(values);
      toast.success(t("infoSaved"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("infoSaveFailed"));
    }
  }

  async function onSavePassword(values: ChangePasswordFormValues) {
    try {
      await changePassword.mutateAsync({ currentPassword: values.currentPassword, newPassword: values.newPassword });
      onOpenChange(false);
      clear();
      toast.success(t("passwordChangedSignInAgain"));
      router.push("/login");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("passwordChangeFailed"));
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogTitle>{t("title")}</DialogTitle>
        <DialogDescription>{t("subtitle")}</DialogDescription>

        <Tabs defaultValue="info" className="mt-4">
          <TabsList className="w-full">
            <TabsTrigger value="info" className="flex-1">
              {t("tabInfo")}
            </TabsTrigger>
            <TabsTrigger value="password" className="flex-1">
              {t("tabPassword")}
            </TabsTrigger>
          </TabsList>

          <TabsContent value="info">
            <form onSubmit={infoForm.handleSubmit(onSaveInfo)} noValidate className="flex flex-col gap-4">
              <FormField
                id="account-displayName"
                label={tAuth("displayName")}
                error={infoForm.formState.errors.displayName && tValidation(infoForm.formState.errors.displayName.message as never)}
              >
                <Input id="account-displayName" {...infoForm.register("displayName")} />
              </FormField>
              <FormField
                id="account-email"
                label={tAuth("email")}
                error={infoForm.formState.errors.email && tValidation(infoForm.formState.errors.email.message as never)}
              >
                <Input id="account-email" type="email" {...infoForm.register("email")} />
              </FormField>
              <Button type="submit" disabled={updateAccount.isPending || !infoForm.formState.isDirty} className="mt-1">
                {updateAccount.isPending ? tAuth("signingIn") : t("saveInfo")}
              </Button>
            </form>
          </TabsContent>

          <TabsContent value="password">
            <form onSubmit={passwordForm.handleSubmit(onSavePassword)} noValidate className="flex flex-col gap-4">
              <p className="rounded-[var(--radius-md)] border border-[var(--warning-100)] bg-[var(--warning-100)]/40 p-2.5 text-xs text-[var(--neutral-600)] dark:text-[var(--neutral-300)]">
                {t("passwordChangeWarning")}
              </p>
              <FormField
                id="account-currentPassword"
                label={t("currentPassword")}
                error={passwordForm.formState.errors.currentPassword && tValidation(passwordForm.formState.errors.currentPassword.message as never)}
              >
                <PasswordField
                  id="account-currentPassword"
                  autoComplete="current-password"
                  showLabel={tAuth("showPassword")}
                  hideLabel={tAuth("hidePassword")}
                  {...passwordForm.register("currentPassword")}
                />
              </FormField>
              <FormField
                id="account-newPassword"
                label={t("newPassword")}
                error={passwordForm.formState.errors.newPassword && tValidation(passwordForm.formState.errors.newPassword.message as never)}
              >
                <PasswordField
                  id="account-newPassword"
                  autoComplete="new-password"
                  showLabel={tAuth("showPassword")}
                  hideLabel={tAuth("hidePassword")}
                  {...passwordForm.register("newPassword")}
                />
                {strength && (
                  <div className="mt-1 flex gap-1">
                    {Array.from({ length: 4 }).map((_, i) => (
                      <span
                        key={i}
                        className={`h-1 flex-1 rounded-full ${i <= strength.score ? "bg-[var(--success-500)]" : "bg-[var(--neutral-200)] dark:bg-[var(--neutral-800)]"}`}
                      />
                    ))}
                  </div>
                )}
              </FormField>
              <FormField
                id="account-confirmNewPassword"
                label={t("confirmNewPassword")}
                error={
                  passwordForm.formState.errors.confirmNewPassword && tValidation(passwordForm.formState.errors.confirmNewPassword.message as never)
                }
              >
                <PasswordField
                  id="account-confirmNewPassword"
                  autoComplete="new-password"
                  showLabel={tAuth("showPassword")}
                  hideLabel={tAuth("hidePassword")}
                  {...passwordForm.register("confirmNewPassword")}
                />
              </FormField>
              <Button type="submit" variant="danger" disabled={changePassword.isPending} className="mt-1">
                {t("savePassword")}
              </Button>
            </form>
          </TabsContent>
        </Tabs>
      </DialogContent>
    </Dialog>
  );
}
