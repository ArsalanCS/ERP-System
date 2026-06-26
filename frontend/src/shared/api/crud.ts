import { api } from './client';
import type { ListParams, Paged } from './types';

/** Default shape of a create response (server returns the new id). */
export interface CreateResult {
  id: string;
}

/**
 * Builds the standard list/get/create/update/archive methods for a module so
 * feature `api.ts` files don't repeat raw URLs and request logic
 * (Refactor Guide §10.3). Modules add their own extra methods alongside the
 * spread of this factory. `TCreateResult` defaults to `{ id }` but can be
 * overridden when a create returns more (e.g. a generated code/number).
 */
export function createCrudApi<TList, TDetail, TCreate, TUpdate, TCreateResult = CreateResult>(
  basePath: string,
) {
  return {
    list: (params?: ListParams) => api.get<Paged<TList>>(basePath, params),
    get: (id: string) => api.get<TDetail>(`${basePath}/${id}`),
    create: (body: TCreate) => api.post<TCreateResult>(basePath, body),
    update: (id: string, body: TUpdate) => api.put<void>(`${basePath}/${id}`, body),
    archive: (id: string) => api.delete<void>(`${basePath}/${id}`),
  };
}
