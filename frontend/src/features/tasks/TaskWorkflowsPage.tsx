import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PageHeader, Card, Button, Icon, Badge, Drawer, Input, Select, Spinner, LoadingBlock, EmptyState, ConfirmDialog } from '@/shared/ui';
import { useToast } from '@/shared/ui/toast-context';
import { ApiError } from '@/shared/api/client';
import { workflowsApi, taskKeys } from './api';
import { TaskStatusCategory, type TaskStatusDto, type TaskStatusTypeDto } from './types';

const CATEGORIES = [
  TaskStatusCategory.Open, TaskStatusCategory.InProgress, TaskStatusCategory.Waiting,
  TaskStatusCategory.Review, TaskStatusCategory.Completed, TaskStatusCategory.Cancelled, TaskStatusCategory.Rejected,
];

export function TaskWorkflowsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const toast = useToast();
  const queryClient = useQueryClient();

  const [typeDrawer, setTypeDrawer] = useState<{ open: boolean; type?: TaskStatusTypeDto }>({ open: false });
  const [statusDrawer, setStatusDrawer] = useState<{ open: boolean; typeId: string; status?: TaskStatusDto }>({ open: false, typeId: '' });
  const [confirm, setConfirm] = useState<{ kind: 'type' | 'status'; id: string; name: string } | null>(null);

  const query = useQuery({ queryKey: taskKeys.workflows, queryFn: workflowsApi.list });
  const invalidate = () => queryClient.invalidateQueries({ queryKey: taskKeys.workflows });

  const setDefaultMutation = useMutation({
    mutationFn: (type: TaskStatusTypeDto) =>
      workflowsApi.updateType(type.id, { name: type.name, description: type.description, sortOrder: type.sortOrder, isActive: type.isActive, isDefault: true }),
    onSuccess: () => { void invalidate(); toast.success(t('tasks.workflow.feedback.saved')); },
    onError: (err) => toast.error(err instanceof Error ? err.message : t('common.loadError')),
  });
  const archiveMutation = useMutation({
    mutationFn: (c: { kind: 'type' | 'status'; id: string }) =>
      c.kind === 'type' ? workflowsApi.archiveType(c.id) : workflowsApi.archiveStatus(c.id),
    onSuccess: () => { void invalidate(); toast.success(t('tasks.workflow.feedback.archived')); setConfirm(null); },
    onError: (err) => { toast.error(err instanceof Error ? err.message : t('common.loadError')); setConfirm(null); },
  });

  return (
    <>
      <PageHeader
        title={t('tasks.workflow.title')}
        subtitle={t('tasks.workflow.subtitle')}
        actions={
          <div className="flex items-center gap-2">
            <Button variant="outline" onClick={() => navigate('/admin/tasks')}>{t('common.back')}</Button>
            <Button leadingIcon={<Icon name="plus" size={16} />} onClick={() => setTypeDrawer({ open: true })}>
              {t('tasks.workflow.newType')}
            </Button>
          </div>
        }
      />

      {query.isLoading ? (
        <Card className="card-pad"><LoadingBlock label={t('common.loading')} /></Card>
      ) : query.isError || !query.data ? (
        <EmptyState icon="alert" title={t('common.loadError')}
          action={<Button variant="outline" size="sm" onClick={() => query.refetch()}>{t('common.retry')}</Button>} />
      ) : query.data.length === 0 ? (
        <EmptyState icon="sitemap" title={t('tasks.workflow.empty.title')} body={t('tasks.workflow.empty.body')}
          action={<Button onClick={() => setTypeDrawer({ open: true })}>{t('tasks.workflow.newType')}</Button>} />
      ) : (
        <div className="flex flex-col gap-4">
          {query.data.map((wf) => (
            <Card key={wf.type.id} className="card-pad">
              <div className="mb-3 flex items-center justify-between gap-3">
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-ink">{wf.type.name}</span>
                  {wf.type.isDefault && <Badge tone="clay">{t('tasks.workflow.default')}</Badge>}
                  {!wf.type.isActive && <Badge tone="neutral">{t('tasks.workflow.inactive')}</Badge>}
                </div>
                <div className="flex items-center gap-1">
                  {!wf.type.isDefault && (
                    <Button variant="ghost" size="sm" onClick={() => setDefaultMutation.mutate(wf.type)}>{t('tasks.workflow.makeDefault')}</Button>
                  )}
                  <IconBtn icon="edit" title={t('common.edit')} onClick={() => setTypeDrawer({ open: true, type: wf.type })} />
                  <IconBtn icon="trash" title={t('tasks.workflow.archiveType')} danger onClick={() => setConfirm({ kind: 'type', id: wf.type.id, name: wf.type.name })} />
                </div>
              </div>
              <ul className="flex flex-col divide-y divide-stone-100 rounded-md border border-stone-150">
                {wf.statuses.map((s) => (
                  <li key={s.id} className="group flex items-center gap-3 px-3 py-2">
                    <span className="text-[13.5px] font-medium text-ink">{s.name}</span>
                    <span className="text-[11.5px] text-ink-4">{t(`tasks.category.${s.category}`)}</span>
                    {s.isInitial && <Badge tone="blue">{t('tasks.workflow.initial')}</Badge>}
                    {s.isFinal && <Badge tone="green">{t('tasks.workflow.final')}</Badge>}
                    <span className="ms-auto flex items-center gap-0.5 opacity-0 transition-opacity group-hover:opacity-100">
                      <IconBtn icon="edit" title={t('common.edit')} onClick={() => setStatusDrawer({ open: true, typeId: wf.type.id, status: s })} />
                      <IconBtn icon="trash" title={t('tasks.workflow.archiveStatus')} danger onClick={() => setConfirm({ kind: 'status', id: s.id, name: s.name })} />
                    </span>
                  </li>
                ))}
                <li className="px-3 py-2">
                  <Button variant="ghost" size="sm" leadingIcon={<Icon name="plus" size={14} />}
                    onClick={() => setStatusDrawer({ open: true, typeId: wf.type.id })}>
                    {t('tasks.workflow.addStatus')}
                  </Button>
                </li>
              </ul>
            </Card>
          ))}
        </div>
      )}

      <StatusTypeDrawer state={typeDrawer} onClose={() => setTypeDrawer({ open: false })}
        onSaved={() => { setTypeDrawer({ open: false }); void invalidate(); toast.success(t('tasks.workflow.feedback.saved')); }} />
      <StatusDrawer state={statusDrawer} onClose={() => setStatusDrawer({ open: false, typeId: '' })}
        onSaved={() => { setStatusDrawer({ open: false, typeId: '' }); void invalidate(); toast.success(t('tasks.workflow.feedback.saved')); }} />

      <ConfirmDialog
        open={confirm !== null}
        title={confirm?.kind === 'type' ? t('tasks.workflow.archiveType') : t('tasks.workflow.archiveStatus')}
        message={t('tasks.workflow.archiveConfirm', { name: confirm?.name ?? '' })}
        confirmLabel={t('tasks.archive')}
        tone="danger"
        loading={archiveMutation.isPending}
        onCancel={() => setConfirm(null)}
        onConfirm={() => confirm && archiveMutation.mutate(confirm)}
      />
    </>
  );
}

function IconBtn({ icon, title, onClick, danger }: { icon: 'edit' | 'trash'; title: string; onClick: () => void; danger?: boolean }) {
  return (
    <button type="button" title={title} onClick={onClick}
      className={`flex h-8 w-8 items-center justify-center rounded-sm text-ink-4 hover:bg-stone-100 ${danger ? 'hover:text-red' : 'hover:text-ink'}`}>
      <Icon name={icon} size={15} />
    </button>
  );
}

function StatusTypeDrawer({ state, onClose, onSaved }: {
  state: { open: boolean; type?: TaskStatusTypeDto }; onClose: () => void; onSaved: () => void;
}) {
  const { t } = useTranslation();
  const [error, setError] = useState<string | null>(null);
  const { register, handleSubmit, reset, formState: { errors } } = useForm<{ name: string; description: string }>({ defaultValues: { name: '', description: '' } });

  useEffect(() => {
    if (state.open) { setError(null); reset({ name: state.type?.name ?? '', description: state.type?.description ?? '' }); }
  }, [state.open, state.type, reset]);

  const mutation = useMutation({
    mutationFn: async (v: { name: string; description: string }) => {
      if (state.type) {
        await workflowsApi.updateType(state.type.id, { name: v.name.trim(), description: v.description.trim() || null, sortOrder: state.type.sortOrder, isActive: state.type.isActive, isDefault: state.type.isDefault });
      } else {
        await workflowsApi.createType({ name: v.name.trim(), description: v.description.trim() || null });
      }
    },
    onSuccess: onSaved,
    onError: (err) => setError(err instanceof ApiError && err.details?.[0]?.message ? err.details[0].message : err instanceof Error ? err.message : t('common.loadError')),
  });

  return (
    <Drawer open={state.open} onClose={onClose} title={state.type ? t('tasks.workflow.editType') : t('tasks.workflow.newType')}
      footer={<>
        <Button variant="outline" size="sm" onClick={onClose} disabled={mutation.isPending}>{t('common.cancel')}</Button>
        <Button size="sm" onClick={handleSubmit((v) => mutation.mutate(v))} disabled={mutation.isPending}>
          {mutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}{t('common.save')}
        </Button>
      </>}>
      <div className="flex flex-col gap-4">
        {error && <div className="rounded-md border border-red-100 bg-red-100 px-3.5 py-2.5 text-[13px] font-medium text-red">{error}</div>}
        <Input label={t('tasks.workflow.fields.name')} error={errors.name?.message} {...register('name', { required: t('tasks.validation.required') })} />
        <Input label={t('tasks.workflow.fields.description')} {...register('description')} />
      </div>
    </Drawer>
  );
}

function StatusDrawer({ state, onClose, onSaved }: {
  state: { open: boolean; typeId: string; status?: TaskStatusDto }; onClose: () => void; onSaved: () => void;
}) {
  const { t } = useTranslation();
  const [error, setError] = useState<string | null>(null);
  const { register, handleSubmit, reset, formState: { errors } } = useForm<{ name: string; category: string; color: string; isInitial: boolean; isFinal: boolean }>({
    defaultValues: { name: '', category: String(TaskStatusCategory.Open), color: '', isInitial: false, isFinal: false },
  });

  useEffect(() => {
    if (state.open) {
      setError(null);
      reset({
        name: state.status?.name ?? '',
        category: String(state.status?.category ?? TaskStatusCategory.Open),
        color: state.status?.color ?? '',
        isInitial: state.status?.isInitial ?? false,
        isFinal: state.status?.isFinal ?? false,
      });
    }
  }, [state.open, state.status, reset]);

  const mutation = useMutation({
    mutationFn: async (v: { name: string; category: string; color: string; isInitial: boolean; isFinal: boolean }) => {
      if (state.status) {
        await workflowsApi.updateStatus(state.status.id, { name: v.name.trim(), category: Number(v.category), color: v.color.trim() || null, sortOrder: state.status.sortOrder, isInitial: v.isInitial, isFinal: v.isFinal });
      } else {
        await workflowsApi.createStatus({ statusTypeId: state.typeId, name: v.name.trim(), category: Number(v.category), color: v.color.trim() || null, isInitial: v.isInitial, isFinal: v.isFinal });
      }
    },
    onSuccess: onSaved,
    onError: (err) => setError(err instanceof ApiError && err.details?.[0]?.message ? err.details[0].message : err instanceof Error ? err.message : t('common.loadError')),
  });

  return (
    <Drawer open={state.open} onClose={onClose} title={state.status ? t('tasks.workflow.editStatus') : t('tasks.workflow.addStatus')}
      footer={<>
        <Button variant="outline" size="sm" onClick={onClose} disabled={mutation.isPending}>{t('common.cancel')}</Button>
        <Button size="sm" onClick={handleSubmit((v) => mutation.mutate(v))} disabled={mutation.isPending}>
          {mutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}{t('common.save')}
        </Button>
      </>}>
      <div className="flex flex-col gap-4">
        {error && <div className="rounded-md border border-red-100 bg-red-100 px-3.5 py-2.5 text-[13px] font-medium text-red">{error}</div>}
        <Input label={t('tasks.workflow.fields.name')} error={errors.name?.message} {...register('name', { required: t('tasks.validation.required') })} />
        <Select label={t('tasks.workflow.fields.category')} options={CATEGORIES.map((c) => ({ value: String(c), label: t(`tasks.category.${c}`) }))} {...register('category')} />
        <label className="flex cursor-pointer items-center gap-2.5 text-sm text-ink-2">
          <input type="checkbox" className="h-4 w-4 accent-clay" {...register('isInitial')} />{t('tasks.workflow.fields.isInitial')}
        </label>
        <label className="flex cursor-pointer items-center gap-2.5 text-sm text-ink-2">
          <input type="checkbox" className="h-4 w-4 accent-clay" {...register('isFinal')} />{t('tasks.workflow.fields.isFinal')}
        </label>
      </div>
    </Drawer>
  );
}
