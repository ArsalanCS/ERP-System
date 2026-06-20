import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Drawer, Button, Icon, Spinner, EmptyState, Badge } from '@/shared/ui';
import { initialsOf } from '@/shared/rbac/session';
import { UserStatusBadge } from '@/features/users/UserStatusBadge';
import { structureApi, structureKeys, type StructureNodeDto } from './api';

interface NodeMembersDialogProps {
  /** The node whose members are shown; null closes the dialog. */
  node: StructureNodeDto | null;
  /** Whether the viewer may add members (shows the add CTA). */
  canAddMember: boolean;
  onClose: () => void;
  onAddMember: (node: StructureNodeDto) => void;
}

/** Read-only panel listing the users placed directly on a structure node. */
export function NodeMembersDialog({ node, canAddMember, onClose, onAddMember }: NodeMembersDialogProps) {
  const { t } = useTranslation();

  const query = useQuery({
    queryKey: node ? structureKeys.members(node.id) : ['structure', 'members', 'none'],
    queryFn: () => structureApi.members(node!.id),
    enabled: node !== null,
  });

  const members = query.data ?? [];

  return (
    <Drawer
      open={node !== null}
      onClose={onClose}
      title={node ? node.name : ''}
      subtitle={node ? t(`structure.nodeTypes.${node.nodeType}`) : undefined}
      footer={
        <>
          <Button variant="outline" size="sm" onClick={onClose}>
            {t('common.close')}
          </Button>
          {canAddMember && node && (
            <Button size="sm" leadingIcon={<Icon name="plus" size={15} />} onClick={() => onAddMember(node)}>
              {t('structure.addMember')}
            </Button>
          )}
        </>
      }
    >
      {query.isLoading ? (
        <div className="flex justify-center py-10">
          <Spinner size={24} />
        </div>
      ) : query.isError ? (
        <EmptyState
          icon="alert"
          title={t('common.loadError')}
          action={
            <Button variant="outline" size="sm" onClick={() => query.refetch()}>
              {t('common.retry')}
            </Button>
          }
        />
      ) : members.length === 0 ? (
        <EmptyState
          icon="users"
          title={t('structure.members.empty.title')}
          body={t('structure.members.empty.body')}
          action={
            canAddMember && node ? (
              <Button size="sm" onClick={() => onAddMember(node)}>
                {t('structure.addMember')}
              </Button>
            ) : undefined
          }
        />
      ) : (
        <ul className="flex flex-col divide-y divide-stone-100 rounded-md border border-stone-150">
          {members.map((m) => (
            <li key={m.userId} className="flex items-center gap-3 px-3.5 py-3">
              <span className="avatar !h-9 !w-9 !text-[12px]" style={{ background: 'var(--color-stone-700)' }}>
                {initialsOf(m.displayName)}
              </span>
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span className="truncate font-semibold text-ink">{m.displayName}</span>
                  {m.isManager && (
                    <Badge tone="clay">{t('structure.members.manager')}</Badge>
                  )}
                </div>
                <div className="truncate text-[12.5px] text-ink-4">{m.jobTitle || m.email}</div>
                {m.mobile && <div className="truncate text-[12px] text-ink-4">{m.mobile}</div>}
              </div>
              <UserStatusBadge status={m.status} />
            </li>
          ))}
        </ul>
      )}
    </Drawer>
  );
}
