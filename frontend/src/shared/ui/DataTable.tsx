import type { ReactNode } from 'react';
import { cn } from '@/shared/lib/cn';
import { LoadingBlock } from './Spinner';

export interface Column<T> {
  key: string;
  header: ReactNode;
  render: (row: T) => ReactNode;
  align?: 'start' | 'center' | 'end';
  /** Extra classes for the cell (e.g. width, whitespace). */
  cellClassName?: string;
  headClassName?: string;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  loading?: boolean;
  loadingLabel?: string;
  /** Rendered (full-width) when there are no rows and not loading. */
  empty?: ReactNode;
  onRowClick?: (row: T) => void;
}

const ALIGN: Record<NonNullable<Column<unknown>['align']>, string> = {
  start: 'text-start',
  center: 'text-center',
  end: 'text-end',
};

/** Card-wrapped table with header, loading, and empty states (handoff table style). */
export function DataTable<T>({
  columns,
  rows,
  rowKey,
  loading = false,
  loadingLabel,
  empty,
  onRowClick,
}: DataTableProps<T>) {
  return (
    <div className="card overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="border-b border-stone-150 bg-stone-50/60">
              {columns.map((col) => (
                <th
                  key={col.key}
                  className={cn(
                    'px-4 py-3 text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4',
                    ALIGN[col.align ?? 'start'],
                    col.headClassName,
                  )}
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={columns.length}>
                  <LoadingBlock label={loadingLabel} />
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="p-0">
                  {empty}
                </td>
              </tr>
            ) : (
              rows.map((row) => (
                <tr
                  key={rowKey(row)}
                  onClick={onRowClick ? () => onRowClick(row) : undefined}
                  className={cn(
                    'border-b border-stone-100 last:border-0 transition-colors',
                    onRowClick && 'cursor-pointer hover:bg-stone-50',
                  )}
                >
                  {columns.map((col) => (
                    <td
                      key={col.key}
                      className={cn('px-4 py-3 align-middle text-ink-2', ALIGN[col.align ?? 'start'], col.cellClassName)}
                    >
                      {col.render(row)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
