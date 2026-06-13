import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Drawer, Input, Button, Spinner } from '@/shared/ui';
import { useToast } from '@/shared/ui/toast-context';
import { ApiError } from '@/shared/api/client';
import { structureApi, structureKeys } from './api';

export type NodeKind = 'organization' | 'cluster' | 'department' | 'team';

export interface AddContext {
  kind: NodeKind;
  organizationId?: string;
  clusterId?: string | null;
  parentClusterId?: string | null;
  departmentId?: string;
}

interface AddNodeDrawerProps {
  context: AddContext | null;
  onClose: () => void;
}

interface FormShape {
  name: string;
  code: string;
  type: string;
  baseCurrency: string;
  country: string;
}

export function AddNodeDrawer({ context, onClose }: AddNodeDrawerProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<FormShape>();

  useEffect(() => {
    if (context) reset({ name: '', code: '', type: 'Branch', baseCurrency: 'SAR', country: '' });
  }, [context, reset]);

  const mutation = useMutation({
    mutationFn: (values: FormShape) => {
      const name = values.name.trim();
      const code = values.code.trim();
      switch (context!.kind) {
        case 'organization':
          return structureApi.createOrganization({
            name,
            code,
            baseCurrency: values.baseCurrency.trim() || null,
            country: values.country.trim() || null,
          });
        case 'cluster':
          return structureApi.createCluster({
            organizationId: context!.organizationId!,
            name,
            code,
            type: values.type.trim() || 'Branch',
            parentClusterId: context!.parentClusterId ?? null,
          });
        case 'department':
          return structureApi.createDepartment({
            organizationId: context!.organizationId!,
            clusterId: context!.clusterId ?? null,
            name,
            code,
          });
        case 'team':
          return structureApi.createTeam({ departmentId: context!.departmentId!, name, code });
      }
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: structureKeys.tree });
      toast.success(t('structure.feedback.created'));
      onClose();
    },
    onError: (err) => {
      if (err instanceof ApiError && err.details?.[0]) {
        setError('code', { message: err.details[0].message });
      } else {
        toast.error(t('common.loadError'));
      }
    },
  });

  const req = { required: t('users.validation.required') };
  const kind = context?.kind;

  return (
    <Drawer
      open={context !== null}
      onClose={onClose}
      title={kind ? t(`structure.add.${kind}`) : ''}
      footer={
        <>
          <Button variant="outline" size="sm" onClick={onClose} disabled={mutation.isPending}>
            {t('common.cancel')}
          </Button>
          <Button size="sm" onClick={handleSubmit((v) => mutation.mutate(v))} disabled={mutation.isPending}>
            {mutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}
            {t('common.create')}
          </Button>
        </>
      }
    >
      <form className="flex flex-col gap-4" onSubmit={handleSubmit((v) => mutation.mutate(v))}>
        <Input label={t('structure.fields.name')} error={errors.name?.message} {...register('name', req)} />
        <Input label={t('structure.fields.code')} error={errors.code?.message} {...register('code', req)} />
        {kind === 'cluster' && (
          <Input label={t('structure.fields.type')} {...register('type')} />
        )}
        {kind === 'organization' && (
          <div className="grid grid-cols-2 gap-3">
            <Input label={t('structure.fields.baseCurrency')} {...register('baseCurrency')} />
            <Input label={t('structure.fields.country')} {...register('country')} />
          </div>
        )}
      </form>
    </Drawer>
  );
}
