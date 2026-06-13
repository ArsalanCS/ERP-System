import { useCallback, useMemo, useRef, useState, type ReactNode } from 'react';
import { Icon } from './Icon';
import { cn } from '@/shared/lib/cn';
import { ToastContext, type ToastApi, type ToastTone } from './toast-context';

interface Toast {
  id: number;
  message: string;
  tone: ToastTone;
}

const TONE: Record<ToastTone, { cls: string; icon: 'check' | 'alert' | 'help' }> = {
  success: { cls: 'border-green-100 bg-green-100 text-green', icon: 'check' },
  error: { cls: 'border-red-100 bg-red-100 text-red', icon: 'alert' },
  info: { cls: 'border-stone-200 bg-paper text-ink-2', icon: 'help' },
};

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const counter = useRef(0);

  const remove = useCallback((id: number) => {
    setToasts((prev) => prev.filter((toast) => toast.id !== id));
  }, []);

  const show = useCallback(
    (message: string, tone: ToastTone = 'info') => {
      const id = ++counter.current;
      setToasts((prev) => [...prev, { id, message, tone }]);
      window.setTimeout(() => remove(id), 4000);
    },
    [remove],
  );

  const api = useMemo<ToastApi>(
    () => ({
      show,
      success: (message) => show(message, 'success'),
      error: (message) => show(message, 'error'),
    }),
    [show],
  );

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="pointer-events-none fixed inset-x-0 bottom-4 z-[60] flex flex-col items-center gap-2 px-4">
        {toasts.map((toast) => {
          const tone = TONE[toast.tone];
          return (
            <button
              type="button"
              key={toast.id}
              onClick={() => remove(toast.id)}
              className={cn(
                'pointer-events-auto flex max-w-[420px] items-center gap-2.5 rounded-md border px-4 py-2.5 text-[13.5px] font-medium shadow-md fade-up',
                tone.cls,
              )}
            >
              <Icon name={tone.icon} size={17} />
              <span>{toast.message}</span>
            </button>
          );
        })}
      </div>
    </ToastContext.Provider>
  );
}
