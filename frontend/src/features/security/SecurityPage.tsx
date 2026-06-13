import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PageHeader, Card, CardHeader, Button, LoadingBlock } from '@/shared/ui';
import { Can } from '@/shared/rbac/Can';
import { Actions } from '@/shared/rbac/permissions';
import { useToast } from '@/shared/ui/toast-context';
import { useDirection } from '@/shared/hooks/useDirection';
import { formatDateTime } from '@/shared/lib/format';
import { accountApi, accountKeys } from '@/features/profile/api';
import { TwoFactorCard } from './TwoFactorCard';
import { SecurityPolicyForm } from './SecurityPolicyForm';

export function SecurityPage() {
  const { t } = useTranslation();
  const { locale } = useDirection();
  const toast = useToast();
  const queryClient = useQueryClient();

  const sessionsQuery = useQuery({ queryKey: accountKeys.sessions, queryFn: accountApi.listSessions });

  const revokeMutation = useMutation({
    mutationFn: (id: string) => accountApi.revokeSession(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: accountKeys.sessions });
      toast.success(t('profile.sessions.revoked'));
    },
    onError: () => toast.error(t('common.loadError')),
  });

  return (
    <>
      <PageHeader title={t('security.title')} subtitle={t('security.subtitle')} />

      <div className="flex max-w-3xl flex-col gap-5">
        <TwoFactorCard />

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

        {/* Workspace security policy (admins) */}
        <Can action={Actions.SecurityView}>
          <SecurityPolicyForm />
        </Can>
      </div>
    </>
  );
}
