/**
 * Mirrors Erp.Domain.Authorization enums. Enums serialize as numbers, so the
 * order MUST match the backend (Identity spec §5.2 / §5.4).
 */
export enum DataScope {
  Own = 0,
  Team = 1,
  Department = 2,
  Cluster = 3,
  Organization = 4,
  Workspace = 5,
  AllTenants = 6,
}

export enum PermissionEffect {
  Allow = 1,
  Deny = 2,
}

export enum RoleType {
  System = 0,
  Workspace = 1,
  Organization = 2,
  Custom = 3,
}

export interface PermissionDto {
  id: string;
  code: string;
  module: string;
  resource: string;
  action: string;
  isHighRisk: boolean;
}

export interface RolePermissionDto {
  permissionId: string;
  code: string;
  scope: DataScope;
}

export interface RoleListItem {
  id: string;
  name: string;
  code: string;
  type: RoleType;
  color: string | null;
  isActive: boolean;
  assignedUsers: number;
}

export interface RoleDetail {
  id: string;
  name: string;
  code: string;
  description: string | null;
  type: RoleType;
  color: string | null;
  isActive: boolean;
  permissions: RolePermissionDto[];
}

export interface CreateRoleRequest {
  name: string;
  code: string;
  description?: string | null;
  color?: string | null;
}

export interface UpdateRoleRequest {
  name: string;
  description?: string | null;
  color?: string | null;
}

export interface PermissionGrant {
  permissionId: string;
  scope: DataScope;
}

export interface SetRolePermissionsRequest {
  permissions: PermissionGrant[];
}

export interface UserOverrideDto {
  permissionId: string;
  code: string;
  effect: PermissionEffect;
  scope: DataScope;
}

export interface UserOverrideInput {
  permissionId: string;
  effect: PermissionEffect;
  scope: DataScope;
}

export interface SetUserOverridesRequest {
  overrides: UserOverrideInput[];
}
