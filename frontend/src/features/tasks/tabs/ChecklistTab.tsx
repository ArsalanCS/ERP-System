import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Card, Button, Icon, Input, Spinner } from '@/shared/ui';
import { tasksApi, taskKeys } from '../api';
import type { ChecklistItem } from '../types';

export function ChecklistTab({ taskId, canEdit }: { taskId: string; canEdit: boolean }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [text, setText] = useState('');

  const query = useQuery({ queryKey: taskKeys.checklist(taskId), queryFn: () => tasksApi.checklist(taskId) });
  const invalidate = () => queryClient.invalidateQueries({ queryKey: taskKeys.checklist(taskId) });

  const add = useMutation({
    mutationFn: () => tasksApi.addChecklistItem(taskId, { text: text.trim() }),
    onSuccess: () => { setText(''); void invalidate(); },
  });
  const toggle = useMutation({
    mutationFn: (item: ChecklistItem) => tasksApi.updateChecklistItem(taskId, item.id, { text: item.text, isDone: !item.isDone, sortOrder: item.sortOrder }),
    onSuccess: () => void invalidate(),
  });
  const remove = useMutation({
    mutationFn: (itemId: string) => tasksApi.removeChecklistItem(taskId, itemId),
    onSuccess: () => void invalidate(),
  });

  const items = query.data ?? [];
  const done = items.filter((i) => i.isDone).length;

  return (
    <Card className="card-pad">
      {items.length > 0 && (
        <div className="mb-3 text-[12.5px] text-ink-4">{t('tasks.checklist.progress', { done, total: items.length })}</div>
      )}
      {query.isLoading ? (
        <div className="flex justify-center py-6"><Spinner size={20} /></div>
      ) : (
        <ul className="flex flex-col gap-1.5">
          {items.map((item) => (
            <li key={item.id} className="group flex items-center gap-2.5">
              <input type="checkbox" className="h-4 w-4 accent-clay" checked={item.isDone}
                disabled={!canEdit || toggle.isPending} onChange={() => toggle.mutate(item)} />
              <span className={`flex-1 text-[13.5px] ${item.isDone ? 'text-ink-4 line-through' : 'text-ink-2'}`}>{item.text}</span>
              {canEdit && (
                <button type="button" title={t('common.delete')} onClick={() => remove.mutate(item.id)}
                  className="opacity-0 transition-opacity group-hover:opacity-100 text-ink-4 hover:text-red">
                  <Icon name="trash" size={14} />
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
      {canEdit && (
        <form className="mt-3 flex items-center gap-2" onSubmit={(e) => { e.preventDefault(); if (text.trim()) add.mutate(); }}>
          <Input placeholder={t('tasks.checklist.addPlaceholder')} value={text} onChange={(e) => setText(e.target.value)} />
          <Button type="submit" size="sm" disabled={!text.trim() || add.isPending}>{t('common.add')}</Button>
        </form>
      )}
    </Card>
  );
}
