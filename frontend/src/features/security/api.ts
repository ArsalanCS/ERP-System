import { api } from '@/shared/api/client';

/** Mirrors Erp.Application.Security contracts (Identity spec §7). */
export interface SecurityPolicy {
  passwordMinLength: number;
  requireUppercase: boolean;
  requireLowercase: boolean;
  requireDigit: boolean;
  requireSymbol: boolean;
  passwordExpiryDays: number | null;
  maxFailedAttempts: number;
  lockoutMinutes: number;
  sessionIdleTimeoutMinutes: number;
  refreshTokenDays: number;
  requireTwoFactor: boolean;
}

export type UpdateSecurityPolicyRequest = SecurityPolicy;

export const securityApi = {
  getPolicy: () => api.get<SecurityPolicy>('/security/policy'),
  updatePolicy: (body: UpdateSecurityPolicyRequest) => api.put<void>('/security/policy', body),
};

export const securityKeys = {
  policy: ['security', 'policy'] as const,
};
