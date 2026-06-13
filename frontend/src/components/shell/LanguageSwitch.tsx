import { useTranslation } from 'react-i18next';
import { useDirection } from '@/shared/hooks/useDirection';
import { Icon } from '@/shared/ui/Icon';

/** Compact AR/EN toggle used in the topbar. Switching flips document direction. */
export function LanguageSwitch() {
  const { t } = useTranslation();
  const { locale, toggleLocale } = useDirection();
  return (
    <button
      type="button"
      onClick={toggleLocale}
      title={t('language.label')}
      className="flex h-[38px] items-center gap-1.5 rounded-sm px-2.5 text-[13px] font-semibold text-ink-3 transition-colors hover:bg-stone-100 hover:text-ink"
    >
      <Icon name="languages" size={18} />
      <span>{locale === 'ar' ? t('language.en') : t('language.ar')}</span>
    </button>
  );
}
