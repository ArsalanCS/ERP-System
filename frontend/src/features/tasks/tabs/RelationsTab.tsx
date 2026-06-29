import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Card, Button, Icon, Select, Spinner, EmptyState } from '@/shared/ui';
import { useToast } from '@/shared/ui/toast-context';
import { tasksApi, taskKeys } from '../api';

export function RelationsTab({ taskId, canEdit }: { taskId: string; canEdit: boolean }) {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();

  const depsQuery = useQuery({ queryKey: taskKeys.dependencies(taskId), queryFn: () => tasksApi.dependencies(taskId) });
  const tasksQuery = useQuery({ queryKey: taskKeys.list({ page: 1, pageSize: 100 }), queryFn: () => tasksApi.list({ page: 1, pageSize: 100 }), enabled: canEdit });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: taskKeys.dependencies(taskId) });

  const [dependsOn, setDependsOn] = useState('');
  const addDep = useMutation({
    mutationFn: () => tasksApi.addDependency(taskId, { dependsOnEventId: dependsOn, isBlocking: true }),
    onSuccess: () => { setDependsOn(''); void invalidate(); },
    onError: (e) => toast.error(e instanceof Error ? e.message : t('common.loadError')),
  });
  const removeDep = useMutation({ mutationFn: (id: string) => tasksApi.removeDependency(taskId, id), onSuccess: () => void invalidate() });

  const taskOptions = useMemo(
    () => [{ value: '', label: t('tasks.relations.selectTask') },
      ...(tasksQuery.data?.items ?? []).filter((x) => x.eventId !== taskId).map((x) => ({ value: x.eventId, label: `${x.referenceNo} · ${x.title}` }))],
    [tasksQuery.data, taskId, t],
  );

  return (
    <div className="flex flex-col gap-4">
      <Card className="card-pad">
        <h3 className="mb-3 text-[13px] font-semibold text-ink">{t('tasks.relations.dependencies')}</h3>
        {depsQuery.isLoading ? <div className="flex justify-center py-4"><Spinner size={18} /></div>
          : (depsQuery.data ?? []).length === 0 ? <EmptyState icon="sitemap" title={t('tasks.relations.noDependencies')} />
          : (
            <ul className="flex flex-col divide-y divide-stone-100 rounded-md border border-stone-150">
              {depsQuery.data!.map((d) => (
                <li key={d.id} className="group flex items-center gap-3 px-3 py-2 text-[13px]">
                  <Icon name="link" size={13} className="flex-none text-clay" />
                  <span className="font-mono text-[11.5px] text-ink-4">{d.dependsOnReferenceNo}</span>
                  <span className="truncate text-ink-2">{d.dependsOnTitle}</span>
                  {d.isBlocking && <span className="text-[11px] font-semibold uppercase text-red">{t('tasks.relations.blocking')}</span>}
                  {canEdit && <button type="button" onClick={() => removeDep.mutate(d.id)} className="ms-auto opacity-0 group-hover:opacity-100 text-ink-4 hover:text-red"><Icon name="trash" size={14} /></button>}
                </li>
              ))}
            </ul>
          )}
        {canEdit && (
          <form className="mt-3 flex items-end gap-2" onSubmit={(e) => { e.preventDefault(); if (dependsOn) addDep.mutate(); }}>
            <div className="flex-1"><Select label={t('tasks.relations.dependsOn')} options={taskOptions} value={dependsOn} onChange={(e) => setDependsOn(e.target.value)} /></div>
            <Button type="submit" size="sm" disabled={!dependsOn || addDep.isPending}>{t('common.add')}</Button>
          </form>
        )}
      </Card>

      <Card className="card-pad">
        <h3 className="mb-1 text-[13px] font-semibold text-ink">{t('tasks.relations.linkedRecords')}</h3>
        <p className="text-[12.5px] text-ink-4">{t('tasks.relations.linkedRecordsHint')}</p>
      </Card>
    </div>
  );
}
