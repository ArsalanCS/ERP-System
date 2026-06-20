import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Drawer, Input, Select, Button, Spinner } from '@/shared/ui';
import { useToast } from '@/shared/ui/toast-context';
import { ApiError } from '@/shared/api/client';
import { structureApi, structureKeys, StructureNodeType, type StructureNodeDto } from './api';

/** Describes what the drawer should create or edit. */
export interface NodeEditorContext {
  mode: 'create' | 'edit';
  parentId: string | null;
  parentName?: string | undefined;
  allowedTypes: StructureNodeType[];
  node?: StructureNodeDto | undefined;
}

interface AddNodeDrawerProps {
  context: NodeEditorContext | null;
  onClose: () => void;
}

export function AddNodeDrawer({ context, onClose }: AddNodeDrawerProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();

  const [name, setName] = useState('');
  const [code, setCode] = useState('');
  const [nodeType, setNodeType] = useState<StructureNodeType>(StructureNodeType.Department);
  const [description, setDescription] = useState('');
  const [codeError, setCodeError] = useState<string | null>(null);

  useEffect(() => {
    if (!context) return;
    setCodeError(null);
    if (context.mode === 'edit' && context.node) {
      setName(context.node.name);
      setCode(context.node.code);
      setNodeType(context.node.nodeType);
      setDescription(context.node.description ?? '');
    } else {
      setName('');
      setCode('');
      setNodeType(context.allowedTypes[0] ?? StructureNodeType.Department);
      setDescription('');
    }
  }, [context]);

  const mutation = useMutation({
    mutationFn: async () => {
      const trimmed = { name: name.trim(), description: description.trim() || null };
      if (context!.mode === 'edit') {
        await structureApi.updateNode(context!.node!.id, trimmed);
        return;
      }
      await structureApi.createNode({
        parentId: context!.parentId,
        nodeType,
        code: code.trim(),
        ...trimmed,
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: structureKeys.tree });
      toast.success(t(context!.mode === 'edit' ? 'structure.feedback.updated' : 'structure.feedback.created'));
      onClose();
    },
    onError: (err) => {
      if (err instanceof ApiError && err.details?.[0]) {
        setCodeError(err.details[0].message);
      } else if (err instanceof ApiError && err.message) {
        setCodeError(err.message);
      } else {
        toast.error(t('common.loadError'));
      }
    },
  });

  const isEdit = context?.mode === 'edit';
  const typeOptions = (context?.allowedTypes ?? []).map((tpe) => ({
    value: String(tpe),
    label: t(`structure.nodeTypes.${tpe}`),
  }));
  const canSubmit = name.trim().length > 0 && (isEdit || code.trim().length > 0);

  const title = !context
    ? ''
    : isEdit
      ? t('structure.editNode')
      : context.parentName
        ? t('structure.addChildTo', { parent: context.parentName })
        : t('structure.addOrganization');

  return (
    <Drawer
      open={context !== null}
      onClose={onClose}
      title={title}
      footer={
        <>
          <Button variant="outline" size="sm" onClick={onClose} disabled={mutation.isPending}>
            {t('common.cancel')}
          </Button>
          <Button size="sm" onClick={() => mutation.mutate()} disabled={mutation.isPending || !canSubmit}>
            {mutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}
            {t(isEdit ? 'common.save' : 'common.create')}
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        {!isEdit && typeOptions.length > 1 && (
          <Select
            label={t('structure.fields.type')}
            value={String(nodeType)}
            onChange={(e) => setNodeType(Number(e.target.value) as StructureNodeType)}
            options={typeOptions}
          />
        )}
        <Input label={t('structure.fields.name')} value={name} onChange={(e) => setName(e.target.value)} autoFocus />
        <Input
          label={t('structure.fields.code')}
          value={code}
          onChange={(e) => {
            setCode(e.target.value);
            setCodeError(null);
          }}
          readOnly={isEdit}
          error={codeError ?? undefined}
        />
        <Input
          label={t('structure.fields.description')}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
      </div>
    </Drawer>
  );
}
