import { z } from "zod";

export const accountInfoSchema = z.object({
  displayName: z.string().min(1, "required").max(200, "tooLong"),
  email: z.string().min(1, "required").email("invalidEmail"),
});
export type AccountInfoFormValues = z.infer<typeof accountInfoSchema>;

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, "required"),
    newPassword: z.string().min(8, "passwordTooShort").max(128, "tooLong"),
    confirmNewPassword: z.string().min(1, "required"),
  })
  .refine((data) => data.newPassword === data.confirmNewPassword, {
    message: "passwordMismatch",
    path: ["confirmNewPassword"],
  });
export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;
