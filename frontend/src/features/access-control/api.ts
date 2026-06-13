import { api } from '@/shared/api/client';
import type {
  CreateRoleRequest,
  PermissionDto,
  RoleDetail,
  RoleListItem,
  SetRolePermissionsRequest,
  SetUserOverridesRequest,
  UpdateRoleRequest,
  UserOverrideDto,
} from './types';

export const accessApi = {
  listPermissions: () => api.get<PermissionDto[]>('/permissions'),
  listRoles: () => api.get<RoleListItem[]>('/roles'),
  getRole: (id: string) => api.get<RoleDetail>(`/roles/${id}`),
  createRole: (body: CreateRoleRequest) => api.post<{ id: string }>('/roles', body),
  updateRole: (id: string, body: UpdateRoleRequest) => api.put<void>(`/roles/${id}`, body),
  setRolePermissions: (id: string, body: SetRolePermissionsRequest) =>
    api.put<void>(`/roles/${id}/permissions`, body),
  deleteRole: (id: string) => api.delete<void>(`/roles/${id}`),
  getUserOverrides: (userId: string) => api.get<UserOverrideDto[]>(`/users/${userId}/overrides`),
  setUserOverrides: (userId: string, body: SetUserOverridesRequest) =>
    api.put<void>(`/users/${userId}/overrides`, body),
};

export const accessKeys = {
  permissions: ['access', 'permissions'] as const,
  roles: ['access', 'roles'] as const,
  role: (id: string) => ['access', 'role', id] as const,
  overrides: (userId: string) => ['access', 'overrides', userId] as const,
};
