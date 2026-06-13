import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import {
  SessionContext,
  initialsOf,
  type AuthStatus,
  type Session,
  type SessionUser,
  type Workspace,
} from '@/shared/rbac/session';
import type { Action } from '@/shared/rbac/permissions';
import { authApi, type AuthUser } from '@/features/auth/api';
import { api } from '@/shared/api/client';
import { tokenStore } from '@/shared/api/tokenStore';

interface SessionState {
  status: AuthStatus;
  user: SessionUser | null;
  workspace: Workspace | null;
  actions: ReadonlySet<Action>;
}

const ANON: SessionState = {
  status: 'unauthenticated',
  user: null,
  workspace: null,
  actions: new Set(),
};

const LOADING: SessionState = { ...ANON, status: 'loading' };

function shortCodeOf(slug: string): string {
  const cleaned = slug.replace(/[^a-zA-Z0-9]+/g, ' ').trim();
  return initialsOf(cleaned || slug);
}

function buildState(user: AuthUser, actions: string[], slug: string | null): SessionState {
  return {
    status: 'authenticated',
    user: {
      id: user.id,
      fullName: user.displayName,
      email: user.email,
      initials: initialsOf(user.displayName),
    },
    workspace: {
      id: user.workspaceId,
      name: slug ?? user.email.split('@')[1] ?? 'Workspace',
      shortCode: shortCodeOf(slug ?? user.displayName),
    },
    actions: new Set(actions),
  };
}

/**
 * Real authentication provider (Identity spec §13.1). Hydrates the session
 * from stored tokens on load, exposes login/logout, and reacts to forced
 * logout when a silent refresh fails (tokenStore.clear → subscribe).
 *
 * The backend remains the source of truth for authorization; `actions` here
 * only drives UI visibility (CLAUDE.md §4.2).
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<SessionState>(() =>
    tokenStore.hasSession() ? LOADING : ANON,
  );
  // Keep the authoritative AuthUser around so a re-hydrate can rebuild state.
  const userRef = useRef<AuthUser | null>(null);

  const hydrate = useCallback(async () => {
    if (!tokenStore.hasSession()) {
      setState(ANON);
      return;
    }
    try {
      const me = await authApi.me();
      // /auth/me lacks the display name; fetch the profile for it on a cold
      // reload (best-effort — falls back to the email local-part).
      let displayName = userRef.current?.displayName ?? me.email.split('@')[0] ?? me.email;
      let twoFactorEnabled = userRef.current?.twoFactorEnabled ?? false;
      try {
        const profile = await api.get<{ displayName: string; twoFactorEnabled: boolean }>(
          '/me/profile',
        );
        displayName = profile.displayName;
        twoFactorEnabled = profile.twoFactorEnabled;
      } catch {
        // Profile is non-essential for hydration; ignore and use the fallback.
      }
      const user: AuthUser = {
        id: me.id,
        workspaceId: me.workspaceId,
        email: me.email,
        displayName,
        preferredLanguage: userRef.current?.preferredLanguage ?? 'en',
        requirePasswordChange: false,
        twoFactorEnabled,
      };
      setState(buildState(user, me.actions, tokenStore.slug));
    } catch {
      tokenStore.clear();
      setState(ANON);
    }
  }, []);

  // Hydrate once on mount.
  useEffect(() => {
    void hydrate();
  }, [hydrate]);

  // React to forced logout (e.g. refresh failed inside the API client).
  useEffect(
    () =>
      tokenStore.subscribe(() => {
        if (!tokenStore.hasSession()) {
          userRef.current = null;
          setState(ANON);
        }
      }),
    [],
  );

  const login = useCallback(
    async (workspaceSlug: string, email: string, password: string, twoFactorCode?: string) => {
    const tokens = await authApi.login(workspaceSlug, email, password, twoFactorCode);
    tokenStore.set(
      { accessToken: tokens.accessToken, refreshToken: tokens.refreshToken },
      workspaceSlug,
    );
    userRef.current = tokens.user;
    const me = await authApi.me();
    setState(buildState(tokens.user, me.actions, workspaceSlug));
  }, []);

  const logout = useCallback(async () => {
    const refresh = tokenStore.refresh;
    if (refresh) {
      try {
        await authApi.logout(refresh);
      } catch {
        // Best-effort server revoke; local clear happens regardless.
      }
    }
    userRef.current = null;
    tokenStore.clear();
    setState(ANON);
  }, []);

  const session = useMemo<Session>(() => {
    const actions = state.actions;
    return {
      status: state.status,
      user: state.user,
      workspaces: state.workspace ? [state.workspace] : [],
      activeWorkspaceId: state.workspace?.id ?? null,
      actions,
      can: (action) => actions.has(action),
      setActiveWorkspace: () => {
        // Per-workspace identity: switching workspaces means signing in to a
        // different workspace slug. No-op here until multi-workspace SSO lands.
      },
      login,
      logout,
    };
  }, [state, login, logout]);

  return <SessionContext.Provider value={session}>{children}</SessionContext.Provider>;
}
