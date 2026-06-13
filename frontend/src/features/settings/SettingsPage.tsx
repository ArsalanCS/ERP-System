import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  PageHeader,
  Card,
  CardHeader,
  Input,
  Select,
  Button,
  Spinner,
  LoadingBlock,
  EmptyState,
} from '@/shared/ui';
import { useToast } from '@/shared/ui/toast-context';
import { usePermissions } from '@/shared/rbac/usePermissions';
import { Actions } from '@/shared/rbac/permissions';
import { ApiError } from '@/shared/api/client';
import { settingsApi, settingsKeys, type UpdateWorkspaceSettingsRequest } from './api';

interface FormShape {
  name: string;
  legalName: string;
  country: string;
  defaultLanguage: string;
  timeZone: string;
  baseCurrency: string;
}

export function SettingsPage() {
  const { t } = useTranslation();
  const toast = useToast();
  const queryClient = useQueryClient();
  const { can } = usePermissions();
  const canManage = can(Actions.SettingsManage);

  const query = useQuery({ queryKey: settingsKeys.workspace, queryFn: settingsApi.get });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<FormShape>();

  useEffect(() => {
    const d = query.data;
    if (d) {
      reset({
        name: d.name,
        legalName: d.legalName ?? '',
        country: d.country ?? '',
        defaultLanguage: d.defaultLanguage,
        timeZone: d.timeZone,
        baseCurrency: d.baseCurrency,
      });
    }
  }, [query.data, reset]);

  const mutation = useMutation({
    mutationFn: (values: FormShape) => {
      const body: UpdateWorkspaceSettingsRequest = {
        name: values.name.trim(),
        legalName: values.legalName.trim() || null,
        defaultLanguage: values.defaultLanguage,
        timeZone: values.timeZone.trim(),
        baseCurrency: values.baseCurrency.trim(),
        country: values.country.trim() || null,
      };
      return settingsApi.update(body);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: settingsKeys.workspace });
      toast.success(t('settings.saved'));
    },
    onError: (err) =>
      toast.error(
        err instanceof ApiError && err.details?.[0]?.message
          ? err.details[0].message
          : t('common.loadError'),
      ),
  });

  const req = { required: t('users.validation.required') };

  return (
    <>
      <PageHeader title={t('settings.title')} subtitle={t('settings.subtitle')} />

      {query.isLoading ? (
        <Card padded>
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
      ) : (
        <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="flex max-w-3xl flex-col gap-5">
          <Card>
            <CardHeader title={t('settings.general')} />
            <div className="grid grid-cols-1 gap-4 p-5 sm:grid-cols-2">
              <Input label={t('settings.fields.name')} error={errors.name?.message} {...register('name', req)} disabled={!canManage} />
              <Input label={t('settings.fields.legalName')} {...register('legalName')} disabled={!canManage} />
              <Input label={t('settings.fields.slug')} value={query.data.slug} disabled readOnly />
              <Input label={t('settings.fields.country')} {...register('country')} disabled={!canManage} />
            </div>
          </Card>

          <Card>
            <CardHeader title={t('settings.localization')} />
            <div className="grid grid-cols-1 gap-4 p-5 sm:grid-cols-3">
              <Select
                label={t('settings.fields.defaultLanguage')}
                options={[
                  { value: 'en', label: t('language.en') },
                  { value: 'ar', label: t('language.ar') },
                ]}
                disabled={!canManage}
                {...register('defaultLanguage')}
              />
              <Input label={t('settings.fields.timeZone')} {...register('timeZone', req)} disabled={!canManage} />
              <Input label={t('settings.fields.baseCurrency')} {...register('baseCurrency', req)} disabled={!canManage} />
            </div>
          </Card>

          {canManage && (
            <div className="flex justify-end">
              <Button type="submit" disabled={mutation.isPending || !isDirty}>
                {mutation.isPending && <Spinner size={15} className="border-white/40 border-t-white" />}
                {t('settings.save')}
              </Button>
            </div>
          )}
        </form>
      )}
    </>
  );
}
