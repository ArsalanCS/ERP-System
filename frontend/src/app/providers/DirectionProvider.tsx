import { useEffect, type ReactNode } from 'react';
import { useDirection } from '@/shared/hooks/useDirection';

/**
 * Keeps the <html> lang + dir attributes in sync with the active locale so
 * Tailwind logical properties and the rtl: variant mirror layouts correctly.
 */
export function DirectionProvider({ children }: { children: ReactNode }) {
  const { locale, dir } = useDirection();

  useEffect(() => {
    const root = document.documentElement;
    root.setAttribute('lang', locale);
    root.setAttribute('dir', dir);
  }, [locale, dir]);

  return <>{children}</>;
}
