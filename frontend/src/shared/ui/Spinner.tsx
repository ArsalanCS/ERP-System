import { cn } from '@/shared/lib/cn';

interface SpinnerProps {
  size?: number;
  className?: string;
}

/** Indeterminate loading spinner (clay accent). */
export function Spinner({ size = 18, className }: SpinnerProps) {
  return (
    <span
      role="status"
      aria-live="polite"
      className={cn(
        'inline-block animate-spin rounded-full border-2 border-stone-200 border-t-clay',
        className,
      )}
      style={{ width: size, height: size }}
    />
  );
}

/** Centered spinner block for in-card / in-table loading states. */
export function LoadingBlock({ label }: { label?: string | undefined }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-16 text-ink-4">
      <Spinner size={26} />
      {label && <span className="text-sm font-medium">{label}</span>}
    </div>
  );
}
