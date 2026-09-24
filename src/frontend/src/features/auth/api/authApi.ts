import { apiRequest, type AuthPayload } from "@/api/httpClient";

export type LoginInput = { email: string; password: string };
export type RegisterInput = LoginInput & {
  displayName: string;
  timeZoneId?: string;
};
export type RegistrationPending = {
  email: string;
  expiresAt: string;
  resendAfterSeconds: number;
};

export const authApi = {
  login: (input: LoginInput) =>
    apiRequest<AuthPayload>("/auth/login", {
      method: "POST",
      headers: { "X-Skip-Refresh": "true" },
      body: JSON.stringify(input),
    }),
  register: (input: RegisterInput) =>
    apiRequest<RegistrationPending | AuthPayload>("/auth/register", {
      method: "POST",
      headers: { "X-Skip-Refresh": "true" },
      body: JSON.stringify(input),
    }),
  verifyEmail: (email: string, code: string) =>
    apiRequest<AuthPayload>("/auth/verify-email", {
      method: "POST",
      headers: { "X-Skip-Refresh": "true" },
      body: JSON.stringify({ email, code }),
    }),
  resendEmailOtp: (email: string) =>
    apiRequest<RegistrationPending>("/auth/resend-email-otp", {
      method: "POST",
      headers: { "X-Skip-Refresh": "true" },
      body: JSON.stringify({ email }),
    }),
  refresh: () =>
    apiRequest<AuthPayload>("/auth/refresh", {
      method: "POST",
      headers: { "X-Skip-Refresh": "true" },
    }),
  logout: () => apiRequest<void>("/auth/logout", { method: "POST" }),
  updateTimeZone: (timeZoneId: string) =>
    apiRequest<AuthPayload["user"]>("/auth/timezone", {
      method: "PUT",
      body: JSON.stringify({ timeZoneId }),
    }),
};
