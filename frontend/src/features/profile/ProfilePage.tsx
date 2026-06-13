import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  PageHeader,
  Card,
  CardHeader,
  Input,
  Select,
  Button,
  Spinner,
  LoadingBlock,
  EmptyState,
} from '@/shared/ui';
import { useToast } from '@/shared/ui/toast-context';
import { useDirection } from '@/shared/hooks/useDirection';
import { formatDateTime } from '@/shared/lib/format';
import { ApiError } from '@/shared/api/client';
import {
  accountApi,
  accountKeys,
  type ChangePasswordRequest,
  type UpdateMyProfileRequest,
} from './api';

interface ProfileForm {
  firstName: string;
  lastName: string;
  displayName: string;
  mobile: string;
  preferredLanguage: string;
  timeZone: string;
}

interface PasswordForm {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export function ProfilePage() {
  const { t } = useTranslation();
  const { locale } = useDirection();
  const toast = useToast();
  const queryClient = useQueryClient();

  const profileQuery = useQuery({ queryKey: accountKeys.profile, queryFn: accountApi.getProfile });
  const sessionsQuery = useQuery({ queryKey: accountKeys.sessions, queryFn: accountApi.listSessions });

  const profileForm = useForm<ProfileForm>();
  const passwordForm = useForm<PasswordForm>();

  useEffect(() => {
    const d = profileQuery.data;
    if (d) {
      profileForm.reset({
        firstName: d.firstName,
        lastName: d.lastName,
        displayName: d.displayName,
        mobile: d.mobile ?? '',
        preferredLanguage: d.preferredLanguage,
        timeZone: d.timeZone,
      });
    }
  }, [profileQuery.data, profileForm]);

  const profileMutation = useMutation({
    mutationFn: (values: ProfileForm) => {
      const body: UpdateMyProfileRequest = {
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        displayName: values.displayName.trim(),
        mobile: values.mobile.trim() || null,
        preferredLanguage: values.preferredLanguage,
        timeZone: values.timeZone.trim(),
      };
      return accountApi.updateProfile(body);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: accountKeys.profile });
      toast.success(t('profile.saved'));
    },
    onError: () => toast.error(t('common.loadError')),
  });

  const passwordMutation = useMutation({
    mutationFn: (values: PasswordForm) => {
      const body: ChangePasswordRequest = {
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      };
      return accountApi.changePassword(body);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: accountKeys.sessions });
      passwordForm.reset({ currentPassword: '', newPassword: '', confirmPassword: '' });
      toast.success(t('profile.password.changed'));
    },
    onError: (err) => {
      if (err instanceof ApiError && err.code === 'ACCOUNT_WRONG_PASSWORD') {
        passwordForm.setError('currentPassword', { message: t('profile.password.wrongCurrent') });
      } else {
        toast.error(t('common.loadError'));
      }
    },
  });

  const revokeMutation = useMutation({
    mutationFn: (id: string) => accountApi.revokeSession(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: accountKeys.sessions });
      toast.success(t('profile.sessions.revoked'));
    },
    onError: () => toast.error(t('common.loadError')),
  });

  const req = { required: t('users.validation.required') };

  if (profileQuery.isLoading) {
    return (
      <>
        <PageHeader title={t('profile.title')} subtitle={t('profile.subtitle')} />
        <Card padded>
          <LoadingBlock label={t('common.loading')} />
        </Card>
      </>
    );
  }

  if (profileQuery.isError || !profileQuery.data) {
    return (
      <>
        <PageHeader title={t('profile.title')} subtitle={t('profile.subtitle')} />
        <EmptyState
          icon="alert"
          title={t('common.loadError')}
          action={
            <Button variant="outline" size="sm" onClick={() => profileQuery.refetch()}>
              {t('common.retry')}
            </Button>
          }
        />
      </>
    );
  }

  const newPassword = passwordForm.watch('newPassword');

  return (
    <>
      <PageHeader title={t('profile.title')} subtitle={t('profile.subtitle')} />

      <div className="flex max-w-3xl flex-col gap-5">
        {/* Personal details */}
        <Card>
          <CardHeader title={t('profile.details')} />
          <form
            onSubmit={profileForm.handleSubmit((v) => profileMutation.mutate(v))}
            className="flex flex-col gap-4 p-5"
          >
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Input
                label={t('profile.fields.firstName')}
                error={profileForm.formState.errors.firstName?.message}
                {...profileForm.register('firstName', req)}
              />
              <Input
                label={t('profile.fields.lastName')}
                error={profileForm.formState.errors.lastName?.message}
                {...profileForm.register('lastName', req)}
              />
              <Input
                label={t('profile.fields.displayName')}
                error={profileForm.formState.errors.displayName?.message}
                {...profileForm.register('displayName', req)}
              />
              <Input label={t('profile.fields.email')} value={profileQuery.data.email} disabled readOnly />
              <Input label={t('profile.fields.mobile')} {...profileForm.register('mobile')} />
              <Select
                label={t('profile.fields.language')}
                options={[
                  { value: 'en', label: t('language.en') },
                  { value: 'ar', label: t('language.ar') },
                ]}
                {...profileForm.register('preferredLanguage')}
              />
              <Input
                label={t('profile.fields.timeZone')}
                {...profileForm.register('timeZone', req)}
              />
            </div>
            <div className="flex justify-end">
              <Button type="submit" disabled={profileMutation.isPending || !profileForm.formState.isDirty}>
                {profileMutation.isPending && (
                  <Spinner size={15} className="border-white/40 border-t-white" />
                )}
                {t('profile.save')}
              </Button>
            </div>
          </form>
        </Card>

        {/* Change password */}
        <Card>
          <CardHeader title={t('profile.password.title')} />
          <form
            onSubmit={passwordForm.handleSubmit((v) => passwordMutation.mutate(v))}
            className="flex flex-col gap-4 p-5"
          >
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <Input
                label={t('profile.password.current')}
                type="password"
                autoComplete="current-password"
                error={passwordForm.formState.errors.currentPassword?.message}
                {...passwordForm.register('currentPassword', req)}
              />
              <Input
                label={t('profile.password.new')}
                type="password"
                autoComplete="new-password"
                error={passwordForm.formState.errors.newPassword?.message}
                {...passwordForm.register('newPassword', {
                  required: t('users.validation.required'),
                  minLength: { value: 8, message: t('profile.password.weak') },
                })}
              />
              <Input
                label={t('profile.password.confirm')}
                type="password"
                autoComplete="new-password"
                error={passwordForm.formState.errors.confirmPassword?.message}
                {...passwordForm.register('confirmPassword', {
                  validate: (v) => v === newPassword || t('profile.password.mismatch'),
                })}
              />
            </div>
            <div className="flex justify-end">
              <Button type="submit" disabled={passwordMutation.isPending}>
                {passwordMutation.isPending && (
                  <Spinner size={15} className="border-white/40 border-t-white" />
                )}
                {t('profile.password.submit')}
              </Button>
            </div>
          </form>
        </Card>

        {/* Active sessions */}
        <Card>
          <CardHeader title={t('profile.sessions.title')} subtitle={t('profile.sessions.subtitle')} />
          <div className="p-5">
            {sessionsQuery.isLoading ? (
              <LoadingBlock label={t('common.loading')} />
            ) : sessionsQuery.data && sessionsQuery.data.length > 0 ? (
              <ul className="flex flex-col divide-y divide-stone-100">
                {sessionsQuery.data.map((s) => (
                  <li key={s.id} className="flex items-center justify-between gap-3 py-3 first:pt-0 last:pb-0">
                    <div className="min-w-0">
                      <div className="font-medium text-ink">{s.createdByIp ?? t('profile.sessions.ip')}</div>
                      <div className="text-[12.5px] text-ink-4">
                        {t('profile.sessions.created')}: {formatDateTime(s.createdAt, locale)} ·{' '}
                        {t('profile.sessions.expires')}: {formatDateTime(s.expiresAt, locale)}
                      </div>
                    </div>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => revokeMutation.mutate(s.id)}
                      disabled={revokeMutation.isPending}
                    >
                      {t('profile.sessions.revoke')}
                    </Button>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-sm text-ink-4">{t('profile.sessions.empty')}</p>
            )}
          </div>
        </Card>
      </div>
    </>
  );
}
