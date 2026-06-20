import { api } from '@/shared/api/client';
import type { UserStatus } from '@/features/users/types';

/** Mirrors Erp.Domain.Structure.StructureStatus (serialized as numbers). */
export enum StructureStatus {
  Active = 0,
  Inactive = 1,
  Archived = 2,
}

/** Mirrors Erp.Domain.Structure.StructureNodeType (serialized as numbers — keep order in sync). */
export enum StructureNodeType {
  Organization = 0,
  Department = 1,
  Branch = 2,
  SubDepartment = 3,
  Team = 4,
  SubTeam = 5,
}

export interface StructureNodeDto {
  id: string;
  parentId: string | null;
  nodeType: StructureNodeType;
  name: string;
  code: string;
  description: string | null;
  managerId: string | null;
  sortOrder: number;
  status: StructureStatus;
  memberCount: number;
}

export interface StructureTree {
  nodes: StructureNodeDto[];
}

/** A user placed directly on a node (mirrors StructureMemberDto). */
export interface StructureMemberDto {
  userId: string;
  displayName: string;
  email: string;
  jobTitle: string | null;
  mobile: string | null;
  employeeNumber: string | null;
  status: UserStatus;
  isManager: boolean;
}

export interface CreateNodeBody {
  parentId: string | null;
  nodeType: StructureNodeType;
  name: string;
  code: string;
  description?: string | null;
  managerId?: string | null;
  sortOrder?: number | null;
}

export interface UpdateNodeBody {
  name: string;
  description?: string | null;
  managerId?: string | null;
  sortOrder?: number | null;
}

export const structureApi = {
  tree: () => api.get<StructureTree>('/structure/tree'),
  members: (nodeId: string) => api.get<StructureMemberDto[]>(`/structure/nodes/${nodeId}/members`),
  createNode: (body: CreateNodeBody) => api.post<{ id: string }>('/structure/nodes', body),
  updateNode: (id: string, body: UpdateNodeBody) => api.put<void>(`/structure/nodes/${id}`, body),
  moveNode: (id: string, parentId: string | null) =>
    api.put<void>(`/structure/nodes/${id}/move`, { parentId }),
  archiveNode: (id: string) => api.delete<void>(`/structure/nodes/${id}`),
};

export const structureKeys = {
  tree: ['structure', 'tree'] as const,
  members: (nodeId: string) => ['structure', 'members', nodeId] as const,
};

/** Sensible child node types offered when adding under a given node type. */
export const CHILD_TYPES: Record<StructureNodeType, StructureNodeType[]> = {
  [StructureNodeType.Organization]: [StructureNodeType.Department, StructureNodeType.Branch],
  [StructureNodeType.Department]: [
    StructureNodeType.Branch,
    StructureNodeType.SubDepartment,
    StructureNodeType.Team,
  ],
  [StructureNodeType.Branch]: [
    StructureNodeType.Department,
    StructureNodeType.SubDepartment,
    StructureNodeType.Team,
  ],
  [StructureNodeType.SubDepartment]: [StructureNodeType.Team],
  [StructureNodeType.Team]: [StructureNodeType.SubTeam],
  [StructureNodeType.SubTeam]: [],
};
