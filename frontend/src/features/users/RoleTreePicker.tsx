import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Spinner, Icon } from '@/shared/ui';
import { accessApi, accessKeys } from '@/features/access-control/api';
import { RoleType, type RoleListItem } from '@/features/access-control/types';

const GROUP_KEY: Record<RoleType, string> = {
  [RoleType.System]: 'system',
  [RoleType.Workspace]: 'workspace',
  [RoleType.Organization]: 'organization',
  [RoleType.Custom]: 'custom',
};
const GROUP_ORDER: RoleType[] = [RoleType.System, RoleType.Workspace, RoleType.Organization, RoleType.Custom];

interface RoleTreePickerProps {
  value: string[];
  onChange: (next: string[]) => void;
  enabled?: boolean;
}

/** Roles presented as a collapsible tree grouped by role type; multi-select. */
export function RoleTreePicker({ value, onChange, enabled = true }: RoleTreePickerProps) {
  const { t } = useTranslation();
  const rolesQuery = useQuery({ queryKey: accessKeys.roles, queryFn: accessApi.listRoles, enabled });
  const [collapsed, setCollapsed] = useState<Set<RoleType>>(new Set());

  const groups = useMemo(() => {
    const byType = new Map<RoleType, RoleListItem[]>();
    for (const role of rolesQuery.data ?? []) {
      (byType.get(role.type) ?? byType.set(role.type, []).get(role.type)!).push(role);
    }
    return GROUP_ORDER.filter((type) => byType.has(type)).map((type) => ({ type, roles: byType.get(type)! }));
  }, [rolesQuery.data]);

  const toggle = (id: string) =>
    onChange(value.includes(id) ? value.filter((r) => r !== id) : [...value, id]);

  const toggleGroup = (type: RoleType) =>
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(type)) next.delete(type);
      else next.add(type);
      return next;
    });

  if (rolesQuery.isLoading) return <Spinner size={18} />;
  if (!groups.length) return <p className="text-[13px] text-ink-4">{t('users.noRoles')}</p>;

  return (
    <div className="flex flex-col gap-1 rounded-md border border-stone-200 p-2">
      {groups.map(({ type, roles }) => {
        const open = !collapsed.has(type);
        return (
          <div key={type}>
            <button
              type="button"
              onClick={() => toggleGroup(type)}
              className="flex w-full items-center gap-1.5 rounded-sm px-1.5 py-1 text-[12px] font-bold uppercase tracking-[0.04em] text-ink-4 hover:bg-stone-50"
            >
              <Icon name={open ? 'chevron-down' : 'chevron-right'} size={13} />
              {t(`users.roleGroups.${GROUP_KEY[type]}`)}
              <span className="ms-auto font-normal normal-case text-ink-4">{roles.length}</span>
            </button>
            {open && (
              <div className="ms-3 border-s border-stone-150 ps-2">
                {roles.map((role) => (
                  <label
                    key={role.id}
                    className="flex cursor-pointer items-center gap-2.5 rounded-sm px-1.5 py-1 text-sm hover:bg-stone-50"
                  >
                    <input
                      type="checkbox"
                      className="h-4 w-4 accent-clay"
                      checked={value.includes(role.id)}
                      onChange={() => toggle(role.id)}
                    />
                    <span className="text-ink-2">{role.name}</span>
                  </label>
                ))}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}
