import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Button, Badge, Spinner } from '@/shared/ui';
import { useToast } from '@/shared/ui/toast-context';
import { accessApi, accessKeys } from './api';
import { DataScope, type PermissionDto, type RoleDetail } from './types';

interface PermissionMatrixProps {
  role: RoleDetail;
  permissions: PermissionDto[];
  editable: boolean;
}

interface GrantState {
  granted: boolean;
  scope: DataScope;
}

const SCOPES: DataScope[] = [
  DataScope.Own,
  DataScope.Team,
  DataScope.Department,
  DataScope.Cluster,
  DataScope.Organization,
  DataScope.Workspace,
  DataScope.AllTenants,
];

export function PermissionMatrix({ role, permissions, editable }: PermissionMatrixProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();

  const initial = useMemo(() => {
    const map = new Map<string, GrantState>();
    const granted = new Map(role.permissions.map((p) => [p.permissionId, p.scope]));
    for (const perm of permissions) {
      const scope = granted.get(perm.id);
      map.set(perm.id, {
        granted: scope !== undefined,
        scope: scope ?? DataScope.Workspace,
      });
    }
    return map;
  }, [role.permissions, permissions]);

  const [state, setState] = useState<Map<string, GrantState>>(initial);
  useEffect(() => setState(initial), [initial]);

  const grouped = useMemo(() => {
    const groups = new Map<string, PermissionDto[]>();
    for (const perm of permissions) {
      const list = groups.get(perm.module) ?? [];
      list.push(perm);
      groups.set(perm.module, list);
    }
    return [...groups.entries()].sort(([a], [b]) => a.localeCompare(b));
  }, [permissions]);

  const setGrant = (id: string, patch: Partial<GrantState>) =>
    setState((prev) => {
      const next = new Map(prev);
      const current = next.get(id) ?? { granted: false, scope: DataScope.Workspace };
      next.set(id, { ...current, ...patch });
      return next;
    });

  const mutation = useMutation({
    mutationFn: () => {
      const grants = [...state.entries()]
        .filter(([, g]) => g.granted)
        .map(([permissionId, g]) => ({ permissionId, scope: g.scope }));
      return accessApi.setRolePermissions(role.id, { permissions: grants });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: accessKeys.role(role.id) });
      void queryClient.invalidateQueries({ queryKey: accessKeys.roles });
      toast.success(t('access.feedback.permissionsSaved'));
    },
    onError: () => toast.error(t('common.loadError')),
  });

  return (
    <div className="flex flex-col">
      <div className="flex items-center justify-between gap-3 border-b border-stone-150 px-5 py-4">
        <div>
          <h3 className="text-[15px] font-semibold">{t('access.matrix.title')}</h3>
          <p className="mt-px text-[12.5px] text-ink-4">{t('access.matrix.subtitle')}</p>
        </div>
        {editable && (
          <Button size="sm" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
            {mutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}
            {t('access.savePermissions')}
          </Button>
        )}
      </div>

      <div className="flex flex-col gap-5 p-5">
        {grouped.map(([module, perms]) => (
          <div key={module}>
            <div className="mb-1.5 text-[11px] font-bold uppercase tracking-[0.06em] text-ink-4">
              {module}
            </div>
            <div className="flex flex-col divide-y divide-stone-100 rounded-md border border-stone-200">
              {perms.map((perm) => {
                const g = state.get(perm.id) ?? { granted: false, scope: DataScope.Workspace };
                return (
                  <div key={perm.id} className="flex items-center gap-3 px-3 py-2.5">
                    <label className="flex flex-1 cursor-pointer items-center gap-2.5">
                      <input
                        type="checkbox"
                        className="h-4 w-4 accent-clay"
                        checked={g.granted}
                        disabled={!editable}
                        onChange={(e) => setGrant(perm.id, { granted: e.target.checked })}
                      />
                      <span className="text-sm text-ink-2">
                        {perm.action}
                        <span className="ms-1.5 font-mono text-[11.5px] text-ink-4">{perm.code}</span>
                      </span>
                      {perm.isHighRisk && (
                        <Badge tone="red">{t('access.matrix.highRisk')}</Badge>
                      )}
                    </label>
                    <select
                      className="input !w-[150px] !py-1.5 cursor-pointer appearance-none text-[13px]"
                      value={g.scope}
                      disabled={!editable || !g.granted}
                      onChange={(e) => setGrant(perm.id, { scope: Number(e.target.value) as DataScope })}
                    >
                      {SCOPES.map((s) => (
                        <option key={s} value={s}>
                          {t(`access.scope.${s}`)}
                        </option>
                      ))}
                    </select>
                  </div>
                );
              })}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
