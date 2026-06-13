import { api } from '@/shared/api/client';

/** Mirrors Erp.Domain.Structure.StructureStatus (serialized as numbers). */
export enum StructureStatus {
  Active = 0,
  Inactive = 1,
  Archived = 2,
}

export interface OrganizationDto {
  id: string;
  name: string;
  code: string;
  legalName: string | null;
  organizationType: string | null;
  country: string | null;
  city: string | null;
  baseCurrency: string;
  status: StructureStatus;
}

export interface ClusterDto {
  id: string;
  organizationId: string;
  parentClusterId: string | null;
  name: string;
  code: string;
  type: string;
  location: string | null;
  managerId: string | null;
  dataIsolationEnabled: boolean;
  permissionInheritanceEnabled: boolean;
  status: StructureStatus;
}

export interface DepartmentDto {
  id: string;
  organizationId: string;
  clusterId: string | null;
  name: string;
  code: string;
  managerId: string | null;
  status: StructureStatus;
}

export interface TeamDto {
  id: string;
  departmentId: string;
  name: string;
  code: string;
  leadId: string | null;
  status: StructureStatus;
}

export interface StructureTree {
  organizations: OrganizationDto[];
  clusters: ClusterDto[];
  departments: DepartmentDto[];
  teams: TeamDto[];
}

export const structureApi = {
  tree: () => api.get<StructureTree>('/structure/tree'),

  createOrganization: (body: { name: string; code: string; baseCurrency?: string | null; country?: string | null }) =>
    api.post<{ id: string }>('/organizations', body),
  archiveOrganization: (id: string) => api.delete<void>(`/organizations/${id}`),

  createCluster: (body: {
    organizationId: string;
    name: string;
    code: string;
    type: string;
    parentClusterId?: string | null;
    dataIsolationEnabled?: boolean;
    permissionInheritanceEnabled?: boolean;
  }) => api.post<{ id: string }>('/clusters', body),
  archiveCluster: (id: string) => api.delete<void>(`/clusters/${id}`),

  createDepartment: (body: { organizationId: string; clusterId?: string | null; name: string; code: string }) =>
    api.post<{ id: string }>('/departments', body),
  archiveDepartment: (id: string) => api.delete<void>(`/departments/${id}`),

  createTeam: (body: { departmentId: string; name: string; code: string }) =>
    api.post<{ id: string }>('/teams', body),
  archiveTeam: (id: string) => api.delete<void>(`/teams/${id}`),
};

export const structureKeys = {
  tree: ['structure', 'tree'] as const,
};
