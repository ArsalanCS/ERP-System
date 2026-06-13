/**
 * Holds the JWT access + refresh tokens for the SPA.
 *
 * The backend issues bearer tokens in the login/refresh response body
 * (Identity spec §13.1), so the SPA is responsible for storing them and
 * attaching the access token to every request. Tokens are kept in
 * localStorage so a page reload can re-hydrate the session; the access token
 * is short-lived and rotated, and the refresh token is single-use with
 * server-side rotation + theft detection, which bounds the XSS exposure.
 *
 * Nothing else in the app reads localStorage for auth — go through here.
 */
const ACCESS_KEY = 'erp.auth.access';
const REFRESH_KEY = 'erp.auth.refresh';
/** Workspace slug the user logged in with — kept only for switcher display. */
const SLUG_KEY = 'erp.auth.ws';

type Listener = () => void;
const listeners = new Set<Listener>();

let accessToken = safeGet(ACCESS_KEY);
let refreshToken = safeGet(REFRESH_KEY);
let workspaceSlug = safeGet(SLUG_KEY);

function safeGet(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function emit() {
  for (const listener of listeners) listener();
}

export interface StoredTokens {
  accessToken: string;
  refreshToken: string;
}

export const tokenStore = {
  get access() {
    return accessToken;
  },
  get refresh() {
    return refreshToken;
  },
  get slug() {
    return workspaceSlug;
  },
  hasSession() {
    return refreshToken !== null;
  },
  set(tokens: StoredTokens, slug?: string) {
    accessToken = tokens.accessToken;
    refreshToken = tokens.refreshToken;
    localStorage.setItem(ACCESS_KEY, accessToken);
    localStorage.setItem(REFRESH_KEY, refreshToken);
    if (slug !== undefined) {
      workspaceSlug = slug;
      localStorage.setItem(SLUG_KEY, slug);
    }
    emit();
  },
  clear() {
    accessToken = null;
    refreshToken = null;
    workspaceSlug = null;
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(SLUG_KEY);
    emit();
  },
  /** Notified on any token change (set/clear), including forced logout on refresh failure. */
  subscribe(listener: Listener) {
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  },
};
