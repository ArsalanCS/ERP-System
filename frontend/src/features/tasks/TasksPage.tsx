import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  PageHeader, Button, Card, Input, Select, Icon, DataTable, Pagination, EmptyState, ConfirmDialog, LoadingBlock, type Column,
} from '@/shared/ui';
import { Can } from '@/shared/rbac/Can';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { useDirection } from '@/shared/hooks/useDirection';
import { useToast } from '@/shared/ui/toast-context';
import { formatDate } from '@/shared/lib/format';
import type { ListParams } from '@/shared/api/types';
import { tasksApi, taskKeys } from './api';
import { TaskPriority, TaskStatusCategory, type MyTasksGroups, type TaskListItem } from './types';
import { TaskStatusBadge } from './TaskStatusBadge';
import { TaskFormDrawer } from './TaskFormDrawer';

const PAGE_SIZE = 25;

export function TasksPage() {
  const { t } = useTranslation();
  const { locale } = useDirection();
  const { can } = usePermissions();
  const toast = useToast();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [priority, setPriority] = useState('');
  const [overdue, setOverdue] = useState(false);

  const [drawer, setDrawer] = useState<{ open: boolean; mode: 'create' | 'edit'; taskId?: string }>({ open: false, mode: 'create' });
  const [archiveTarget, setArchiveTarget] = useState<TaskListItem | null>(null);
  const [mine, setMine] = useState(false);

  const myQuery = useQuery({ queryKey: taskKeys.my, queryFn: tasksApi.my, enabled: mine });

  useEffect(() => {
    const id = window.setTimeout(() => { setSearch(searchInput.trim()); setPage(1); }, 350);
    return () => window.clearTimeout(id);
  }, [searchInput]);

  const params = useMemo<ListParams>(() => ({
    page, pageSize: PAGE_SIZE,
    search: search || undefined,
    category: category === '' ? undefined : Number(category),
    priority: priority === '' ? undefined : Number(priority),
    overdue: overdue ? true : undefined,
  }), [page, search, category, priority, overdue]);

  const listQuery = useQuery({ queryKey: taskKeys.list(params), queryFn: () => tasksApi.list(params) });

  const archiveMutation = useMutation({
    mutationFn: (task: TaskListItem) => tasksApi.archive(task.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: taskKeys.all });
      toast.success(t('tasks.feedback.archived'));
      setArchiveTarget(null);
    },
    onError: (err) => { toast.error(err instanceof Error ? err.message : t('common.loadError')); setArchiveTarget(null); },
  });

  const categoryOptions = [
    { value: '', label: t('tasks.filters.allStatuses') },
    ...[TaskStatusCategory.Open, TaskStatusCategory.InProgress, TaskStatusCategory.Waiting,
      TaskStatusCategory.Review, TaskStatusCategory.Completed, TaskStatusCategory.Cancelled, TaskStatusCategory.Rejected]
      .map((c) => ({ value: String(c), label: t(`tasks.category.${c}`) })),
  ];
  const priorityOptions = [
    { value: '', label: t('tasks.filters.allPriorities') },
    ...[TaskPriority.Low, TaskPriority.Normal, TaskPriority.High, TaskPriority.Urgent]
      .map((p) => ({ value: String(p), label: t(`tasks.priority.${p}`) })),
  ];

  const canManage = can(Actions.TasksUpdate);
  const canArchive = can(Actions.TasksArchive);

  const columns: Column<TaskListItem>[] = [
    {
      key: 'task', header: t('tasks.columns.task'),
      render: (x) => (
        <div className="min-w-0">
          <div className="truncate font-semibold text-ink">{x.title}</div>
          <div className="font-mono text-[11.5px] text-ink-4">{x.taskNumber}</div>
        </div>
      ),
    },
    { key: 'status', header: t('tasks.columns.status'), render: (x) => <TaskStatusBadge name={x.statusName} category={x.statusCategory} /> },
    { key: 'priority', header: t('tasks.columns.priority'), render: (x) => <span className="text-[13px] text-ink-2">{t(`tasks.priority.${x.priority}`)}</span> },
    { key: 'assignee', header: t('tasks.columns.assignee'), render: (x) => <span className="text-[13px] text-ink-3">{x.assigneeName ?? '—'}</span> },
    {
      key: 'due', header: t('tasks.columns.due'),
      render: (x) => x.dueDate
        ? <span className={`text-[13px] ${x.isOverdue ? 'font-semibold text-red' : 'text-ink-3'}`}>{formatDate(x.dueDate, locale)}</span>
        : <span className="text-ink-4">—</span>,
    },
  ];

  if (canManage || canArchive) {
    columns.push({
      key: 'actions', header: t('tasks.columns.actions'), align: 'end', cellClassName: 'whitespace-nowrap',
      render: (x) => (
        <div className="flex items-center justify-end gap-1" onClick={(e) => e.stopPropagation()}>
          {canManage && (
            <button type="button" title={t('common.edit')} onClick={() => setDrawer({ open: true, mode: 'edit', taskId: x.id })}
              className="flex h-8 w-8 items-center justify-center rounded-sm text-ink-4 hover:bg-stone-100 hover:text-ink">
              <Icon name="edit" size={16} />
            </button>
          )}
          {canArchive && (
            <button type="button" title={t('tasks.archive')} onClick={() => setArchiveTarget(x)}
              className="flex h-8 w-8 items-center justify-center rounded-sm text-ink-4 hover:bg-stone-100 hover:text-red">
              <Icon name="trash" size={16} />
            </button>
          )}
        </div>
      ),
    });
  }

  const data = listQuery.data;

  return (
    <>
      <PageHeader
        title={t('tasks.title')}
        subtitle={t('tasks.subtitle')}
        actions={
          <div className="flex items-center gap-2">
            <Button variant={mine ? 'primary' : 'outline'} onClick={() => setMine((v) => !v)}>
              {mine ? t('tasks.allTasks') : t('tasks.myTasks')}
            </Button>
            <Can action={Actions.TaskWorkflowManage}>
              <Button variant="outline" onClick={() => navigate('/admin/tasks/workflows')}>{t('tasks.manageWorkflows')}</Button>
            </Can>
            <Can action={Actions.TasksCreate}>
              <Button leadingIcon={<Icon name="plus" size={16} />} onClick={() => setDrawer({ open: true, mode: 'create' })}>
                {t('tasks.newTask')}
              </Button>
            </Can>
          </div>
        }
      />

      {mine ? (
        <MyTasksView groups={myQuery.data} loading={myQuery.isLoading} onOpen={(taskId) => navigate(`/admin/tasks/${taskId}`)} />
      ) : (
      <>
      {/* all-tasks view */}

      <div className="mb-4 flex flex-wrap items-end gap-3">
        <div className="min-w-[220px] flex-1">
          <Input placeholder={t('tasks.searchPlaceholder')} value={searchInput} onChange={(e) => setSearchInput(e.target.value)} />
        </div>
        <div className="w-[180px]"><Select options={categoryOptions} value={category} onChange={(e) => { setCategory(e.target.value); setPage(1); }} /></div>
        <div className="w-[160px]"><Select options={priorityOptions} value={priority} onChange={(e) => { setPriority(e.target.value); setPage(1); }} /></div>
        <label className="flex h-9 cursor-pointer items-center gap-2 text-sm text-ink-2">
          <input type="checkbox" className="h-4 w-4 accent-clay" checked={overdue} onChange={(e) => { setOverdue(e.target.checked); setPage(1); }} />
          {t('tasks.filters.overdue')}
        </label>
      </div>

      {listQuery.isError ? (
        <EmptyState icon="alert" title={t('common.loadError')}
          action={<Button variant="outline" size="sm" onClick={() => listQuery.refetch()}>{t('common.retry')}</Button>} />
      ) : (
        <>
          <DataTable
            columns={columns}
            rows={data?.items ?? []}
            rowKey={(x) => x.id}
            loading={listQuery.isLoading}
            loadingLabel={t('common.loading')}
            onRowClick={(x) => navigate(`/admin/tasks/${x.id}`)}
            empty={<EmptyState icon="check" title={t('tasks.empty.title')} body={t('tasks.empty.body')} />}
          />
          {data && data.total > PAGE_SIZE && (
            <Pagination page={data.page} pageSize={data.pageSize} total={data.total} onPageChange={setPage} />
          )}
        </>
      )}
      </>
      )}

      <TaskFormDrawer
        open={drawer.open}
        mode={drawer.mode}
        taskId={drawer.taskId}
        onClose={() => setDrawer((d) => ({ ...d, open: false }))}
        onSaved={(kind) => { setDrawer((d) => ({ ...d, open: false })); toast.success(t(`tasks.feedback.${kind}`)); }}
      />

      <ConfirmDialog
        open={archiveTarget !== null}
        title={t('tasks.archive')}
        message={t('tasks.archiveConfirm', { title: archiveTarget?.title ?? '' })}
        confirmLabel={t('tasks.archive')}
        tone="danger"
        loading={archiveMutation.isPending}
        onCancel={() => setArchiveTarget(null)}
        onConfirm={() => archiveTarget && archiveMutation.mutate(archiveTarget)}
      />
    </>
  );
}

function MyTasksView({ groups, loading, onOpen }: {
  groups: MyTasksGroups | undefined; loading: boolean; onOpen: (taskId: string) => void;
}) {
  const { t } = useTranslation();
  const { locale } = useDirection();
  if (loading || !groups) {
    return <Card className="card-pad"><LoadingBlock label={t('common.loading')} /></Card>;
  }
  const sections: { key: keyof MyTasksGroups; label: string }[] = [
    { key: 'overdue', label: t('tasks.my.overdue') },
    { key: 'today', label: t('tasks.my.today') },
    { key: 'upcoming', label: t('tasks.my.upcoming') },
    { key: 'waiting', label: t('tasks.my.waiting') },
  ];
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      {sections.map((s) => (
        <Card key={s.key} className="card-pad">
          <div className="mb-2 flex items-center justify-between">
            <span className="text-[13px] font-semibold text-ink">{s.label}</span>
            <span className="text-[12px] text-ink-4">{groups[s.key].length}</span>
          </div>
          {groups[s.key].length === 0 ? (
            <p className="py-3 text-[12.5px] text-ink-4">{t('tasks.my.none')}</p>
          ) : (
            <ul className="flex flex-col gap-1.5">
              {groups[s.key].map((x) => (
                <li key={x.id}>
                  <button type="button" onClick={() => onOpen(x.id)}
                    className="w-full rounded-md border border-stone-150 px-2.5 py-2 text-start hover:border-clay">
                    <div className="truncate text-[13px] font-medium text-ink">{x.title}</div>
                    <div className="flex items-center gap-2">
                      <TaskStatusBadge name={x.statusName} category={x.statusCategory} />
                      {x.dueDate && <span className={`text-[11px] ${x.isOverdue ? 'text-red' : 'text-ink-4'}`}>{formatDate(x.dueDate, locale)}</span>}
                    </div>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </Card>
      ))}
    </div>
  );
}
