import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Card, Button, Icon, Input, Select, Spinner, EmptyState } from '@/shared/ui';
import { useToast } from '@/shared/ui/toast-context';
import { tasksApi, taskKeys } from '../api';
import { TaskDependencyType, TaskRelationRole } from '../types';

export function RelationsTab({ taskId, canEdit }: { taskId: string; canEdit: boolean }) {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();

  const depsQuery = useQuery({ queryKey: taskKeys.dependencies(taskId), queryFn: () => tasksApi.dependencies(taskId) });
  const relsQuery = useQuery({ queryKey: taskKeys.relations(taskId), queryFn: () => tasksApi.relations(taskId) });
  const tasksQuery = useQuery({ queryKey: taskKeys.list({ page: 1, pageSize: 100 }), queryFn: () => tasksApi.list({ page: 1, pageSize: 100 }), enabled: canEdit });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: taskKeys.dependencies(taskId) });
    void queryClient.invalidateQueries({ queryKey: taskKeys.relations(taskId) });
  };

  const [dependsOn, setDependsOn] = useState('');
  const [entityType, setEntityType] = useState('');
  const [entityId, setEntityId] = useState('');
  const [role, setRole] = useState(String(TaskRelationRole.Supporting));

  const addDep = useMutation({
    mutationFn: () => tasksApi.addDependency(taskId, { dependsOnTaskId: dependsOn, dependencyType: TaskDependencyType.FinishToStart, isBlocking: true }),
    onSuccess: () => { setDependsOn(''); invalidate(); },
    onError: (e) => toast.error(e instanceof Error ? e.message : t('common.loadError')),
  });
  const removeDep = useMutation({ mutationFn: (id: string) => tasksApi.removeDependency(taskId, id), onSuccess: invalidate });
  const addRel = useMutation({
    mutationFn: () => tasksApi.addRelation(taskId, { relatedEntityType: entityType.trim(), relatedEntityId: entityId.trim(), role: Number(role), reason: null }),
    onSuccess: () => { setEntityType(''); setEntityId(''); invalidate(); },
    onError: (e) => toast.error(e instanceof Error ? e.message : t('common.loadError')),
  });
  const removeRel = useMutation({ mutationFn: (id: string) => tasksApi.removeRelation(taskId, id), onSuccess: invalidate });
  const refresh = useMutation({ mutationFn: () => tasksApi.refreshRelations(taskId), onSuccess: () => { invalidate(); toast.success(t('tasks.relations.refreshed')); } });

  const roleOptions = [TaskRelationRole.PrimarySource, TaskRelationRole.Supporting, TaskRelationRole.Reference]
    .map((r) => ({ value: String(r), label: t(`tasks.relations.roles.${r}`) }));
  const taskOptions = useMemo(
    () => [{ value: '', label: t('tasks.relations.selectTask') },
      ...(tasksQuery.data?.items ?? []).filter((x) => x.id !== taskId).map((x) => ({ value: x.id, label: `${x.taskNumber} · ${x.title}` }))],
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
                  <span className="font-mono text-[11.5px] text-ink-4">{d.dependsOnNumber}</span>
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
        <div className="mb-3 flex items-center justify-between">
          <h3 className="text-[13px] font-semibold text-ink">{t('tasks.relations.linkedRecords')}</h3>
          {canEdit && <Button variant="ghost" size="sm" leadingIcon={<Icon name="refresh" size={14} />} onClick={() => refresh.mutate()}>{t('tasks.relations.refresh')}</Button>}
        </div>
        {relsQuery.isLoading ? <div className="flex justify-center py-4"><Spinner size={18} /></div>
          : (relsQuery.data ?? []).length === 0 ? <EmptyState icon="sitemap" title={t('tasks.relations.noRecords')} />
          : (
            <ul className="flex flex-col divide-y divide-stone-100 rounded-md border border-stone-150">
              {relsQuery.data!.map((r) => (
                <li key={r.id} className="group flex items-center gap-3 px-3 py-2 text-[13px]">
                  <span className="font-semibold text-ink">{r.relatedEntityType}</span>
                  <span className="font-mono text-[11px] text-ink-4">{r.relatedEntityId.slice(0, 8)}</span>
                  <span className="text-[11px] uppercase text-clay">{t(`tasks.relations.roles.${r.role}`)}</span>
                  {canEdit && <button type="button" onClick={() => removeRel.mutate(r.id)} className="ms-auto opacity-0 group-hover:opacity-100 text-ink-4 hover:text-red"><Icon name="trash" size={14} /></button>}
                </li>
              ))}
            </ul>
          )}
        {canEdit && (
          <form className="mt-3 flex flex-wrap items-end gap-2" onSubmit={(e) => { e.preventDefault(); if (entityType.trim() && entityId.trim()) addRel.mutate(); }}>
            <div className="w-[140px]"><Input label={t('tasks.relations.entityType')} value={entityType} onChange={(e) => setEntityType(e.target.value)} /></div>
            <div className="min-w-[200px] flex-1"><Input label={t('tasks.relations.entityId')} value={entityId} onChange={(e) => setEntityId(e.target.value)} /></div>
            <div className="w-[150px]"><Select label={t('tasks.relations.role')} options={roleOptions} value={role} onChange={(e) => setRole(e.target.value)} /></div>
            <Button type="submit" size="sm" disabled={!entityType.trim() || !entityId.trim() || addRel.isPending}>{t('common.add')}</Button>
          </form>
        )}
      </Card>
    </div>
  );
}
