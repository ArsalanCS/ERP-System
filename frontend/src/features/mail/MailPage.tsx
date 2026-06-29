import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  PageHeader,
  Button,
  Select,
  Badge,
  Drawer,
  DataTable,
  Pagination,
  EmptyState,
  Spinner,
  type BadgeTone,
  type Column,
} from '@/shared/ui';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { useToast } from '@/shared/ui/toast-context';
import { useDirection } from '@/shared/hooks/useDirection';
import { formatDateTime } from '@/shared/lib/format';
import { mailApi, mailKeys, type OutboxParams } from './api';
import { SendStatus, type MailTemplate, type SendMailListItem, type UpdateMailTemplateBody } from './types';

const PAGE_SIZE = 25;

const STATUS_TONE: Record<SendStatus, BadgeTone> = {
  [SendStatus.Pending]: 'amber',
  [SendStatus.Processing]: 'blue',
  [SendStatus.Sent]: 'green',
  [SendStatus.Failed]: 'red',
  [SendStatus.Cancelled]: 'neutral',
};

type Tab = 'outbox' | 'templates';

export function MailPage() {
  const { t } = useTranslation();
  const { can } = usePermissions();
  const [tab, setTab] = useState<Tab>('outbox');
  const canManage = can(Actions.MailManage);

  const tabs: { id: Tab; label: string }[] = [
    { id: 'outbox', label: t('mail.tabs.outbox') },
    { id: 'templates', label: t('mail.tabs.templates') },
  ];

  return (
    <>
      <PageHeader title={t('mail.title')} subtitle={t('mail.subtitle')} />
      <div className="mb-4 flex items-center gap-1 border-b border-stone-150">
        {tabs.map((x) => (
          <button key={x.id} type="button" onClick={() => setTab(x.id)}
            className={`-mb-px border-b-2 px-3.5 py-2 text-[13.5px] font-medium transition-colors ${
              tab === x.id ? 'border-clay text-ink' : 'border-transparent text-ink-4 hover:text-ink-2'}`}>
            {x.label}
          </button>
        ))}
      </div>
      {tab === 'outbox' ? <OutboxTab canManage={canManage} /> : <TemplatesTab canManage={canManage} />}
    </>
  );
}

function OutboxTab({ canManage }: { canManage: boolean }) {
  const { t } = useTranslation();
  const { locale } = useDirection();
  const toast = useToast();
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<string>('');
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const params = useMemo<OutboxParams>(
    () => ({ page, pageSize: PAGE_SIZE, status: status === '' ? undefined : (Number(status) as SendStatus) }),
    [page, status],
  );
  const query = useQuery({ queryKey: mailKeys.outbox(params), queryFn: () => mailApi.outbox(params) });
  const detail = useQuery({
    queryKey: mailKeys.detail(selectedId ?? ''),
    queryFn: () => mailApi.get(selectedId!),
    enabled: !!selectedId,
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['mail', 'outbox'] });
    if (selectedId) void queryClient.invalidateQueries({ queryKey: mailKeys.detail(selectedId) });
  };

  const retry = useMutation({
    mutationFn: (id: string) => mailApi.retry(id),
    onSuccess: () => { invalidate(); toast.success(t('mail.feedback.requeued')); },
    onError: (e) => toast.error(e instanceof Error ? e.message : t('common.loadError')),
  });
  const cancel = useMutation({
    mutationFn: (id: string) => mailApi.cancel(id),
    onSuccess: () => { invalidate(); toast.success(t('mail.feedback.cancelled')); },
    onError: (e) => toast.error(e instanceof Error ? e.message : t('common.loadError')),
  });

  const statusOptions = [
    { value: '', label: t('mail.filters.allStatuses') },
    ...[SendStatus.Pending, SendStatus.Processing, SendStatus.Sent, SendStatus.Failed, SendStatus.Cancelled].map((s) => ({
      value: String(s),
      label: t(`mail.status.${s}`),
    })),
  ];

  const columns: Column<SendMailListItem>[] = [
    {
      key: 'subject',
      header: t('mail.columns.subject'),
      render: (r) => <span className="font-medium text-ink-2">{r.subject}</span>,
    },
    {
      key: 'status',
      header: t('mail.columns.status'),
      render: (r) => <Badge tone={STATUS_TONE[r.status]} dot>{t(`mail.status.${r.status}`)}</Badge>,
    },
    { key: 'recipients', header: t('mail.columns.recipients'), render: (r) => r.recipientCount },
    { key: 'attempts', header: t('mail.columns.attempts'), render: (r) => `${r.retryCount}` },
    {
      key: 'created',
      header: t('mail.columns.created'),
      cellClassName: 'whitespace-nowrap',
      render: (r) => <span className="text-[13px] text-ink-3">{formatDateTime(r.createdAt, locale)}</span>,
    },
  ];

  const data = query.data;
  const d = detail.data;

  return (
    <>
      <div className="mb-4 flex flex-wrap items-end gap-3">
        <div className="w-[180px]">
          <Select options={statusOptions} value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }} />
        </div>
      </div>

      {query.isError ? (
        <EmptyState icon="alert" title={t('common.loadError')}
          action={<Button variant="outline" size="sm" onClick={() => query.refetch()}>{t('common.retry')}</Button>} />
      ) : (
        <>
          <DataTable
            columns={columns}
            rows={data?.items ?? []}
            rowKey={(r) => r.id}
            loading={query.isLoading}
            loadingLabel={t('common.loading')}
            onRowClick={(r) => setSelectedId(r.id)}
            empty={<EmptyState icon="mail" title={t('mail.empty.title')} body={t('mail.empty.body')} />}
          />
          {data && data.total > PAGE_SIZE && (
            <Pagination page={data.page} pageSize={data.pageSize} total={data.total} onPageChange={setPage} />
          )}
        </>
      )}

      <Drawer
        open={!!selectedId}
        onClose={() => setSelectedId(null)}
        title={d?.subject ?? t('mail.detail.title')}
        subtitle={d ? t(`mail.status.${d.status}`) : undefined}
        width={520}
        footer={
          canManage && d ? (
            <>
              {(d.status === SendStatus.Failed || d.status === SendStatus.Cancelled) && (
                <Button size="sm" onClick={() => retry.mutate(d.id)} disabled={retry.isPending}>{t('mail.actions.retry')}</Button>
              )}
              {(d.status === SendStatus.Pending || d.status === SendStatus.Failed) && (
                <Button size="sm" variant="outline" onClick={() => cancel.mutate(d.id)} disabled={cancel.isPending}>{t('mail.actions.cancel')}</Button>
              )}
            </>
          ) : undefined
        }
      >
        {detail.isLoading || !d ? (
          <div className="flex justify-center py-8"><Spinner size={22} /></div>
        ) : (
          <div className="flex flex-col gap-4 text-[13.5px]">
            <section>
              <h3 className="mb-1 text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{t('mail.detail.recipients')}</h3>
              <ul className="flex flex-col gap-1">
                {d.recipients.map((r) => (
                  <li key={r.address} className="text-ink-2">{r.displayName ? `${r.displayName} · ` : ''}{r.address}</li>
                ))}
              </ul>
            </section>
            <section>
              <h3 className="mb-1 text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{t('mail.detail.body')}</h3>
              <div className="rounded-md border border-stone-150 p-3 text-ink-2" dangerouslySetInnerHTML={{ __html: d.bodyHtml }} />
            </section>
            {d.lastError && (
              <section>
                <h3 className="mb-1 text-[12px] font-semibold uppercase tracking-[0.04em] text-red">{t('mail.detail.lastError')}</h3>
                <p className="whitespace-pre-wrap text-[12.5px] text-ink-3">{d.lastError}</p>
              </section>
            )}
            <section>
              <h3 className="mb-1 text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">
                {t('mail.detail.attempts')} ({d.retryCount}/{d.maxRetries})
              </h3>
              {d.attempts.length === 0 ? (
                <p className="text-ink-4">{t('mail.detail.noAttempts')}</p>
              ) : (
                <ul className="flex flex-col gap-1.5">
                  {d.attempts.map((a) => (
                    <li key={a.id} className="flex items-center gap-2">
                      <Badge tone={a.success ? 'green' : 'red'} dot>{a.success ? t('mail.attempt.ok') : t('mail.attempt.fail')}</Badge>
                      <span className="text-[12.5px] text-ink-4">{formatDateTime(a.attemptedAt, locale)}</span>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </div>
        )}
      </Drawer>
    </>
  );
}

function TemplatesTab({ canManage }: { canManage: boolean }) {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<MailTemplate | null>(null);
  const [form, setForm] = useState<UpdateMailTemplateBody>({
    name: '', subjectTemplate: '', bodyHtmlTemplate: '', bodyTextTemplate: '', isActive: true,
  });

  const query = useQuery({ queryKey: mailKeys.templates, queryFn: () => mailApi.templates() });

  const open = (tpl: MailTemplate) => {
    setEditing(tpl);
    setForm({
      name: tpl.name,
      subjectTemplate: tpl.subjectTemplate,
      bodyHtmlTemplate: tpl.bodyHtmlTemplate,
      bodyTextTemplate: tpl.bodyTextTemplate ?? '',
      isActive: tpl.isActive,
    });
  };

  const save = useMutation({
    mutationFn: () => mailApi.updateTemplate(editing!.id, form),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: mailKeys.templates });
      setEditing(null);
      toast.success(t('mail.feedback.templateSaved'));
    },
    onError: (e) => toast.error(e instanceof Error ? e.message : t('common.loadError')),
  });

  const templates = query.data ?? [];

  return (
    <>
      {query.isLoading ? (
        <div className="flex justify-center py-8"><Spinner size={22} /></div>
      ) : (
        <ul className="flex flex-col gap-2.5">
          {templates.map((tpl) => (
            <li key={tpl.id} className="flex items-center gap-3 rounded-md border border-stone-150 px-4 py-3">
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span className="font-medium text-ink-2">{tpl.name}</span>
                  {tpl.isGlobal && <Badge tone="blue">{t('mail.templates.global')}</Badge>}
                  {!tpl.isActive && <Badge tone="neutral">{t('mail.templates.inactive')}</Badge>}
                </div>
                <div className="truncate text-[12.5px] text-ink-4">{tpl.subjectTemplate}</div>
              </div>
              {canManage && (
                <Button size="sm" variant="outline" onClick={() => open(tpl)}>{t('common.edit')}</Button>
              )}
            </li>
          ))}
        </ul>
      )}

      <Drawer
        open={!!editing}
        onClose={() => setEditing(null)}
        title={editing?.name ?? ''}
        subtitle={t('mail.templates.editHint')}
        width={560}
        footer={
          <>
            <Button variant="ghost" onClick={() => setEditing(null)}>{t('common.cancel')}</Button>
            <Button onClick={() => save.mutate()} disabled={save.isPending || !form.subjectTemplate.trim()}>{t('common.save')}</Button>
          </>
        }
      >
        <div className="flex flex-col gap-4">
          {editing?.isGlobal && (
            <p className="rounded-md border border-blue/20 bg-blue/5 px-3 py-2 text-[12px] text-ink-3">{t('mail.templates.overrideHint')}</p>
          )}
          <label className="flex flex-col gap-1 text-[12.5px] text-ink-3">
            {t('mail.templates.name')}
            <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </label>
          <label className="flex flex-col gap-1 text-[12.5px] text-ink-3">
            {t('mail.templates.subject')}
            <input className="input" value={form.subjectTemplate} onChange={(e) => setForm({ ...form, subjectTemplate: e.target.value })} />
          </label>
          <label className="flex flex-col gap-1 text-[12.5px] text-ink-3">
            {t('mail.templates.body')}
            <textarea className="input min-h-[160px] resize-y font-mono text-[12.5px]" value={form.bodyHtmlTemplate}
              onChange={(e) => setForm({ ...form, bodyHtmlTemplate: e.target.value })} />
          </label>
          <label className="flex flex-col gap-1 text-[12.5px] text-ink-3">
            {t('mail.templates.bodyText')}
            <textarea className="input min-h-[100px] resize-y font-mono text-[12.5px]" value={form.bodyTextTemplate ?? ''}
              onChange={(e) => setForm({ ...form, bodyTextTemplate: e.target.value })} />
          </label>
          <label className="flex cursor-pointer items-center gap-2 text-[13px] text-ink-2">
            <input type="checkbox" className="h-4 w-4 accent-clay" checked={form.isActive}
              onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
            {t('mail.templates.active')}
          </label>
          <p className="text-[12px] text-ink-4">{t('mail.templates.placeholdersHint')}</p>
        </div>
      </Drawer>
    </>
  );
}
