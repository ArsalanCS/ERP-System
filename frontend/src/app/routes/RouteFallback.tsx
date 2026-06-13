import { useTranslation } from 'react-i18next';

/** Centered loading state used while the session hydrates. */
export function RouteFallback() {
  const { t } = useTranslation();
  return (
    <div className="flex min-h-screen items-center justify-center bg-canvas">
      <div className="flex flex-col items-center gap-3 text-ink-4">
        <span className="h-7 w-7 animate-spin rounded-full border-2 border-stone-200 border-t-clay" />
        <span className="text-sm font-medium">{t('common.loading')}</span>
      </div>
    </div>
  );
}
