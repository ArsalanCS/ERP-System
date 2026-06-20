import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Drawer, Input, Select, Button, Spinner } from '@/shared/ui';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { ApiError } from '@/shared/api/client';
import { structureApi, structureKeys, type StructureNodeDto } from '@/features/structure/api';
import { usersApi, userKeys } from './api';
import { RoleTreePicker } from './RoleTreePicker';
import type { CreateUserRequest, UpdateUserRequest } from './types';

type Mode = 'create' | 'edit';

interface UserFormDrawerProps {
  open: boolean;
  mode: Mode;
  userId?: string | undefined;
  /** Pre-selects a structure node as the new hire's placement (used by "add member"). */
  presetPlacementNodeId?: string | undefined;
  onClose: () => void;
  onSaved: (kind: 'created' | 'invited' | 'updated') => void;
}

interface FormShape {
  firstName: string;
  lastName: string;
  displayName: string;
  email: string;
  mobile: string;
  jobTitle: string;
  employeeNumber: string;
  preferredLanguage: string;
  timeZone: string;
  accessStartDate: string;
  accessExpiryDate: string;
}

const DEFAULTS: FormShape = {
  firstName: '',
  lastName: '',
  displayName: '',
  email: '',
  mobile: '',
  jobTitle: '',
  employeeNumber: '',
  preferredLanguage: 'en',
  timeZone: 'Asia/Riyadh',
  accessStartDate: '',
  accessExpiryDate: '',
};

const toIsoOrNull = (value: string): string | null => (value ? new Date(value).toISOString() : null);
const toDateInput = (value: string | null): string => (value ? value.slice(0, 10) : '');

/** Descendants of a node (DFS) with depth, for an indented placement picker. */
function descendantsOf(nodes: StructureNodeDto[], rootId: string): { node: StructureNodeDto; depth: number }[] {
  const byParent = new Map<string | null, StructureNodeDto[]>();
  for (const n of nodes) (byParent.get(n.parentId) ?? byParent.set(n.parentId, []).get(n.parentId)!).push(n);
  const out: { node: StructureNodeDto; depth: number }[] = [];
  const walk = (id: string, depth: number) => {
    for (const child of byParent.get(id) ?? []) {
      out.push({ node: child, depth });
      walk(child.id, depth + 1);
    }
  };
  walk(rootId, 1);
  return out;
}

export function UserFormDrawer({
  open,
  mode,
  userId,
  presetPlacementNodeId,
  onClose,
  onSaved,
}: UserFormDrawerProps) {
  const { t } = useTranslation();
  const { can } = usePermissions();
  const queryClient = useQueryClient();
  const canSeeRoles = can(Actions.AccessControlView);
  const canSeeStructure = can(Actions.BusinessStructureView);

  const [step, setStep] = useState(1);
  const [roleIds, setRoleIds] = useState<string[]>([]);
  const [orgId, setOrgId] = useState('');
  const [placementNodeId, setPlacementNodeId] = useState('');
  const [sendInvitation, setSendInvitation] = useState(true);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    trigger,
    formState: { errors },
  } = useForm<FormShape>({ defaultValues: DEFAULTS });

  const structureQuery = useQuery({
    queryKey: structureKeys.tree,
    queryFn: structureApi.tree,
    enabled: open && canSeeStructure,
  });
  const nodes = useMemo(() => structureQuery.data?.nodes ?? [], [structureQuery.data]);
  const roots = useMemo(() => nodes.filter((n) => n.parentId === null), [nodes]);
  const placementOptions = useMemo(() => {
    if (!orgId) return [];
    const org = nodes.find((n) => n.id === orgId);
    const rows = [{ node: org!, depth: 0 }, ...descendantsOf(nodes, orgId)].filter((r) => r.node);
    return rows.map(({ node, depth }) => ({
      value: node.id,
      label: `${'  '.repeat(depth)}${node.name} · ${t(`structure.nodeTypes.${node.nodeType}`)}`,
    }));
  }, [nodes, orgId, t]);

  const detailQuery = useQuery({
    queryKey: userKeys.detail(userId ?? ''),
    queryFn: () => usersApi.get(userId!),
    enabled: open && mode === 'edit' && !!userId,
  });

  useEffect(() => {
    if (!open) return;
    setFormError(null);
    setStep(1);
    if (mode === 'create') {
      reset(DEFAULTS);
      setRoleIds([]);
      setOrgId('');
      setPlacementNodeId(presetPlacementNodeId ?? '');
      setSendInvitation(true);
      return;
    }
    const d = detailQuery.data;
    if (d) {
      reset({
        firstName: d.firstName,
        lastName: d.lastName,
        displayName: d.displayName,
        email: d.email,
        mobile: d.mobile ?? '',
        jobTitle: d.jobTitle ?? '',
        employeeNumber: d.employeeNumber ?? '',
        preferredLanguage: d.preferredLanguage,
        timeZone: d.timeZone,
        accessStartDate: toDateInput(d.accessStartDate),
        accessExpiryDate: toDateInput(d.accessExpiryDate),
      });
      setRoleIds(d.roleIds);
      setPlacementNodeId(d.placementNodeId ?? '');
    }
  }, [open, mode, detailQuery.data, reset, presetPlacementNodeId]);

  // When editing, derive the organization from the saved placement node.
  useEffect(() => {
    if (!placementNodeId || !nodes.length) return;
    let current = nodes.find((n) => n.id === placementNodeId);
    while (current?.parentId) current = nodes.find((n) => n.id === current!.parentId);
    if (current) setOrgId(current.id);
  }, [placementNodeId, nodes]);

  const mutation = useMutation({
    mutationFn: async (values: FormShape) => {
      const employee = {
        mobile: values.mobile.trim() || null,
        jobTitle: values.jobTitle.trim() || null,
        employeeNumber: values.employeeNumber.trim() || null,
        placementNodeId: placementNodeId || null,
      };
      if (mode === 'create') {
        const body: CreateUserRequest = {
          email: values.email.trim(),
          firstName: values.firstName.trim(),
          lastName: values.lastName.trim(),
          preferredLanguage: values.preferredLanguage,
          timeZone: values.timeZone.trim() || null,
          ...employee,
          roleIds: canSeeRoles ? roleIds : undefined,
          sendInvitation,
        };
        await usersApi.create(body);
        return sendInvitation ? ('invited' as const) : ('created' as const);
      }
      const body: UpdateUserRequest = {
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        displayName: values.displayName.trim(),
        preferredLanguage: values.preferredLanguage,
        timeZone: values.timeZone.trim(),
        ...employee,
        accessStartDate: toIsoOrNull(values.accessStartDate),
        accessExpiryDate: toIsoOrNull(values.accessExpiryDate),
        roleIds: canSeeRoles ? roleIds : undefined,
      };
      await usersApi.update(userId!, body);
      return 'updated' as const;
    },
    onSuccess: (kind) => {
      void queryClient.invalidateQueries({ queryKey: userKeys.all });
      onSaved(kind);
    },
    onError: (err) => {
      setFormError(
        err instanceof ApiError && err.details?.[0]?.message
          ? err.details[0].message
          : err instanceof Error
            ? err.message
            : t('common.loadError'),
      );
    },
  });

  const req = { required: t('users.validation.required') };
  const loadingDetail = mode === 'edit' && detailQuery.isLoading;
  const isWizard = mode === 'create';
  const lastStep = 3;

  const next = async () => {
    // Validate the account fields before leaving step 1.
    if (step === 1) {
      const ok = await trigger(['firstName', 'lastName', ...(mode === 'create' ? (['email'] as const) : [])]);
      if (!ok) return;
    }
    setStep((s) => Math.min(s + 1, lastStep));
  };

  const accountSection = (
    <div className="flex flex-col gap-4">
      <div className="grid grid-cols-2 gap-3">
        <Input label={t('users.fields.firstName')} error={errors.firstName?.message} {...register('firstName', req)} />
        <Input label={t('users.fields.lastName')} error={errors.lastName?.message} {...register('lastName', req)} />
      </div>
      {mode === 'create' ? (
        <Input
          label={t('users.fields.email')}
          type="email"
          error={errors.email?.message}
          {...register('email', {
            required: t('users.validation.required'),
            pattern: { value: /^[^@\s]+@[^@\s]+\.[^@\s]+$/, message: t('users.validation.email') },
          })}
        />
      ) : (
        <Input label={t('users.fields.displayName')} error={errors.displayName?.message} {...register('displayName', req)} />
      )}
      <div className="grid grid-cols-2 gap-3">
        <Input label={t('users.fields.mobile')} {...register('mobile')} />
        <Input label={t('users.fields.jobTitle')} {...register('jobTitle')} />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <Input label={t('users.fields.employeeNumber')} {...register('employeeNumber')} />
        <Select
          label={t('users.fields.language')}
          options={[
            { value: 'en', label: t('language.en') },
            { value: 'ar', label: t('language.ar') },
          ]}
          {...register('preferredLanguage')}
        />
      </div>
      {mode === 'edit' && (
        <div className="grid grid-cols-2 gap-3">
          <Input label={t('users.fields.accessStart')} type="date" {...register('accessStartDate')} />
          <Input label={t('users.fields.accessExpiry')} type="date" {...register('accessExpiryDate')} />
        </div>
      )}
    </div>
  );

  const placementSection = (
    <div className="flex flex-col gap-4">
      <p className="text-[13px] text-ink-3">{t('users.placement.hint')}</p>
      {!canSeeStructure ? (
        <p className="text-[13px] text-ink-4">{t('users.placement.noAccess')}</p>
      ) : roots.length === 0 ? (
        <p className="text-[13px] text-ink-4">{t('users.placement.empty')}</p>
      ) : (
        <>
          <Select
            label={t('users.placement.organization')}
            placeholder={t('users.placement.selectOrg')}
            value={orgId}
            onChange={(e) => {
              setOrgId(e.target.value);
              setPlacementNodeId(e.target.value); // default placement = org root
            }}
            options={roots.map((r) => ({ value: r.id, label: r.name }))}
          />
          {orgId && (
            <Select
              label={t('users.placement.node')}
              value={placementNodeId}
              onChange={(e) => setPlacementNodeId(e.target.value)}
              options={placementOptions}
            />
          )}
        </>
      )}
    </div>
  );

  const rolesSection = (
    <div className="flex flex-col gap-3">
      {canSeeRoles ? (
        <div className="field">
          <span className="label">{t('users.fields.roles')}</span>
          <p className="-mt-1 text-[12px] text-ink-4">{t('users.rolesHint')}</p>
          <RoleTreePicker value={roleIds} onChange={setRoleIds} enabled={open && canSeeRoles} />
        </div>
      ) : (
        <p className="text-[13px] text-ink-4">{t('users.noRoles')}</p>
      )}
      {mode === 'create' && (
        <label className="flex cursor-pointer items-center gap-2.5 text-sm text-ink-2">
          <input
            type="checkbox"
            className="h-4 w-4 accent-clay"
            checked={sendInvitation}
            onChange={(e) => setSendInvitation(e.target.checked)}
          />
          {t('users.fields.sendInvitation')}
        </label>
      )}
    </div>
  );

  const STEP_LABELS = [t('users.steps.account'), t('users.steps.placement'), t('users.steps.roles')];

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={mode === 'create' ? t('users.newUser') : t('users.editUser')}
      subtitle={mode === 'edit' ? detailQuery.data?.email : t('users.subtitle')}
      footer={
        <>
          <Button variant="outline" size="sm" onClick={onClose} disabled={mutation.isPending}>
            {t('common.cancel')}
          </Button>
          {isWizard && step > 1 && (
            <Button variant="outline" size="sm" onClick={() => setStep((s) => s - 1)} disabled={mutation.isPending}>
              {t('common.back')}
            </Button>
          )}
          {isWizard && step < lastStep ? (
            <Button size="sm" onClick={next}>
              {t('common.next')}
            </Button>
          ) : (
            <Button
              size="sm"
              onClick={handleSubmit((v) => mutation.mutate(v))}
              disabled={mutation.isPending || loadingDetail}
            >
              {mutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}
              {mode === 'create' ? t('common.create') : t('common.save')}
            </Button>
          )}
        </>
      }
    >
      {loadingDetail ? (
        <div className="flex justify-center py-10">
          <Spinner size={24} />
        </div>
      ) : (
        <div className="flex flex-col gap-5">
          {formError && (
            <div className="rounded-md border border-red-100 bg-red-100 px-3.5 py-2.5 text-[13px] font-medium text-red">
              {formError}
            </div>
          )}

          {isWizard && (
            <div className="flex items-center gap-2 text-[12px] font-semibold">
              {STEP_LABELS.map((label, i) => (
                <div key={label} className="flex items-center gap-2">
                  <span
                    className={`flex h-6 w-6 items-center justify-center rounded-full text-[11px] ${
                      step === i + 1 ? 'bg-clay text-white' : step > i + 1 ? 'bg-green text-white' : 'bg-stone-150 text-ink-4'
                    }`}
                  >
                    {step > i + 1 ? '✓' : i + 1}
                  </span>
                  <span className={step >= i + 1 ? 'text-ink' : 'text-ink-4'}>{label}</span>
                  {i < STEP_LABELS.length - 1 && <span className="mx-1 h-px w-4 bg-stone-200" />}
                </div>
              ))}
            </div>
          )}

          {/* In edit mode all sections stack; in create mode show the active step. */}
          {(!isWizard || step === 1) && accountSection}
          {(!isWizard || step === 2) && placementSection}
          {(!isWizard || step === 3) && rolesSection}
        </div>
      )}
    </Drawer>
  );
}
