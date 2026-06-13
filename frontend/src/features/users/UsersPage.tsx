import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  PageHeader,
  Button,
  Input,
  Select,
  Icon,
  DataTable,
  Pagination,
  EmptyState,
  ConfirmDialog,
  type Column,
} from '@/shared/ui';
import { Can } from '@/shared/rbac/Can';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { initialsOf } from '@/shared/rbac/session';
import { useDirection } from '@/shared/hooks/useDirection';
import { useToast } from '@/shared/ui/toast-context';
import { formatDateTime } from '@/shared/lib/format';
import { usersApi, userKeys, type UserListParams } from './api';
import { UserStatus, type UserListItem } from './types';
import { UserStatusBadge } from './UserStatusBadge';
import { UserFormDrawer } from './UserFormDrawer';

type DialogKind = 'suspend' | 'reactivate' | 'archive';

interface DrawerState {
  open: boolean;
  mode: 'create' | 'edit';
  userId?: string;
}

const PAGE_SIZE = 25;

export function UsersPage() {
  const { t } = useTranslation();
  const { locale } = useDirection();
  const { can } = usePermissions();
  const toast = useToast();
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<string>('');

  const [drawer, setDrawer] = useState<DrawerState>({ open: false, mode: 'create' });
  const [dialog, setDialog] = useState<{ kind: DialogKind; user: UserListItem } | null>(null);

  // Debounce the search box → query param.
  useEffect(() => {
    const id = window.setTimeout(() => {
      setSearch(searchInput.trim());
      setPage(1);
    }, 350);
    return () => window.clearTimeout(id);
  }, [searchInput]);

  const params = useMemo<UserListParams>(
    () => ({
      page,
      pageSize: PAGE_SIZE,
      search: search || undefined,
      status: status === '' ? undefined : (Number(status) as UserStatus),
    }),
    [page, search, status],
  );

  const listQuery = useQuery({
    queryKey: userKeys.list(params),
    queryFn: () => usersApi.list(params),
  });

  const statusMutation = useMutation({
    mutationFn: async ({
      kind,
      user,
      reason,
    }: {
      kind: DialogKind;
      user: UserListItem;
      reason: string;
    }) => {
      if (kind === 'suspend') return usersApi.suspend(user.id, reason);
      if (kind === 'reactivate') return usersApi.reactivate(user.id);
      return usersApi.archive(user.id);
    },
    onSuccess: (_data, vars) => {
      void queryClient.invalidateQueries({ queryKey: userKeys.all });
      toast.success(t(`users.feedback.${vars.kind === 'suspend' ? 'suspended' : vars.kind === 'reactivate' ? 'reactivated' : 'archived'}`));
      setDialog(null);
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : t('common.loadError')),
  });

  const statusOptions = [
    { value: '', label: t('users.filters.allStatuses') },
    ...[
      UserStatus.PendingInvitation,
      UserStatus.Active,
      UserStatus.Inactive,
      UserStatus.Suspended,
      UserStatus.Locked,
      UserStatus.Archived,
    ].map((s) => ({ value: String(s), label: t(`users.status.${s}`) })),
  ];

  const canManage = can(Actions.UsersManage);

  const columns: Column<UserListItem>[] = [
    {
      key: 'user',
      header: t('users.columns.user'),
      render: (u) => (
        <div className="flex items-center gap-2.5">
          <span className="avatar !h-9 !w-9 !text-[12px]" style={{ background: 'var(--color-stone-700)' }}>
            {initialsOf(u.displayName)}
          </span>
          <div className="min-w-0">
            <div className="truncate font-semibold text-ink">{u.displayName}</div>
            <div className="truncate text-[12.5px] text-ink-4">{u.email}</div>
          </div>
        </div>
      ),
    },
    {
      key: 'contact',
      header: t('users.columns.contact'),
      render: (u) => (
        <div className="text-[13px]">
          {u.jobTitle && <div className="text-ink-2">{u.jobTitle}</div>}
          <div className="text-ink-4">{u.mobile ?? '—'}</div>
        </div>
      ),
    },
    {
      key: 'status',
      header: t('users.columns.status'),
      render: (u) => <UserStatusBadge status={u.status} />,
    },
    {
      key: 'lastLogin',
      header: t('users.columns.lastLogin'),
      render: (u) => (
        <span className="text-[13px] text-ink-3">
          {u.lastLoginAt ? formatDateTime(u.lastLoginAt, locale) : t('common.never')}
        </span>
      ),
    },
    {
      key: 'twoFactor',
      header: t('users.columns.twoFactor'),
      align: 'center',
      render: (u) =>
        u.twoFactorEnabled ? (
          <Icon name="check" size={17} className="mx-auto text-green" />
        ) : (
          <span className="text-ink-4">—</span>
        ),
    },
  ];

  if (canManage) {
    columns.push({
      key: 'actions',
      header: t('users.columns.actions'),
      align: 'end',
      cellClassName: 'whitespace-nowrap',
      render: (u) => (
        <div className="flex items-center justify-end gap-1" onClick={(e) => e.stopPropagation()}>
          <button
            type="button"
            title={t('users.actions.edit')}
            onClick={() => setDrawer({ open: true, mode: 'edit', userId: u.id })}
            className="flex h-8 w-8 items-center justify-center rounded-sm text-ink-4 hover:bg-stone-100 hover:text-ink"
          >
            <Icon name="edit" size={16} />
          </button>
          {u.status === UserStatus.Active ? (
            <button
              type="button"
              title={t('users.actions.suspend')}
              onClick={() => setDialog({ kind: 'suspend', user: u })}
              className="flex h-8 w-8 items-center justify-center rounded-sm text-ink-4 hover:bg-stone-100 hover:text-amber"
            >
              <Icon name="lock" size={16} />
            </button>
          ) : (
            u.status !== UserStatus.Archived && (
              <button
                type="button"
                title={t('users.actions.reactivate')}
                onClick={() => setDialog({ kind: 'reactivate', user: u })}
                className="flex h-8 w-8 items-center justify-center rounded-sm text-ink-4 hover:bg-stone-100 hover:text-green"
              >
                <Icon name="refresh" size={16} />
              </button>
            )
          )}
          {u.status !== UserStatus.Archived && (
            <button
              type="button"
              title={t('users.actions.archive')}
              onClick={() => setDialog({ kind: 'archive', user: u })}
              className="flex h-8 w-8 items-center justify-center rounded-sm text-ink-4 hover:bg-stone-100 hover:text-red"
            >
              <Icon name="trash" size={16} />
            </button>
          )}
        </div>
      ),
    });
  }

  const data = listQuery.data;

  return (
    <>
      <PageHeader
        title={t('users.title')}
        subtitle={t('users.subtitle')}
        actions={
          <Can action={Actions.UsersManage}>
            <Button
              leadingIcon={<Icon name="plus" size={16} />}
              onClick={() => setDrawer({ open: true, mode: 'create' })}
            >
              {t('users.invite')}
            </Button>
          </Can>
        }
      />

      <div className="mb-4 flex flex-wrap items-end gap-3">
        <div className="min-w-[240px] flex-1">
          <Input
            placeholder={t('users.searchPlaceholder')}
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
          />
        </div>
        <div className="w-[200px]">
          <Select
            options={statusOptions}
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
          />
        </div>
      </div>

      {listQuery.isError ? (
        <EmptyState
          icon="alert"
          title={t('common.loadError')}
          action={
            <Button variant="outline" size="sm" onClick={() => listQuery.refetch()}>
              {t('common.retry')}
            </Button>
          }
        />
      ) : (
        <>
          <DataTable
            columns={columns}
            rows={data?.items ?? []}
            rowKey={(u) => u.id}
            loading={listQuery.isLoading}
            loadingLabel={t('common.loading')}
            empty={<EmptyState icon="users" title={t('users.empty.title')} body={t('users.empty.body')} />}
          />
          {data && data.total > PAGE_SIZE && (
            <Pagination
              page={data.page}
              pageSize={data.pageSize}
              total={data.total}
              onPageChange={setPage}
            />
          )}
        </>
      )}

      <UserFormDrawer
        open={drawer.open}
        mode={drawer.mode}
        userId={drawer.userId}
        onClose={() => setDrawer((d) => ({ ...d, open: false }))}
        onSaved={(kind) => {
          setDrawer((d) => ({ ...d, open: false }));
          toast.success(t(`users.feedback.${kind}`));
        }}
      />

      <ConfirmDialog
        open={dialog?.kind === 'suspend'}
        title={t('users.suspendDialog.title')}
        reasonLabel={t('users.suspendDialog.reason')}
        confirmLabel={t('users.suspendDialog.confirm')}
        tone="danger"
        loading={statusMutation.isPending}
        onCancel={() => setDialog(null)}
        onConfirm={(reason) =>
          dialog && statusMutation.mutate({ kind: 'suspend', user: dialog.user, reason })
        }
      />
      <ConfirmDialog
        open={dialog?.kind === 'reactivate'}
        title={t('users.reactivateDialog.title')}
        message={t('users.reactivateDialog.message')}
        confirmLabel={t('users.reactivateDialog.confirm')}
        loading={statusMutation.isPending}
        onCancel={() => setDialog(null)}
        onConfirm={() =>
          dialog && statusMutation.mutate({ kind: 'reactivate', user: dialog.user, reason: '' })
        }
      />
      <ConfirmDialog
        open={dialog?.kind === 'archive'}
        title={t('users.archiveDialog.title')}
        message={t('users.archiveDialog.message')}
        confirmLabel={t('users.archiveDialog.confirm')}
        tone="danger"
        loading={statusMutation.isPending}
        onCancel={() => setDialog(null)}
        onConfirm={() =>
          dialog && statusMutation.mutate({ kind: 'archive', user: dialog.user, reason: '' })
        }
      />
    </>
  );
}
