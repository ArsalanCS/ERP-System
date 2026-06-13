import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Card, CardHeader, Badge, Button, Input, Spinner, LoadingBlock } from '@/shared/ui';
import { useToast } from '@/shared/ui/toast-context';
import { ApiError } from '@/shared/api/client';
import { accountApi, accountKeys, type TwoFactorSetup } from '@/features/profile/api';

type Mode = 'idle' | 'enrolling' | 'disabling';

export function TwoFactorCard() {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();

  const profileQuery = useQuery({ queryKey: accountKeys.profile, queryFn: accountApi.getProfile });
  const [mode, setMode] = useState<Mode>('idle');
  const [setup, setSetup] = useState<TwoFactorSetup | null>(null);
  const [code, setCode] = useState('');
  const [codeError, setCodeError] = useState<string | null>(null);

  const reset = () => {
    setMode('idle');
    setSetup(null);
    setCode('');
    setCodeError(null);
  };

  const setupMutation = useMutation({
    mutationFn: accountApi.setupTwoFactor,
    onSuccess: (data) => {
      setSetup(data);
      setMode('enrolling');
    },
    onError: () => toast.error(t('common.loadError')),
  });

  const enableMutation = useMutation({
    mutationFn: () => accountApi.enableTwoFactor(code.trim()),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: accountKeys.profile });
      toast.success(t('security.twoFactor.enabledToast'));
      reset();
    },
    onError: (err) =>
      setCodeError(err instanceof ApiError ? t('security.twoFactor.invalidCode') : t('common.loadError')),
  });

  const disableMutation = useMutation({
    mutationFn: () => accountApi.disableTwoFactor(code.trim()),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: accountKeys.profile });
      void queryClient.invalidateQueries({ queryKey: accountKeys.sessions });
      toast.success(t('security.twoFactor.disabledToast'));
      reset();
    },
    onError: (err) =>
      setCodeError(err instanceof ApiError ? t('security.twoFactor.invalidCode') : t('common.loadError')),
  });

  const enabled = profileQuery.data?.twoFactorEnabled ?? false;
  const busy = enableMutation.isPending || disableMutation.isPending;

  return (
    <Card>
      <CardHeader
        title={t('security.twoFactor.title')}
        subtitle={t('security.twoFactor.subtitle')}
        action={
          <Badge tone={enabled ? 'green' : 'neutral'} dot>
            {enabled ? t('security.twoFactor.on') : t('security.twoFactor.off')}
          </Badge>
        }
      />
      <div className="p-5">
        {profileQuery.isLoading ? (
          <LoadingBlock label={t('common.loading')} />
        ) : mode === 'enrolling' && setup ? (
          <div className="flex max-w-md flex-col gap-3">
            <p className="text-[13px] text-ink-3">{t('security.twoFactor.setupHint')}</p>
            <div>
              <span className="label">{t('security.twoFactor.secretLabel')}</span>
              <code className="mt-1 block break-all rounded-md border border-stone-200 bg-stone-50 px-3 py-2 font-mono text-[15px] tracking-[0.15em] text-ink">
                {setup.secret}
              </code>
            </div>
            <Input
              label={t('security.twoFactor.codeLabel')}
              inputMode="numeric"
              autoComplete="one-time-code"
              value={code}
              error={codeError ?? undefined}
              onChange={(e) => {
                setCode(e.target.value);
                setCodeError(null);
              }}
            />
            <div className="flex gap-2.5">
              <Button onClick={() => enableMutation.mutate()} disabled={busy || code.trim().length < 6}>
                {enableMutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}
                {t('security.twoFactor.verify')}
              </Button>
              <Button variant="outline" onClick={reset} disabled={busy}>
                {t('security.twoFactor.cancel')}
              </Button>
            </div>
          </div>
        ) : mode === 'disabling' ? (
          <div className="flex max-w-md flex-col gap-3">
            <p className="text-[13px] text-ink-3">{t('security.twoFactor.disablePrompt')}</p>
            <Input
              label={t('security.twoFactor.codeLabel')}
              inputMode="numeric"
              autoComplete="one-time-code"
              value={code}
              error={codeError ?? undefined}
              onChange={(e) => {
                setCode(e.target.value);
                setCodeError(null);
              }}
            />
            <div className="flex gap-2.5">
              <Button
                className="btn-danger"
                onClick={() => disableMutation.mutate()}
                disabled={busy || code.trim().length < 6}
              >
                {disableMutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}
                {t('security.twoFactor.disable')}
              </Button>
              <Button variant="outline" onClick={reset} disabled={busy}>
                {t('security.twoFactor.cancel')}
              </Button>
            </div>
          </div>
        ) : enabled ? (
          <div className="flex items-center justify-between gap-3">
            <p className="text-[13px] text-ink-3">{t('security.twoFactor.enabledMsg')}</p>
            <Button variant="outline" onClick={() => setMode('disabling')}>
              {t('security.twoFactor.disable')}
            </Button>
          </div>
        ) : (
          <Button onClick={() => setupMutation.mutate()} disabled={setupMutation.isPending}>
            {setupMutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}
            {t('security.twoFactor.setup')}
          </Button>
        )}
      </div>
    </Card>
  );
}
