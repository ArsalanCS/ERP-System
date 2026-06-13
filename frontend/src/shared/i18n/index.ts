import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import {
  DEFAULT_LOCALE,
  FALLBACK_LOCALE,
  STORAGE_KEY,
  SUPPORTED_LOCALES,
} from './config';
import enCommon from './locales/en/common.json';
import arCommon from './locales/ar/common.json';

export const defaultNS = 'common';

export const resources = {
  en: { common: enCommon },
  ar: { common: arCommon },
} as const;

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    defaultNS,
    ns: ['common'],
    fallbackLng: FALLBACK_LOCALE,
    supportedLngs: [...SUPPORTED_LOCALES],
    nonExplicitSupportedLngs: true,
    load: 'languageOnly',
    detection: {
      order: ['localStorage', 'navigator', 'htmlTag'],
      lookupLocalStorage: STORAGE_KEY,
      caches: ['localStorage'],
    },
    interpolation: {
      escapeValue: false,
    },
    react: {
      useSuspense: false,
    },
  });

// Ensure we never end up on an unsupported language.
if (!SUPPORTED_LOCALES.includes(i18n.resolvedLanguage as (typeof SUPPORTED_LOCALES)[number])) {
  void i18n.changeLanguage(DEFAULT_LOCALE);
}

export default i18n;
