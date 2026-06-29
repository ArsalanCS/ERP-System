import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Card, Button, Icon, Drawer, Badge, Spinner, EmptyState, ConfirmDialog } from '@/shared/ui';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { useToast } from '@/shared/ui/toast-context';
import { tasksApi, taskKeys } from './api';
import { TASK_PRIORITY, TASK_STATUS, type CreateStatusBody, type StatusOption, type TaskSettings } from './types';

export function TaskSettingsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { can } = usePermissions();
  const canManage = can(Actions.TaskWorkflowManage);

  return (
    <>
      <div className="mb-4">
        <button type="button" onClick={() => navigate('/admin/tasks')}
          className="mb-2 inline-flex items-center gap-1 text-[12.5px] text-ink-4 hover:text-clay">
          <Icon name="chevron-left" size={14} /> {t('tasks.title')}
        </button>
        <h1 className="text-[22px] font-semibold tracking-[-0.01em] text-ink">{t('tasks.settings.title')}</h1>
        <p className="mt-1 text-[13.5px] text-ink-4">{t('tasks.settings.subtitle')}</p>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <StatusList code={TASK_STATUS} title={t('tasks.settings.statuses')} canManage={canManage} workflow />
        <StatusList code={TASK_PRIORITY} title={t('tasks.settings.priorities')} canManage={canManage} workflow={false} />
      </div>

      <div className="mt-4">
        <WorkspaceConfig canManage={canManage} />
      </div>
    </>
  );
}

function WorkspaceConfig({ canManage }: { canManage: boolean }) {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();
  const query = useQuery({ queryKey: taskKeys.config, queryFn: () => tasksApi.getConfig() });
  const [form, setForm] = useState<TaskSettings | null>(null);

  const cfg = form ?? query.data ?? null;
  const set = <K extends keyof TaskSettings>(key: K, value: TaskSettings[K]) =>
    cfg && setForm({ ...cfg, [key]: value });

  const save = useMutation({
    mutationFn: () => tasksApi.updateConfig(cfg!),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: taskKeys.config });
      setForm(null);
      toast.success(t('tasks.settings.saved'));
    },
    onError: (e) => toast.error(e instanceof Error ? e.message : t('common.loadError')),
  });

  if (query.isLoading || !cfg) {
    return <Card className="card-pad"><div className="flex justify-center py-6"><Spinner size={20} /></div></Card>;
  }

  const toggle = (key: keyof TaskSettings, label: string, hint?: string) => (
    <label className="flex cursor-pointer items-start gap-2.5 rounded-md border border-stone-150 px-3 py-2.5">
      <input type="checkbox" className="mt-0.5 h-4 w-4 accent-clay" disabled={!canManage}
        checked={cfg[key] as boolean} onChange={(e) => set(key, e.target.checked as never)} />
      <span className="flex flex-col">
        <span className="text-[13px] font-medium text-ink-2">{label}</span>
        {hint && <span className="text-[12px] text-ink-4">{hint}</span>}
      </span>
    </label>
  );

  return (
    <Card className="card-pad">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-[14px] font-semibold text-ink">{t('tasks.settings.config.title')}</h2>
        {canManage && form && (
          <Button size="sm" onClick={() => save.mutate()} disabled={save.isPending}>{t('common.save')}</Button>
        )}
      </div>

      <div className="grid grid-cols-1 gap-5 md:grid-cols-3">
        <section className="flex flex-col gap-2">
          <h3 className="text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{t('tasks.settings.config.dailyRules')}</h3>
          {toggle('dailyReportRequired', t('tasks.settings.config.dailyReportRequired'))}
          {toggle('allowStatusChangeFromReport', t('tasks.settings.config.allowStatusChangeFromReport'), t('tasks.settings.config.allowStatusChangeFromReportHint'))}
          {toggle('requireEstimatedTime', t('tasks.settings.config.requireEstimatedTime'))}
          {toggle('requireActualTime', t('tasks.settings.config.requireActualTime'))}
          {toggle('allowMultipleReportsPerDay', t('tasks.settings.config.allowMultipleReportsPerDay'))}
        </section>

        <section className="flex flex-col gap-2">
          <h3 className="text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{t('tasks.settings.config.notifications')}</h3>
          {toggle('notifyOnTaskCreated', t('tasks.settings.config.notifyOnTaskCreated'))}
          {toggle('notifyOnTaskAssigned', t('tasks.settings.config.notifyOnTaskAssigned'))}
          {toggle('notifyOnStatusChange', t('tasks.settings.config.notifyOnStatusChange'))}
          {toggle('notifyOnDailyReport', t('tasks.settings.config.notifyOnDailyReport'))}
        </section>

        <section className="flex flex-col gap-2">
          <h3 className="text-[12px] font-semibold uppercase tracking-[0.04em] text-ink-4">{t('tasks.settings.config.dashboard')}</h3>
          <label className="flex flex-col gap-1 rounded-md border border-stone-150 px-3 py-2.5 text-[12.5px] text-ink-3">
            {t('tasks.settings.config.dashboardDefaultRangeDays')}
            <input type="number" min={1} max={365} className="input !w-28" disabled={!canManage}
              value={cfg.dashboardDefaultRangeDays}
              onChange={(e) => set('dashboardDefaultRangeDays', Number(e.target.value))} />
          </label>
        </section>
      </div>
    </Card>
  );
}

function StatusList({ code, title, canManage, workflow }: { code: string; title: string; canManage: boolean; workflow: boolean }) {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<StatusOption | 'new' | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<StatusOption | null>(null);

  const query = useQuery({ queryKey: taskKeys.settingsStatuses(code), queryFn: () => tasksApi.settingsStatuses(code) });
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: taskKeys.settingsStatuses(code) });
    void queryClient.invalidateQueries({ queryKey: taskKeys.statuses(code) });
  };

  const remove = useMutation({
    mutationFn: (id: string) => tasksApi.deleteStatus(id),
    onSuccess: () => { invalidate(); setDeleteTarget(null); toast.success(t('tasks.settings.deleted')); },
    onError: (e) => { setDeleteTarget(null); toast.error(e instanceof Error ? e.message : t('common.loadError')); },
  });

  const items = query.data ?? [];

  return (
    <Card className="card-pad">
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-[14px] font-semibold text-ink">{title}</h2>
        {canManage && (
          <Button size="sm" variant="outline" leadingIcon={<Icon name="plus" size={14} />} onClick={() => setEditing('new')}>
            {t('common.add')}
          </Button>
        )}
      </div>

      {query.isLoading ? (
        <div className="flex justify-center py-6"><Spinner size={20} /></div>
      ) : items.length === 0 ? (
        <EmptyState icon="alert" title={t('tasks.settings.empty')} />
      ) : (
        <ul className="flex flex-col gap-1.5">
          {items.map((s) => (
            <li key={s.id} className="group flex items-center gap-2.5 rounded-md border border-stone-150 px-3 py-2">
              <span className="inline-block h-3 w-3 flex-none rounded-full" style={{ background: s.color ?? '#cbd5e1' }} />
              <span className="font-medium text-ink-2">{s.name}</span>
              {workflow && s.isInitial && <Badge tone="blue">{t('tasks.settings.initial')}</Badge>}
              {workflow && s.isClosed && <Badge tone="green">{t('tasks.settings.closed')}</Badge>}
              {!s.isActive && <Badge tone="neutral">{t('tasks.settings.inactive')}</Badge>}
              {canManage && (
                <span className="ms-auto flex items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100">
                  <button type="button" title={t('common.edit')} onClick={() => setEditing(s)}
                    className="text-ink-4 hover:text-clay"><Icon name="edit" size={14} /></button>
                  <button type="button" title={t('common.delete')} onClick={() => setDeleteTarget(s)}
                    className="text-ink-4 hover:text-red"><Icon name="trash" size={14} /></button>
                </span>
              )}
            </li>
          ))}
        </ul>
      )}

      {editing && (
        <StatusDrawer
          code={code}
          workflow={workflow}
          status={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={() => { setEditing(null); invalidate(); }}
        />
      )}

      <ConfirmDialog
        open={!!deleteTarget}
        title={t('tasks.settings.deleteTitle')}
        message={t('tasks.settings.deleteConfirm', { name: deleteTarget?.name ?? '' })}
        confirmLabel={t('common.delete')}
        tone="danger"
        onCancel={() => setDeleteTarget(null)}
        onConfirm={() => deleteTarget && remove.mutate(deleteTarget.id)}
      />
    </Card>
  );
}

function StatusDrawer({ code, workflow, status, onClose, onSaved }: {
  code: string; workflow: boolean; status: StatusOption | null; onClose: () => void; onSaved: () => void;
}) {
  const { t } = useTranslation();
  const toast = useToast();
  const isEdit = !!status;

  const [name, setName] = useState(status?.name ?? '');
  const [color, setColor] = useState(status?.color ?? '#64748b');
  const [isClosed, setIsClosed] = useState(status?.isClosed ?? false);
  const [isInitial, setIsInitial] = useState(status?.isInitial ?? false);
  const [isActive, setIsActive] = useState(status?.isActive ?? true);

  const save = useMutation({
    mutationFn: async () => {
      if (isEdit && status) {
        await tasksApi.updateStatus(status.id, { name: name.trim(), color, isClosed, isInitial, isActive });
        return;
      }
      const body: CreateStatusBody = { statusTypeCode: code, name: name.trim(), color, isClosed, isInitial };
      await tasksApi.createStatus(body);
    },
    onSuccess: () => { toast.success(t('tasks.settings.saved')); onSaved(); },
    onError: (e) => toast.error(e instanceof Error ? e.message : t('common.loadError')),
  });

  return (
    <Drawer
      open
      onClose={onClose}
      title={isEdit ? t('tasks.settings.editStatus') : t('tasks.settings.newStatus')}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>{t('common.cancel')}</Button>
          <Button onClick={() => save.mutate()} disabled={!name.trim() || save.isPending}>{t('common.save')}</Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        <label className="flex flex-col gap-1 text-[12.5px] text-ink-3">
          {t('tasks.settings.name')}
          <input className="input" value={name} onChange={(e) => setName(e.target.value)} autoFocus />
        </label>
        <label className="flex items-center gap-3 text-[12.5px] text-ink-3">
          {t('tasks.settings.color')}
          <input type="color" className="h-8 w-12 cursor-pointer rounded border border-stone-200"
            value={color} onChange={(e) => setColor(e.target.value)} />
          <span className="font-mono text-ink-4">{color}</span>
        </label>
        {workflow && (
          <>
            <label className="flex cursor-pointer items-center gap-2 text-[13px] text-ink-2">
              <input type="checkbox" className="h-4 w-4 accent-clay" checked={isInitial} onChange={(e) => setIsInitial(e.target.checked)} />
              {t('tasks.settings.initialHint')}
            </label>
            <label className="flex cursor-pointer items-center gap-2 text-[13px] text-ink-2">
              <input type="checkbox" className="h-4 w-4 accent-clay" checked={isClosed} onChange={(e) => setIsClosed(e.target.checked)} />
              {t('tasks.settings.closedHint')}
            </label>
          </>
        )}
        {isEdit && (
          <label className="flex cursor-pointer items-center gap-2 text-[13px] text-ink-2">
            <input type="checkbox" className="h-4 w-4 accent-clay" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
            {t('tasks.settings.activeHint')}
          </label>
        )}
      </div>
    </Drawer>
  );
}
