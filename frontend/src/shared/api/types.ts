/**
 * Shared API contracts. The error envelope shape mirrors the backend
 * standard envelope defined in docs/CONVENTIONS.md (same shape for 4xx/5xx).
 */

export interface ApiErrorDetail {
  field: string;
  message: string;
}

export interface ApiErrorEnvelope {
  error: {
    code: string;
    message: string;
    correlationId?: string;
    details?: ApiErrorDetail[];
  };
}

/** Standard server-side paginated list response. */
export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface ListParams {
  page?: number | undefined;
  pageSize?: number | undefined;
  sort?: string | undefined;
  search?: string | undefined;
  [filter: string]: string | number | boolean | undefined;
}
