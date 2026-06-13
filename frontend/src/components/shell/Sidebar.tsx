import { NavLink, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { NAV } from './nav';
import { WorkspaceSwitcher } from './WorkspaceSwitcher';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { useSession } from '@/shared/rbac/session';
import { Icon } from '@/shared/ui/Icon';
import { cn } from '@/shared/lib/cn';

/**
 * Left navigation (handoff `.sidebar`, 248px). Unauthorized items are hidden
 * completely — never shown disabled (Identity spec §1.3 / §2.2).
 */
export function Sidebar() {
  const { t } = useTranslation();
  const { can } = usePermissions();
  const { user, logout } = useSession();
  const navigate = useNavigate();

  const handleSignOut = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <aside className="sticky top-0 flex h-screen w-[248px] flex-none flex-col border-e border-stone-200 bg-paper">
      {/* brand */}
      <div className="flex items-center gap-2.5 px-[18px] pb-3.5 pt-[18px]">
        <span className="logo-mark">
          <Icon name="bolt" size={18} />
        </span>
        <span className="logo">{t('app.brand')}</span>
      </div>

      <WorkspaceSwitcher />

      {/* grouped nav */}
      <nav className="flex-1 overflow-y-auto px-3 pb-3 pt-2">
        {NAV.map((group) => {
          const items = group.items.filter((item) => !item.action || can(item.action));
          if (items.length === 0) return null;
          return (
            <div key={group.labelKey}>
              <div className="px-2.5 pb-1.5 pt-3.5 text-[11px] font-bold uppercase tracking-[0.07em] text-ink-4">
                {t(group.labelKey)}
              </div>
              {items.map((item) => (
                <NavLink
                  key={item.id}
                  to={item.to}
                  className={({ isActive }) =>
                    cn(
                      'mb-px flex items-center gap-[11px] rounded-sm px-2.5 py-2 text-sm font-medium transition-colors',
                      isActive
                        ? 'bg-clay-100 font-semibold text-clay-active'
                        : 'text-ink-2 hover:bg-stone-100 hover:text-ink',
                    )
                  }
                >
                  {({ isActive }) => (
                    <>
                      <Icon name={item.icon} className={isActive ? 'text-clay' : 'text-ink-4'} />
                      <span className="min-w-0 flex-1 truncate">{t(item.labelKey)}</span>
                    </>
                  )}
                </NavLink>
              ))}
            </div>
          );
        })}
      </nav>

      {/* footer user card */}
      {user && (
        <div className="flex-none border-t border-stone-200 p-3">
          <div className="flex cursor-pointer items-center gap-2.5 rounded-sm px-2 py-1.5 hover:bg-stone-100">
            <span className="avatar" style={{ background: 'var(--color-stone-700)' }}>
              {user.initials}
            </span>
            <span className="min-w-0 flex-1">
              <span className="block truncate text-[13.5px] font-semibold text-ink">{user.fullName}</span>
              {user.jobTitle && (
                <span className="block truncate text-[11.5px] text-ink-4">{user.jobTitle}</span>
              )}
            </span>
            <button
              type="button"
              title={t('common.signOut')}
              onClick={handleSignOut}
              className="flex h-8 w-8 flex-none items-center justify-center rounded-sm text-ink-4 hover:bg-stone-150 hover:text-ink"
            >
              <Icon name="log-out" size={17} />
            </button>
          </div>
        </div>
      )}
    </aside>
  );
}
