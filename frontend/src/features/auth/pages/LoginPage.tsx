import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { useSession } from '@/shared/rbac/session';
import { ApiError } from '@/shared/api/client';
import { Input } from '@/shared/ui/Input';
import { Button } from '@/shared/ui/Button';
import { AuthCard, FormBanner } from '../components/AuthCard';

interface FlashState {
  from?: { pathname: string };
  reset?: boolean;
}

const schema = z.object({
  workspaceSlug: z.string().min(1, 'auth.errors.slugRequired'),
  email: z.string().min(1, 'auth.errors.emailRequired').email('auth.errors.emailInvalid'),
  password: z.string().min(1, 'auth.errors.passwordRequired'),
});

type LoginForm = z.infer<typeof schema>;

export function LoginPage() {
  const { t } = useTranslation();
  const { login } = useSession();
  const navigate = useNavigate();
  const location = useLocation();
  const flash = location.state as FlashState | null;
  const [formError, setFormError] = useState<string | null>(null);
  // When the account has 2FA enabled, the backend asks for a code on the first try.
  const [twoFactorRequired, setTwoFactorRequired] = useState(false);
  const [code, setCode] = useState('');

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: LoginForm) => {
    setFormError(null);
    try {
      await login(
        values.workspaceSlug.trim(),
        values.email.trim(),
        values.password,
        twoFactorRequired ? code.trim() : undefined,
      );
      const to = flash?.from?.pathname ?? '/admin/overview';
      navigate(to, { replace: true });
    } catch (err) {
      if (err instanceof ApiError && err.code === 'AUTH_2FA_REQUIRED') {
        // Move to the code step; keep the entered credentials.
        setTwoFactorRequired(true);
        return;
      }
      if (err instanceof ApiError && err.code === 'AUTH_INVALID_2FA_CODE') {
        setFormError(t('auth.errors.invalid2fa'));
        return;
      }
      // Credential errors are deliberately generic on the backend (no enumeration).
      setFormError(
        err instanceof ApiError && err.status === 429
          ? t('auth.errors.rateLimited')
          : t('auth.errors.invalidCredentials'),
      );
    }
  };

  return (
    <AuthCard
      title={t('auth.login.title')}
      subtitle={t('auth.login.subtitle')}
      footer={
        <Link to="/forgot-password" className="font-semibold text-clay hover:underline">
          {t('auth.login.forgotLink')}
        </Link>
      }
    >
      {flash?.reset && !formError && (
        <FormBanner tone="success">{t('auth.reset.success')}</FormBanner>
      )}
      {twoFactorRequired && !formError && (
        <FormBanner tone="success">{t('auth.twoFactor.prompt')}</FormBanner>
      )}
      {formError && <FormBanner tone="error">{formError}</FormBanner>}
      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3.5" noValidate>
        <Input
          label={t('auth.fields.workspace')}
          autoComplete="organization"
          autoFocus
          readOnly={twoFactorRequired}
          error={errors.workspaceSlug && t(errors.workspaceSlug.message!)}
          {...register('workspaceSlug')}
        />
        <Input
          label={t('auth.fields.email')}
          type="email"
          autoComplete="username"
          readOnly={twoFactorRequired}
          error={errors.email && t(errors.email.message!)}
          {...register('email')}
        />
        <Input
          label={t('auth.fields.password')}
          type="password"
          autoComplete="current-password"
          readOnly={twoFactorRequired}
          error={errors.password && t(errors.password.message!)}
          {...register('password')}
        />
        {twoFactorRequired && (
          <Input
            label={t('auth.fields.twoFactorCode')}
            inputMode="numeric"
            autoComplete="one-time-code"
            autoFocus
            value={code}
            onChange={(e) => setCode(e.target.value)}
          />
        )}
        <Button type="submit" block disabled={isSubmitting} className="mt-1">
          {isSubmitting
            ? t('common.loading')
            : twoFactorRequired
              ? t('auth.twoFactor.verify')
              : t('auth.login.submit')}
        </Button>
      </form>
    </AuthCard>
  );
}
