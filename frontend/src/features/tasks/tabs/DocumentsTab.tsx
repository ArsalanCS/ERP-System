import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Card, Button, Icon, Input, Spinner, EmptyState } from '@/shared/ui';
import { tasksApi, taskKeys } from '../api';

export function DocumentsTab({ taskId, canManage }: { taskId: string; canManage: boolean }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [fileName, setFileName] = useState('');
  const [filePath, setFilePath] = useState('');

  const query = useQuery({ queryKey: taskKeys.documents(taskId), queryFn: () => tasksApi.documents(taskId) });
  const invalidate = () => queryClient.invalidateQueries({ queryKey: taskKeys.documents(taskId) });

  const add = useMutation({
    mutationFn: () => tasksApi.addDocument(taskId, { fileName: fileName.trim(), filePath: filePath.trim(), mimeType: null }),
    onSuccess: () => { setFileName(''); setFilePath(''); void invalidate(); },
  });
  const remove = useMutation({
    mutationFn: (docId: string) => tasksApi.removeDocument(taskId, docId),
    onSuccess: () => void invalidate(),
  });

  const docs = query.data ?? [];

  return (
    <Card className="card-pad">
      <p className="mb-3 text-[12px] text-ink-4">{t('tasks.documents.hint')}</p>
      {query.isLoading ? (
        <div className="flex justify-center py-6"><Spinner size={20} /></div>
      ) : docs.length === 0 ? (
        <EmptyState icon="download" title={t('tasks.documents.empty')} />
      ) : (
        <ul className="flex flex-col divide-y divide-stone-100 rounded-md border border-stone-150">
          {docs.map((d) => (
            <li key={d.id} className="group flex items-center gap-3 px-3 py-2.5">
              <Icon name="download" size={15} className="flex-none text-ink-4" />
              <div className="min-w-0 flex-1">
                {d.filePath ? (
                  <a href={d.filePath} target="_blank" rel="noreferrer" className="truncate text-[13.5px] font-medium text-clay hover:underline">{d.fileName}</a>
                ) : (
                  <span className="truncate text-[13.5px] font-medium text-ink">{d.fileName}</span>
                )}
                {d.mimeType && <span className="ms-2 text-[11px] uppercase text-ink-4">{d.mimeType}</span>}
              </div>
              {canManage && (
                <button type="button" title={t('common.delete')} onClick={() => remove.mutate(d.id)}
                  className="opacity-0 transition-opacity group-hover:opacity-100 text-ink-4 hover:text-red">
                  <Icon name="trash" size={14} />
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
      {canManage && (
        <form className="mt-3 flex flex-wrap items-end gap-2" onSubmit={(e) => { e.preventDefault(); if (fileName.trim() && filePath.trim()) add.mutate(); }}>
          <div className="min-w-[160px] flex-1"><Input label={t('tasks.documents.fileName')} value={fileName} onChange={(e) => setFileName(e.target.value)} /></div>
          <div className="min-w-[200px] flex-1"><Input label={t('tasks.documents.link')} value={filePath} onChange={(e) => setFilePath(e.target.value)} /></div>
          <Button type="submit" size="sm" disabled={!fileName.trim() || !filePath.trim() || add.isPending}>{t('common.add')}</Button>
        </form>
      )}
    </Card>
  );
}
