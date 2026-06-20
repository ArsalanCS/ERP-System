import { api } from '@/shared/api/client';

/** Mirrors backend Erp.Application.Auth contracts (Identity spec §13.1). */
export interface AuthUser {
  id: string;
  workspaceId: string;
  email: string;
  displayName: string;
  preferredLanguage: string;
  requirePasswordChange: boolean;
  twoFactorEnabled: boolean;
}

export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: AuthUser;
}

/** Shape of GET /auth/me — the authorization mirror for the UI. */
export interface MeResponse {
  id: string;
  workspaceId: string;
  email: string;
  isPlatformAdmin: boolean;
  actions: string[];
}

export const authApi = {
  login: (workspaceSlug: string, email: string, password: string, twoFactorCode?: string) =>
    api.postAnon<AuthTokens>('/auth/login', {
      workspaceSlug,
      email,
      password,
      ...(twoFactorCode ? { twoFactorCode } : {}),
    }),

  /** Current caller's identity + effective actions (requires a valid access token). */
  me: () => api.get<MeResponse>('/auth/me'),

  logout: (refreshToken: string) => api.postAnon<void>('/auth/logout', { refreshToken }),

  // Always resolves 202 regardless of whether the account exists (no enumeration).
  forgotPassword: (workspaceSlug: string, email: string) =>
    api.postAnon<void>('/auth/forgot-password', { workspaceSlug, email }),

  resetPassword: (token: string, newPassword: string) =>
    api.postAnon<void>('/auth/reset-password', { token, newPassword }),

  /** Self-service signup: provisions a workspace + owner and emails a verification link. */
  register: (body: RegisterWorkspaceRequest) =>
    api.postAnon<RegisterWorkspaceResult>('/auth/register', body),

  /** Confirms an email-verification link and activates the owner account. */
  verifyEmail: (token: string) => api.postAnon<void>('/auth/verify-email', { token }),

  /** Re-sends the verification link. Always resolves (no account enumeration). */
  resendVerification: (workspaceSlug: string, email: string) =>
    api.postAnon<void>('/auth/resend-verification', { workspaceSlug, email }),
};

/** Mirrors backend Erp.Application.Auth.RegisterWorkspaceRequest. */
export interface RegisterWorkspaceRequest {
  workspaceName: string;
  slug: string;
  baseCurrency: string;
  language: string;
  fullName: string;
  email: string;
  password: string;
}

export interface RegisterWorkspaceResult {
  slug: string;
  email: string;
}
