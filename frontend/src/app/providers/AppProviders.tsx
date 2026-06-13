import type { ReactNode } from 'react';
import { QueryProvider } from './QueryProvider';
import { DirectionProvider } from './DirectionProvider';
import { AuthProvider } from './AuthProvider';
import { ToastProvider } from '@/shared/ui/toast';

/** Composition root for all cross-cutting providers. */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <QueryProvider>
      <AuthProvider>
        <DirectionProvider>
          <ToastProvider>{children}</ToastProvider>
        </DirectionProvider>
      </AuthProvider>
    </QueryProvider>
  );
}
