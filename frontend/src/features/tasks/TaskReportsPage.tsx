import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Card, Icon, Select, Spinner, EmptyState, DataTable, Pagination,
  type Column,
} from '@/shared/ui';
import { useDirection } from '@/shared/hooks/useDirection';
import { formatDate } from '@/shared/lib/format';
import { tasksApi, taskKeys, type DailyReportReportParams } from './api';
import { TASK_PRIORITY, TASK_STATUS, type TaskBucket, type TaskDailyReportRow } from './types';

const PAGE_SIZE = 25;
type Tab = 'tasks' | 'dailyReports';

export function TaskReportsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>('tasks');

  const tabs: { id: Tab; label: string }[] = [
    { id: 'tasks', label: t('tasks.reports.tabs.tasks') },
    { id: 'dailyReports', label: t('tasks.reports.tabs.dailyReports') },
  ];

  return (
    <>
      <div className="mb-4">
        <button type="button" onClick={() => navigate('/admin/tasks')}
          className="mb-2 inline-flex items-center gap-1 text-[12.5px] text-ink-4 hover:text-clay">
          <Icon name="chevron-left" size={14} /> {t('tasks.title')}
        </button>
        <h1 className="text-[22px] font-semibold tracking-[-0.01em] text-ink">{t('tasks.reports.title')}</h1>
        <p className="mt-1 text-[13.5px] text-ink-4">{t('tasks.reports.subtitle')}</p>
      </div>

      <div className="mb-4 flex items-center gap-1 border-b border-stone-150">
        {tabs.map((x) => (
          <button key={x.id} type="button" onClick={() => setTab(x.id)}
            className={`-mb-px border-b-2 px-3.5 py-2 text-[13.5px] font-medium transition-colors ${
              tab === x.id ? 'border-clay text-ink' : 'border-transparent text-ink-4 hover:text-ink-2'}`}>
            {x.label}
          </button>
        ))}
      </div>

      {tab === 'tasks' ? <TaskReportView /> : <DailyReportsReportView />}
    </>
  );
}

function TaskReportView() {
  const { t } = useTranslation();
  const [statusId, setStatusId] = useState('');
  const [priorityId, setPriorityId] = useState('');
  const [scope, setScope] = useState('');

  const statuses = useQuery({ queryKey: taskKeys.statuses(TASK_STATUS), queryFn: () => tasksApi.statuses(TASK_STATUS) });
  const priorities = useQuery({ queryKey: taskKeys.statuses(TASK_PRIORITY), queryFn: () => tasksApi.statuses(TASK_PRIORITY) });

  const params = useMemo(() => ({
    statusId: statusId === '' ? undefined : statusId,
    priorityStatusId: priorityId === '' ? undefined : priorityId,
    overdue: scope === 'overdue' ? true : undefined,
    closedOnly: scope === 'closed' ? true : undefined,
    page: 1,
    pageSize: 1,
  }), [statusId, priorityId, scope]);

  const query = useQuery({ queryKey: taskKeys.report(params), queryFn: () => tasksApi.report(params) });
  const r = query.data;

  return (
    <>
      <div className="mb-4 flex flex-wrap items-end gap-3">
        <Filter label={t('tasks.filters.status')} value={statusId} onChange={setStatusId}
          options={[{ value: '', label: t('tasks.filters.allStatuses') }, ...(statuses.data ?? []).map((s) => ({ value: s.id, label: s.name }))]} />
        <Filter label={t('tasks.filters.priority')} value={priorityId} onChange={setPriorityId}
          options={[{ value: '', label: t('tasks.filters.allPriorities') }, ...(priorities.data ?? []).map((s) => ({ value: s.id, label: s.name }))]} />
        <Filter label={t('tasks.reports.scope')} value={scope} onChange={setScope}
          options={[
            { value: '', label: t('tasks.reports.scopeAll') },
            { value: 'overdue', label: t('tasks.reports.scopeOverdue') },
            { value: 'closed', label: t('tasks.reports.scopeClosed') },
          ]} />
      </div>

      {query.isLoading || !r ? (
        <div className="flex justify-center py-8"><Spinner size={22} /></div>
      ) : (
        <>
          <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
            <Kpi label={t('tasks.dashboard.total')} value={r.total} />
            <Kpi label={t('tasks.dashboard.open')} value={r.open} />
            <Kpi label={t('tasks.dashboard.completed')} value={r.completed} tone="text-green-700" />
            <Kpi label={t('tasks.dashboard.overdue')} value={r.overdue} tone="text-red" />
            <Kpi label={t('tasks.dashboard.estimatedTotal')} value={`${r.estimatedTotal}h`} />
            <Kpi label={t('tasks.dashboard.actualTotal')} value={`${r.actualTotal}h`} />
          </div>
          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <BucketCard title={t('tasks.dashboard.byStatus')} buckets={r.byStatus} total={r.total} empty={t('tasks.fields.noStatus')} />
            <BucketCard title={t('tasks.dashboard.byPriority')} buckets={r.byPriority} total={r.total} empty={t('tasks.fields.noPriority')} />
            <Card className="card-pad">
              <h2 className="mb-3 text-[14px] font-semibold text-ink">{t('tasks.dashboard.byAssignee')}</h2>
              {r.byAssignee.length === 0 ? <p className="text-[13px] text-ink-4">—</p> : (
                <ul className="flex flex-col gap-2 text-[13px]">
                  {r.byAssignee.map((a) => (
                    <li key={a.assigneeId ?? 'none'} className="flex items-center justify-between">
                      <span className="truncate text-ink-2">{a.assigneeName ?? t('tasks.fields.unassigned')}</span>
                      <span className="tabular-nums text-ink-3">{t('tasks.dashboard.openCount', { count: a.open })}</span>
                    </li>
                  ))}
                </ul>
              )}
            </Card>
          </div>
        </>
      )}
    </>
  );
}

function DailyReportsReportView() {
  const { t } = useTranslation();
  const { locale } = useDirection();
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [statusId, setStatusId] = useState('');

  const statuses = useQuery({ queryKey: taskKeys.statuses(TASK_STATUS), queryFn: () => tasksApi.statuses(TASK_STATUS) });

  const params = useMemo<DailyReportReportParams>(() => ({
    page,
    pageSize: PAGE_SIZE,
    fromDate: fromDate === '' ? undefined : fromDate,
    toDate: toDate === '' ? undefined : toDate,
    statusId: statusId === '' ? undefined : statusId,
  }), [page, fromDate, toDate, statusId]);

  const query = useQuery({ queryKey: taskKeys.dailyReportsReport(params), queryFn: () => tasksApi.dailyReportsReport(params) });
  const data = query.data;

  const columns: Column<TaskDailyReportRow>[] = [
    { key: 'date', header: t('tasks.dailyReports.date'), cellClassName: 'whitespace-nowrap',
      render: (r) => formatDate(r.reportDate, locale) },
    { key: 'task', header: t('tasks.reports.task'),
      render: (r) => (
        <button type="button" onClick={() => navigate(`/admin/tasks/${r.eventId}`)} className="text-start hover:text-clay">
          <span className="font-mono text-[11px] text-ink-4">{r.referenceNo}</span> <span className="text-ink-2">{r.taskTitle}</span>
        </button>
      ) },
    { key: 'desc', header: t('tasks.dailyReports.description'), render: (r) => <span className="text-ink-2">{r.description}</span> },
    { key: 'est', header: t('tasks.dailyReports.estimatedShort'), render: (r) => (r.estimatedTime ?? '—') },
    { key: 'act', header: t('tasks.dailyReports.actualShort'), render: (r) => (r.actualTime ?? '—') },
    { key: 'status', header: t('tasks.filters.status'),
      render: (r) => r.statusName
        ? <span className="rounded px-1.5 py-0.5 text-[11px] font-medium"
            style={{ backgroundColor: (r.statusColor ?? '#64748b') + '1a', color: r.statusColor ?? '#64748b' }}>{r.statusName}</span>
        : '—' },
    { key: 'author', header: t('tasks.reports.author'), render: (r) => r.authorName ?? '—' },
  ];

  return (
    <>
      <div className="mb-4 flex flex-wrap items-end gap-3">
        <label className="flex flex-col gap-1 text-[12px] text-ink-4">
          {t('tasks.reports.from')}
          <input type="date" className="input !w-auto" value={fromDate} onChange={(e) => { setFromDate(e.target.value); setPage(1); }} />
        </label>
        <label className="flex flex-col gap-1 text-[12px] text-ink-4">
          {t('tasks.reports.to')}
          <input type="date" className="input !w-auto" value={toDate} onChange={(e) => { setToDate(e.target.value); setPage(1); }} />
        </label>
        <Filter label={t('tasks.filters.status')} value={statusId} onChange={(v) => { setStatusId(v); setPage(1); }}
          options={[{ value: '', label: t('tasks.filters.allStatuses') }, ...(statuses.data ?? []).map((s) => ({ value: s.id, label: s.name }))]} />
      </div>

      <DataTable
        columns={columns}
        rows={data?.items ?? []}
        rowKey={(r) => r.id}
        loading={query.isLoading}
        loadingLabel={t('common.loading')}
        empty={<EmptyState icon="history" title={t('tasks.reports.noReports')} />}
      />
      {data && data.total > PAGE_SIZE && (
        <Pagination page={data.page} pageSize={data.pageSize} total={data.total} onPageChange={setPage} />
      )}
    </>
  );
}

function Filter({ label, value, onChange, options }: {
  label: string; value: string; onChange: (v: string) => void; options: { value: string; label: string }[];
}) {
  return (
    <label className="flex flex-col gap-1 text-[12px] text-ink-4">
      {label}
      <div className="w-[180px]"><Select options={options} value={value} onChange={(e) => onChange(e.target.value)} /></div>
    </label>
  );
}

function Kpi({ label, value, tone }: { label: string; value: string | number; tone?: string }) {
  return (
    <Card className="card-pad">
      <div className="text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{label}</div>
      <div className={`mt-1 text-[24px] font-semibold tabular-nums ${tone ?? 'text-ink'}`}>{value}</div>
    </Card>
  );
}

function BucketCard({ title, buckets, total, empty }: { title: string; buckets: TaskBucket[]; total: number; empty: string }) {
  return (
    <Card className="card-pad">
      <h2 className="mb-3 text-[14px] font-semibold text-ink">{title}</h2>
      {buckets.length === 0 ? <p className="text-[13px] text-ink-4">—</p> : (
        <ul className="flex flex-col gap-2.5">
          {buckets.map((b) => (
            <li key={b.id ?? 'none'}>
              <div className="mb-1 flex items-center justify-between text-[13px]">
                <span className="flex items-center gap-2 text-ink-2">
                  <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ background: b.color ?? '#cbd5e1' }} />
                  {b.name ?? empty}
                </span>
                <span className="tabular-nums text-ink-3">{b.count}</span>
              </div>
              <div className="h-1.5 w-full overflow-hidden rounded-full bg-stone-150">
                <div className="h-full rounded-full" style={{ width: `${total ? (b.count / total) * 100 : 0}%`, background: b.color ?? '#94a3b8' }} />
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
