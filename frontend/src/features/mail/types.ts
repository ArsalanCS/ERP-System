// Mirrors the backend Mail outbox DTOs. Enums serialize as numbers; order MUST match
// Erp.Domain.Mail.

export enum SendStatus {
  Pending = 0,
  Processing = 1,
  Sent = 2,
  Failed = 3,
  Cancelled = 4,
}

export enum MailRecipientKind {
  To = 0,
  Cc = 1,
  Bcc = 2,
}

export interface SendMailListItem {
  id: string;
  subject: string;
  status: SendStatus;
  templateCode: string | null;
  recipientCount: number;
  retryCount: number;
  scheduledAt: string;
  sentAt: string | null;
  nextAttemptAt: string | null;
  lastError: string | null;
  createdAt: string;
}

export interface SendMailRecipient {
  address: string;
  displayName: string | null;
  kind: MailRecipientKind;
}

export interface SendMailAttempt {
  id: string;
  attemptNo: number;
  success: boolean;
  providerResponse: string | null;
  errorMessage: string | null;
  attemptedAt: string;
}

export interface SendMailDetail {
  id: string;
  subject: string;
  bodyHtml: string;
  bodyText: string | null;
  templateDataJson: string | null;
  status: SendStatus;
  templateCode: string | null;
  retryCount: number;
  maxRetries: number;
  scheduledAt: string;
  sentAt: string | null;
  nextAttemptAt: string | null;
  lastError: string | null;
  createdAt: string;
  recipients: SendMailRecipient[];
  attempts: SendMailAttempt[];
}

export interface MailTemplate {
  id: string;
  code: string;
  name: string;
  subjectTemplate: string;
  bodyHtmlTemplate: string;
  bodyTextTemplate: string | null;
  isActive: boolean;
  isGlobal: boolean;
  updatedAt: string | null;
}

export interface UpdateMailTemplateBody {
  name: string;
  subjectTemplate: string;
  bodyHtmlTemplate: string;
  bodyTextTemplate: string | null;
  isActive: boolean;
}
