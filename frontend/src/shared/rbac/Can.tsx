import type { ReactNode } from 'react';
import { usePermissions } from './usePermissions';
import type { Action } from './permissions';

interface CanProps {
  /** Single action, or a list. With `mode="all"` every action is required. */
  action: Action | Action[];
  mode?: 'any' | 'all';
  children: ReactNode;
  /** Rendered when the check fails. Defaults to nothing (hide, don't disable). */
  fallback?: ReactNode;
}

/**
 * Permission guard. Renders children only when the current session is allowed.
 * Hiding here is UX only — the backend still authorizes every request.
 */
export function Can({ action, mode = 'any', children, fallback = null }: CanProps) {
  const { can, canAll, canAny } = usePermissions();
  const actions = Array.isArray(action) ? action : [action];
  const allowed = mode === 'all' ? canAll(actions) : actions.length === 1 ? can(actions[0]!) : canAny(actions);
  return <>{allowed ? children : fallback}</>;
}
