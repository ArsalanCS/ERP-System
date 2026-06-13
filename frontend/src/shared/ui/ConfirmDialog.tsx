import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from './Button';
import { Spinner } from './Spinner';
import { cn } from '@/shared/lib/cn';

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message?: string;
  confirmLabel?: string;
  tone?: 'default' | 'danger';
  /** When set, shows a required reason field passed back to onConfirm. */
  reasonLabel?: string;
  loading?: boolean;
  onConfirm: (reason: string) => void;
  onCancel: () => void;
}

/** Small centered confirmation modal, optionally capturing a reason. */
export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel,
  tone = 'default',
  reasonLabel,
  loading = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const { t } = useTranslation();
  const [reason, setReason] = useState('');

  useEffect(() => {
    if (open) setReason('');
  }, [open]);

  if (!open) return null;

  const reasonRequired = reasonLabel !== undefined;
  const canConfirm = !loading && (!reasonRequired || reason.trim().length > 0);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <button
        type="button"
        aria-label={t('common.close')}
        onClick={onCancel}
        className="absolute inset-0 bg-stone-900/30 backdrop-blur-[1px]"
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className="relative w-full max-w-[420px] rounded-lg bg-paper p-5 shadow-xl fade-up"
      >
        <h2 className="text-[16px] font-semibold tracking-[-0.01em]">{title}</h2>
        {message && <p className="mt-1.5 text-sm text-ink-3">{message}</p>}

        {reasonRequired && (
          <div className="field mt-4">
            <label className="label" htmlFor="confirm-reason">
              {reasonLabel}
            </label>
            <textarea
              id="confirm-reason"
              className="input min-h-[84px] resize-y"
              value={reason}
              autoFocus
              onChange={(e) => setReason(e.target.value)}
            />
          </div>
        )}

        <div className="mt-5 flex items-center justify-end gap-2.5">
          <Button variant="outline" size="sm" onClick={onCancel} disabled={loading}>
            {t('common.cancel')}
          </Button>
          <Button
            size="sm"
            onClick={() => onConfirm(reason.trim())}
            disabled={!canConfirm}
            className={cn(tone === 'danger' && 'btn-danger')}
          >
            {loading && <Spinner size={15} className="border-white/40 border-t-white" />}
            {confirmLabel ?? t('common.confirm')}
          </Button>
        </div>
      </div>
    </div>
  );
}
