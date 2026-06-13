import type { ReactNode } from 'react';

interface AuthCardProps {
  title: string;
  subtitle?: string;
  children: ReactNode;
  footer?: ReactNode;
}

/** Shared card chrome for the auth pages (title + body + footer link row). */
export function AuthCard({ title, subtitle, children, footer }: AuthCardProps) {
  return (
    <div className="fade-up">
      <div className="card card-pad">
        <h1 className="text-[20px] font-bold tracking-[-0.01em]">{title}</h1>
        {subtitle && <p className="mt-1.5 text-sm text-ink-3">{subtitle}</p>}
        <div className="mt-6">{children}</div>
      </div>
      {footer && <div className="mt-4 text-center text-[13px] text-ink-3">{footer}</div>}
    </div>
  );
}

/** Inline error/success banner for form-level feedback. */
export function FormBanner({ tone, children }: { tone: 'error' | 'success'; children: ReactNode }) {
  const cls =
    tone === 'error'
      ? 'border-red-100 bg-red-100 text-red'
      : 'border-green-100 bg-green-100 text-green';
  return (
    <div className={`mb-4 rounded-md border px-3.5 py-2.5 text-[13px] font-medium ${cls}`} role="alert">
      {children}
    </div>
  );
}
