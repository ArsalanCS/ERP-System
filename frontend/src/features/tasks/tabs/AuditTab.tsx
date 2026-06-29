import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Card, Spinner, EmptyState } from '@/shared/ui';
import { useDirection } from '@/shared/hooks/useDirection';
import { formatDateTime } from '@/shared/lib/format';
import { tasksApi, taskKeys } from '../api';

export function AuditTab({ taskId }: { taskId: string }) {
  const { t } = useTranslation();
  const { locale } = useDirection();
  const query = useQuery({ queryKey: taskKeys.audit(taskId), queryFn: () => tasksApi.audit(taskId) });
  const rows = query.data ?? [];

  return (
    <Card className="card-pad">
      <p className="mb-3 text-[12px] text-ink-4">{t('tasks.audit.hint')}</p>
      {query.isLoading ? (
        <div className="flex justify-center py-6"><Spinner size={20} /></div>
      ) : rows.length === 0 ? (
        <EmptyState icon="shield" title={t('tasks.audit.empty')} />
      ) : (
        <ul className="flex flex-col divide-y divide-stone-100 rounded-md border border-stone-150">
          {rows.map((a) => (
            <li key={a.id} className="flex items-center gap-3 px-3 py-2 text-[13px]">
              <span className="font-mono text-[11.5px] uppercase text-ink-4">{a.action}</span>
              <span className="text-ink-2">{a.actorName ?? '—'}</span>
              <span className="ms-auto text-[11.5px] text-ink-4">{formatDateTime(a.createdAt, locale)}</span>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
