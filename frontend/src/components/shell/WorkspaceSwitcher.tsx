import { useTranslation } from 'react-i18next';
import { useSession } from '@/shared/rbac/session';
import { Icon } from '@/shared/ui/Icon';

/**
 * Workspace switcher (handoff `.sb-ws`). One user may belong to multiple
 * workspaces (Identity spec §4.6). Switching re-scopes every query.
 */
export function WorkspaceSwitcher() {
  const { t } = useTranslation();
  const { workspaces, activeWorkspaceId } = useSession();
  const active = workspaces.find((w) => w.id === activeWorkspaceId) ?? workspaces[0];

  if (!active) return null;

  return (
    <button
      type="button"
      title={t('workspace.switcher')}
      className="mx-3 mb-2 flex cursor-pointer items-center gap-2.5 rounded-md border border-stone-200 bg-stone-50 px-[11px] py-[9px] text-start transition-colors hover:bg-stone-100"
    >
      <span
        className="avatar !h-7 !w-7 !text-[11px]"
        style={{ background: 'var(--color-clay)' }}
        aria-hidden="true"
      >
        {active.shortCode}
      </span>
      <span className="min-w-0 flex-1">
        <span className="block truncate text-[13.5px] font-semibold text-ink">{active.name}</span>
        <span className="block truncate text-[11.5px] text-ink-4">{t('workspace.current')}</span>
      </span>
      <Icon name="chevron-down" size={16} className="text-ink-4" />
    </button>
  );
}
