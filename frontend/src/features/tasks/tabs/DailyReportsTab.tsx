import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Card, Button, Icon, Spinner, EmptyState, Select } from '@/shared/ui';
import { useDirection } from '@/shared/hooks/useDirection';
import { formatDate, formatDateTime } from '@/shared/lib/format';
import { useToast } from '@/shared/ui/toast-context';
import { tasksApi, taskKeys } from '../api';
import { TASK_STATUS } from '../types';

const today = () => new Date().toISOString().slice(0, 10);

export function DailyReportsTab({ taskId, canManage }: { taskId: string; canManage: boolean }) {
  const { t } = useTranslation();
  const { locale } = useDirection();
  const toast = useToast();
  const queryClient = useQueryClient();

  const [reportDate, setReportDate] = useState(today());
  const [description, setDescription] = useState('');
  const [estimated, setEstimated] = useState('');
  const [actual, setActual] = useState('');
  const [remaining, setRemaining] = useState('');
  const [statusId, setStatusId] = useState('');

  const query = useQuery({ queryKey: taskKeys.dailyReports(taskId), queryFn: () => tasksApi.dailyReports(taskId) });
  const statuses = useQuery({ queryKey: taskKeys.statuses(TASK_STATUS), queryFn: () => tasksApi.statuses(TASK_STATUS) });
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: taskKeys.dailyReports(taskId) });
    void queryClient.invalidateQueries({ queryKey: taskKeys.detail(taskId) });
    void queryClient.invalidateQueries({ queryKey: taskKeys.activity(taskId) });
  };

  const reset = () => {
    setDescription(''); setEstimated(''); setActual(''); setRemaining(''); setStatusId(''); setReportDate(today());
  };

  const num = (v: string) => (v.trim() === '' ? null : Number(v));

  const add = useMutation({
    mutationFn: () => tasksApi.addDailyReport(taskId, {
      reportDate,
      description: description.trim(),
      estimatedTime: num(estimated),
      actualTime: num(actual),
      remainingTime: num(remaining),
      statusId: statusId === '' ? null : statusId,
    }),
    onSuccess: () => { reset(); invalidate(); toast.success(t('tasks.dailyReports.filed')); },
    onError: (err) => toast.error(err instanceof Error ? err.message : t('common.loadError')),
  });
  const remove = useMutation({
    mutationFn: (reportId: string) => tasksApi.removeDailyReport(taskId, reportId),
    onSuccess: () => invalidate(),
  });

  const reports = query.data ?? [];
  const statusOptions = [
    { value: '', label: t('tasks.dailyReports.noStatusChange') },
    ...(statuses.data ?? []).map((s) => ({ value: s.id, label: s.name })),
  ];

  return (
    <Card className="card-pad">
      {canManage && (
        <div className="mb-5 flex flex-col gap-2 rounded-md border border-stone-150 p-3.5">
          <div className="flex flex-wrap items-center gap-3">
            <label className="flex items-center gap-2 text-[12.5px] text-ink-3">
              {t('tasks.dailyReports.date')}
              <input type="date" className="input !w-auto" value={reportDate} onChange={(e) => setReportDate(e.target.value)} />
            </label>
            <label className="flex items-center gap-2 text-[12.5px] text-ink-3">
              {t('tasks.dailyReports.estimated')}
              <input type="number" min={0} step={0.5} className="input !w-24" value={estimated} onChange={(e) => setEstimated(e.target.value)} />
            </label>
            <label className="flex items-center gap-2 text-[12.5px] text-ink-3">
              {t('tasks.dailyReports.actual')}
              <input type="number" min={0} step={0.5} className="input !w-24" value={actual} onChange={(e) => setActual(e.target.value)} />
            </label>
            <label className="flex items-center gap-2 text-[12.5px] text-ink-3">
              {t('tasks.dailyReports.remaining')}
              <input type="number" min={0} step={0.5} className="input !w-24" value={remaining} onChange={(e) => setRemaining(e.target.value)} />
            </label>
          </div>
          <label className="flex flex-col gap-1 text-[12.5px] text-ink-3">
            {t('tasks.dailyReports.statusChange')}
            <div className="w-[220px]">
              <Select options={statusOptions} value={statusId} onChange={(e) => setStatusId(e.target.value)} />
            </div>
          </label>
          <textarea className="input min-h-[64px] resize-y" placeholder={t('tasks.dailyReports.descriptionPlaceholder')}
            value={description} onChange={(e) => setDescription(e.target.value)} />
          <Button size="sm" className="self-end" disabled={!description.trim() || add.isPending} onClick={() => description.trim() && add.mutate()}>
            {t('tasks.dailyReports.file')}
          </Button>
        </div>
      )}
      {query.isLoading ? (
        <div className="flex justify-center py-6"><Spinner size={20} /></div>
      ) : reports.length === 0 ? (
        <EmptyState icon="history" title={t('tasks.dailyReports.empty')} />
      ) : (
        <ul className="flex flex-col gap-3">
          {reports.map((r) => (
            <li key={r.id} className="group rounded-md border border-stone-150 px-3.5 py-2.5">
              <div className="mb-1 flex flex-wrap items-center gap-2 text-[12px]">
                <span className="font-semibold text-ink-2">{formatDate(r.reportDate, locale)}</span>
                {r.estimatedTime != null && <span className="text-ink-4">· {t('tasks.dailyReports.estimatedShort')} {r.estimatedTime}h</span>}
                {r.actualTime != null && <span className="text-ink-4">· {t('tasks.dailyReports.actualShort')} {r.actualTime}h</span>}
                {r.remainingTime != null && <span className="text-ink-4">· {t('tasks.dailyReports.remainingShort')} {r.remainingTime}h</span>}
                {r.statusName && (
                  <span className="inline-flex items-center gap-1 rounded px-1.5 py-0.5 text-[11px] font-medium"
                    style={{ backgroundColor: (r.statusColor ?? '#64748b') + '1a', color: r.statusColor ?? '#64748b' }}>
                    {r.statusName}
                  </span>
                )}
                <span className="text-ink-4">· {r.authorName ?? '—'} · {formatDateTime(r.createdAt, locale)}</span>
                {canManage && (
                  <button type="button" title={t('common.delete')} onClick={() => remove.mutate(r.id)}
                    className="ms-auto opacity-0 transition-opacity group-hover:opacity-100 text-ink-4 hover:text-red">
                    <Icon name="trash" size={14} />
                  </button>
                )}
              </div>
              <div className="whitespace-pre-wrap text-[13.5px] text-ink-2">{r.description}</div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
