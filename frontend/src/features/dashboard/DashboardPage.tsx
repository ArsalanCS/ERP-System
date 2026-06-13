import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { PageHeader, Card, Icon, LoadingBlock, EmptyState, Button, type IconName } from '@/shared/ui';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { useDirection } from '@/shared/hooks/useDirection';
import { formatNumber } from '@/shared/lib/format';
import { dashboardApi, dashboardKeys, type DashboardSummary } from './api';

interface Stat {
  key: keyof DashboardSummary;
  icon: IconName;
  /** Where the card's "View" link goes, or undefined to hide it. */
  to?: string;
  /** Require this action to render the link. */
  linkAction?: string;
  /** Emphasize (amber/red) when value > 0. */
  alert?: 'amber' | 'red';
}

interface Group {
  labelKey: string;
  stats: Stat[];
}

const GROUPS: Group[] = [
  {
    labelKey: 'dashboard.groups.people',
    stats: [
      { key: 'totalUsers', icon: 'users', to: '/admin/users', linkAction: Actions.UsersView },
      { key: 'activeUsers', icon: 'check', to: '/admin/users', linkAction: Actions.UsersView },
      {
        key: 'pendingInvitations',
        icon: 'mail',
        to: '/admin/users?status=0',
        linkAction: Actions.UsersView,
        alert: 'amber',
      },
      {
        key: 'suspendedUsers',
        icon: 'lock',
        to: '/admin/users?status=3',
        linkAction: Actions.UsersView,
        alert: 'red',
      },
    ],
  },
  {
    labelKey: 'dashboard.groups.structure',
    stats: [
      {
        key: 'organizations',
        icon: 'building',
        to: '/admin/business-structure',
        linkAction: Actions.BusinessStructureView,
      },
      {
        key: 'clusters',
        icon: 'sitemap',
        to: '/admin/business-structure',
        linkAction: Actions.BusinessStructureView,
      },
    ],
  },
  {
    labelKey: 'dashboard.groups.access',
    stats: [
      { key: 'roles', icon: 'shield', to: '/admin/access-control', linkAction: Actions.AccessControlView },
      { key: 'activeSessions', icon: 'key', to: '/admin/security', linkAction: Actions.SecurityView },
    ],
  },
];

export function DashboardPage() {
  const { t } = useTranslation();
  const { locale } = useDirection();
  const { can } = usePermissions();

  const query = useQuery({ queryKey: dashboardKeys.summary, queryFn: dashboardApi.summary });

  return (
    <>
      <PageHeader title={t('dashboard.title')} subtitle={t('dashboard.subtitle')} />

      {query.isLoading ? (
        <Card padded>
          <LoadingBlock label={t('common.loading')} />
        </Card>
      ) : query.isError || !query.data ? (
        <EmptyState
          icon="alert"
          title={t('common.loadError')}
          action={
            <Button variant="outline" size="sm" onClick={() => query.refetch()}>
              {t('common.retry')}
            </Button>
          }
        />
      ) : (
        <div className="flex flex-col gap-7">
          {GROUPS.map((group) => (
            <section key={group.labelKey}>
              <h2 className="mb-3 text-[12px] font-bold uppercase tracking-[0.07em] text-ink-4">
                {t(group.labelKey)}
              </h2>
              <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
                {group.stats.map((stat) => {
                  const value = query.data[stat.key];
                  const active = stat.alert !== undefined && value > 0;
                  const iconCls = !active
                    ? 'bg-stone-100 text-ink-3'
                    : stat.alert === 'red'
                      ? 'bg-red-100 text-red'
                      : 'bg-amber-100 text-amber';
                  const valueCls = !active ? 'text-ink' : stat.alert === 'red' ? 'text-red' : 'text-amber';
                  const showLink = stat.to && (!stat.linkAction || can(stat.linkAction));
                  return (
                    <Card key={stat.key} padded className="flex flex-col gap-3">
                      <div className="flex items-center justify-between">
                        <span className={`flex h-9 w-9 items-center justify-center rounded-md ${iconCls}`}>
                          <Icon name={stat.icon} size={18} />
                        </span>
                        {showLink && (
                          <Link
                            to={stat.to!}
                            className="flex items-center gap-0.5 text-[12.5px] font-semibold text-clay hover:underline"
                          >
                            {t('dashboard.viewAll')}
                            <Icon name="arrow-right" size={14} className="rtl:-scale-x-100" />
                          </Link>
                        )}
                      </div>
                      <div>
                        <div className={`text-[26px] font-bold tracking-[-0.02em] ${valueCls}`}>
                          {formatNumber(value, locale)}
                        </div>
                        <div className="text-[13px] text-ink-3">{t(`dashboard.cards.${stat.key}`)}</div>
                      </div>
                    </Card>
                  );
                })}
              </div>
            </section>
          ))}
        </div>
      )}
    </>
  );
}
