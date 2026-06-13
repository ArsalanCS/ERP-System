import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Drawer, Input, Button, Spinner } from '@/shared/ui';
import { useToast } from '@/shared/ui/toast-context';
import { ApiError } from '@/shared/api/client';
import { accessApi, accessKeys } from './api';
import type { CreateRoleRequest } from './types';

interface RoleFormDrawerProps {
  open: boolean;
  onClose: () => void;
  onCreated: (roleId: string) => void;
}

interface FormShape {
  name: string;
  code: string;
  description: string;
}

const slugify = (value: string) =>
  value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');

export function RoleFormDrawer({ open, onClose, onCreated }: RoleFormDrawerProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    setError,
    formState: { errors },
  } = useForm<FormShape>({ defaultValues: { name: '', code: '', description: '' } });

  useEffect(() => {
    if (open) reset({ name: '', code: '', description: '' });
  }, [open, reset]);

  const name = watch('name');
  useEffect(() => {
    setValue('code', slugify(name), { shouldValidate: false });
  }, [name, setValue]);

  const mutation = useMutation({
    mutationFn: (values: FormShape) => {
      const body: CreateRoleRequest = {
        name: values.name.trim(),
        code: values.code.trim(),
        description: values.description.trim() || null,
      };
      return accessApi.createRole(body);
    },
    onSuccess: (res) => {
      void queryClient.invalidateQueries({ queryKey: accessKeys.roles });
      toast.success(t('access.feedback.created'));
      onCreated(res.id);
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

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={t('access.newRole')}
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
        <Input label={t('access.fields.name')} error={errors.name?.message} {...register('name', req)} />
        <Input label={t('access.fields.code')} error={errors.code?.message} {...register('code', req)} />
        <div className="field">
          <label className="label" htmlFor="role-description">
            {t('access.fields.description')}
          </label>
          <textarea
            id="role-description"
            className="input min-h-[88px] resize-y"
            {...register('description')}
          />
        </div>
      </form>
    </Drawer>
  );
}
