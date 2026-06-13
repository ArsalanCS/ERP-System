import { useTranslation } from 'react-i18next';
import { PageHeader, EmptyState } from '@/shared/ui';

interface AdminPlaceholderProps {
  /** i18n key for the page title (under `nav.*`). */
  titleKey: string;
}

/**
 * Generic placeholder for admin sections not yet implemented. The seven
 * Identity pages replace these one vertical slice at a time (ROADMAP Phase 1).
 */
export function AdminPlaceholder({ titleKey }: AdminPlaceholderProps) {
  const { t } = useTranslation();
  return (
    <>
      <PageHeader title={t(titleKey)} />
      <EmptyState title={t('common.comingSoon')} body={t('common.comingSoonBody')} />
    </>
  );
}
