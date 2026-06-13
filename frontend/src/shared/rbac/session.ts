import { createContext, useContext } from 'react';
import type { Action } from './permissions';

export interface Workspace {
  id: string;
  name: string;
  shortCode: string;
}

export interface SessionUser {
  id: string;
  fullName: string;
  email: string;
  jobTitle?: string;
  initials: string;
}

/** Lifecycle of the auth session: hydrating, signed in, or signed out. */
export type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated';

export interface Session {
  status: AuthStatus;
  /** Null unless status === 'authenticated'. */
  user: SessionUser | null;
  workspaces: Workspace[];
  activeWorkspaceId: string | null;
  /** Effective allowed actions, scoped to the active workspace + cluster. */
  actions: ReadonlySet<Action>;
  /** True once an explicit allow exists. Deny-wins is enforced on the backend. */
  can: (action: Action) => boolean;
  setActiveWorkspace: (workspaceId: string) => void;
  /** Authenticate against a workspace; throws ApiError on failure (incl. 2FA-required). */
  login: (workspaceSlug: string, email: string, password: string, twoFactorCode?: string) => Promise<void>;
  /** Revoke the refresh token server-side and clear local state. */
  logout: () => Promise<void>;
}

export const SessionContext = createContext<Session | null>(null);

export function useSession(): Session {
  const ctx = useContext(SessionContext);
  if (!ctx) {
    throw new Error('useSession must be used within an AuthProvider');
  }
  return ctx;
}

/** Two-letter avatar initials from a display name (e.g. "Ahmed Al-Qahtani" → "AA"). */
export function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return '?';
  if (parts.length === 1) return parts[0]!.slice(0, 2).toUpperCase();
  return (parts[0]![0]! + parts[parts.length - 1]![0]!).toUpperCase();
}
