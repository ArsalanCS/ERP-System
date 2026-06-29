import { api } from '@/shared/api/client';
import type { ListParams, Paged } from '@/shared/api/types';
import type {
  MailTemplate,
  SendMailDetail,
  SendMailListItem,
  SendStatus,
  UpdateMailTemplateBody,
} from './types';

export interface OutboxParams extends ListParams {
  status?: SendStatus | undefined;
}

export const mailApi = {
  outbox: (params: OutboxParams) => api.get<Paged<SendMailListItem>>('/mail/outbox', params),
  get: (id: string) => api.get<SendMailDetail>(`/mail/outbox/${id}`),
  retry: (id: string) => api.post<void>(`/mail/outbox/${id}/retry`, {}),
  cancel: (id: string) => api.post<void>(`/mail/outbox/${id}/cancel`, {}),
  templates: () => api.get<MailTemplate[]>('/mail/templates'),
  updateTemplate: (id: string, body: UpdateMailTemplateBody) => api.put<void>(`/mail/templates/${id}`, body),
};

export const mailKeys = {
  outbox: (params: OutboxParams) => ['mail', 'outbox', params] as const,
  detail: (id: string) => ['mail', 'outbox', 'detail', id] as const,
  templates: ['mail', 'templates'] as const,
};
