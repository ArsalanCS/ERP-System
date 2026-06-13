import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  PageHeader,
  Card,
  Button,
  Icon,
  Badge,
  LoadingBlock,
  EmptyState,
  ConfirmDialog,
  type BadgeTone,
  type IconName,
} from '@/shared/ui';
import { Can } from '@/shared/rbac/Can';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { useToast } from '@/shared/ui/toast-context';
import { structureApi, structureKeys, StructureStatus, type StructureTree } from './api';
import { AddNodeDrawer, type AddContext, type NodeKind } from './AddNodeDrawer';

const STATUS_TONE: Record<StructureStatus, BadgeTone> = {
  [StructureStatus.Active]: 'green',
  [StructureStatus.Inactive]: 'neutral',
  [StructureStatus.Archived]: 'neutral',
};

const KIND_ICON: Record<NodeKind, IconName> = {
  organization: 'building',
  cluster: 'sitemap',
  department: 'users',
  team: 'user',
};

interface NodeRowProps {
  kind: NodeKind;
  depth: number;
  name: string;
  code: string;
  meta?: string | undefined;
  status: StructureStatus;
  canManage: boolean;
  addActions?: { label: string; onClick: () => void }[];
  onArchive?: () => void;
}

function NodeRow({ kind, depth, name, code, meta, status, canManage, addActions, onArchive }: NodeRowProps) {
  const { t } = useTranslation();
  return (
    <div
      className="flex items-center gap-3 border-b border-stone-100 px-3 py-2.5 last:border-0 hover:bg-stone-50"
      style={{ paddingInlineStart: 12 + depth * 22 }}
    >
      <span className="flex h-7 w-7 flex-none items-center justify-center rounded-md bg-stone-100 text-ink-3">
        <Icon name={KIND_ICON[kind]} size={15} />
      </span>
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="truncate font-semibold text-ink">{name}</span>
          <span className="font-mono text-[11.5px] text-ink-4">{code}</span>
        </div>
        <div className="text-[12px] text-ink-4">
          {t(`structure.kinds.${kind}`)}
          {meta ? ` · ${meta}` : ''}
        </div>
      </div>
      <Badge tone={STATUS_TONE[status]} dot={status === StructureStatus.Active}>
        {t(`structure.status.${status}`)}
      </Badge>
      {canManage && (
        <div className="flex items-center gap-1">
          {addActions?.map((a) => (
            <button
              key={a.label}
              type="button"
              title={a.label}
              onClick={a.onClick}
              className="flex h-8 w-8 items-center justify-center rounded-sm text-ink-4 hover:bg-stone-150 hover:text-ink"
            >
              <Icon name="plus" size={15} />
            </button>
          ))}
          {onArchive && status !== StructureStatus.Archived && (
            <button
              type="button"
              title={t('structure.archive')}
              onClick={onArchive}
              className="flex h-8 w-8 items-center justify-center rounded-sm text-ink-4 hover:bg-stone-150 hover:text-red"
            >
              <Icon name="trash" size={15} />
            </button>
          )}
        </div>
      )}
    </div>
  );
}

export function BusinessStructurePage() {
  const { t } = useTranslation();
  const { can } = usePermissions();
  const toast = useToast();
  const queryClient = useQueryClient();
  const canManage = can(Actions.BusinessStructureManage);

  const [addContext, setAddContext] = useState<AddContext | null>(null);
  const [archiveTarget, setArchiveTarget] = useState<{ kind: NodeKind; id: string } | null>(null);

  const query = useQuery({ queryKey: structureKeys.tree, queryFn: structureApi.tree });

  const archiveMutation = useMutation({
    mutationFn: ({ kind, id }: { kind: NodeKind; id: string }) => {
      switch (kind) {
        case 'organization':
          return structureApi.archiveOrganization(id);
        case 'cluster':
          return structureApi.archiveCluster(id);
        case 'department':
          return structureApi.archiveDepartment(id);
        case 'team':
          return structureApi.archiveTeam(id);
      }
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: structureKeys.tree });
      toast.success(t('structure.feedback.archived'));
      setArchiveTarget(null);
    },
    onError: () => {
      toast.error(t('common.loadError'));
      setArchiveTarget(null);
    },
  });

  const renderTree = (tree: StructureTree): ReactNode => {
    const rows: ReactNode[] = [];

    const renderTeams = (departmentId: string, depth: number) => {
      for (const team of tree.teams.filter((x) => x.departmentId === departmentId)) {
        rows.push(
          <NodeRow
            key={`team-${team.id}`}
            kind="team"
            depth={depth}
            name={team.name}
            code={team.code}
            status={team.status}
            canManage={canManage}
            onArchive={() => setArchiveTarget({ kind: 'team', id: team.id })}
          />,
        );
      }
    };

    const renderDepartments = (predicate: (clusterId: string | null) => boolean, orgId: string, depth: number) => {
      for (const dept of tree.departments.filter((x) => x.organizationId === orgId && predicate(x.clusterId))) {
        rows.push(
          <NodeRow
            key={`dept-${dept.id}`}
            kind="department"
            depth={depth}
            name={dept.name}
            code={dept.code}
            status={dept.status}
            canManage={canManage}
            addActions={[{ label: t('structure.add.team'), onClick: () => setAddContext({ kind: 'team', departmentId: dept.id }) }]}
            onArchive={() => setArchiveTarget({ kind: 'department', id: dept.id })}
          />,
        );
        renderTeams(dept.id, depth + 1);
      }
    };

    const renderClusters = (orgId: string, parentId: string | null, depth: number) => {
      for (const cluster of tree.clusters.filter((x) => x.organizationId === orgId && x.parentClusterId === parentId)) {
        rows.push(
          <NodeRow
            key={`cluster-${cluster.id}`}
            kind="cluster"
            depth={depth}
            name={cluster.name}
            code={cluster.code}
            meta={cluster.type}
            status={cluster.status}
            canManage={canManage}
            addActions={[
              {
                label: t('structure.add.cluster'),
                onClick: () => setAddContext({ kind: 'cluster', organizationId: orgId, parentClusterId: cluster.id }),
              },
              {
                label: t('structure.add.department'),
                onClick: () => setAddContext({ kind: 'department', organizationId: orgId, clusterId: cluster.id }),
              },
            ]}
            onArchive={() => setArchiveTarget({ kind: 'cluster', id: cluster.id })}
          />,
        );
        renderClusters(orgId, cluster.id, depth + 1);
        renderDepartments((clusterId) => clusterId === cluster.id, orgId, depth + 1);
      }
    };

    for (const org of tree.organizations) {
      rows.push(
        <NodeRow
          key={`org-${org.id}`}
          kind="organization"
          depth={0}
          name={org.name}
          code={org.code}
          meta={org.baseCurrency}
          status={org.status}
          canManage={canManage}
          addActions={[
            {
              label: t('structure.add.cluster'),
              onClick: () => setAddContext({ kind: 'cluster', organizationId: org.id, parentClusterId: null }),
            },
            {
              label: t('structure.add.department'),
              onClick: () => setAddContext({ kind: 'department', organizationId: org.id, clusterId: null }),
            },
          ]}
          onArchive={() => setArchiveTarget({ kind: 'organization', id: org.id })}
        />,
      );
      renderClusters(org.id, null, 1);
      renderDepartments((clusterId) => clusterId === null, org.id, 1);
    }

    return rows;
  };

  const tree = query.data;
  const isEmpty = tree && tree.organizations.length === 0;

  return (
    <>
      <PageHeader
        title={t('structure.title')}
        subtitle={t('structure.subtitle')}
        actions={
          <Can action={Actions.BusinessStructureManage}>
            <Button
              leadingIcon={<Icon name="plus" size={16} />}
              onClick={() => setAddContext({ kind: 'organization' })}
            >
              {t('structure.addOrganization')}
            </Button>
          </Can>
        }
      />

      {query.isLoading ? (
        <Card padded>
          <LoadingBlock label={t('common.loading')} />
        </Card>
      ) : query.isError || !tree ? (
        <EmptyState
          icon="alert"
          title={t('common.loadError')}
          action={
            <Button variant="outline" size="sm" onClick={() => query.refetch()}>
              {t('common.retry')}
            </Button>
          }
        />
      ) : isEmpty ? (
        <EmptyState
          icon="building"
          title={t('structure.empty.title')}
          body={t('structure.empty.body')}
          action={
            <Can action={Actions.BusinessStructureManage}>
              <Button onClick={() => setAddContext({ kind: 'organization' })}>
                {t('structure.addOrganization')}
              </Button>
            </Can>
          }
        />
      ) : (
        <Card className="overflow-hidden">{renderTree(tree)}</Card>
      )}

      <AddNodeDrawer context={addContext} onClose={() => setAddContext(null)} />

      <ConfirmDialog
        open={archiveTarget !== null}
        title={t('structure.archive')}
        message={t('structure.archiveConfirm', {
          kind: archiveTarget ? t(`structure.kinds.${archiveTarget.kind}`) : '',
        })}
        confirmLabel={t('structure.archive')}
        tone="danger"
        loading={archiveMutation.isPending}
        onCancel={() => setArchiveTarget(null)}
        onConfirm={() => archiveTarget && archiveMutation.mutate(archiveTarget)}
      />
    </>
  );
}
