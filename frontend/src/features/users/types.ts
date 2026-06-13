import type { Paged } from '@/shared/api/types';

/**
 * Mirrors Erp.Domain.Identity.UserStatus. The API serializes enums as their
 * numeric value, so the order here MUST match the backend enum (Identity §4.3).
 */
export enum UserStatus {
  PendingInvitation = 0,
  Active = 1,
  Inactive = 2,
  Suspended = 3,
  Locked = 4,
  Archived = 5,
}

export interface UserListItem {
  id: string;
  email: string;
  displayName: string;
  mobile: string | null;
  jobTitle: string | null;
  status: UserStatus;
  twoFactorEnabled: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

export interface UserDetail {
  id: string;
  workspaceId: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  mobile: string | null;
  preferredLanguage: string;
  timeZone: string;
  jobTitle: string | null;
  status: UserStatus;
  twoFactorEnabled: boolean;
  requirePasswordChange: boolean;
  accessStartDate: string | null;
  accessExpiryDate: string | null;
  lastLoginAt: string | null;
  createdAt: string;
  roleIds: string[];
}

export interface CreateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  mobile?: string | null;
  jobTitle?: string | null;
  preferredLanguage: string;
  timeZone?: string | null;
  roleIds?: string[] | undefined;
  sendInvitation: boolean;
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  displayName: string;
  mobile?: string | null;
  jobTitle?: string | null;
  preferredLanguage: string;
  timeZone: string;
  accessStartDate?: string | null;
  accessExpiryDate?: string | null;
  roleIds?: string[] | undefined;
}

export interface CreateUserResult {
  userId: string;
  invitationToken: string | null;
}

export type UserList = Paged<UserListItem>;
