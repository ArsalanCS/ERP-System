import type { ReactNode } from 'react';
import { Icon, type IconName } from './Icon';

interface EmptyStateProps {
  icon?: IconName;
  title: string;
  body?: string;
  action?: ReactNode;
}

export function EmptyState({ icon = 'gauge', title, body, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-stone-200 bg-paper px-6 py-16 text-center">
      <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-stone-100 text-ink-4">
        <Icon name={icon} size={22} />
      </div>
      <h3 className="text-[15px] font-semibold">{title}</h3>
      {body && <p className="mt-1.5 max-w-sm text-sm text-ink-3">{body}</p>}
      {action && <div className="mt-5">{action}</div>}
    </div>
  );
}
