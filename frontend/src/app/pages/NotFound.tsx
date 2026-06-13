import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/ui';

export function NotFound() {
  const { t } = useTranslation();
  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-canvas px-6 text-center">
      <p className="text-[64px] font-bold leading-none tracking-[-0.03em] text-clay">404</p>
      <h1 className="mt-4 text-[22px] font-semibold">{t('errors.notFoundTitle')}</h1>
      <p className="mt-2 max-w-sm text-sm text-ink-3">{t('errors.notFoundBody')}</p>
      <Link to="/admin/overview" className="mt-6">
        <Button variant="outline">{t('errors.backToOverview')}</Button>
      </Link>
    </div>
  );
}
