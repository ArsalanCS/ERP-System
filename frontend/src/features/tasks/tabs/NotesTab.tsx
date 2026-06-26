import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Card, Button, Icon, Badge, Spinner, EmptyState } from '@/shared/ui';
import { useDirection } from '@/shared/hooks/useDirection';
import { formatDateTime } from '@/shared/lib/format';
import { tasksApi, taskKeys } from '../api';

export function NotesTab({ taskId, canManage }: { taskId: string; canManage: boolean }) {
  const { t } = useTranslation();
  const { locale } = useDirection();
  const queryClient = useQueryClient();
  const [body, setBody] = useState('');
  const [pinned, setPinned] = useState(false);
  const [internal, setInternal] = useState(true);

  const query = useQuery({ queryKey: taskKeys.notes(taskId), queryFn: () => tasksApi.notes(taskId) });
  const invalidate = () => queryClient.invalidateQueries({ queryKey: taskKeys.notes(taskId) });

  const add = useMutation({
    mutationFn: () => tasksApi.addNote(taskId, { body: body.trim(), isPinned: pinned, isInternal: internal }),
    onSuccess: () => { setBody(''); setPinned(false); void invalidate(); },
  });
  const remove = useMutation({
    mutationFn: (noteId: string) => tasksApi.removeNote(taskId, noteId),
    onSuccess: () => void invalidate(),
  });

  const notes = query.data ?? [];

  return (
    <Card className="card-pad">
      {canManage && (
        <div className="mb-4 flex flex-col gap-2">
          <textarea className="input min-h-[70px] resize-y" placeholder={t('tasks.notes.addPlaceholder')}
            value={body} onChange={(e) => setBody(e.target.value)} />
          <div className="flex items-center gap-4">
            <label className="flex cursor-pointer items-center gap-2 text-[12.5px] text-ink-2">
              <input type="checkbox" className="h-4 w-4 accent-clay" checked={pinned} onChange={(e) => setPinned(e.target.checked)} />
              {t('tasks.notes.pin')}
            </label>
            <label className="flex cursor-pointer items-center gap-2 text-[12.5px] text-ink-2">
              <input type="checkbox" className="h-4 w-4 accent-clay" checked={internal} onChange={(e) => setInternal(e.target.checked)} />
              {t('tasks.notes.internal')}
            </label>
            <Button size="sm" className="ms-auto" disabled={!body.trim() || add.isPending} onClick={() => body.trim() && add.mutate()}>
              {t('tasks.notes.add')}
            </Button>
          </div>
        </div>
      )}
      {query.isLoading ? (
        <div className="flex justify-center py-6"><Spinner size={20} /></div>
      ) : notes.length === 0 ? (
        <EmptyState icon="mail" title={t('tasks.notes.empty')} />
      ) : (
        <ul className="flex flex-col gap-3">
          {notes.map((n) => (
            <li key={n.id} className="group rounded-md border border-stone-150 px-3.5 py-2.5">
              <div className="mb-1 flex items-center gap-2">
                {n.isPinned && <Badge tone="amber">{t('tasks.notes.pinned')}</Badge>}
                {n.isInternal && <Badge tone="neutral">{t('tasks.notes.internalBadge')}</Badge>}
                <span className="text-[11.5px] text-ink-4">{n.authorName ? `${n.authorName} · ` : ''}{formatDateTime(n.createdAt, locale)}</span>
                {canManage && (
                  <button type="button" title={t('common.delete')} onClick={() => remove.mutate(n.id)}
                    className="ms-auto opacity-0 transition-opacity group-hover:opacity-100 text-ink-4 hover:text-red">
                    <Icon name="trash" size={14} />
                  </button>
                )}
              </div>
              <div className="whitespace-pre-wrap text-[13.5px] text-ink-2">{n.body}</div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
