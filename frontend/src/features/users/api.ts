import { api } from '@/shared/api/client';
import type { ListParams } from '@/shared/api/types';
import type {
  CreateUserRequest,
  CreateUserResult,
  UpdateUserRequest,
  UserDetail,
  UserList,
  UserStatus,
} from './types';

export interface UserListParams extends ListParams {
  status?: UserStatus | undefined;
}

export const usersApi = {
  list: (params: UserListParams) => api.get<UserList>('/users', params),
  get: (id: string) => api.get<UserDetail>(`/users/${id}`),
  create: (body: CreateUserRequest) => api.post<CreateUserResult>('/users', body),
  update: (id: string, body: UpdateUserRequest) => api.put<void>(`/users/${id}`, body),
  suspend: (id: string, reason: string) => api.post<void>(`/users/${id}/suspend`, { reason }),
  reactivate: (id: string) => api.post<void>(`/users/${id}/reactivate`),
  archive: (id: string) => api.delete<void>(`/users/${id}`),
};

/** TanStack Query keys for the users feature. */
export const userKeys = {
  all: ['users'] as const,
  list: (params: UserListParams) => ['users', 'list', params] as const,
  detail: (id: string) => ['users', 'detail', id] as const,
};
