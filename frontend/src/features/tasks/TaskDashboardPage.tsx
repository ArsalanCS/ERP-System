import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Card, Button, Icon, EmptyState, LoadingBlock } from '@/shared/ui';
import { useDirection } from '@/shared/hooks/useDirection';
import { formatDate, formatDateTime } from '@/shared/lib/format';
import { tasksApi, taskKeys } from './api';
import type { TaskBucket, TaskGanttItem } from './types';

export function TaskDashboardPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { locale } = useDirection();

  const query = useQuery({ queryKey: taskKeys.dashboard, queryFn: tasksApi.dashboard });
  const d = query.data;

  if (query.isLoading) return <Card className="card-pad"><LoadingBlock label={t('common.loading')} /></Card>;
  if (query.isError || !d) {
    return (
      <EmptyState icon="alert" title={t('common.loadError')}
        action={<Button variant="outline" size="sm" onClick={() => query.refetch()}>{t('common.retry')}</Button>} />
    );
  }

  const maxTrend = Math.max(1, ...d.trend.map((p) => Math.max(p.created, p.completed)));

  const kpis = [
    { label: t('tasks.dashboard.total'), value: d.total },
    { label: t('tasks.dashboard.open'), value: d.open },
    { label: t('tasks.dashboard.inProgress'), value: d.inProgress },
    { label: t('tasks.dashboard.overdue'), value: d.overdue, tone: 'text-red' },
    { label: t('tasks.dashboard.dueToday'), value: d.dueToday },
    { label: t('tasks.dashboard.dueThisWeek'), value: d.dueThisWeek },
    { label: t('tasks.dashboard.highPriority'), value: d.highPriority, tone: 'text-amber-700' },
    { label: t('tasks.dashboard.completed'), value: d.completed, tone: 'text-green-700' },
    { label: t('tasks.dashboard.unassigned'), value: d.unassigned },
    { label: t('tasks.dashboard.completed7'), value: d.completedLast7Days },
    { label: t('tasks.dashboard.reportsToday'), value: d.reportsToday },
    { label: t('tasks.dashboard.avgCompletion'), value: `${d.avgCompletionPercent}%` },
  ];

  return (
    <>
      <div className="mb-4 flex items-center justify-between gap-3">
        <div>
          <button type="button" onClick={() => navigate('/admin/tasks')}
            className="mb-2 inline-flex items-center gap-1 text-[12.5px] text-ink-4 hover:text-clay">
            <Icon name="chevron-left" size={14} /> {t('tasks.title')}
          </button>
          <h1 className="text-[22px] font-semibold tracking-[-0.01em] text-ink">{t('tasks.dashboard.title')}</h1>
        </div>
      </div>

      <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
        {kpis.map((k) => (
          <Card key={k.label} className="card-pad">
            <div className="text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{k.label}</div>
            <div className={`mt-1 text-[26px] font-semibold tabular-nums ${k.tone ?? 'text-ink'}`}>{k.value}</div>
          </Card>
        ))}
      </div>

      <div className="mb-4 grid grid-cols-1 gap-4 lg:grid-cols-2">
        <BucketCard title={t('tasks.dashboard.byStatus')} buckets={d.byStatus} total={d.total} emptyLabel={t('tasks.fields.noStatus')} />
        <BucketCard title={t('tasks.dashboard.byPriority')} buckets={d.byPriority} total={d.total} emptyLabel={t('tasks.fields.noPriority')} />
      </div>

      <Card className="card-pad mb-4">
        <h2 className="mb-3 text-[14px] font-semibold text-ink">{t('tasks.dashboard.trend')}</h2>
        <div className="flex items-end gap-1.5" style={{ height: 120 }}>
          {d.trend.map((p) => (
            <div key={p.date} className="flex flex-1 flex-col items-center justify-end gap-0.5" title={`${p.date}: +${p.created} / ✓${p.completed}`}>
              <div className="flex w-full items-end justify-center gap-0.5" style={{ height: 100 }}>
                <div className="w-1/2 rounded-t bg-clay/70" style={{ height: `${(p.created / maxTrend) * 100}%` }} />
                <div className="w-1/2 rounded-t bg-green-600/70" style={{ height: `${(p.completed / maxTrend) * 100}%` }} />
              </div>
              <span className="text-[9px] text-ink-4">{p.date.slice(8, 10)}</span>
            </div>
          ))}
        </div>
        <div className="mt-2 flex items-center gap-4 text-[12px] text-ink-4">
          <span className="flex items-center gap-1.5"><span className="inline-block h-2.5 w-2.5 rounded-sm bg-clay/70" /> {t('tasks.dashboard.created')}</span>
          <span className="flex items-center gap-1.5"><span className="inline-block h-2.5 w-2.5 rounded-sm bg-green-600/70" /> {t('tasks.dashboard.completedLegend')}</span>
        </div>
      </Card>

      <div className="mb-4 grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card className="card-pad">
          <h2 className="mb-3 text-[14px] font-semibold text-ink">{t('tasks.dashboard.byAssignee')}</h2>
          {d.byAssignee.length === 0 ? (
            <EmptyState icon="users" title={t('tasks.dashboard.noData')} />
          ) : (
            <ul className="flex flex-col gap-2">
              {d.byAssignee.map((a) => (
                <li key={a.assigneeId ?? 'none'} className="flex items-center gap-3 text-[13.5px]">
                  <span className="w-48 truncate text-ink-2">{a.assigneeName ?? t('tasks.fields.unassigned')}</span>
                  <span className="text-ink-3">{t('tasks.dashboard.openCount', { count: a.open })}</span>
                  {a.overdue > 0 && <span className="text-red">· {t('tasks.dashboard.overdueCount', { count: a.overdue })}</span>}
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card className="card-pad">
          <h2 className="mb-3 text-[14px] font-semibold text-ink">{t('tasks.dashboard.timeAnalysis')}</h2>
          <div className="flex items-center gap-6">
            <div>
              <div className="text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{t('tasks.dashboard.estimatedTotal')}</div>
              <div className="mt-1 text-[22px] font-semibold tabular-nums text-ink">{d.estimatedTotal}h</div>
            </div>
            <div>
              <div className="text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{t('tasks.dashboard.actualTotal')}</div>
              <div className="mt-1 text-[22px] font-semibold tabular-nums text-ink">{d.actualTotal}h</div>
            </div>
            <div>
              <div className="text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{t('tasks.dashboard.variance')}</div>
              <div className={`mt-1 text-[22px] font-semibold tabular-nums ${d.actualTotal > d.estimatedTotal ? 'text-red' : 'text-green-700'}`}>
                {d.actualTotal - d.estimatedTotal > 0 ? '+' : ''}{d.actualTotal - d.estimatedTotal}h
              </div>
            </div>
          </div>
        </Card>
      </div>

      <Card className="card-pad mb-4">
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-[14px] font-semibold text-ink">{t('tasks.dashboard.gantt')}</h2>
        </div>
        {d.gantt.length === 0 ? (
          <EmptyState icon="calendar" title={t('tasks.dashboard.noData')} />
        ) : (
          <GanttChart items={d.gantt} locale={locale} onPick={(id) => navigate(`/admin/tasks/${id}`)} />
        )}
      </Card>

      <Card className="card-pad">
        <h2 className="mb-3 text-[14px] font-semibold text-ink">{t('tasks.dashboard.recentActivity')}</h2>
        {d.recentActivity.length === 0 ? (
          <EmptyState icon="history" title={t('tasks.dashboard.noData')} />
        ) : (
          <ul className="flex flex-col gap-2">
            {d.recentActivity.map((a) => (
              <li key={a.id} className="flex items-start gap-2 text-[13px]">
                <button type="button" onClick={() => navigate(`/admin/tasks/${a.eventId}`)}
                  className="font-mono text-[12px] text-clay hover:underline">{a.referenceNo}</button>
                <span className="flex-1 text-ink-2">{a.message}</span>
                <span className="whitespace-nowrap text-[12px] text-ink-4">{a.actorName ?? '—'} · {formatDateTime(a.occurredAt, locale)}</span>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </>
  );
}

function GanttChart({ items, locale, onPick }: { items: TaskGanttItem[]; locale: string; onPick: (id: string) => void }) {
  const dates = items.flatMap((i) => [i.startAt, i.dueAt].filter(Boolean) as string[]).map((d) => new Date(d).getTime());
  const min = Math.min(...dates);
  const max = Math.max(...dates);
  const span = Math.max(1, max - min);
  const pct = (ts: number) => ((ts - min) / span) * 100;

  return (
    <ul className="flex flex-col gap-1.5">
      {items.map((i) => {
        const start = i.startAt ? new Date(i.startAt).getTime() : (i.dueAt ? new Date(i.dueAt).getTime() : min);
        const end = i.dueAt ? new Date(i.dueAt).getTime() : start;
        const left = pct(Math.min(start, end));
        const width = Math.max(2, pct(Math.max(start, end)) - left);
        return (
          <li key={i.eventId} className="flex items-center gap-3 text-[12.5px]">
            <button type="button" onClick={() => onPick(i.eventId)}
              className="w-40 flex-none truncate text-start text-ink-2 hover:text-clay" title={i.title}>
              <span className="font-mono text-[11px] text-ink-4">{i.referenceNo}</span> {i.title}
            </button>
            <div className="relative h-4 flex-1 rounded bg-stone-100">
              <div className="absolute top-0 h-4 rounded" title={`${formatDate(i.startAt ?? i.dueAt ?? '', locale)} → ${formatDate(i.dueAt ?? '', locale)}`}
                style={{ insetInlineStart: `${left}%`, width: `${width}%`, background: (i.statusColor ?? '#94a3b8') + 'cc' }}>
                <div className="h-full rounded bg-ink/20" style={{ width: `${i.completionPercent}%` }} />
              </div>
            </div>
          </li>
        );
      })}
    </ul>
  );
}

function BucketCard({ title, buckets, total, emptyLabel }: { title: string; buckets: TaskBucket[]; total: number; emptyLabel: string }) {
  return (
    <Card className="card-pad">
      <h2 className="mb-3 text-[14px] font-semibold text-ink">{title}</h2>
      {buckets.length === 0 ? (
        <p className="text-[13px] text-ink-4">—</p>
      ) : (
        <ul className="flex flex-col gap-2.5">
          {buckets.map((b) => (
            <li key={b.id ?? 'none'}>
              <div className="mb-1 flex items-center justify-between text-[13px]">
                <span className="flex items-center gap-2 text-ink-2">
                  <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ background: b.color ?? '#cbd5e1' }} />
                  {b.name ?? emptyLabel}
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
