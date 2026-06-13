/**
 * Centralized access to build-time environment configuration.
 * Never read import.meta.env directly elsewhere — go through here so the
 * surface is typed and easy to audit (CLAUDE.md §4.4: no secrets in the client).
 */
export const env = {
  /** Base URL for the API. Empty string => use the dev proxy / same-origin `/api`. */
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? '',
  isDev: import.meta.env.DEV,
  isProd: import.meta.env.PROD,
} as const;
