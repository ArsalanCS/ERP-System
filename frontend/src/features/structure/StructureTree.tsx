import { useTranslation } from 'react-i18next';
import { Icon, Badge, type IconName, type BadgeTone } from '@/shared/ui';
import { StructureNodeType, StructureStatus, type StructureNodeDto } from './api';

const TYPE_ICON: Record<StructureNodeType, IconName> = {
  [StructureNodeType.Organization]: 'building',
  [StructureNodeType.Department]: 'users',
  [StructureNodeType.Branch]: 'sitemap',
  [StructureNodeType.SubDepartment]: 'users',
  [StructureNodeType.Team]: 'user',
  [StructureNodeType.SubTeam]: 'user',
};

const STATUS_TONE: Record<StructureStatus, BadgeTone> = {
  [StructureStatus.Active]: 'green',
  [StructureStatus.Inactive]: 'neutral',
  [StructureStatus.Archived]: 'neutral',
};

interface StructureTreeProps {
  nodes: StructureNodeDto[];
  canManage: boolean;
  canAddMember: boolean;
  canAddChild: (node: StructureNodeDto) => boolean;
  onAddChild: (node: StructureNodeDto) => void;
  onAddMember: (node: StructureNodeDto) => void;
  onViewMembers: (node: StructureNodeDto) => void;
  onEdit: (node: StructureNodeDto) => void;
  onArchive: (node: StructureNodeDto) => void;
}

/** Indented row list of the business-structure node tree (depth = inset). */
export function StructureTree({
  nodes,
  canManage,
  canAddMember,
  canAddChild,
  onAddChild,
  onAddMember,
  onViewMembers,
  onEdit,
  onArchive,
}: StructureTreeProps) {
  const byParent = new Map<string | null, StructureNodeDto[]>();
  for (const n of nodes) {
    const key = n.parentId;
    (byParent.get(key) ?? byParent.set(key, []).get(key)!).push(n);
  }

  // Flatten depth-first so children render directly under their parent.
  const rows: { node: StructureNodeDto; depth: number }[] = [];
  const walk = (parentId: string | null, depth: number) => {
    for (const node of byParent.get(parentId) ?? []) {
      rows.push({ node, depth });
      walk(node.id, depth + 1);
    }
  };
  walk(null, 0);

  return (
    <>
      {rows.map(({ node, depth }) => (
        <NodeRow
          key={node.id}
          node={node}
          depth={depth}
          canManage={canManage}
          canAddMember={canAddMember}
          canAddChild={canAddChild}
          onAddChild={onAddChild}
          onAddMember={onAddMember}
          onViewMembers={onViewMembers}
          onEdit={onEdit}
          onArchive={onArchive}
        />
      ))}
    </>
  );
}

interface RowProps extends Omit<StructureTreeProps, 'nodes'> {
  node: StructureNodeDto;
  depth: number;
}

function NodeRow({
  node,
  depth,
  canManage,
  canAddMember,
  canAddChild,
  onAddChild,
  onAddMember,
  onViewMembers,
  onEdit,
  onArchive,
}: RowProps) {
  const { t } = useTranslation();
  const memberLabel =
    node.memberCount > 0
      ? t('structure.memberCount', { count: node.memberCount })
      : t('structure.noMembersShort');

  return (
    <div
      className="group flex items-center gap-3 border-b border-stone-100 px-3 py-2.5 last:border-0 hover:bg-stone-50"
      style={{ paddingInlineStart: 12 + depth * 22 }}
    >
      <span className="flex h-7 w-7 flex-none items-center justify-center rounded-md bg-stone-100 text-ink-3">
        <Icon name={TYPE_ICON[node.nodeType]} size={15} />
      </span>
      <button
        type="button"
        onClick={() => onViewMembers(node)}
        title={t('structure.members.view')}
        className="min-w-0 flex-1 text-start"
      >
        <div className="flex items-center gap-2">
          <span className="truncate font-semibold text-ink group-hover:text-clay">{node.name}</span>
          <span className="font-mono text-[11.5px] text-ink-4">{node.code}</span>
        </div>
        <div className="text-[12px] text-ink-4">
          {t(`structure.nodeTypes.${node.nodeType}`)} · {memberLabel}
        </div>
      </button>
      <Badge tone={STATUS_TONE[node.status]} dot={node.status === StructureStatus.Active}>
        {t(`structure.status.${node.status}`)}
      </Badge>
      {(canManage || canAddMember) && (
        <div className="flex items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100">
          {canAddMember && (
            <IconBtn title={t('structure.addMember')} icon="user" onClick={() => onAddMember(node)} />
          )}
          {canManage && canAddChild(node) && (
            <IconBtn title={t('structure.addChild')} icon="plus" onClick={() => onAddChild(node)} />
          )}
          {canManage && <IconBtn title={t('common.edit')} icon="edit" onClick={() => onEdit(node)} />}
          {canManage && node.status !== StructureStatus.Archived && (
            <IconBtn title={t('structure.archive')} icon="trash" danger onClick={() => onArchive(node)} />
          )}
        </div>
      )}
    </div>
  );
}

function IconBtn({
  title,
  icon,
  onClick,
  danger,
}: {
  title: string;
  icon: IconName;
  onClick: () => void;
  danger?: boolean;
}) {
  return (
    <button
      type="button"
      title={title}
      onClick={onClick}
      className={`flex h-8 w-8 items-center justify-center rounded-sm text-ink-4 hover:bg-stone-150 ${
        danger ? 'hover:text-red' : 'hover:text-ink'
      }`}
    >
      <Icon name={icon} size={15} />
    </button>
  );
}
