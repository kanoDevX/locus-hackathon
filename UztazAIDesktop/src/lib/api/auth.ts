"use client";

import { useMutation } from "@tanstack/react-query";
import { apiFetch } from "./client";
import { useAuthStore } from "@/lib/stores/auth-store";
import type { AccountDto, AuthResult } from "./types";

export function useRegister() {
  const setSession = useAuthStore((s) => s.setSession);
  return useMutation({
    mutationFn: (input: { email: string; password: string; displayName: string }) =>
      apiFetch<AuthResult>("/api/v1/auth/register", { method: "POST", body: input, anonymous: true }),
    onSuccess: (data) => setSession(data),
  });
}

export function useLogin() {
  const setSession = useAuthStore((s) => s.setSession);
  return useMutation({
    mutationFn: (input: { email: string; password: string }) =>
      apiFetch<AuthResult>("/api/v1/auth/login", { method: "POST", body: input, anonymous: true }),
    onSuccess: (data) => setSession(data),
  });
}

export function useLogout() {
  const { refreshToken, clear } = useAuthStore();
  return useMutation({
    mutationFn: () =>
      apiFetch<void>("/api/v1/auth/logout", { method: "POST", body: { refreshToken }, anonymous: true }),
    onSuccess: () => clear(),
  });
}

export function useUpdateAccount() {
  const updateAccountInfo = useAuthStore((s) => s.updateAccountInfo);
  return useMutation({
    mutationFn: (input: { displayName: string; email: string }) =>
      apiFetch<AccountDto>("/api/v1/auth/account", { method: "PUT", body: input }),
    onSuccess: (data) => updateAccountInfo({ displayName: data.displayName, email: data.email }),
  });
}

/** Succeeds with the session already revoked server-side (ChangePasswordCommand logs out every
 * device, this one included) — the caller is responsible for clearing local session state and
 * sending the user back to sign in with the new password. */
export function useChangePassword() {
  return useMutation({
    mutationFn: (input: { currentPassword: string; newPassword: string }) =>
      apiFetch<void>("/api/v1/auth/change-password", { method: "POST", body: input }),
  });
}
