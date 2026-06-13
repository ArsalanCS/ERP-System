import type { HTMLAttributes, ReactNode } from 'react';
import { cn } from '@/shared/lib/cn';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  /** Apply default body padding (handoff `.card-pad`). */
  padded?: boolean;
}

export function Card({ padded = false, className, children, ...props }: CardProps) {
  return (
    <div className={cn('card', padded && 'card-pad', className)} {...props}>
      {children}
    </div>
  );
}

interface CardHeaderProps {
  title: ReactNode;
  subtitle?: ReactNode;
  action?: ReactNode;
}

/** Section header row inside a card (handoff `.sec-head`). */
export function CardHeader({ title, subtitle, action }: CardHeaderProps) {
  return (
    <div className="flex items-center justify-between gap-3.5 border-b border-stone-150 px-5 py-4">
      <div>
        <h3 className="text-[15px] font-semibold tracking-[-0.01em]">{title}</h3>
        {subtitle && <p className="mt-px text-[12.5px] text-ink-4">{subtitle}</p>}
      </div>
      {action}
    </div>
  );
}
