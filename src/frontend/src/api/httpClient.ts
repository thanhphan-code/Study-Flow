import { useAuthStore } from "@/features/auth/store/authStore";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "/api";
let refreshInFlight: Promise<boolean> | null = null;

function refreshSession(): Promise<boolean> {
  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      const response = await fetch(`${apiBaseUrl}/auth/refresh`, {
        method: "POST",
        credentials: "include",
      });
      if (response.ok) {
        useAuthStore
          .getState()
          .setSession((await response.json()) as AuthPayload);
        return true;
      }
      if (response.status === 401 || response.status === 403)
        useAuthStore.getState().clearSession();
      return false;
    })().finally(() => {
      refreshInFlight = null;
    });
  }
  return refreshInFlight;
}

export async function apiRequest<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const send = () => {
    const headers = new Headers(init?.headers);
    if (!(init?.body instanceof FormData) && !headers.has("Content-Type"))
      headers.set("Content-Type", "application/json");
    const token = useAuthStore.getState().accessToken;
    if (token) headers.set("Authorization", `Bearer ${token}`);
    return fetch(`${apiBaseUrl}${path}`, {
      ...init,
      credentials: "include",
      headers,
    });
  };
  let response = await send();
  if (
    response.status === 401 &&
    path !== "/auth/refresh" &&
    !new Headers(init?.headers).has("X-Skip-Refresh")
  ) {
    if (await refreshSession()) response = await send();
  }
  if (!response.ok) {
    const error = (await response
      .json()
      .catch(() => ({
        code: "REQUEST_FAILED",
        message: "Request failed.",
      }))) as ApiError;
    if (response.status === 403 && error.code === "SUSPENDED") {
      useAuthStore.getState().clearSession();
    }
    throw new ApiRequestError(
      response.status,
      error.code,
      error.message,
      error.errors,
    );
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export type AuthPayload = {
  user: {
    id: string;
    email: string;
    displayName: string;
    avatarUrl: string | null;
    timeZoneId: string;
    isEmailVerified: boolean;
    role: "User" | "Admin";
    isSuspended: boolean;
  };
  accessToken: string;
  accessTokenExpiresAt: string;
};
type ApiError = {
  code: string;
  message: string;
  errors?: Record<string, string[]>;
};
export class ApiRequestError extends Error {
  status: number;
  code: string;
  errors?: Record<string, string[]>;
  constructor(
    status: number,
    code: string,
    message: string,
    errors?: Record<string, string[]>,
  ) {
    super(message);
    this.status = status;
    this.code = code;
    this.errors = errors;
  }
}
