import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { authApi } from '../api';
import { ApiError } from '@/shared/api/client';
import { Input } from '@/shared/ui/Input';
import { Button } from '@/shared/ui/Button';
import { AuthCard, FormBanner } from '../components/AuthCard';

const schema = z
  .object({
    newPassword: z.string().min(8, 'auth.errors.passwordWeak'),
    confirmPassword: z.string().min(1, 'auth.errors.confirmRequired'),
  })
  .refine((v) => v.newPassword === v.confirmPassword, {
    path: ['confirmPassword'],
    message: 'auth.errors.passwordMismatch',
  });

type ResetForm = z.infer<typeof schema>;

export function ResetPasswordPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const token = params.get('token') ?? '';
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ResetForm>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: ResetForm) => {
    setFormError(null);
    try {
      await authApi.resetPassword(token, values.newPassword);
      navigate('/login', { replace: true, state: { reset: true } });
    } catch (err) {
      setFormError(
        err instanceof ApiError && err.details?.[0]?.message
          ? err.details[0].message
          : t('auth.reset.failed'),
      );
    }
  };

  if (!token) {
    return (
      <AuthCard
        title={t('auth.reset.title')}
        footer={
          <Link to="/forgot-password" className="font-semibold text-clay hover:underline">
            {t('auth.reset.requestNew')}
          </Link>
        }
      >
        <FormBanner tone="error">{t('auth.reset.missingToken')}</FormBanner>
      </AuthCard>
    );
  }

  return (
    <AuthCard
      title={t('auth.reset.title')}
      subtitle={t('auth.reset.subtitle')}
      footer={
        <Link to="/login" className="font-semibold text-clay hover:underline">
          {t('auth.backToLogin')}
        </Link>
      }
    >
      {formError && <FormBanner tone="error">{formError}</FormBanner>}
      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3.5" noValidate>
        <Input
          label={t('auth.fields.newPassword')}
          type="password"
          autoComplete="new-password"
          autoFocus
          error={errors.newPassword && t(errors.newPassword.message!)}
          {...register('newPassword')}
        />
        <Input
          label={t('auth.fields.confirmPassword')}
          type="password"
          autoComplete="new-password"
          error={errors.confirmPassword && t(errors.confirmPassword.message!)}
          {...register('confirmPassword')}
        />
        <Button type="submit" block disabled={isSubmitting} className="mt-1">
          {isSubmitting ? t('common.loading') : t('auth.reset.submit')}
        </Button>
      </form>
    </AuthCard>
  );
}
