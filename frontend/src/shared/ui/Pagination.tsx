import { useTranslation } from 'react-i18next';
import { Icon } from './Icon';
import { Button } from './Button';

interface PaginationProps {
  page: number;
  pageSize: number;
  total: number;
  onPageChange: (page: number) => void;
}

/** Page navigator with a localized "showing X–Y of N" summary. */
export function Pagination({ page, pageSize, total, onPageChange }: PaginationProps) {
  const { t } = useTranslation();
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const from = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);

  return (
    <div className="mt-3 flex items-center justify-between gap-3 px-1 text-[13px] text-ink-3">
      <span>{t('common.pagination.summary', { from, to, total })}</span>
      <div className="flex items-center gap-1.5">
        <Button
          variant="outline"
          size="sm"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
          leadingIcon={<Icon name="chevron-left" size={15} className="rtl:-scale-x-100" />}
        >
          {t('common.pagination.prev')}
        </Button>
        <span className="px-1.5 font-medium text-ink-2">
          {t('common.pagination.page', { page, totalPages })}
        </span>
        <Button
          variant="outline"
          size="sm"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          {t('common.pagination.next')}
          <Icon name="chevron-right" size={15} className="rtl:-scale-x-100" />
        </Button>
      </div>
    </div>
  );
}
