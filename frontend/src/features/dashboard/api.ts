import { api } from '@/shared/api/client';

/** Mirrors Erp.Application.Dashboard.DashboardSummary (scope-aware counts). */
export interface DashboardSummary {
  totalUsers: number;
  activeUsers: number;
  suspendedUsers: number;
  pendingInvitations: number;
  organizations: number;
  clusters: number;
  roles: number;
  activeSessions: number;
}

export const dashboardApi = {
  summary: () => api.get<DashboardSummary>('/admin/overview'),
};

export const dashboardKeys = {
  summary: ['dashboard', 'summary'] as const,
};
