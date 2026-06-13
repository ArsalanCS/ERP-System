import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { authApi } from '../api';
import { Input } from '@/shared/ui/Input';
import { Button } from '@/shared/ui/Button';
import { AuthCard, FormBanner } from '../components/AuthCard';

const schema = z.object({
  workspaceSlug: z.string().min(1, 'auth.errors.slugRequired'),
  email: z.string().min(1, 'auth.errors.emailRequired').email('auth.errors.emailInvalid'),
});

type ForgotForm = z.infer<typeof schema>;

export function ForgotPasswordPage() {
  const { t } = useTranslation();
  const [sent, setSent] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotForm>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: ForgotForm) => {
    // Always reports success — the backend never reveals whether the account exists.
    try {
      await authApi.forgotPassword(values.workspaceSlug.trim(), values.email.trim());
    } finally {
      setSent(true);
    }
  };

  return (
    <AuthCard
      title={t('auth.forgot.title')}
      subtitle={t('auth.forgot.subtitle')}
      footer={
        <Link to="/login" className="font-semibold text-clay hover:underline">
          {t('auth.backToLogin')}
        </Link>
      }
    >
      {sent ? (
        <FormBanner tone="success">{t('auth.forgot.sent')}</FormBanner>
      ) : (
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3.5" noValidate>
          <Input
            label={t('auth.fields.workspace')}
            autoComplete="organization"
            autoFocus
            error={errors.workspaceSlug && t(errors.workspaceSlug.message!)}
            {...register('workspaceSlug')}
          />
          <Input
            label={t('auth.fields.email')}
            type="email"
            autoComplete="username"
            error={errors.email && t(errors.email.message!)}
            {...register('email')}
          />
          <Button type="submit" block disabled={isSubmitting} className="mt-1">
            {isSubmitting ? t('common.loading') : t('auth.forgot.submit')}
          </Button>
        </form>
      )}
    </AuthCard>
  );
}
