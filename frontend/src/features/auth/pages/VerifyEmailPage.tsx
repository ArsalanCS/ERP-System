import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { authApi } from '../api';
import { Button } from '@/shared/ui/Button';
import { Spinner } from '@/shared/ui/Spinner';
import { AuthCard, FormBanner } from '../components/AuthCard';

type Status = 'verifying' | 'success' | 'error';

/**
 * Confirms a self-service signup email-verification link. Always reachable
 * (outside PublicOnly) so an already-signed-in visitor can still land here.
 */
export function VerifyEmailPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const token = params.get('token') ?? '';
  const [status, setStatus] = useState<Status>(token ? 'verifying' : 'error');
  // The token is single-use; guard against React StrictMode's double effect.
  const started = useRef(false);

  useEffect(() => {
    if (!token || started.current) return;
    started.current = true;
    authApi
      .verifyEmail(token)
      .then(() => setStatus('success'))
      .catch(() => setStatus('error'));
  }, [token]);

  if (status === 'verifying') {
    return (
      <AuthCard title={t('verifyEmail.verifying')}>
        <div className="flex justify-center py-4">
          <Spinner size={28} />
        </div>
      </AuthCard>
    );
  }

  if (status === 'success') {
    return (
      <AuthCard title={t('verifyEmail.successTitle')} subtitle={t('verifyEmail.successBody')}>
        <Button block onClick={() => navigate('/login', { replace: true })}>
          {t('verifyEmail.goLogin')}
        </Button>
      </AuthCard>
    );
  }

  return (
    <AuthCard
      title={t('verifyEmail.failTitle')}
      footer={
        <Link to="/login" className="font-semibold text-clay hover:underline">
          {t('verifyEmail.backToLogin')}
        </Link>
      }
    >
      <FormBanner tone="error">
        {token ? t('verifyEmail.failBody') : t('verifyEmail.missingToken')}
      </FormBanner>
    </AuthCard>
  );
}
