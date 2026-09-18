"use client";

import { useAuthStore } from "@/lib/stores/auth-store";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5299";

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public details?: unknown
  ) {
    super(message);
    this.name = "ApiError";
  }
}

let refreshPromise: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  const { refreshToken, setSession, email, displayName, role, userId } = useAuthStore.getState();
  if (!refreshToken) return false;

  const response = await fetch(`${API_BASE_URL}/api/v1/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });
  if (!response.ok) return false;

  const data = await response.json();
  setSession({
    accessToken: data.accessToken,
    refreshToken: data.refreshToken,
    userId: data.userId ?? userId ?? "",
    email: data.email ?? email ?? "",
    displayName: data.displayName ?? displayName ?? "",
    role: data.role ?? role ?? "Student",
  });
  return true;
}

interface RequestOptions {
  method?: "GET" | "POST" | "PATCH" | "DELETE" | "PUT";
  body?: unknown;
  query?: Record<string, string | number | boolean | undefined | null>;
  anonymous?: boolean;
}

function buildUrl(path: string, query?: RequestOptions["query"]) {
  const url = new URL(path.startsWith("http") ? path : `${API_BASE_URL}${path}`);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== "") url.searchParams.set(key, String(value));
    }
  }
  return url.toString();
}

export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = "GET", body, query, anonymous } = options;
  const url = buildUrl(path, query);

  const doFetch = async () => {
    const headers: Record<string, string> = { "Content-Type": "application/json" };
    if (typeof window !== "undefined") {
      const uiLocale = window.location.pathname.split("/")[1];
      if (["ru", "kk", "en"].includes(uiLocale)) headers["X-Locale"] = uiLocale;
    }
    if (!anonymous) {
      const token = useAuthStore.getState().accessToken;
      if (token) headers.Authorization = `Bearer ${token}`;
    }
    return fetch(url, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  };

  let response = await doFetch();

  if (response.status === 401 && !anonymous) {
    refreshPromise ??= tryRefresh().finally(() => {
      refreshPromise = null;
    });
    const refreshed = await refreshPromise;
    if (refreshed) {
      response = await doFetch();
    } else {
      useAuthStore.getState().clear();
      throw new ApiError(401, "Session expired — please sign in again.");
    }
  }

  if (!response.ok) {
    let details: unknown;
    try {
      details = await response.json();
    } catch {
      /* no body */
    }
    const message =
      (details as { title?: string })?.title ?? `Request failed with status ${response.status}`;
    throw new ApiError(response.status, message, details);
  }

  if (response.status === 204) return undefined as T;

  const text = await response.text();
  if (text.length === 0) return undefined as T;
  return JSON.parse(text) as T;
}
