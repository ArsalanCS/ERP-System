import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Drawer, Input, Select, Button, Spinner } from '@/shared/ui';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { ApiError } from '@/shared/api/client';
import { usersApi, userKeys } from '@/features/users/api';
import { tasksApi, workflowsApi, taskKeys } from './api';
import { TaskPriority, type CreateTaskBody, type UpdateTaskBody } from './types';

type Mode = 'create' | 'edit';

interface Props {
  open: boolean;
  mode: Mode;
  taskId?: string | undefined;
  onClose: () => void;
  onSaved: (kind: 'created' | 'updated') => void;
}

interface FormShape {
  title: string;
  description: string;
  priority: string;
  statusTypeId: string;
  assigneeId: string;
  startDate: string;
  dueDate: string;
  estimatedHours: string;
  actualHours: string;
  completionPercent: string;
}

const DEFAULTS: FormShape = {
  title: '', description: '', priority: String(TaskPriority.Normal), statusTypeId: '',
  assigneeId: '', startDate: '', dueDate: '', estimatedHours: '', actualHours: '', completionPercent: '0',
};

const toIso = (v: string): string | null => (v ? new Date(v).toISOString() : null);
const toDateInput = (v: string | null): string => (v ? v.slice(0, 10) : '');
const toNum = (v: string): number | null => (v.trim() === '' ? null : Number(v));

export function TaskFormDrawer({ open, mode, taskId, onClose, onSaved }: Props) {
  const { t } = useTranslation();
  const { can } = usePermissions();
  const queryClient = useQueryClient();
  const canViewUsers = can(Actions.UsersView);

  const [formError, setFormError] = useState<string | null>(null);
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormShape>({ defaultValues: DEFAULTS });

  const workflowsQuery = useQuery({ queryKey: taskKeys.workflows, queryFn: workflowsApi.list, enabled: open && mode === 'create' });
  const usersQuery = useQuery({
    queryKey: userKeys.list({ page: 1, pageSize: 100 }),
    queryFn: () => usersApi.list({ page: 1, pageSize: 100 }),
    enabled: open && mode === 'create' && canViewUsers,
  });
  const detailQuery = useQuery({
    queryKey: taskKeys.detail(taskId ?? ''),
    queryFn: () => tasksApi.get(taskId!),
    enabled: open && mode === 'edit' && !!taskId,
  });

  const priorityOptions = useMemo(
    () => [TaskPriority.Low, TaskPriority.Normal, TaskPriority.High, TaskPriority.Urgent]
      .map((p) => ({ value: String(p), label: t(`tasks.priority.${p}`) })),
    [t],
  );

  useEffect(() => {
    if (!open) return;
    setFormError(null);
    if (mode === 'create') {
      reset(DEFAULTS);
      return;
    }
    const d = detailQuery.data;
    if (d) {
      reset({
        title: d.title,
        description: d.description ?? '',
        priority: String(d.priority),
        statusTypeId: d.statusTypeId,
        assigneeId: d.assigneeId ?? '',
        startDate: toDateInput(d.startDate),
        dueDate: toDateInput(d.dueDate),
        estimatedHours: d.estimatedHours?.toString() ?? '',
        actualHours: d.actualHours?.toString() ?? '',
        completionPercent: String(d.completionPercent),
      });
    }
  }, [open, mode, detailQuery.data, reset]);

  const mutation = useMutation({
    mutationFn: async (v: FormShape) => {
      if (mode === 'create') {
        const body: CreateTaskBody = {
          title: v.title.trim(),
          description: v.description.trim() || null,
          priority: Number(v.priority),
          statusTypeId: v.statusTypeId || null,
          assigneeId: v.assigneeId || null,
          startDate: toIso(v.startDate),
          dueDate: toIso(v.dueDate),
          estimatedHours: toNum(v.estimatedHours),
        };
        await tasksApi.create(body);
        return 'created' as const;
      }
      const body: UpdateTaskBody = {
        title: v.title.trim(),
        description: v.description.trim() || null,
        priority: Number(v.priority),
        startDate: toIso(v.startDate),
        dueDate: toIso(v.dueDate),
        estimatedHours: toNum(v.estimatedHours),
        actualHours: toNum(v.actualHours),
        completionPercent: Number(v.completionPercent) || 0,
      };
      await tasksApi.update(taskId!, body);
      return 'updated' as const;
    },
    onSuccess: (kind) => {
      void queryClient.invalidateQueries({ queryKey: taskKeys.all });
      onSaved(kind);
    },
    onError: (err) => setFormError(
      err instanceof ApiError && err.details?.[0]?.message ? err.details[0].message
        : err instanceof Error ? err.message : t('common.loadError'),
    ),
  });

  const req = { required: t('tasks.validation.required') };
  const loadingDetail = mode === 'edit' && detailQuery.isLoading;
  const workflowOptions = (workflowsQuery.data ?? [])
    .filter((w) => w.type.isActive)
    .map((w) => ({ value: w.type.id, label: w.type.name }));
  const userOptions = [
    { value: '', label: t('tasks.fields.unassigned') },
    ...(usersQuery.data?.items ?? []).map((u) => ({ value: u.id, label: u.displayName })),
  ];

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={mode === 'create' ? t('tasks.newTask') : t('tasks.editTask')}
      footer={
        <>
          <Button variant="outline" size="sm" onClick={onClose} disabled={mutation.isPending}>{t('common.cancel')}</Button>
          <Button size="sm" onClick={handleSubmit((v) => mutation.mutate(v))} disabled={mutation.isPending || loadingDetail}>
            {mutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}
            {mode === 'create' ? t('common.create') : t('common.save')}
          </Button>
        </>
      }
    >
      {loadingDetail ? (
        <div className="flex justify-center py-10"><Spinner size={24} /></div>
      ) : (
        <div className="flex flex-col gap-4">
          {formError && (
            <div className="rounded-md border border-red-100 bg-red-100 px-3.5 py-2.5 text-[13px] font-medium text-red">{formError}</div>
          )}
          <Input label={t('tasks.fields.title')} error={errors.title?.message} {...register('title', req)} />
          <div className="field">
            <span className="label">{t('tasks.fields.description')}</span>
            <textarea className="input min-h-[80px] resize-y" {...register('description')} />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <Select label={t('tasks.fields.priority')} options={priorityOptions} {...register('priority')} />
            {mode === 'create' && (
              <Select
                label={t('tasks.fields.workflow')}
                placeholder={t('tasks.fields.defaultWorkflow')}
                options={workflowOptions}
                {...register('statusTypeId')}
              />
            )}
          </div>
          {mode === 'create' && canViewUsers && (
            <Select label={t('tasks.fields.assignee')} options={userOptions} {...register('assigneeId')} />
          )}
          <div className="grid grid-cols-2 gap-3">
            <Input type="date" label={t('tasks.fields.startDate')} {...register('startDate')} />
            <Input type="date" label={t('tasks.fields.dueDate')} {...register('dueDate')} />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <Input type="number" step="0.5" label={t('tasks.fields.estimatedHours')} {...register('estimatedHours')} />
            {mode === 'edit' && (
              <Input type="number" step="0.5" label={t('tasks.fields.actualHours')} {...register('actualHours')} />
            )}
          </div>
          {mode === 'edit' && (
            <Input type="number" min="0" max="100" label={t('tasks.fields.completion')} {...register('completionPercent')} />
          )}
        </div>
      )}
    </Drawer>
  );
}
