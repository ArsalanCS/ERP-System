import { api } from '@/shared/api/client';
import { createCrudApi } from '@/shared/api/crud';
import type { ListParams, Paged } from '@/shared/api/types';
import type {
  AssignBody,
  ChangeStatusBody,
  CreateDailyReportBody,
  CreateStatusBody,
  ReorderStatusesBody,
  UpdateStatusBody,
  CreateDependencyBody,
  CreateDocumentBody,
  CreateNoteBody,
  CreateTaskBody,
  CreateTaskResult,
  MyTasksGroups,
  SetPriorityBody,
  StatusOption,
  TaskActivity,
  TaskAudit,
  TaskDailyReport,
  TaskDailyReportRow,
  TaskDashboard,
  TaskDependency,
  TaskDetails,
  TaskDocument,
  TaskListItem,
  TaskNote,
  TaskReport,
  TaskSettings,
  UpdateDailyReportBody,
  UpdateNoteBody,
  UpdateTaskBody,
  UpdateTaskSettingsBody,
} from './types';

export interface DailyReportReportParams extends ListParams {
  fromDate?: string | undefined;
  toDate?: string | undefined;
  authorId?: string | undefined;
  statusId?: string | undefined;
}

/** Standard list/get/create/update/archive + task-specific commands. Keyed by event id. */
const crud = createCrudApi<TaskListItem, TaskDetails, CreateTaskBody, UpdateTaskBody, CreateTaskResult>('/tasks');

export const tasksApi = {
  ...crud,
  list: (params?: ListParams) => crud.list(params),
  changeStatus: (id: string, body: ChangeStatusBody) => api.post<void>(`/tasks/${id}/status`, body),
  assign: (id: string, body: AssignBody) => api.post<void>(`/tasks/${id}/assign`, body),
  setPriority: (id: string, body: SetPriorityBody) => api.post<void>(`/tasks/${id}/priority`, body),
  activity: (id: string) => api.get<TaskActivity[]>(`/tasks/${id}/activity`),
  audit: (id: string) => api.get<TaskAudit[]>(`/tasks/${id}/audit`),
  my: () => api.get<MyTasksGroups>('/tasks/my'),

  statuses: (code: string) => api.get<StatusOption[]>('/tasks/statuses', { code }),
  dashboard: () => api.get<TaskDashboard>('/tasks/dashboard'),
  report: (params?: ListParams) => api.get<TaskReport>('/tasks/report', params),
  dailyReportsReport: (params?: DailyReportReportParams) =>
    api.get<Paged<TaskDailyReportRow>>('/tasks/report/daily-reports', params),

  subtasks: (id: string) => api.get<TaskListItem[]>(`/tasks/${id}/subtasks`),
  createSubtask: (id: string, body: CreateTaskBody) => api.post<CreateTaskResult>(`/tasks/${id}/subtasks`, body),

  dependencies: (id: string) => api.get<TaskDependency[]>(`/tasks/${id}/dependencies`),
  addDependency: (id: string, body: CreateDependencyBody) => api.post<{ id: string }>(`/tasks/${id}/dependencies`, body),
  removeDependency: (id: string, depId: string) => api.delete<void>(`/tasks/${id}/dependencies/${depId}`),

  notes: (id: string) => api.get<TaskNote[]>(`/tasks/${id}/notes`),
  addNote: (id: string, body: CreateNoteBody) => api.post<{ id: string }>(`/tasks/${id}/notes`, body),
  updateNote: (id: string, noteId: string, body: UpdateNoteBody) => api.put<void>(`/tasks/${id}/notes/${noteId}`, body),
  removeNote: (id: string, noteId: string) => api.delete<void>(`/tasks/${id}/notes/${noteId}`),

  documents: (id: string) => api.get<TaskDocument[]>(`/tasks/${id}/documents`),
  addDocument: (id: string, body: CreateDocumentBody) => api.post<{ id: string }>(`/tasks/${id}/documents`, body),
  removeDocument: (id: string, docId: string) => api.delete<void>(`/tasks/${id}/documents/${docId}`),

  // settings — manage statuses & priorities
  settingsStatuses: (code: string) => api.get<StatusOption[]>('/tasks/settings/statuses', { code }),
  createStatus: (body: CreateStatusBody) => api.post<{ id: string }>('/tasks/settings/statuses', body),
  updateStatus: (id: string, body: UpdateStatusBody) => api.put<void>(`/tasks/settings/statuses/${id}`, body),
  reorderStatuses: (body: ReorderStatusesBody) => api.post<void>('/tasks/settings/statuses/reorder', body),
  deleteStatus: (id: string) => api.delete<void>(`/tasks/settings/statuses/${id}`),

  // settings — workspace config (daily-report rules / notifications / dashboard defaults)
  getConfig: () => api.get<TaskSettings>('/tasks/settings/config'),
  updateConfig: (body: UpdateTaskSettingsBody) => api.put<void>('/tasks/settings/config', body),

  dailyReports: (id: string) => api.get<TaskDailyReport[]>(`/tasks/${id}/daily-reports`),
  addDailyReport: (id: string, body: CreateDailyReportBody) => api.post<{ id: string }>(`/tasks/${id}/daily-reports`, body),
  updateDailyReport: (id: string, reportId: string, body: UpdateDailyReportBody) =>
    api.put<void>(`/tasks/${id}/daily-reports/${reportId}`, body),
  removeDailyReport: (id: string, reportId: string) => api.delete<void>(`/tasks/${id}/daily-reports/${reportId}`),
};

export const taskKeys = {
  all: ['tasks'] as const,
  list: (params: ListParams) => ['tasks', 'list', params] as const,
  detail: (id: string) => ['tasks', 'detail', id] as const,
  activity: (id: string) => ['tasks', 'activity', id] as const,
  audit: (id: string) => ['tasks', 'audit', id] as const,
  my: ['tasks', 'my'] as const,
  dashboard: ['tasks', 'dashboard'] as const,
  report: (params: ListParams) => ['tasks', 'report', params] as const,
  dailyReportsReport: (params: DailyReportReportParams) => ['tasks', 'report', 'daily-reports', params] as const,
  config: ['tasks', 'settings', 'config'] as const,
  statuses: (code: string) => ['tasks', 'statuses', code] as const,
  settingsStatuses: (code: string) => ['tasks', 'settings', 'statuses', code] as const,
  subtasks: (id: string) => ['tasks', 'subtasks', id] as const,
  dependencies: (id: string) => ['tasks', 'dependencies', id] as const,
  notes: (id: string) => ['tasks', 'notes', id] as const,
  documents: (id: string) => ['tasks', 'documents', id] as const,
  dailyReports: (id: string) => ['tasks', 'daily-reports', id] as const,
};
