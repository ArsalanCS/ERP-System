import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  PageHeader,
  Card,
  Button,
  Input,
  Icon,
  Badge,
  Spinner,
  LoadingBlock,
  EmptyState,
  ConfirmDialog,
  type BadgeTone,
} from '@/shared/ui';
import { Can } from '@/shared/rbac/Can';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { useToast } from '@/shared/ui/toast-context';
import { cn } from '@/shared/lib/cn';
import { accessApi, accessKeys } from './api';
import { RoleType, type RoleListItem } from './types';
import { RoleFormDrawer } from './RoleFormDrawer';
import { PermissionMatrix } from './PermissionMatrix';

const TYPE_TONE: Record<RoleType, BadgeTone> = {
  [RoleType.System]: 'neutral',
  [RoleType.Workspace]: 'blue',
  [RoleType.Organization]: 'violet',
  [RoleType.Custom]: 'clay',
};

interface MetaForm {
  name: string;
  description: string;
}

export function AccessControlPage() {
  const { t } = useTranslation();
  const { can } = usePermissions();
  const toast = useToast();
  const queryClient = useQueryClient();
  const canManage = can(Actions.AccessControlManage);

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);

  const rolesQuery = useQuery({ queryKey: accessKeys.roles, queryFn: accessApi.listRoles });
  const permissionsQuery = useQuery({ queryKey: accessKeys.permissions, queryFn: accessApi.listPermissions });
  const roleQuery = useQuery({
    queryKey: accessKeys.role(selectedId ?? ''),
    queryFn: () => accessApi.getRole(selectedId!),
    enabled: !!selectedId,
  });

  // Default to the first role once loaded.
  useEffect(() => {
    if (!selectedId && rolesQuery.data && rolesQuery.data.length > 0) {
      setSelectedId(rolesQuery.data[0]!.id);
    }
  }, [rolesQuery.data, selectedId]);

  const metaForm = useForm<MetaForm>();
  useEffect(() => {
    if (roleQuery.data) {
      metaForm.reset({
        name: roleQuery.data.name,
        description: roleQuery.data.description ?? '',
      });
    }
  }, [roleQuery.data, metaForm]);

  const role = roleQuery.data;
  const editable = canManage && role?.type !== RoleType.System;

  const metaMutation = useMutation({
    mutationFn: (values: MetaForm) =>
      accessApi.updateRole(role!.id, {
        name: values.name.trim(),
        description: values.description.trim() || null,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: accessKeys.roles });
      void queryClient.invalidateQueries({ queryKey: accessKeys.role(role!.id) });
      toast.success(t('access.feedback.updated'));
    },
    onError: () => toast.error(t('common.loadError')),
  });

  const deleteMutation = useMutation({
    mutationFn: () => accessApi.deleteRole(role!.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: accessKeys.roles });
      toast.success(t('access.feedback.deleted'));
      setConfirmDelete(false);
      setSelectedId(null);
    },
    onError: () => {
      toast.error(t('common.loadError'));
      setConfirmDelete(false);
    },
  });

  const renderRoleRow = (r: RoleListItem) => (
    <button
      key={r.id}
      type="button"
      onClick={() => setSelectedId(r.id)}
      className={cn(
        'flex w-full items-center gap-3 border-b border-stone-100 px-4 py-3 text-start transition-colors last:border-0',
        selectedId === r.id ? 'bg-clay-100' : 'hover:bg-stone-50',
      )}
    >
      <span
        className="h-8 w-1 flex-none rounded-full"
        style={{ background: r.color ?? 'var(--color-stone-300)' }}
      />
      <div className="min-w-0 flex-1">
        <div className="truncate font-semibold text-ink">{r.name}</div>
        <div className="text-[12px] text-ink-4">{t('access.assignedUsers', { count: r.assignedUsers })}</div>
      </div>
      <Badge tone={TYPE_TONE[r.type]}>{t(`access.type.${r.type}`)}</Badge>
    </button>
  );

  return (
    <>
      <PageHeader
        title={t('access.title')}
        subtitle={t('access.subtitle')}
        actions={
          <Can action={Actions.AccessControlManage}>
            <Button leadingIcon={<Icon name="plus" size={16} />} onClick={() => setDrawerOpen(true)}>
              {t('access.newRole')}
            </Button>
          </Can>
        }
      />

      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[320px_1fr]">
        {/* Roles list */}
        <Card className="self-start overflow-hidden">
          <div className="border-b border-stone-150 px-4 py-3 text-[13px] font-semibold text-ink-3">
            {t('access.rolesTitle')}
          </div>
          {rolesQuery.isLoading ? (
            <LoadingBlock label={t('common.loading')} />
          ) : rolesQuery.data && rolesQuery.data.length > 0 ? (
            <div className="flex flex-col">{rolesQuery.data.map(renderRoleRow)}</div>
          ) : (
            <p className="p-5 text-sm text-ink-4">{t('common.none')}</p>
          )}
        </Card>

        {/* Selected role */}
        {!selectedId ? (
          <EmptyState icon="shield" title={t('access.selectRole')} body={t('access.selectRoleBody')} />
        ) : roleQuery.isLoading || !role ? (
          <Card padded>
            <LoadingBlock label={t('common.loading')} />
          </Card>
        ) : (
          <div className="flex flex-col gap-5">
            <Card>
              <div className="flex items-start justify-between gap-3 border-b border-stone-150 px-5 py-4">
                <div className="flex items-center gap-2.5">
                  <h2 className="text-[16px] font-semibold">{role.name}</h2>
                  <span className="font-mono text-[12px] text-ink-4">{role.code}</span>
                  <Badge tone={TYPE_TONE[role.type]}>{t(`access.type.${role.type}`)}</Badge>
                </div>
                {editable && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setConfirmDelete(true)}
                    className="text-red"
                    leadingIcon={<Icon name="trash" size={15} />}
                  >
                    {t('access.deleteRole')}
                  </Button>
                )}
              </div>

              {!editable ? (
                <div className="p-5">
                  {role.description && <p className="text-sm text-ink-2">{role.description}</p>}
                  {role.type === RoleType.System && (
                    <p className="mt-2 text-[13px] text-ink-4">{t('access.systemReadonly')}</p>
                  )}
                </div>
              ) : (
                <form
                  onSubmit={metaForm.handleSubmit((v) => metaMutation.mutate(v))}
                  className="flex flex-col gap-4 p-5"
                >
                  <Input
                    label={t('access.fields.name')}
                    error={metaForm.formState.errors.name?.message}
                    {...metaForm.register('name', { required: t('users.validation.required') })}
                  />
                  <div className="field">
                    <label className="label" htmlFor="role-meta-description">
                      {t('access.fields.description')}
                    </label>
                    <textarea
                      id="role-meta-description"
                      className="input min-h-[72px] resize-y"
                      {...metaForm.register('description')}
                    />
                  </div>
                  <div className="flex justify-end">
                    <Button type="submit" size="sm" disabled={metaMutation.isPending || !metaForm.formState.isDirty}>
                      {metaMutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}
                      {t('access.saveRole')}
                    </Button>
                  </div>
                </form>
              )}
            </Card>

            <Card className="overflow-hidden">
              {permissionsQuery.isLoading || !permissionsQuery.data ? (
                <LoadingBlock label={t('common.loading')} />
              ) : (
                <PermissionMatrix role={role} permissions={permissionsQuery.data} editable={!!editable} />
              )}
            </Card>
          </div>
        )}
      </div>

      <RoleFormDrawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        onCreated={(id) => {
          setDrawerOpen(false);
          setSelectedId(id);
        }}
      />

      <ConfirmDialog
        open={confirmDelete}
        title={t('access.deleteRole')}
        message={t('access.deleteConfirm')}
        confirmLabel={t('common.delete')}
        tone="danger"
        loading={deleteMutation.isPending}
        onCancel={() => setConfirmDelete(false)}
        onConfirm={() => deleteMutation.mutate()}
      />
    </>
  );
}
