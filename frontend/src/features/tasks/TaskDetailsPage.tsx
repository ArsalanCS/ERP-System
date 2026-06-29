import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Card, Button, Icon, Select, Spinner, EmptyState, LoadingBlock } from '@/shared/ui';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { useDirection } from '@/shared/hooks/useDirection';
import { useToast } from '@/shared/ui/toast-context';
import { formatDate, formatDateTime } from '@/shared/lib/format';
import { initialsOf } from '@/shared/rbac/session';
import { usersApi, userKeys } from '@/features/users/api';
import { tasksApi, taskKeys } from './api';
import { TASK_PRIORITY, TASK_STATUS } from './types';
import { TaskStatusBadge } from './TaskStatusBadge';
import { TaskFormDrawer } from './TaskFormDrawer';
import { SubtasksTab } from './tabs/SubtasksTab';
import { NotesTab } from './tabs/NotesTab';
import { DailyReportsTab } from './tabs/DailyReportsTab';
import { DocumentsTab } from './tabs/DocumentsTab';
import { RelationsTab } from './tabs/RelationsTab';
import { GanttTab } from './tabs/GanttTab';
import { AuditTab } from './tabs/AuditTab';

type Tab = 'general' | 'planning' | 'subtasks' | 'dailyReports' | 'notes' | 'documents' | 'relations' | 'gantt' | 'logs' | 'audit';

export function TaskDetailsPage() {
  const { id = '' } = useParams();
  const { t } = useTranslation();
  const { locale } = useDirection();
  const { can } = usePermissions();
  const toast = useToast();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [tab, setTab] = useState<Tab>('general');
  const [editOpen, setEditOpen] = useState(false);

  const canChangeStatus = can(Actions.TasksChangeStatus);
  const canAssign = can(Actions.TasksAssign);
  const canUpdate = can(Actions.TasksUpdate);
  const canCreate = can(Actions.TasksCreate);
  const canViewUsers = can(Actions.UsersView);
  const canNoteManage = can(Actions.TaskNoteManage);
  const canDailyReportManage = can(Actions.TaskDailyReportManage);
  const canDocManage = can(Actions.TaskDocumentManage);
  const canAuditView = can(Actions.TaskAuditView);

  const detailQuery = useQuery({ queryKey: taskKeys.detail(id), queryFn: () => tasksApi.get(id), enabled: !!id });
  const activityQuery = useQuery({ queryKey: taskKeys.activity(id), queryFn: () => tasksApi.activity(id), enabled: !!id && tab === 'logs' });
  const statusesQuery = useQuery({ queryKey: taskKeys.statuses(TASK_STATUS), queryFn: () => tasksApi.statuses(TASK_STATUS), enabled: !!id && canChangeStatus });
  const prioritiesQuery = useQuery({ queryKey: taskKeys.statuses(TASK_PRIORITY), queryFn: () => tasksApi.statuses(TASK_PRIORITY), enabled: !!id && canUpdate });
  const usersQuery = useQuery({
    queryKey: userKeys.list({ page: 1, pageSize: 100 }),
    queryFn: () => usersApi.list({ page: 1, pageSize: 100 }),
    enabled: !!id && canAssign && canViewUsers,
  });

  const task = detailQuery.data;

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: taskKeys.detail(id) });
    void queryClient.invalidateQueries({ queryKey: taskKeys.activity(id) });
    void queryClient.invalidateQueries({ queryKey: taskKeys.all });
  };

  const statusMutation = useMutation({
    mutationFn: (statusId: string) => tasksApi.changeStatus(id, { statusId, note: null }),
    onSuccess: () => { invalidate(); toast.success(t('tasks.feedback.statusChanged')); },
    onError: (err) => toast.error(err instanceof Error ? err.message : t('common.loadError')),
  });
  const assignMutation = useMutation({
    mutationFn: (assigneeId: string) => tasksApi.assign(id, { assigneeId: assigneeId || null }),
    onSuccess: () => { invalidate(); toast.success(t('tasks.feedback.assigned')); },
    onError: (err) => toast.error(err instanceof Error ? err.message : t('common.loadError')),
  });
  const priorityMutation = useMutation({
    mutationFn: (priorityStatusId: string) => tasksApi.setPriority(id, { priorityStatusId: priorityStatusId || null }),
    onSuccess: () => { invalidate(); toast.success(t('tasks.feedback.updated')); },
    onError: (err) => toast.error(err instanceof Error ? err.message : t('common.loadError')),
  });

  if (detailQuery.isLoading) return <Card className="card-pad"><LoadingBlock label={t('common.loading')} /></Card>;
  if (detailQuery.isError || !task) {
    return (
      <EmptyState icon="alert" title={t('common.loadError')}
        action={<Button variant="outline" size="sm" onClick={() => navigate('/admin/tasks')}>{t('common.back')}</Button>} />
    );
  }

  const statusOptions = (statusesQuery.data ?? []).map((s) => ({ value: s.id, label: s.name }));
  const priorityOptions = [
    { value: '', label: t('tasks.fields.noPriority') },
    ...(prioritiesQuery.data ?? []).map((p) => ({ value: p.id, label: p.name })),
  ];
  const userOptions = [
    { value: '', label: t('tasks.fields.unassigned') },
    ...(usersQuery.data?.items ?? []).map((u) => ({ value: u.id, label: u.displayName })),
  ];

  const tabs: { id: Tab; label: string }[] = [
    { id: 'general', label: t('tasks.tabs.general') },
    { id: 'planning', label: t('tasks.tabs.planning') },
    { id: 'subtasks', label: t('tasks.tabs.subtasks') },
    { id: 'dailyReports', label: t('tasks.tabs.dailyReports') },
    { id: 'notes', label: t('tasks.tabs.notes') },
    { id: 'documents', label: t('tasks.tabs.documents') },
    { id: 'relations', label: t('tasks.tabs.relations') },
    { id: 'gantt', label: t('tasks.tabs.gantt') },
    { id: 'logs', label: t('tasks.tabs.logs') },
    ...(canAuditView ? [{ id: 'audit' as Tab, label: t('tasks.tabs.audit') }] : []),
  ];

  return (
    <>
      <div className="mb-4 flex items-start justify-between gap-4">
        <div className="min-w-0">
          <button type="button" onClick={() => navigate('/admin/tasks')}
            className="mb-2 inline-flex items-center gap-1 text-[12.5px] text-ink-4 hover:text-clay">
            <Icon name="chevron-left" size={14} /> {t('tasks.title')}
          </button>
          <div className="flex items-center gap-3">
            <h1 className="truncate text-[22px] font-semibold tracking-[-0.01em] text-ink">{task.title}</h1>
            <TaskStatusBadge name={task.statusName} color={task.statusColor} />
          </div>
          <div className="mt-1 font-mono text-[12px] text-ink-4">{task.referenceNo}</div>
        </div>
        <div className="flex flex-none items-center gap-2">
          {canChangeStatus && statusOptions.length > 0 && (
            <Select value={task.statusId ?? ''} onChange={(e) => statusMutation.mutate(e.target.value)} options={statusOptions} disabled={statusMutation.isPending} />
          )}
          {canUpdate && (
            <Button variant="outline" leadingIcon={<Icon name="edit" size={15} />} onClick={() => setEditOpen(true)}>
              {t('common.edit')}
            </Button>
          )}
        </div>
      </div>

      <div className="mb-4 flex items-center gap-1 overflow-x-auto border-b border-stone-150">
        {tabs.map((x) => (
          <button key={x.id} type="button" onClick={() => setTab(x.id)}
            className={`-mb-px flex-none whitespace-nowrap border-b-2 px-3.5 py-2 text-[13.5px] font-medium transition-colors ${
              tab === x.id ? 'border-clay text-ink' : 'border-transparent text-ink-4 hover:text-ink-2'}`}>
            {x.label}
          </button>
        ))}
      </div>

      {tab === 'general' && (
        <Card className="card-pad">
          <dl className="grid grid-cols-2 gap-x-8 gap-y-4 text-[13.5px]">
            <Field label={t('tasks.fields.priority')}>
              {canUpdate ? (
                <Select value={task.priorityStatusId ?? ''} options={priorityOptions}
                  onChange={(e) => priorityMutation.mutate(e.target.value)} disabled={priorityMutation.isPending} />
              ) : (
                <TaskStatusBadge name={task.priorityName} color={task.priorityColor} />
              )}
            </Field>
            <Field label={t('tasks.fields.completion')}>
              <div className="flex items-center gap-2">
                <div className="h-1.5 w-32 overflow-hidden rounded-full bg-stone-150">
                  <div className="h-full rounded-full bg-clay" style={{ width: `${task.completionPercent}%` }} />
                </div>
                <span className="text-ink-3">{task.completionPercent}%</span>
              </div>
            </Field>
            <Field label={t('tasks.fields.reporter')}>{task.reporterName ?? '—'}</Field>
            <Field label={t('tasks.fields.assignee')}>
              {canAssign && canViewUsers ? (
                <Select value={task.assigneeId ?? ''} options={userOptions}
                  onChange={(e) => assignMutation.mutate(e.target.value)} disabled={assignMutation.isPending} />
              ) : (
                task.assigneeName ?? '—'
              )}
            </Field>
            <div className="col-span-2">
              <dt className="mb-1 text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{t('tasks.fields.description')}</dt>
              <dd className="whitespace-pre-wrap text-ink-2">{task.description || '—'}</dd>
            </div>
          </dl>
        </Card>
      )}

      {tab === 'planning' && (
        <Card className="card-pad">
          <dl className="grid grid-cols-2 gap-x-8 gap-y-4 text-[13.5px]">
            <Field label={t('tasks.fields.startDate')}>{task.startAt ? formatDate(task.startAt, locale) : '—'}</Field>
            <Field label={t('tasks.fields.dueDate')}>
              <span className={task.isOverdue ? 'font-semibold text-red' : ''}>
                {task.dueAt ? formatDate(task.dueAt, locale) : '—'}
                {task.isOverdue ? ` · ${t('tasks.overdue')}` : ''}
              </span>
            </Field>
            <Field label={t('tasks.fields.estimatedHours')}>{task.estimatedTime ?? '—'}</Field>
            <Field label={t('tasks.fields.actualHours')}>{task.actualTime ?? '—'}</Field>
          </dl>
        </Card>
      )}

      {tab === 'logs' && (
        <Card className="card-pad">
          {activityQuery.isLoading ? (
            <div className="flex justify-center py-8"><Spinner size={22} /></div>
          ) : (activityQuery.data ?? []).length === 0 ? (
            <EmptyState icon="history" title={t('tasks.logs.empty')} />
          ) : (
            <ul className="flex flex-col gap-3">
              {activityQuery.data!.map((a) => (
                <li key={a.id} className="flex items-start gap-3">
                  <span className="avatar !h-7 !w-7 !text-[10px]" style={{ background: 'var(--color-stone-700)' }}>
                    {a.actorName ? initialsOf(a.actorName) : '•'}
                  </span>
                  <div className="min-w-0 flex-1">
                    <div className="text-[13px] text-ink-2">{a.message}</div>
                    <div className="text-[11.5px] text-ink-4">
                      {a.actorName ? `${a.actorName} · ` : ''}{formatDateTime(a.occurredAt, locale)}
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Card>
      )}

      {tab === 'subtasks' && <SubtasksTab taskId={id} canCreate={canCreate} />}
      {tab === 'dailyReports' && <DailyReportsTab taskId={id} canManage={canDailyReportManage} />}
      {tab === 'notes' && <NotesTab taskId={id} canManage={canNoteManage} />}
      {tab === 'documents' && <DocumentsTab taskId={id} canManage={canDocManage} />}
      {tab === 'relations' && <RelationsTab taskId={id} canEdit={canUpdate} />}
      {tab === 'gantt' && <GanttTab task={task} />}
      {tab === 'audit' && canAuditView && <AuditTab taskId={id} />}

      <TaskFormDrawer
        open={editOpen}
        mode="edit"
        taskId={id}
        onClose={() => setEditOpen(false)}
        onSaved={() => { setEditOpen(false); invalidate(); toast.success(t('tasks.feedback.updated')); }}
      />
    </>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="mb-1 text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{label}</dt>
      <dd className="text-ink-2">{children}</dd>
    </div>
  );
}
