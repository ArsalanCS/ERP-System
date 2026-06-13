import { useEffect, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Icon } from './Icon';
import { cn } from '@/shared/lib/cn';

interface DrawerProps {
  open: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string | undefined;
  /** Sticky footer (actions). */
  footer?: ReactNode;
  /** Drawer width in px (default 460). */
  width?: number;
  children: ReactNode;
}

/**
 * Right-aligned slide-over panel (handoff full-screen drawer). Mirrors layout
 * direction via logical positioning so it docks on the inline-end edge in RTL.
 */
export function Drawer({ open, onClose, title, subtitle, footer, width = 460, children }: DrawerProps) {
  const { t } = useTranslation();

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', onKey);
    const prevOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = prevOverflow;
    };
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-40 flex justify-end">
      <button
        type="button"
        aria-label={t('common.close')}
        onClick={onClose}
        className="absolute inset-0 bg-stone-900/30 backdrop-blur-[1px]"
      />
      <aside
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className={cn(
          'relative flex h-full w-full flex-col bg-paper shadow-xl',
          'animate-[drawerIn_.18s_ease-out]',
        )}
        style={{ maxWidth: width }}
      >
        <header className="flex items-start justify-between gap-3 border-b border-stone-150 px-5 py-4">
          <div className="min-w-0">
            <h2 className="truncate text-[16px] font-semibold tracking-[-0.01em]">{title}</h2>
            {subtitle && <p className="mt-0.5 truncate text-[12.5px] text-ink-4">{subtitle}</p>}
          </div>
          <button
            type="button"
            onClick={onClose}
            title={t('common.close')}
            className="-me-1.5 flex h-8 w-8 flex-none items-center justify-center rounded-sm text-ink-4 hover:bg-stone-100 hover:text-ink"
          >
            <Icon name="close" size={18} />
          </button>
        </header>

        <div className="min-h-0 flex-1 overflow-y-auto px-5 py-5">{children}</div>

        {footer && (
          <footer className="flex flex-none items-center justify-end gap-2.5 border-t border-stone-150 px-5 py-3.5">
            {footer}
          </footer>
        )}
      </aside>
    </div>
  );
}
