// Mirrors the backend Event/Asset Task Management DTOs. A task is identified by its
// event id. Status & priority are generic statuses (status_types/statuses).
// Enums are serialized as numbers by the API; order MUST match Erp.Domain.Events.

export enum EventActivityKind {
  Created = 0,
  Updated = 1,
  Assigned = 2,
  StatusChanged = 3,
  PriorityChanged = 4,
  Scheduled = 5,
  Archived = 6,
  SubtaskAdded = 7,
  NoteAdded = 8,
  DocumentAdded = 9,
  RelationChanged = 10,
  DailyReportAdded = 11,
}

/** Status type codes (for the status/priority dropdowns). */
export const TASK_STATUS = 'TASK_STATUS';
export const TASK_PRIORITY = 'TASK_PRIORITY';

export interface StatusOption {
  id: string;
  code: string;
  name: string;
  color: string | null;
  sortOrder: number;
  isInitial: boolean;
  isClosed: boolean;
  isActive: boolean;
}

export interface TaskListItem {
  eventId: string;
  referenceNo: string;
  title: string;
  statusId: string | null;
  statusName: string | null;
  statusColor: string | null;
  statusIsClosed: boolean;
  priorityStatusId: string | null;
  priorityName: string | null;
  priorityColor: string | null;
  assigneeId: string | null;
  assigneeName: string | null;
  dueAt: string | null;
  isOverdue: boolean;
  completionPercent: number;
  createdAt: string;
}

export interface TaskDetails {
  eventId: string;
  referenceNo: string;
  title: string;
  description: string | null;
  statusId: string | null;
  statusName: string | null;
  statusColor: string | null;
  statusIsClosed: boolean;
  priorityStatusId: string | null;
  priorityName: string | null;
  priorityColor: string | null;
  assigneeId: string | null;
  assigneeName: string | null;
  reporterId: string | null;
  reporterName: string | null;
  parentEventId: string | null;
  startAt: string | null;
  dueAt: string | null;
  estimatedTime: number | null;
  actualTime: number | null;
  completionPercent: number;
  isOverdue: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface TaskActivity {
  id: string;
  kind: EventActivityKind;
  message: string;
  actorId: string | null;
  actorName: string | null;
  occurredAt: string;
}

export interface TaskNote {
  id: string;
  body: string;
  isPinned: boolean;
  isInternal: boolean;
  authorId: string | null;
  authorName: string | null;
  createdAt: string;
}

export interface TaskDocument {
  id: string;
  fileName: string;
  filePath: string;
  mimeType: string | null;
  uploadedById: string | null;
  uploadedByName: string | null;
  createdAt: string;
}

export interface TaskDependency {
  id: string;
  dependsOnEventId: string;
  dependsOnReferenceNo: string;
  dependsOnTitle: string;
  isBlocking: boolean;
}

export interface TaskDailyReport {
  id: string;
  reportDate: string;
  description: string;
  estimatedTime: number | null;
  actualTime: number | null;
  remainingTime: number | null;
  statusId: string | null;
  statusName: string | null;
  statusColor: string | null;
  authorId: string | null;
  authorName: string | null;
  createdAt: string;
}

export interface TaskDailyReportRow extends TaskDailyReport {
  eventId: string;
  referenceNo: string;
  taskTitle: string;
}

export interface TaskAudit {
  id: string;
  action: string;
  createdAt: string;
  actorUserId: string | null;
  actorName: string | null;
}

export interface MyTasksGroups {
  overdue: TaskListItem[];
  today: TaskListItem[];
  upcoming: TaskListItem[];
  waiting: TaskListItem[];
}

export interface CreateTaskResult {
  eventId: string;
  referenceNo: string;
}

export interface TaskBucket {
  id: string | null;
  name: string | null;
  color: string | null;
  count: number;
}

export interface TaskAssigneeLoad {
  assigneeId: string | null;
  assigneeName: string | null;
  open: number;
  overdue: number;
}

export interface TaskTrendPoint {
  date: string;
  created: number;
  completed: number;
}

export interface TaskRecentActivity {
  id: string;
  eventId: string;
  referenceNo: string;
  message: string;
  actorId: string | null;
  actorName: string | null;
  occurredAt: string;
}

export interface TaskGanttItem {
  eventId: string;
  referenceNo: string;
  title: string;
  startAt: string | null;
  dueAt: string | null;
  completionPercent: number;
  statusColor: string | null;
  isClosed: boolean;
}

export interface TaskDashboard {
  total: number;
  open: number;
  inProgress: number;
  overdue: number;
  dueToday: number;
  dueThisWeek: number;
  highPriority: number;
  completed: number;
  unassigned: number;
  completedLast7Days: number;
  reportsToday: number;
  avgCompletionPercent: number;
  estimatedTotal: number;
  actualTotal: number;
  byStatus: TaskBucket[];
  byPriority: TaskBucket[];
  byAssignee: TaskAssigneeLoad[];
  trend: TaskTrendPoint[];
  recentActivity: TaskRecentActivity[];
  gantt: TaskGanttItem[];
}

export interface TaskReport {
  total: number;
  open: number;
  completed: number;
  overdue: number;
  estimatedTotal: number;
  actualTotal: number;
  byStatus: TaskBucket[];
  byPriority: TaskBucket[];
  byAssignee: TaskAssigneeLoad[];
}

// ---- request bodies ----
export interface CreateTaskBody {
  title: string;
  description: string | null;
  assigneeId: string | null;
  priorityStatusId: string | null;
  startAt: string | null;
  dueAt: string | null;
  estimatedTime: number | null;
}

export interface UpdateTaskBody {
  title: string;
  description: string | null;
  startAt: string | null;
  dueAt: string | null;
  estimatedTime: number | null;
  actualTime: number | null;
  completionPercent: number;
}

export interface ChangeStatusBody {
  statusId: string;
  note: string | null;
}
export interface AssignBody {
  assigneeId: string | null;
}
export interface SetPriorityBody {
  priorityStatusId: string | null;
}
export interface CreateNoteBody {
  body: string;
  isPinned: boolean;
  isInternal: boolean;
}
export interface UpdateNoteBody {
  body: string;
  isPinned: boolean;
  isInternal: boolean;
}
export interface CreateDocumentBody {
  fileName: string;
  filePath: string;
  mimeType: string | null;
}
export interface CreateDependencyBody {
  dependsOnEventId: string;
  isBlocking: boolean;
}
export interface CreateStatusBody {
  statusTypeCode: string;
  name: string;
  color: string | null;
  isClosed: boolean;
  isInitial: boolean;
}
export interface UpdateStatusBody {
  name: string;
  color: string | null;
  isClosed: boolean;
  isInitial: boolean;
  isActive: boolean;
}
export interface ReorderStatusesBody {
  statusTypeCode: string;
  orderedIds: string[];
}
export interface CreateDailyReportBody {
  reportDate: string | null;
  description: string;
  estimatedTime: number | null;
  actualTime: number | null;
  remainingTime: number | null;
  statusId: string | null;
}
export interface UpdateDailyReportBody {
  reportDate: string | null;
  description: string;
  estimatedTime: number | null;
  actualTime: number | null;
  remainingTime: number | null;
  statusId: string | null;
}

// ---- task settings (workspace config) ----
export interface TaskSettings {
  dailyReportRequired: boolean;
  allowStatusChangeFromReport: boolean;
  requireActualTime: boolean;
  requireEstimatedTime: boolean;
  allowMultipleReportsPerDay: boolean;
  notifyOnTaskCreated: boolean;
  notifyOnTaskAssigned: boolean;
  notifyOnStatusChange: boolean;
  notifyOnDailyReport: boolean;
  dashboardDefaultRangeDays: number;
}

export type UpdateTaskSettingsBody = TaskSettings;
