import { useSession } from './session';
import type { Action } from './permissions';

export interface Permissions {
  can: (action: Action) => boolean;
  /** True if the user holds every action in the list. */
  canAll: (actions: Action[]) => boolean;
  /** True if the user holds at least one action in the list. */
  canAny: (actions: Action[]) => boolean;
}

export function usePermissions(): Permissions {
  const { can } = useSession();
  return {
    can,
    canAll: (actions) => actions.every(can),
    canAny: (actions) => actions.some(can),
  };
}
