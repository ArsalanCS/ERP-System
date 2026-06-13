import type { ReactNode } from 'react';

interface PageHeaderProps {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
}

/** Standard page title block (handoff `.page-head`). */
export function PageHeader({ title, subtitle, actions }: PageHeaderProps) {
  return (
    <div className="mb-[22px] flex flex-wrap items-end justify-between gap-5">
      <div>
        <h1 className="text-[24px] font-bold tracking-[-0.02em]">{title}</h1>
        {subtitle && <p className="mt-[3px] text-sm text-ink-3">{subtitle}</p>}
      </div>
      {actions}
    </div>
  );
}
