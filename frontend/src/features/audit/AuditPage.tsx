import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  PageHeader,
  Button,
  Input,
  Select,
  Icon,
  Badge,
  DataTable,
  Pagination,
  EmptyState,
  type BadgeTone,
  type Column,
} from '@/shared/ui';
import { Can } from '@/shared/rbac/Can';
import { Actions } from '@/shared/rbac/permissions';
import { useToast } from '@/shared/ui/toast-context';
import { useDirection } from '@/shared/hooks/useDirection';
import { formatDateTime } from '@/shared/lib/format';
import { auditApi, auditKeys, AuditResult, type AuditLogDto, type AuditSearchParams } from './api';

const PAGE_SIZE = 25;

const RESULT_TONE: Record<AuditResult, BadgeTone> = {
  [AuditResult.Success]: 'green',
  [AuditResult.Denied]: 'amber',
  [AuditResult.Failed]: 'red',
};

function toCsv(rows: AuditLogDto[]): string {
  const header = ['Time', 'Actor', 'Action', 'Module', 'ResourceType', 'ResourceId', 'Result', 'IP', 'CorrelationId'];
  const escape = (v: string) => `"${v.replace(/"/g, '""')}"`;
  const lines = rows.map((r) =>
    [
      r.occurredAt,
      r.actorDisplayName ?? '',
      r.action,
      r.module,
      r.resourceType,
      r.resourceId ?? '',
      AuditResult[r.result],
      r.ipAddress ?? '',
      r.correlationId,
    ]
      .map((v) => escape(String(v)))
      .join(','),
  );
  return [header.join(','), ...lines].join('\n');
}

export function AuditPage() {
  const { t } = useTranslation();
  const { locale } = useDirection();
  const toast = useToast();

  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [result, setResult] = useState<string>('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [exporting, setExporting] = useState(false);

  useEffect(() => {
    const id = window.setTimeout(() => {
      setSearch(searchInput.trim());
      setPage(1);
    }, 350);
    return () => window.clearTimeout(id);
  }, [searchInput]);

  const params = useMemo<AuditSearchParams>(
    () => ({
      page,
      pageSize: PAGE_SIZE,
      search: search || undefined,
      result: result === '' ? undefined : (Number(result) as AuditResult),
      from: from ? new Date(from).toISOString() : undefined,
      to: to ? new Date(to).toISOString() : undefined,
    }),
    [page, search, result, from, to],
  );

  const query = useQuery({ queryKey: auditKeys.list(params), queryFn: () => auditApi.search(params) });

  const handleExport = async () => {
    setExporting(true);
    try {
      const data = await auditApi.export({ ...params, page: 1, pageSize: 1000 });
      const blob = new Blob([toCsv(data.items)], { type: 'text/csv;charset=utf-8;' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `audit-${new Date().toISOString().slice(0, 10)}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      toast.error(t('common.loadError'));
    } finally {
      setExporting(false);
    }
  };

  const resultOptions = [
    { value: '', label: t('audit.filters.allResults') },
    ...[AuditResult.Success, AuditResult.Denied, AuditResult.Failed].map((r) => ({
      value: String(r),
      label: t(`audit.result.${r}`),
    })),
  ];

  const columns: Column<AuditLogDto>[] = [
    {
      key: 'time',
      header: t('audit.columns.time'),
      cellClassName: 'whitespace-nowrap',
      render: (r) => <span className="text-[13px] text-ink-3">{formatDateTime(r.occurredAt, locale)}</span>,
    },
    {
      key: 'actor',
      header: t('audit.columns.actor'),
      render: (r) => (
        <span className="font-medium text-ink-2">{r.actorDisplayName ?? t('audit.system')}</span>
      ),
    },
    {
      key: 'action',
      header: t('audit.columns.action'),
      render: (r) => <span className="font-mono text-[12.5px] text-ink-2">{r.action}</span>,
    },
    { key: 'module', header: t('audit.columns.module'), render: (r) => r.module },
    {
      key: 'resource',
      header: t('audit.columns.resource'),
      render: (r) => (
        <span className="text-[13px] text-ink-3">
          {r.resourceType}
          {r.resourceId ? ` · ${r.resourceId.slice(0, 8)}` : ''}
        </span>
      ),
    },
    {
      key: 'result',
      header: t('audit.columns.result'),
      render: (r) => (
        <Badge tone={RESULT_TONE[r.result]} dot>
          {t(`audit.result.${r.result}`)}
        </Badge>
      ),
    },
    {
      key: 'ip',
      header: t('audit.columns.ip'),
      render: (r) => <span className="text-[12.5px] text-ink-4">{r.ipAddress ?? '—'}</span>,
    },
  ];

  const data = query.data;

  return (
    <>
      <PageHeader
        title={t('audit.title')}
        subtitle={t('audit.subtitle')}
        actions={
          <Can action={Actions.AuditExport}>
            <Button
              variant="outline"
              leadingIcon={<Icon name="download" size={16} />}
              onClick={handleExport}
              disabled={exporting || !data || data.total === 0}
            >
              {t('audit.export')}
            </Button>
          </Can>
        }
      />

      <div className="mb-4 flex flex-wrap items-end gap-3">
        <div className="min-w-[220px] flex-1">
          <Input
            placeholder={t('audit.searchPlaceholder')}
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
          />
        </div>
        <div className="w-[160px]">
          <Select
            options={resultOptions}
            value={result}
            onChange={(e) => {
              setResult(e.target.value);
              setPage(1);
            }}
          />
        </div>
        <div className="w-[150px]">
          <Input
            type="date"
            aria-label={t('audit.filters.from')}
            value={from}
            onChange={(e) => {
              setFrom(e.target.value);
              setPage(1);
            }}
          />
        </div>
        <div className="w-[150px]">
          <Input
            type="date"
            aria-label={t('audit.filters.to')}
            value={to}
            onChange={(e) => {
              setTo(e.target.value);
              setPage(1);
            }}
          />
        </div>
      </div>

      {query.isError ? (
        <EmptyState
          icon="alert"
          title={t('common.loadError')}
          action={
            <Button variant="outline" size="sm" onClick={() => query.refetch()}>
              {t('common.retry')}
            </Button>
          }
        />
      ) : (
        <>
          <DataTable
            columns={columns}
            rows={data?.items ?? []}
            rowKey={(r) => r.id}
            loading={query.isLoading}
            loadingLabel={t('common.loading')}
            empty={<EmptyState icon="history" title={t('audit.empty.title')} body={t('audit.empty.body')} />}
          />
          {data && data.total > PAGE_SIZE && (
            <Pagination page={data.page} pageSize={data.pageSize} total={data.total} onPageChange={setPage} />
          )}
        </>
      )}
    </>
  );
}
