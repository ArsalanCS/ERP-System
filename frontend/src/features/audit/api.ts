import { api } from '@/shared/api/client';
import type { ListParams, Paged } from '@/shared/api/types';

/** Mirrors Erp.Domain.Auditing enums (serialized as numbers). */
export enum AuditResult {
  Success = 0,
  Denied = 1,
  Failed = 2,
}

export enum AuditSource {
  Ui = 0,
  Api = 1,
  BackgroundJob = 2,
  Integration = 3,
}

export interface AuditLogDto {
  id: string;
  occurredAt: string;
  actorUserId: string | null;
  actorDisplayName: string | null;
  action: string;
  module: string;
  resourceType: string;
  resourceId: string | null;
  result: AuditResult;
  source: AuditSource;
  ipAddress: string | null;
  correlationId: string;
  reason: string | null;
  oldValues: string | null;
  newValues: string | null;
}

export interface AuditSearchParams extends ListParams {
  from?: string | undefined;
  to?: string | undefined;
  result?: AuditResult | undefined;
  module?: string | undefined;
  actorUserId?: string | undefined;
  action?: string | undefined;
}

export const auditApi = {
  search: (params: AuditSearchParams) => api.get<Paged<AuditLogDto>>('/audit', params),
  export: (params: AuditSearchParams) => api.get<Paged<AuditLogDto>>('/audit/export', params),
};

export const auditKeys = {
  list: (params: AuditSearchParams) => ['audit', 'list', params] as const,
};
