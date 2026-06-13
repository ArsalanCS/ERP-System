import { useTranslation } from 'react-i18next';
import { directionFor, isLocale, DEFAULT_LOCALE, type Locale } from '@/shared/i18n/config';

export interface DirectionState {
  locale: Locale;
  dir: 'rtl' | 'ltr';
  isRtl: boolean;
  setLocale: (locale: Locale) => void;
  toggleLocale: () => void;
}

/** Resolves the active locale + text direction and exposes locale switchers. */
export function useDirection(): DirectionState {
  const { i18n } = useTranslation();
  const resolved = i18n.resolvedLanguage ?? i18n.language;
  const locale: Locale = isLocale(resolved) ? resolved : DEFAULT_LOCALE;
  const dir = directionFor(locale);

  const setLocale = (next: Locale) => {
    void i18n.changeLanguage(next);
  };

  return {
    locale,
    dir,
    isRtl: dir === 'rtl',
    setLocale,
    toggleLocale: () => setLocale(locale === 'ar' ? 'en' : 'ar'),
  };
}
