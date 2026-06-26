import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Card, Button, Input, Spinner, EmptyState } from '@/shared/ui';
import { tasksApi, taskKeys } from '../api';
import { TaskPriority } from '../types';
import { TaskStatusBadge } from '../TaskStatusBadge';

export function SubtasksTab({ taskId, canCreate }: { taskId: string; canCreate: boolean }) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [title, setTitle] = useState('');

  const query = useQuery({ queryKey: taskKeys.subtasks(taskId), queryFn: () => tasksApi.subtasks(taskId) });
  const add = useMutation({
    mutationFn: () => tasksApi.createSubtask(taskId, { title: title.trim(), priority: TaskPriority.Normal }),
    onSuccess: () => {
      setTitle('');
      void queryClient.invalidateQueries({ queryKey: taskKeys.subtasks(taskId) });
      void queryClient.invalidateQueries({ queryKey: taskKeys.all });
    },
  });

  const items = query.data ?? [];

  return (
    <Card className="card-pad">
      {query.isLoading ? (
        <div className="flex justify-center py-6"><Spinner size={20} /></div>
      ) : items.length === 0 ? (
        <EmptyState icon="check" title={t('tasks.subtasks.empty')} />
      ) : (
        <ul className="flex flex-col divide-y divide-stone-100 rounded-md border border-stone-150">
          {items.map((s) => (
            <li key={s.id}>
              <button type="button" onClick={() => navigate(`/admin/tasks/${s.id}`)}
                className="flex w-full items-center gap-3 px-3 py-2.5 text-start hover:bg-stone-50">
                <div className="min-w-0 flex-1">
                  <div className="truncate text-[13.5px] font-medium text-ink">{s.title}</div>
                  <div className="font-mono text-[11px] text-ink-4">{s.taskNumber}</div>
                </div>
                <TaskStatusBadge name={s.statusName} category={s.statusCategory} />
              </button>
            </li>
          ))}
        </ul>
      )}
      {canCreate && (
        <form className="mt-3 flex items-center gap-2" onSubmit={(e) => { e.preventDefault(); if (title.trim()) add.mutate(); }}>
          <Input placeholder={t('tasks.subtasks.addPlaceholder')} value={title} onChange={(e) => setTitle(e.target.value)} />
          <Button type="submit" size="sm" disabled={!title.trim() || add.isPending}>{t('common.add')}</Button>
        </form>
      )}
    </Card>
  );
}
