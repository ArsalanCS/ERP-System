import { api } from '@/shared/api/client';

/** Mirrors Erp.Application.Settings contracts (Identity spec §9). */
export interface WorkspaceSettings {
  id: string;
  name: string;
  legalName: string | null;
  slug: string;
  defaultLanguage: string;
  timeZone: string;
  baseCurrency: string;
  country: string | null;
  status: string;
}

export interface UpdateWorkspaceSettingsRequest {
  name: string;
  legalName?: string | null;
  defaultLanguage: string;
  timeZone: string;
  baseCurrency: string;
  country?: string | null;
}

export const settingsApi = {
  get: () => api.get<WorkspaceSettings>('/settings'),
  update: (body: UpdateWorkspaceSettingsRequest) => api.put<void>('/settings', body),
};

export const settingsKeys = {
  workspace: ['settings', 'workspace'] as const,
};
