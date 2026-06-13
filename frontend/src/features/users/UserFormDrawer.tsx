import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Drawer, Input, Select, Button, Spinner } from '@/shared/ui';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { ApiError } from '@/shared/api/client';
import { accessApi, accessKeys } from '@/features/access-control/api';
import { usersApi, userKeys } from './api';
import type { CreateUserRequest, UpdateUserRequest } from './types';

type Mode = 'create' | 'edit';

interface UserFormDrawerProps {
  open: boolean;
  mode: Mode;
  userId?: string | undefined;
  onClose: () => void;
  onSaved: (kind: 'created' | 'invited' | 'updated') => void;
}

interface FormShape {
  firstName: string;
  lastName: string;
  displayName: string;
  email: string;
  mobile: string;
  jobTitle: string;
  preferredLanguage: string;
  timeZone: string;
  accessStartDate: string;
  accessExpiryDate: string;
}

const DEFAULTS: FormShape = {
  firstName: '',
  lastName: '',
  displayName: '',
  email: '',
  mobile: '',
  jobTitle: '',
  preferredLanguage: 'en',
  timeZone: 'Asia/Riyadh',
  accessStartDate: '',
  accessExpiryDate: '',
};

const toIsoOrNull = (value: string): string | null =>
  value ? new Date(value).toISOString() : null;
const toDateInput = (value: string | null): string => (value ? value.slice(0, 10) : '');

export function UserFormDrawer({ open, mode, userId, onClose, onSaved }: UserFormDrawerProps) {
  const { t } = useTranslation();
  const { can } = usePermissions();
  const queryClient = useQueryClient();
  const canSeeRoles = can(Actions.AccessControlView);

  const [roleIds, setRoleIds] = useState<string[]>([]);
  const [sendInvitation, setSendInvitation] = useState(true);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormShape>({ defaultValues: DEFAULTS });

  // Roles for the assignment list (only when the caller may view them).
  const rolesQuery = useQuery({
    queryKey: accessKeys.roles,
    queryFn: accessApi.listRoles,
    enabled: open && canSeeRoles,
  });

  // Existing user details when editing.
  const detailQuery = useQuery({
    queryKey: userKeys.detail(userId ?? ''),
    queryFn: () => usersApi.get(userId!),
    enabled: open && mode === 'edit' && !!userId,
  });

  useEffect(() => {
    if (!open) return;
    setFormError(null);
    if (mode === 'create') {
      reset(DEFAULTS);
      setRoleIds([]);
      setSendInvitation(true);
      return;
    }
    const d = detailQuery.data;
    if (d) {
      reset({
        firstName: d.firstName,
        lastName: d.lastName,
        displayName: d.displayName,
        email: d.email,
        mobile: d.mobile ?? '',
        jobTitle: d.jobTitle ?? '',
        preferredLanguage: d.preferredLanguage,
        timeZone: d.timeZone,
        accessStartDate: toDateInput(d.accessStartDate),
        accessExpiryDate: toDateInput(d.accessExpiryDate),
      });
      setRoleIds(d.roleIds);
    }
  }, [open, mode, detailQuery.data, reset]);

  const mutation = useMutation({
    mutationFn: async (values: FormShape) => {
      if (mode === 'create') {
        const body: CreateUserRequest = {
          email: values.email.trim(),
          firstName: values.firstName.trim(),
          lastName: values.lastName.trim(),
          mobile: values.mobile.trim() || null,
          jobTitle: values.jobTitle.trim() || null,
          preferredLanguage: values.preferredLanguage,
          timeZone: values.timeZone.trim() || null,
          roleIds: canSeeRoles ? roleIds : undefined,
          sendInvitation,
        };
        await usersApi.create(body);
        return sendInvitation ? ('invited' as const) : ('created' as const);
      }
      const body: UpdateUserRequest = {
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        displayName: values.displayName.trim(),
        mobile: values.mobile.trim() || null,
        jobTitle: values.jobTitle.trim() || null,
        preferredLanguage: values.preferredLanguage,
        timeZone: values.timeZone.trim(),
        accessStartDate: toIsoOrNull(values.accessStartDate),
        accessExpiryDate: toIsoOrNull(values.accessExpiryDate),
        roleIds: canSeeRoles ? roleIds : undefined,
      };
      await usersApi.update(userId!, body);
      return 'updated' as const;
    },
    onSuccess: (kind) => {
      void queryClient.invalidateQueries({ queryKey: userKeys.all });
      onSaved(kind);
    },
    onError: (err) => {
      setFormError(
        err instanceof ApiError && err.details?.[0]?.message
          ? err.details[0].message
          : err instanceof Error
            ? err.message
            : t('common.loadError'),
      );
    },
  });

  const toggleRole = (id: string) =>
    setRoleIds((prev) => (prev.includes(id) ? prev.filter((r) => r !== id) : [...prev, id]));

  const req = { required: t('users.validation.required') };
  const loadingDetail = mode === 'edit' && detailQuery.isLoading;

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={mode === 'create' ? t('users.newUser') : t('users.editUser')}
      subtitle={mode === 'edit' ? detailQuery.data?.email : t('users.subtitle')}
      footer={
        <>
          <Button variant="outline" size="sm" onClick={onClose} disabled={mutation.isPending}>
            {t('common.cancel')}
          </Button>
          <Button
            size="sm"
            onClick={handleSubmit((v) => mutation.mutate(v))}
            disabled={mutation.isPending || loadingDetail}
          >
            {mutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}
            {mode === 'create' ? t('common.create') : t('common.save')}
          </Button>
        </>
      }
    >
      {loadingDetail ? (
        <div className="flex justify-center py-10">
          <Spinner size={24} />
        </div>
      ) : (
        <form className="flex flex-col gap-4" onSubmit={handleSubmit((v) => mutation.mutate(v))}>
          {formError && (
            <div className="rounded-md border border-red-100 bg-red-100 px-3.5 py-2.5 text-[13px] font-medium text-red">
              {formError}
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <Input
              label={t('users.fields.firstName')}
              error={errors.firstName?.message}
              {...register('firstName', req)}
            />
            <Input
              label={t('users.fields.lastName')}
              error={errors.lastName?.message}
              {...register('lastName', req)}
            />
          </div>

          {mode === 'create' ? (
            <Input
              label={t('users.fields.email')}
              type="email"
              error={errors.email?.message}
              {...register('email', {
                required: t('users.validation.required'),
                pattern: { value: /^[^@\s]+@[^@\s]+\.[^@\s]+$/, message: t('users.validation.email') },
              })}
            />
          ) : (
            <Input
              label={t('users.fields.displayName')}
              error={errors.displayName?.message}
              {...register('displayName', req)}
            />
          )}

          <div className="grid grid-cols-2 gap-3">
            <Input label={t('users.fields.mobile')} {...register('mobile')} />
            <Input label={t('users.fields.jobTitle')} {...register('jobTitle')} />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <Select
              label={t('users.fields.language')}
              options={[
                { value: 'en', label: t('language.en') },
                { value: 'ar', label: t('language.ar') },
              ]}
              {...register('preferredLanguage')}
            />
            <Input label={t('users.fields.timeZone')} {...register('timeZone')} />
          </div>

          {mode === 'edit' && (
            <div className="grid grid-cols-2 gap-3">
              <Input
                label={t('users.fields.accessStart')}
                type="date"
                {...register('accessStartDate')}
              />
              <Input
                label={t('users.fields.accessExpiry')}
                type="date"
                {...register('accessExpiryDate')}
              />
            </div>
          )}

          {canSeeRoles && (
            <div className="field">
              <span className="label">{t('users.fields.roles')}</span>
              <p className="-mt-1 text-[12px] text-ink-4">{t('users.rolesHint')}</p>
              {rolesQuery.isLoading ? (
                <Spinner size={18} />
              ) : rolesQuery.data && rolesQuery.data.length > 0 ? (
                <div className="mt-1 flex flex-col gap-1.5 rounded-md border border-stone-200 p-2.5">
                  {rolesQuery.data.map((role) => (
                    <label
                      key={role.id}
                      className="flex cursor-pointer items-center gap-2.5 rounded-sm px-1.5 py-1 text-sm hover:bg-stone-50"
                    >
                      <input
                        type="checkbox"
                        className="h-4 w-4 accent-clay"
                        checked={roleIds.includes(role.id)}
                        onChange={() => toggleRole(role.id)}
                      />
                      <span className="text-ink-2">{role.name}</span>
                    </label>
                  ))}
                </div>
              ) : (
                <p className="text-[13px] text-ink-4">{t('users.noRoles')}</p>
              )}
            </div>
          )}

          {mode === 'create' && (
            <label className="flex cursor-pointer items-center gap-2.5 text-sm text-ink-2">
              <input
                type="checkbox"
                className="h-4 w-4 accent-clay"
                checked={sendInvitation}
                onChange={(e) => setSendInvitation(e.target.checked)}
              />
              {t('users.fields.sendInvitation')}
            </label>
          )}
        </form>
      )}
    </Drawer>
  );
}
