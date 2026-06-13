import { Suspense } from 'react';
import { Outlet, useMatches } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Sidebar } from '@/components/shell/Sidebar';
import { Topbar } from '@/components/shell/Topbar';
import { LoadingBlock } from '@/shared/ui';

interface RouteCrumbHandle {
  crumbKey?: string;
}

/**
 * Admin shell: persistent sidebar + topbar with a routed content area
 * (handoff app shell). Built once; every admin page nests inside it.
 */
export function AdminLayout() {
  const { t } = useTranslation();
  const matches = useMatches();
  const crumbKey = [...matches]
    .reverse()
    .map((m) => (m.handle as RouteCrumbHandle | undefined)?.crumbKey)
    .find(Boolean);

  return (
    <div className="grid min-h-screen grid-cols-[248px_1fr] max-[920px]:grid-cols-1">
      <div className="max-[920px]:hidden">
        <Sidebar />
      </div>
      <div className="flex min-w-0 flex-col bg-canvas">
        <Topbar crumb={crumbKey ? t(crumbKey) : undefined} />
        <main className="mx-auto w-full max-w-[1320px] flex-1 px-[30px] pb-16 pt-[26px]">
          <div className="fade-up">
            <Suspense fallback={<LoadingBlock label={t('common.loading')} />}>
              <Outlet />
            </Suspense>
          </div>
        </main>
      </div>
    </div>
  );
}
