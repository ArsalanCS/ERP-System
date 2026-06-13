import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Card, CardHeader, Input, Button, Spinner, LoadingBlock, EmptyState } from '@/shared/ui';
import { useToast } from '@/shared/ui/toast-context';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { securityApi, securityKeys, type UpdateSecurityPolicyRequest } from './api';

interface FormShape {
  passwordMinLength: number;
  requireUppercase: boolean;
  requireLowercase: boolean;
  requireDigit: boolean;
  requireSymbol: boolean;
  passwordExpiryDays: string; // '' = never
  maxFailedAttempts: number;
  lockoutMinutes: number;
  sessionIdleTimeoutMinutes: number;
  refreshTokenDays: number;
  requireTwoFactor: boolean;
}

function Toggle({
  label,
  disabled,
  ...rest
}: { label: string; disabled?: boolean } & React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <label className="flex cursor-pointer items-center gap-2.5 py-1 text-sm text-ink-2">
      <input type="checkbox" className="h-4 w-4 accent-clay" disabled={disabled} {...rest} />
      {label}
    </label>
  );
}

export function SecurityPolicyForm() {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();
  const { can } = usePermissions();
  const canManage = can(Actions.SecurityManage);

  const query = useQuery({ queryKey: securityKeys.policy, queryFn: securityApi.getPolicy });

  const { register, handleSubmit, reset, formState: { isDirty } } = useForm<FormShape>();

  useEffect(() => {
    const p = query.data;
    if (p) {
      reset({
        passwordMinLength: p.passwordMinLength,
        requireUppercase: p.requireUppercase,
        requireLowercase: p.requireLowercase,
        requireDigit: p.requireDigit,
        requireSymbol: p.requireSymbol,
        passwordExpiryDays: p.passwordExpiryDays?.toString() ?? '',
        maxFailedAttempts: p.maxFailedAttempts,
        lockoutMinutes: p.lockoutMinutes,
        sessionIdleTimeoutMinutes: p.sessionIdleTimeoutMinutes,
        refreshTokenDays: p.refreshTokenDays,
        requireTwoFactor: p.requireTwoFactor,
      });
    }
  }, [query.data, reset]);

  const mutation = useMutation({
    mutationFn: (values: FormShape) => {
      const body: UpdateSecurityPolicyRequest = {
        passwordMinLength: Number(values.passwordMinLength),
        requireUppercase: values.requireUppercase,
        requireLowercase: values.requireLowercase,
        requireDigit: values.requireDigit,
        requireSymbol: values.requireSymbol,
        passwordExpiryDays: values.passwordExpiryDays === '' ? null : Number(values.passwordExpiryDays),
        maxFailedAttempts: Number(values.maxFailedAttempts),
        lockoutMinutes: Number(values.lockoutMinutes),
        sessionIdleTimeoutMinutes: Number(values.sessionIdleTimeoutMinutes),
        refreshTokenDays: Number(values.refreshTokenDays),
        requireTwoFactor: values.requireTwoFactor,
      };
      return securityApi.updatePolicy(body);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: securityKeys.policy });
      toast.success(t('security.policy.saved'));
    },
    onError: () => toast.error(t('common.loadError')),
  });

  if (query.isLoading) {
    return (
      <Card>
        <CardHeader title={t('security.policy.title')} subtitle={t('security.policy.subtitle')} />
        <LoadingBlock label={t('common.loading')} />
      </Card>
    );
  }
  if (query.isError || !query.data) {
    return (
      <Card padded>
        <EmptyState
          icon="alert"
          title={t('common.loadError')}
          action={
            <Button variant="outline" size="sm" onClick={() => query.refetch()}>
              {t('common.retry')}
            </Button>
          }
        />
      </Card>
    );
  }

  const ro = !canManage;

  return (
    <Card>
      <CardHeader title={t('security.policy.title')} subtitle={t('security.policy.subtitle')} />
      <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="flex flex-col gap-6 p-5">
        <section>
          <h4 className="mb-2 text-[12px] font-bold uppercase tracking-[0.06em] text-ink-4">
            {t('security.policy.password')}
          </h4>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Input
              type="number"
              label={t('security.policy.minLength')}
              disabled={ro}
              {...register('passwordMinLength')}
            />
            <Input
              type="number"
              label={t('security.policy.expiryDays')}
              disabled={ro}
              {...register('passwordExpiryDays')}
            />
          </div>
          <div className="mt-2 grid grid-cols-1 gap-x-6 sm:grid-cols-2">
            <Toggle label={t('security.policy.requireUppercase')} disabled={ro} {...register('requireUppercase')} />
            <Toggle label={t('security.policy.requireLowercase')} disabled={ro} {...register('requireLowercase')} />
            <Toggle label={t('security.policy.requireDigit')} disabled={ro} {...register('requireDigit')} />
            <Toggle label={t('security.policy.requireSymbol')} disabled={ro} {...register('requireSymbol')} />
          </div>
        </section>

        <section>
          <h4 className="mb-2 text-[12px] font-bold uppercase tracking-[0.06em] text-ink-4">
            {t('security.policy.lockout')}
          </h4>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Input type="number" label={t('security.policy.maxFailed')} disabled={ro} {...register('maxFailedAttempts')} />
            <Input type="number" label={t('security.policy.lockoutMinutes')} disabled={ro} {...register('lockoutMinutes')} />
          </div>
        </section>

        <section>
          <h4 className="mb-2 text-[12px] font-bold uppercase tracking-[0.06em] text-ink-4">
            {t('security.policy.sessions')}
          </h4>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Input type="number" label={t('security.policy.idleTimeout')} disabled={ro} {...register('sessionIdleTimeoutMinutes')} />
            <Input type="number" label={t('security.policy.refreshDays')} disabled={ro} {...register('refreshTokenDays')} />
          </div>
          <div className="mt-2">
            <Toggle label={t('security.policy.requireTwoFactor')} disabled={ro} {...register('requireTwoFactor')} />
          </div>
        </section>

        {canManage && (
          <div className="flex justify-end">
            <Button type="submit" disabled={mutation.isPending || !isDirty}>
              {mutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}
              {t('security.policy.save')}
            </Button>
          </div>
        )}
      </form>
    </Card>
  );
}
