import { api } from '@/shared/api/client';
import { createCrudApi } from '@/shared/api/crud';
import type { ListParams } from '@/shared/api/types';
import type {
  AssignBody,
  ChangeStatusBody,
  ChecklistItem,
  CreateChecklistItemBody,
  CreateDependencyBody,
  CreateDocumentBody,
  CreateNoteBody,
  CreateRelationBody,
  CreateStatusBody,
  CreateStatusTypeBody,
  CreateTaskBody,
  CreateTaskResult,
  MyTasksGroups,
  TaskActivity,
  TaskAudit,
  TaskDependency,
  TaskDetails,
  TaskDocument,
  TaskListItem,
  TaskNote,
  TaskRelation,
  TaskWorkflowDto,
  UpdateChecklistItemBody,
  UpdateStatusBody,
  UpdateStatusTypeBody,
  UpdateTaskBody,
} from './types';

/** Standard list/get/create/update/archive (Refactor Guide §10.3) + task-specific commands. */
const crud = createCrudApi<TaskListItem, TaskDetails, CreateTaskBody, UpdateTaskBody, CreateTaskResult>('/tasks');

export const tasksApi = {
  ...crud,
  list: (params?: ListParams) => crud.list(params),
  changeStatus: (id: string, body: ChangeStatusBody) => api.post<void>(`/tasks/${id}/status`, body),
  assign: (id: string, body: AssignBody) => api.post<void>(`/tasks/${id}/assign`, body),
  activity: (id: string) => api.get<TaskActivity[]>(`/tasks/${id}/activity`),
  audit: (id: string) => api.get<TaskAudit[]>(`/tasks/${id}/audit`),
  my: () => api.get<MyTasksGroups>('/tasks/my'),

  subtasks: (id: string) => api.get<TaskListItem[]>(`/tasks/${id}/subtasks`),
  createSubtask: (id: string, body: CreateTaskBody) => api.post<CreateTaskResult>(`/tasks/${id}/subtasks`, body),

  checklist: (id: string) => api.get<ChecklistItem[]>(`/tasks/${id}/checklist`),
  addChecklistItem: (id: string, body: CreateChecklistItemBody) => api.post<{ id: string }>(`/tasks/${id}/checklist`, body),
  updateChecklistItem: (id: string, itemId: string, body: UpdateChecklistItemBody) => api.put<void>(`/tasks/${id}/checklist/${itemId}`, body),
  removeChecklistItem: (id: string, itemId: string) => api.delete<void>(`/tasks/${id}/checklist/${itemId}`),

  dependencies: (id: string) => api.get<TaskDependency[]>(`/tasks/${id}/dependencies`),
  addDependency: (id: string, body: CreateDependencyBody) => api.post<{ id: string }>(`/tasks/${id}/dependencies`, body),
  removeDependency: (id: string, depId: string) => api.delete<void>(`/tasks/${id}/dependencies/${depId}`),

  relations: (id: string) => api.get<TaskRelation[]>(`/tasks/${id}/relations`),
  addRelation: (id: string, body: CreateRelationBody) => api.post<{ id: string }>(`/tasks/${id}/relations`, body),
  removeRelation: (id: string, relId: string) => api.delete<void>(`/tasks/${id}/relations/${relId}`),
  refreshRelations: (id: string) => api.post<TaskRelation[]>(`/tasks/${id}/relations/refresh`),

  notes: (id: string) => api.get<TaskNote[]>(`/tasks/${id}/notes`),
  addNote: (id: string, body: CreateNoteBody) => api.post<{ id: string }>(`/tasks/${id}/notes`, body),
  removeNote: (id: string, noteId: string) => api.delete<void>(`/tasks/${id}/notes/${noteId}`),

  documents: (id: string) => api.get<TaskDocument[]>(`/tasks/${id}/documents`),
  addDocument: (id: string, body: CreateDocumentBody) => api.post<{ id: string }>(`/tasks/${id}/documents`, body),
  removeDocument: (id: string, docId: string) => api.delete<void>(`/tasks/${id}/documents/${docId}`),
};

export const workflowsApi = {
  list: () => api.get<TaskWorkflowDto[]>('/task-workflows'),
  createType: (body: CreateStatusTypeBody) => api.post<{ id: string }>('/task-workflows/types', body),
  updateType: (id: string, body: UpdateStatusTypeBody) => api.put<void>(`/task-workflows/types/${id}`, body),
  archiveType: (id: string) => api.delete<void>(`/task-workflows/types/${id}`),
  createStatus: (body: CreateStatusBody) => api.post<{ id: string }>('/task-workflows/statuses', body),
  updateStatus: (id: string, body: UpdateStatusBody) => api.put<void>(`/task-workflows/statuses/${id}`, body),
  archiveStatus: (id: string) => api.delete<void>(`/task-workflows/statuses/${id}`),
};

export const taskKeys = {
  all: ['tasks'] as const,
  list: (params: ListParams) => ['tasks', 'list', params] as const,
  detail: (id: string) => ['tasks', 'detail', id] as const,
  activity: (id: string) => ['tasks', 'activity', id] as const,
  audit: (id: string) => ['tasks', 'audit', id] as const,
  my: ['tasks', 'my'] as const,
  subtasks: (id: string) => ['tasks', 'subtasks', id] as const,
  checklist: (id: string) => ['tasks', 'checklist', id] as const,
  dependencies: (id: string) => ['tasks', 'dependencies', id] as const,
  relations: (id: string) => ['tasks', 'relations', id] as const,
  notes: (id: string) => ['tasks', 'notes', id] as const,
  documents: (id: string) => ['tasks', 'documents', id] as const,
  workflows: ['tasks', 'workflows'] as const,
};
