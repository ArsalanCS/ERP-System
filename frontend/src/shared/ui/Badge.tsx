import type { HTMLAttributes } from 'react';
import { cn } from '@/shared/lib/cn';

export type BadgeTone = 'neutral' | 'green' | 'amber' | 'red' | 'blue' | 'violet' | 'clay';

const TONE_CLASS: Record<BadgeTone, string> = {
  neutral: '',
  green: 'chip-green',
  amber: 'chip-amber',
  red: 'chip-red',
  blue: 'chip-blue',
  violet: 'chip-violet',
  clay: 'chip-clay',
};

interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  tone?: BadgeTone;
  dot?: boolean;
}

/** Pill chip / status badge (handoff `.chip`). */
export function Badge({ tone = 'neutral', dot = false, className, children, ...props }: BadgeProps) {
  return (
    <span className={cn('chip', TONE_CLASS[tone], className)} {...props}>
      {dot && <span className="dot" />}
      {children}
    </span>
  );
}
