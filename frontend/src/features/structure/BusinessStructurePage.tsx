import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PageHeader, Card, Button, Icon, LoadingBlock, EmptyState, ConfirmDialog } from '@/shared/ui';
import { Can } from '@/shared/rbac/Can';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { useToast } from '@/shared/ui/toast-context';
import {
  structureApi,
  structureKeys,
  CHILD_TYPES,
  StructureNodeType,
  type StructureNodeDto,
} from './api';
import { StructureTree } from './StructureTree';
import { AddNodeDrawer, type NodeEditorContext } from './AddNodeDrawer';
import { AssignMemberDialog } from './AssignMemberDialog';
import { NodeMembersDialog } from './NodeMembersDialog';

export function BusinessStructurePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { can } = usePermissions();
  const toast = useToast();
  const queryClient = useQueryClient();
  const canManage = can(Actions.BusinessStructureManage);
  const canAddMember = can(Actions.UsersManage);

  const [editor, setEditor] = useState<NodeEditorContext | null>(null);
  const [archiveTarget, setArchiveTarget] = useState<StructureNodeDto | null>(null);
  const [memberNode, setMemberNode] = useState<StructureNodeDto | null>(null);
  const [viewNode, setViewNode] = useState<StructureNodeDto | null>(null);

  const query = useQuery({ queryKey: structureKeys.tree, queryFn: structureApi.tree });

  const archiveMutation = useMutation({
    mutationFn: (node: StructureNodeDto) => structureApi.archiveNode(node.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: structureKeys.tree });
      toast.success(t('structure.feedback.archived'));
      setArchiveTarget(null);
    },
    onError: (err) => {
      toast.error(err instanceof Error && err.message ? err.message : t('common.loadError'));
      setArchiveTarget(null);
    },
  });

  const addOrganization = () =>
    setEditor({ mode: 'create', parentId: null, allowedTypes: [StructureNodeType.Organization] });

  const addChild = (node: StructureNodeDto) =>
    setEditor({
      mode: 'create',
      parentId: node.id,
      parentName: node.name,
      allowedTypes: CHILD_TYPES[node.nodeType],
    });

  const editNode = (node: StructureNodeDto) =>
    setEditor({ mode: 'edit', parentId: node.parentId, allowedTypes: [], node });

  const nodes = query.data?.nodes ?? [];
  const isEmpty = query.data && nodes.length === 0;

  return (
    <>
      <PageHeader
        title={t('structure.title')}
        subtitle={t('structure.subtitle')}
        actions={
          <Can action={Actions.BusinessStructureManage}>
            <Button leadingIcon={<Icon name="plus" size={16} />} onClick={addOrganization}>
              {t('structure.addOrganization')}
            </Button>
          </Can>
        }
      />

      {query.isLoading ? (
        <Card className="card-pad">
          <LoadingBlock label={t('common.loading')} />
        </Card>
      ) : query.isError || !query.data ? (
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
              <Button onClick={addOrganization}>{t('structure.addOrganization')}</Button>
            </Can>
          }
        />
      ) : (
        <Card className="overflow-hidden">
          <StructureTree
            nodes={nodes}
            canManage={canManage}
            canAddMember={canAddMember}
            canAddChild={(node) => CHILD_TYPES[node.nodeType].length > 0}
            onAddChild={addChild}
            onAddMember={setMemberNode}
            onViewMembers={setViewNode}
            onEdit={editNode}
            onArchive={setArchiveTarget}
          />
        </Card>
      )}

      <AddNodeDrawer context={editor} onClose={() => setEditor(null)} />

      <NodeMembersDialog
        node={viewNode}
        canAddMember={canAddMember}
        onClose={() => setViewNode(null)}
        onAddMember={(node) => {
          setViewNode(null);
          setMemberNode(node);
        }}
      />

      <AssignMemberDialog
        node={memberNode}
        onClose={() => setMemberNode(null)}
        onAddNew={(node) => {
          setMemberNode(null);
          // Jump to the Users page and auto-open the new-user flow pre-placed here.
          navigate('/admin/users', { state: { createUserPlacementNodeId: node.id } });
        }}
        onAssigned={() => {
          setMemberNode(null);
          void queryClient.invalidateQueries({ queryKey: structureKeys.tree });
          toast.success(t('structure.assign.assigned'));
        }}
      />

      <ConfirmDialog
        open={archiveTarget !== null}
        title={t('structure.archive')}
        message={t('structure.archiveConfirm', { name: archiveTarget?.name ?? '' })}
        confirmLabel={t('structure.archive')}
        tone="danger"
        loading={archiveMutation.isPending}
        onCancel={() => setArchiveTarget(null)}
        onConfirm={() => archiveTarget && archiveMutation.mutate(archiveTarget)}
      />
    </>
  );
}
