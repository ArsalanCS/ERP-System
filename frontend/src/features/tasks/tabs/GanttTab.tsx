import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Card, Spinner, EmptyState, Icon } from '@/shared/ui';
import { tasksApi, taskKeys } from '../api';
import type { TaskDetails } from '../types';

interface Row { id: string; label: string; number: string; start: number | null; due: number | null; progress: number; overdue: boolean; hasDeps: boolean }

const DAY = 86_400_000;

export function GanttTab({ task }: { task: TaskDetails }) {
  const { t } = useTranslation();
  const subsQuery = useQuery({ queryKey: taskKeys.subtasks(task.id), queryFn: () => tasksApi.subtasks(task.id) });
  const depsQuery = useQuery({ queryKey: taskKeys.dependencies(task.id), queryFn: () => tasksApi.dependencies(task.id) });

  if (subsQuery.isLoading) return <Card className="card-pad"><div className="flex justify-center py-6"><Spinner size={20} /></div></Card>;

  const subs = subsQuery.data ?? [];
  const deps = depsQuery.data ?? [];
  const rows: Row[] = [
    { id: task.id, label: task.title, number: task.taskNumber, start: ms(task.startDate), due: ms(task.dueDate), progress: task.completionPercent, overdue: task.isOverdue, hasDeps: deps.length > 0 },
    ...subs.map((s) => ({ id: s.id, label: s.title, number: s.taskNumber, start: null, due: ms(s.dueDate), progress: s.completionPercent, overdue: s.isOverdue, hasDeps: false })),
  ];

  const points = rows.flatMap((r) => [r.start, r.due]).filter((x): x is number => x !== null);
  if (points.length === 0) {
    return <Card className="card-pad"><EmptyState icon="history" title={t('tasks.gantt.noDates')} body={t('tasks.gantt.noDatesBody')} /></Card>;
  }
  const min = Math.min(...points);
  const max = Math.max(...points) + DAY; // pad a day so single-day bars are visible
  const span = Math.max(max - min, DAY);

  const pct = (v: number) => `${((v - min) / span) * 100}%`;

  return (
    <Card className="card-pad">
      <div className="flex flex-col gap-2">
        {rows.map((r) => {
          const start = r.start ?? r.due;
          const end = r.due ?? r.start;
          return (
            <div key={r.id} className="flex items-center gap-3">
              <div className="flex w-40 flex-none items-center gap-1 truncate text-[12.5px] text-ink-2" title={r.label}>
                {r.hasDeps && <Icon name="link" size={12} className="flex-none text-clay" />}
                <span className="truncate"><span className="font-mono text-[10.5px] text-ink-4">{r.number}</span> {r.label}</span>
              </div>
              <div className="relative h-5 flex-1 rounded bg-stone-100">
                {start !== null && end !== null ? (
                  <div
                    className={`absolute top-0 h-5 rounded ${r.overdue ? 'bg-red' : 'bg-clay'}`}
                    style={{ insetInlineStart: pct(start), width: `calc(${pct(end)} - ${pct(start)} + 6px)` }}
                    title={`${r.progress}%`}
                  >
                    <div className="h-full rounded bg-white/30" style={{ width: `${100 - r.progress}%`, marginInlineStart: 'auto' }} />
                  </div>
                ) : (
                  <span className="absolute inset-y-0 inline-flex items-center px-2 text-[11px] text-ink-4">{t('tasks.gantt.noDate')}</span>
                )}
              </div>
            </div>
          );
        })}
      </div>
      {deps.length > 0 && (
        <div className="mt-4 border-t border-stone-150 pt-3">
          <div className="mb-1.5 text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{t('tasks.relations.dependencies')}</div>
          <ul className="flex flex-col gap-1">
            {deps.map((d) => (
              <li key={d.id} className="flex items-center gap-2 text-[12.5px] text-ink-2">
                <Icon name="link" size={12} className="flex-none text-clay" />
                <span className="font-mono text-[11px] text-ink-4">{task.taskNumber}</span>
                <span className="text-ink-4">{t('tasks.relations.dependsOn')}</span>
                <span className="font-mono text-[11px] text-ink-4">{d.dependsOnNumber}</span>
                <span className="truncate">{d.dependsOnTitle}</span>
                {d.isBlocking && <span className="text-[11px] font-semibold uppercase text-red">{t('tasks.relations.blocking')}</span>}
              </li>
            ))}
          </ul>
        </div>
      )}
      <p className="mt-3 text-[11.5px] text-ink-4">{t('tasks.gantt.legend')}</p>
    </Card>
  );
}

const ms = (iso: string | null): number | null => (iso ? new Date(iso).getTime() : null);
