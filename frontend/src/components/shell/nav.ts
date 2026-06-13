import type { IconName } from '@/shared/ui/Icon';
import type { Action } from '@/shared/rbac/permissions';
import { Actions } from '@/shared/rbac/permissions';

export interface NavItem {
  id: string;
  /** i18n key under `nav.*`. */
  labelKey: string;
  to: string;
  icon: IconName;
  /** Action required to SEE this item. Undefined => always visible. */
  action?: Action;
}

export interface NavGroup {
  /** i18n key under `nav.groups.*`. */
  labelKey: string;
  items: NavItem[];
}

/**
 * The Identity module is intentionally limited to seven primary admin pages
 * plus the personal account area (spec §1.2 / §18). Do not add sidebar pages
 * without an approved change request.
 */
export const NAV: NavGroup[] = [
  {
    labelKey: 'nav.groups.administration',
    items: [
      { id: 'overview', labelKey: 'nav.overview', to: '/admin/overview', icon: 'gauge', action: Actions.OverviewView },
      { id: 'users', labelKey: 'nav.users', to: '/admin/users', icon: 'users', action: Actions.UsersView },
      { id: 'access-control', labelKey: 'nav.accessControl', to: '/admin/access-control', icon: 'shield', action: Actions.AccessControlView },
      { id: 'business-structure', labelKey: 'nav.businessStructure', to: '/admin/business-structure', icon: 'sitemap', action: Actions.BusinessStructureView },
      { id: 'security', labelKey: 'nav.security', to: '/admin/security', icon: 'lock', action: Actions.SecurityView },
      { id: 'audit', labelKey: 'nav.audit', to: '/admin/audit', icon: 'history', action: Actions.AuditView },
      { id: 'settings', labelKey: 'nav.settings', to: '/admin/settings', icon: 'settings', action: Actions.SettingsView },
    ],
  },
  {
    labelKey: 'nav.groups.account',
    items: [
      // Personal account is always visible — every authenticated user has it (spec §10).
      { id: 'profile', labelKey: 'nav.profile', to: '/admin/profile', icon: 'user' },
    ],
  },
];
