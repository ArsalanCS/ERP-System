/**
 * Action-based RBAC primitives (frontend mirror only).
 *
 * The backend is always the source of truth for authorization (CLAUDE.md §4.2,
 * CONVENTIONS "RBAC in the UI"). The UI uses these to HIDE unauthorized nav and
 * actions — never to make a security decision.
 *
 * Permission keys follow `module.action` (e.g. "user.manage"), matching the
 * permission model in the Identity spec §5.2 (module × resource × action).
 */
export type Action = string;

/** Canonical action keys used by the admin shell. Extend per feature slice. */
export const Actions = {
  // page-level visibility gates (Identity spec §2.2 page visibility matrix)
  OverviewView: 'admin.overview.view',
  UsersView: 'user.view',
  UsersManage: 'user.manage',
  UsersInvite: 'user.invite',
  UsersExport: 'user.export',
  AccessControlView: 'role.view',
  AccessControlManage: 'role.manage',
  BusinessStructureView: 'structure.view',
  BusinessStructureManage: 'structure.manage',
  SecurityView: 'security.view',
  SecurityManage: 'security.manage',
  AuditView: 'audit.view',
  AuditExport: 'audit.export',
  SettingsView: 'settings.view',
  SettingsManage: 'settings.manage',
  TasksView: 'task.view',
  TasksCreate: 'task.create',
  TasksUpdate: 'task.update',
  TasksAssign: 'task.assign',
  TasksChangeStatus: 'task.change-status',
  TasksArchive: 'task.archive',
  TaskWorkflowManage: 'task.workflow.manage',
  TaskAuditView: 'task.audit.view',
  TaskNoteManage: 'task.note.manage',
  TaskDocumentManage: 'task.document.manage',
  TaskDailyReportManage: 'task.daily-report.manage',
  MailView: 'mail.view',
  MailManage: 'mail.manage',
} as const;

export type ActionKey = (typeof Actions)[keyof typeof Actions];
