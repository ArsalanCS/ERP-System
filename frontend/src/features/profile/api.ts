import { api } from '@/shared/api/client';

/** Mirrors Erp.Application.Account contracts (Identity spec §10). */
export interface MyProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  mobile: string | null;
  preferredLanguage: string;
  timeZone: string;
  jobTitle: string | null;
  twoFactorEnabled: boolean;
}

export interface UpdateMyProfileRequest {
  firstName: string;
  lastName: string;
  displayName: string;
  mobile?: string | null;
  preferredLanguage: string;
  timeZone: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface SessionDto {
  id: string;
  createdByIp: string | null;
  createdAt: string;
  expiresAt: string;
}

export interface TwoFactorSetup {
  secret: string;
  otpAuthUri: string;
}

export const accountApi = {
  getProfile: () => api.get<MyProfile>('/me/profile'),
  updateProfile: (body: UpdateMyProfileRequest) => api.put<void>('/me/profile', body),
  changePassword: (body: ChangePasswordRequest) => api.post<void>('/me/change-password', body),
  listSessions: () => api.get<SessionDto[]>('/me/sessions'),
  revokeSession: (id: string) => api.delete<void>(`/me/sessions/${id}`),

  // Two-factor (TOTP authenticator)
  setupTwoFactor: () => api.post<TwoFactorSetup>('/me/2fa/setup'),
  enableTwoFactor: (code: string) => api.post<void>('/me/2fa/enable', { code }),
  disableTwoFactor: (code: string) => api.post<void>('/me/2fa/disable', { code }),
};

export const accountKeys = {
  profile: ['account', 'profile'] as const,
  sessions: ['account', 'sessions'] as const,
};
