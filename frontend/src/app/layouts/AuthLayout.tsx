import { Outlet } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Icon } from '@/shared/ui/Icon';
import { LanguageSwitch } from '@/components/shell/LanguageSwitch';

/** Centered card shell for the unauthenticated auth pages (login/forgot/reset). */
export function AuthLayout() {
  const { t } = useTranslation();
  return (
    <div className="flex min-h-screen flex-col bg-canvas">
      <header className="flex items-center justify-between px-6 py-5">
        <div className="flex items-center gap-2.5">
          <span className="logo-mark">
            <Icon name="bolt" size={18} />
          </span>
          <span className="logo">{t('app.brand')}</span>
        </div>
        <LanguageSwitch />
      </header>

      <main className="flex flex-1 items-center justify-center px-4 pb-16">
        <div className="w-full max-w-[400px]">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
