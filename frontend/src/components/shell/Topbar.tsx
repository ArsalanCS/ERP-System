import { useTranslation } from 'react-i18next';
import { Icon } from '@/shared/ui/Icon';
import { Button } from '@/shared/ui/Button';
import { LanguageSwitch } from './LanguageSwitch';

interface TopbarProps {
  /** Breadcrumb label for the current page (handoff `data-crumb`). */
  crumb?: string | undefined;
}

/** Top bar (handoff `.topbar`, 62px, blurred translucent). */
export function Topbar({ crumb }: TopbarProps) {
  const { t } = useTranslation();
  return (
    <header className="sticky top-0 z-20 flex h-[62px] flex-none items-center gap-4 border-b border-stone-200 bg-[rgba(255,255,255,0.82)] px-[26px] backdrop-blur-[10px]">
      <div className="flex items-center gap-1.5 text-[13px] font-medium text-ink-4">
        <Icon name="home" size={16} />
        {crumb && (
          <>
            <Icon name="chevron-right" size={14} className="rtl:-scale-x-100" />
            <span className="text-ink-2">{crumb}</span>
          </>
        )}
      </div>

      {/* center search */}
      <label className="mx-auto flex w-full max-w-[440px] cursor-text items-center gap-2.5 rounded-md border border-transparent bg-stone-100 px-[13px] py-2 text-ink-4 transition focus-within:border-clay focus-within:bg-paper">
        <Icon name="search" size={17} />
        <input
          type="search"
          placeholder={t('topbar.searchPlaceholder')}
          className="min-w-0 flex-1 border-none bg-transparent text-sm text-ink outline-none placeholder:text-ink-4"
        />
        <kbd className="rounded-[5px] border border-stone-200 bg-paper px-1.5 py-0.5 text-[11px] font-semibold text-ink-4">
          ⌘K
        </kbd>
      </label>

      <div className="flex items-center gap-1">
        <LanguageSwitch />
        <Button size="sm" leadingIcon={<Icon name="plus" size={16} />}>
          {t('topbar.newAction')}
        </Button>
        <button
          type="button"
          title={t('topbar.notifications')}
          className="relative flex h-[38px] w-[38px] items-center justify-center rounded-sm text-ink-3 hover:bg-stone-100"
        >
          <Icon name="bell" size={19} />
          <span className="absolute right-2.5 top-2 h-[7px] w-[7px] rounded-full border-2 border-paper bg-clay" />
        </button>
        <button
          type="button"
          title={t('topbar.help')}
          className="flex h-[38px] w-[38px] items-center justify-center rounded-sm text-ink-3 hover:bg-stone-100"
        >
          <Icon name="help" size={19} />
        </button>
      </div>
    </header>
  );
}
