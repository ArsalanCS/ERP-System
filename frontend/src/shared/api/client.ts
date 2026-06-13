import { env } from '@/shared/lib/env';
import { tokenStore } from './tokenStore';
import type { ApiErrorDetail, ApiErrorEnvelope, ListParams } from './types';

const API_PREFIX = '/api/v1';

/**
 * Typed error thrown for any non-2xx response. Carries the parsed envelope
 * fields so callers (and form layers) can map field-level details to inputs.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly correlationId?: string;
  readonly details?: ApiErrorDetail[];

  constructor(status: number, envelope: ApiErrorEnvelope | undefined, fallback: string) {
    const err = envelope?.error;
    super(err?.message ?? fallback);
    this.name = 'ApiError';
    this.status = status;
    this.code = err?.code ?? 'UNKNOWN';
    if (err?.correlationId !== undefined) this.correlationId = err.correlationId;
    if (err?.details !== undefined) this.details = err.details;
  }
}

interface RequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown;
  /** Query params for GET/list requests; undefined values are dropped. */
  params?: ListParams | undefined;
  /** Skip the access-token header + silent refresh (auth endpoints). */
  anonymous?: boolean;
}

function buildUrl(path: string, params?: ListParams): string {
  const base = `${env.apiBaseUrl}${API_PREFIX}${path}`;
  if (!params) return base;
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      search.append(key, String(value));
    }
  }
  const qs = search.toString();
  return qs ? `${base}?${qs}` : base;
}

// ---- Silent refresh ---------------------------------------------------------
// A single in-flight refresh is shared by all callers so a burst of 401s
// triggers exactly one rotation (avoids invalidating each other's tokens).
let refreshInFlight: Promise<boolean> | null = null;

async function doRefresh(): Promise<boolean> {
  const refreshToken = tokenStore.refresh;
  if (!refreshToken) return false;
  try {
    const res = await fetch(`${env.apiBaseUrl}${API_PREFIX}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify({ refreshToken }),
    });
    if (!res.ok) {
      tokenStore.clear();
      return false;
    }
    const data = (await res.json()) as { accessToken: string; refreshToken: string };
    tokenStore.set({ accessToken: data.accessToken, refreshToken: data.refreshToken });
    return true;
  } catch {
    return false;
  }
}

function refreshTokens(): Promise<boolean> {
  refreshInFlight ??= doRefresh().finally(() => {
    refreshInFlight = null;
  });
  return refreshInFlight;
}

async function send(method: string, path: string, options: RequestOptions): Promise<Response> {
  const { body, params, headers, anonymous, ...rest } = options;
  const authHeader =
    !anonymous && tokenStore.access ? { Authorization: `Bearer ${tokenStore.access}` } : {};
  return fetch(buildUrl(path, params), {
    method,
    headers: {
      Accept: 'application/json',
      ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
      ...authHeader,
      ...headers,
    },
    ...(body !== undefined ? { body: JSON.stringify(body) } : {}),
    ...rest,
  });
}

async function request<T>(method: string, path: string, options: RequestOptions = {}): Promise<T> {
  let res = await send(method, path, options);

  // Access token expired/revoked → try one silent refresh, then replay once.
  if (res.status === 401 && !options.anonymous && tokenStore.refresh) {
    if (await refreshTokens()) {
      res = await send(method, path, options);
    }
  }

  if (res.status === 204) {
    return undefined as T;
  }

  const isJson = res.headers.get('content-type')?.includes('application/json') ?? false;
  const payload = isJson ? await res.json().catch(() => undefined) : undefined;

  if (!res.ok) {
    throw new ApiError(res.status, payload as ApiErrorEnvelope | undefined, res.statusText);
  }

  return payload as T;
}

export const api = {
  get: <T>(path: string, params?: ListParams) => request<T>('GET', path, { params }),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, { body }),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, { body }),
  patch: <T>(path: string, body?: unknown) => request<T>('PATCH', path, { body }),
  delete: <T>(path: string) => request<T>('DELETE', path),
  /** Anonymous POST for auth endpoints (no bearer header, no silent refresh). */
  postAnon: <T>(path: string, body?: unknown) => request<T>('POST', path, { body, anonymous: true }),
};
