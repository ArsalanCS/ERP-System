/**
 * Frontend mirrors of the Task module contracts. Enums are serialized as numbers
 * by the API, so the order here MUST match the backend enums (Erp.Domain.Tasks).
 */

export enum TaskPriority {
  Low = 0,
  Normal = 1,
  High = 2,
  Urgent = 3,
}

export enum TaskStatusCategory {
  Open = 0,
  InProgress = 1,
  Waiting = 2,
  Review = 3,
  Completed = 4,
  Cancelled = 5,
  Rejected = 6,
}

export enum TaskEventType {
  Task = 1,
}

export enum TaskActivityKind {
  Created = 0,
  Updated = 1,
  Assigned = 2,
  StatusChanged = 3,
  Scheduled = 4,
  Archived = 5,
  SubtaskAdded = 6,
  NoteAdded = 7,
  DocumentAdded = 8,
  RelationChanged = 9,
}

export enum TaskRelationRole {
  PrimarySource = 0,
  Supporting = 1,
  Reference = 2,
}

export enum TaskDependencyType {
  FinishToStart = 0,
  StartToStart = 1,
  FinishToFinish = 2,
  StartToFinish = 3,
}

export interface TaskListItem {
  id: string;
  taskNumber: string;
  title: string;
  priority: TaskPriority;
  statusId: string;
  statusName: string;
  statusCategory: TaskStatusCategory;
  statusColor: string | null;
  assigneeId: string | null;
  assigneeName: string | null;
  dueDate: string | null;
  isOverdue: boolean;
  completionPercent: number;
  createdAt: string;
}

export interface TaskDetails {
  id: string;
  taskNumber: string;
  eventType: TaskEventType;
  title: string;
  description: string | null;
  statusTypeId: string;
  statusId: string;
  statusName: string;
  statusCategory: TaskStatusCategory;
  statusColor: string | null;
  statusIsFinal: boolean;
  priority: TaskPriority;
  assigneeId: string | null;
  assigneeName: string | null;
  reporterId: string | null;
  reporterName: string | null;
  parentTaskId: string | null;
  sourceType: string | null;
  sourceId: string | null;
  startDate: string | null;
  dueDate: string | null;
  estimatedHours: number | null;
  actualHours: number | null;
  reminderAt: string | null;
  completionPercent: number;
  isOverdue: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface TaskActivity {
  id: string;
  kind: TaskActivityKind;
  message: string;
  actorId: string | null;
  actorName: string | null;
  occurredAt: string;
}

export interface TaskStatusDto {
  id: string;
  statusTypeId: string;
  name: string;
  category: TaskStatusCategory;
  color: string | null;
  sortOrder: number;
  isInitial: boolean;
  isFinal: boolean;
}

export interface TaskStatusTypeDto {
  id: string;
  name: string;
  description: string | null;
  isDefault: boolean;
  isActive: boolean;
  sortOrder: number;
}

export interface TaskWorkflowDto {
  type: TaskStatusTypeDto;
  statuses: TaskStatusDto[];
}

// ---- request bodies ----
export interface CreateTaskBody {
  title: string;
  description?: string | null;
  priority: TaskPriority;
  statusTypeId?: string | null;
  assigneeId?: string | null;
  startDate?: string | null;
  dueDate?: string | null;
  estimatedHours?: number | null;
  reminderAt?: string | null;
}

export interface UpdateTaskBody {
  title: string;
  description?: string | null;
  priority: TaskPriority;
  startDate?: string | null;
  dueDate?: string | null;
  estimatedHours?: number | null;
  actualHours?: number | null;
  reminderAt?: string | null;
  completionPercent: number;
}

export interface ChangeStatusBody {
  statusId: string;
}

export interface AssignBody {
  assigneeId: string | null;
}

export interface CreateStatusTypeBody {
  name: string;
  description?: string | null;
}

export interface UpdateStatusTypeBody {
  name: string;
  description?: string | null;
  sortOrder: number;
  isActive: boolean;
  isDefault: boolean;
}

export interface CreateStatusBody {
  statusTypeId: string;
  name: string;
  category: TaskStatusCategory;
  color?: string | null;
  isInitial: boolean;
  isFinal: boolean;
}

export interface UpdateStatusBody {
  name: string;
  category: TaskStatusCategory;
  color?: string | null;
  sortOrder: number;
  isInitial: boolean;
  isFinal: boolean;
}

export interface CreateTaskResult {
  id: string;
  taskNumber: string;
}

// ---- Subtasks / collaboration ----
export interface ChecklistItem {
  id: string;
  text: string;
  isDone: boolean;
  sortOrder: number;
}

export interface TaskNote {
  id: string;
  body: string;
  isPinned: boolean;
  isInternal: boolean;
  authorId: string | null;
  authorName: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface TaskDocument {
  id: string;
  fileName: string;
  fileType: string | null;
  url: string | null;
  note: string | null;
  uploadedBy: string | null;
  uploadedByName: string | null;
  createdAt: string;
}

export interface TaskDependency {
  id: string;
  dependsOnTaskId: string;
  dependsOnNumber: string;
  dependsOnTitle: string;
  dependencyType: TaskDependencyType;
  isBlocking: boolean;
}

export interface TaskRelation {
  id: string;
  relatedEntityType: string;
  relatedEntityId: string;
  role: TaskRelationRole;
  reason: string | null;
}

export interface TaskAudit {
  id: string;
  action: string;
  actorName: string | null;
  occurredAt: string;
  reason: string | null;
}

export interface MyTasksGroups {
  overdue: TaskListItem[];
  today: TaskListItem[];
  upcoming: TaskListItem[];
  waiting: TaskListItem[];
}

export interface CreateChecklistItemBody { text: string }
export interface UpdateChecklistItemBody { text: string; isDone: boolean; sortOrder: number }
export interface CreateNoteBody { body: string; isPinned: boolean; isInternal: boolean }
export interface CreateDocumentBody { fileName: string; fileType?: string | null; url?: string | null; note?: string | null }
export interface CreateDependencyBody { dependsOnTaskId: string; dependencyType: TaskDependencyType; isBlocking: boolean }
export interface CreateRelationBody { relatedEntityType: string; relatedEntityId: string; role: TaskRelationRole; reason?: string | null }
