import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Drawer, Input, Button, Icon, Spinner, EmptyState } from '@/shared/ui';
import { initialsOf } from '@/shared/rbac/session';
import { ApiError } from '@/shared/api/client';
import { usersApi, userKeys } from '@/features/users/api';
import { UserStatus } from '@/features/users/types';
import { structureKeys, type StructureNodeDto } from './api';

interface AssignMemberDialogProps {
  /** The node to add a member to; null closes the dialog. */
  node: StructureNodeDto | null;
  onClose: () => void;
  /** Caller navigates to the new-user flow pre-placed at this node. */
  onAddNew: (node: StructureNodeDto) => void;
  onAssigned: () => void;
}

/**
 * "Add member" entry point on a structure node. Lets the manager either jump to
 * the new-user flow (pre-placed at the node) or assign an existing user by moving
 * their placement to this node.
 */
export function AssignMemberDialog({ node, onClose, onAddNew, onAssigned }: AssignMemberDialogProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!node) {
      setSearchInput('');
      setSearch('');
      setError(null);
    }
  }, [node]);

  // Debounce the search box.
  useEffect(() => {
    const id = window.setTimeout(() => setSearch(searchInput.trim()), 300);
    return () => window.clearTimeout(id);
  }, [searchInput]);

  const params = useMemo(() => ({ page: 1, pageSize: 25, search: search || undefined }), [search]);
  const listQuery = useQuery({
    queryKey: userKeys.list(params),
    queryFn: () => usersApi.list(params),
    enabled: node !== null,
  });

  // Assignment moves an existing user's placement to this node. We re-read the
  // user's detail first so the rest of their employee record is preserved.
  const assignMutation = useMutation({
    mutationFn: async (userId: string) => {
      if (!node) return;
      const d = await usersApi.get(userId);
      await usersApi.update(userId, {
        firstName: d.firstName,
        lastName: d.lastName,
        displayName: d.displayName,
        mobile: d.mobile,
        jobTitle: d.jobTitle,
        preferredLanguage: d.preferredLanguage,
        timeZone: d.timeZone,
        employeeNumber: d.employeeNumber,
        placementNodeId: node.id,
        managerId: d.managerId,
        hireDate: d.hireDate,
        accessStartDate: d.accessStartDate,
        accessExpiryDate: d.accessExpiryDate,
        roleIds: d.roleIds,
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: userKeys.all });
      void queryClient.invalidateQueries({ queryKey: structureKeys.tree });
      onAssigned();
    },
    onError: (err) => {
      setError(
        err instanceof ApiError && err.details?.[0]?.message
          ? err.details[0].message
          : err instanceof Error
            ? err.message
            : t('common.loadError'),
      );
    },
  });

  const users = (listQuery.data?.items ?? []).filter((u) => u.status !== UserStatus.Archived);

  return (
    <Drawer
      open={node !== null}
      onClose={onClose}
      title={t('structure.assign.title')}
      subtitle={node ? node.name : undefined}
      footer={
        <Button variant="outline" size="sm" onClick={onClose} disabled={assignMutation.isPending}>
          {t('common.close')}
        </Button>
      }
    >
      <div className="flex flex-col gap-5">
        {error && (
          <div className="rounded-md border border-red-100 bg-red-100 px-3.5 py-2.5 text-[13px] font-medium text-red">
            {error}
          </div>
        )}

        <Button
          variant="outline"
          leadingIcon={<Icon name="plus" size={16} />}
          onClick={() => node && onAddNew(node)}
          disabled={assignMutation.isPending}
        >
          {t('structure.assign.addNew')}
        </Button>

        <div className="flex items-center gap-3">
          <span className="h-px flex-1 bg-stone-150" />
          <span className="text-[11px] font-semibold uppercase tracking-[0.04em] text-ink-4">
            {t('structure.assign.or')}
          </span>
          <span className="h-px flex-1 bg-stone-150" />
        </div>

        <div className="flex flex-col gap-3">
          <p className="text-[13px] text-ink-3">{t('structure.assign.existingHint')}</p>
          <Input
            placeholder={t('users.searchPlaceholder')}
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
          />

          {listQuery.isLoading ? (
            <div className="flex justify-center py-8">
              <Spinner size={22} />
            </div>
          ) : users.length === 0 ? (
            <EmptyState icon="users" title={t('structure.assign.noUsers')} />
          ) : (
            <ul className="flex flex-col divide-y divide-stone-100 rounded-md border border-stone-150">
              {users.map((u) => {
                const placedHere = u.id === assignMutation.variables && assignMutation.isPending;
                return (
                  <li key={u.id} className="flex items-center gap-2.5 px-3 py-2.5">
                    <span
                      className="avatar !h-8 !w-8 !text-[11px]"
                      style={{ background: 'var(--color-stone-700)' }}
                    >
                      {initialsOf(u.displayName)}
                    </span>
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-[13.5px] font-semibold text-ink">{u.displayName}</div>
                      <div className="truncate text-[12px] text-ink-4">{u.jobTitle || u.email}</div>
                    </div>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => assignMutation.mutate(u.id)}
                      disabled={assignMutation.isPending}
                    >
                      {placedHere && <Spinner size={13} />}
                      {t('structure.assign.assign')}
                    </Button>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
      </div>
    </Drawer>
  );
}
