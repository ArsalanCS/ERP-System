import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { authApi } from '../api';
import { ApiError } from '@/shared/api/client';
import { Button } from '@/shared/ui/Button';
import { Spinner } from '@/shared/ui/Spinner';
import { LanguageSwitch } from '@/components/shell/LanguageSwitch';

type Step = 1 | 2 | 3 | 'done';

const CURRENCIES = ['SAR', 'AED', 'USD'] as const;
const LANGUAGES = ['en', 'ar'] as const;
const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function slugify(value: string): string {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 40);
}

function passwordScore(v: string): number {
  let s = 0;
  if (v.length >= 8) s++;
  if (/[A-Z]/.test(v) && /[a-z]/.test(v)) s++;
  if (/[0-9]/.test(v)) s++;
  if (/[^A-Za-z0-9]/.test(v)) s++;
  return s;
}

export function SignupPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const [step, setStep] = useState<Step>(1);
  const [submitting, setSubmitting] = useState(false);
  const [resent, setResent] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Form state
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [workspaceName, setWorkspaceName] = useState('');
  const [slug, setSlug] = useState('');
  const [slugEdited, setSlugEdited] = useState(false);
  const [currency, setCurrency] = useState<(typeof CURRENCIES)[number]>('SAR');
  const [language, setLanguage] = useState<(typeof LANGUAGES)[number]>('en');
  const [agree, setAgree] = useState(true);

  const score = useMemo(() => passwordScore(password), [password]);
  const meterColors = ['', 'bg-red', 'bg-amber', 'bg-green', 'bg-green'];
  const pwHint =
    score >= 4 ? t('signup.pwHint.strong') : score >= 3 ? t('signup.pwHint.good') : t('signup.pwHint.weak');

  const onWorkspaceName = (v: string) => {
    setWorkspaceName(v);
    if (!slugEdited) setSlug(slugify(v));
  };

  const validateStep1 = () => {
    const e: Record<string, string> = {};
    if (!fullName.trim()) e['fullName'] = t('signup.errors.generic');
    if (!EMAIL_RE.test(email.trim())) e['email'] = t('auth.errors.emailInvalid');
    if (password.length < 8 || score < 2) e['password'] = t('signup.pwHint.weak');
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const validateStep2 = () => {
    const e: Record<string, string> = {};
    if (!workspaceName.trim()) e['workspaceName'] = t('signup.errors.generic');
    if (slug.length < 3) e['slug'] = t('signup.workspace.slugHint');
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const goReview = () => {
    if (validateStep2()) setStep(3);
  };

  const submit = async () => {
    if (!agree) {
      setError(t('signup.errors.agreeRequired'));
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await authApi.register({
        workspaceName: workspaceName.trim(),
        slug,
        baseCurrency: currency,
        language,
        fullName: fullName.trim(),
        email: email.trim(),
        password,
      });
      setStep('done');
    } catch (err) {
      if (err instanceof ApiError && err.code === 'AUTH_SLUG_TAKEN') {
        setErrors({ slug: t('signup.errors.slugTaken') });
        setStep(2);
      } else if (err instanceof ApiError && err.details?.[0]?.message) {
        setError(err.details[0].message);
      } else {
        setError(t('signup.errors.generic'));
      }
    } finally {
      setSubmitting(false);
    }
  };

  const resend = async () => {
    try {
      await authApi.resendVerification(slug, email.trim());
      setResent(true);
    } catch {
      /* best-effort */
    }
  };

  const stepIndex = step === 'done' ? 4 : step;

  return (
    <div className="min-h-screen bg-canvas">
      <nav className="mx-auto flex max-w-[1080px] items-center justify-between px-6 py-5">
        <Link to="/" className="logo">
          <span className="logo-mark">
            <BoltIcon />
          </span>
          {t('app.brand')}
        </Link>
        <div className="flex items-center gap-3 text-[13px] text-ink-3">
          <LanguageSwitch />
          <span className="hidden sm:inline">
            {t('signup.loginPrompt')}{' '}
            <Link to="/login" className="font-semibold text-clay hover:underline">
              {t('signup.loginLink')}
            </Link>
          </span>
        </div>
      </nav>

      <div className="mx-auto mb-20 mt-2 max-w-[520px] px-6">
        {step !== 'done' && (
          <div className="mb-7 flex items-center">
            <StepDot n={1} cur={stepIndex} label={t('signup.steps.account')} />
            <StepLine done={stepIndex > 1} />
            <StepDot n={2} cur={stepIndex} label={t('signup.steps.workspace')} />
            <StepLine done={stepIndex > 2} />
            <StepDot n={3} cur={stepIndex} label={t('signup.steps.review')} />
          </div>
        )}

        <div className="card fade-up p-8">
          {step === 1 && (
            <>
              <h1 className="text-[22px] font-bold tracking-[-0.02em]">{t('signup.account.title')}</h1>
              <p className="mt-1.5 text-sm text-ink-3">{t('signup.account.subtitle')}</p>
              <div className="mt-6 flex flex-col gap-4">
                <Field label={t('signup.account.fullName')} error={errors['fullName']}>
                  <input
                    className="input"
                    autoFocus
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)}
                  />
                </Field>
                <Field label={t('signup.account.email')} error={errors['email']}>
                  <input
                    className="input"
                    type="email"
                    autoComplete="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                  />
                </Field>
                <Field label={t('signup.account.password')} error={errors['password']}>
                  <input
                    className="input"
                    type="password"
                    autoComplete="new-password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                  />
                  <div className="mt-1.5 flex gap-1.5">
                    {[1, 2, 3, 4].map((i) => (
                      <span
                        key={i}
                        className={`h-1 flex-1 rounded-full ${i <= score ? meterColors[score] : 'bg-stone-200'}`}
                      />
                    ))}
                  </div>
                  <span className="mt-1 block text-[12.5px] text-ink-4">{pwHint}</span>
                </Field>
                <Button block size="lg" onClick={() => validateStep1() && setStep(2)} className="mt-1">
                  {t('signup.account.continue')}
                </Button>
              </div>
            </>
          )}

          {step === 2 && (
            <>
              <h1 className="text-[22px] font-bold tracking-[-0.02em]">{t('signup.workspace.title')}</h1>
              <p className="mt-1.5 text-sm text-ink-3">{t('signup.workspace.subtitle')}</p>
              <div className="mt-6 flex flex-col gap-4">
                <Field label={t('signup.workspace.name')} error={errors['workspaceName']}>
                  <input
                    className="input"
                    autoFocus
                    value={workspaceName}
                    onChange={(e) => onWorkspaceName(e.target.value)}
                  />
                </Field>
                <Field label={t('signup.workspace.slug')} error={errors['slug']} hint={t('signup.workspace.slugHint')}>
                  <input
                    className="input font-mono tracking-[0.02em]"
                    value={slug}
                    onChange={(e) => {
                      setSlugEdited(true);
                      setSlug(slugify(e.target.value));
                    }}
                  />
                </Field>
                <Field label={t('signup.workspace.currency')}>
                  <Segmented options={CURRENCIES} value={currency} onChange={setCurrency} />
                </Field>
                <Field label={t('signup.workspace.language')}>
                  <Segmented
                    options={LANGUAGES}
                    value={language}
                    onChange={setLanguage}
                    render={(l) => (l === 'en' ? 'English' : 'العربية')}
                  />
                </Field>
                <div className="mt-1 flex gap-3">
                  <Button variant="outline" size="lg" onClick={() => setStep(1)}>
                    {t('signup.workspace.back')}
                  </Button>
                  <Button size="lg" block onClick={goReview}>
                    {t('signup.workspace.continue')}
                  </Button>
                </div>
              </div>
            </>
          )}

          {step === 3 && (
            <>
              <h1 className="text-[22px] font-bold tracking-[-0.02em]">{t('signup.review.title')}</h1>
              <p className="mt-1.5 text-sm text-ink-3">{t('signup.review.subtitle')}</p>
              {error && (
                <div className="mt-4 rounded-md border border-red-100 bg-red-100 px-3.5 py-2.5 text-[13px] font-medium text-red">
                  {error}
                </div>
              )}
              <div className="mt-5 overflow-hidden rounded-md border border-stone-200">
                <ReviewRow k={t('signup.review.admin')} v={fullName} />
                <ReviewRow k={t('signup.review.email')} v={email} />
                <ReviewRow k={t('signup.review.workspace')} v={workspaceName} />
                <ReviewRow k={t('signup.review.address')} v={slug} mono />
                <ReviewRow k={t('signup.review.currency')} v={currency} />
                <ReviewRow
                  k={t('signup.review.plan')}
                  v={<span className="chip chip-clay">{t('signup.review.trial')}</span>}
                />
              </div>
              <label className="mt-4 flex items-start gap-2.5 text-[13.5px] text-ink-2">
                <input
                  type="checkbox"
                  checked={agree}
                  onChange={(e) => setAgree(e.target.checked)}
                  className="mt-0.5 accent-clay"
                />
                <span>{t('signup.review.agree')}</span>
              </label>
              <div className="mt-5 flex gap-3">
                <Button variant="outline" size="lg" onClick={() => setStep(2)} disabled={submitting}>
                  {t('signup.review.back')}
                </Button>
                <Button size="lg" block onClick={submit} disabled={submitting}>
                  {submitting && <Spinner size={15} className="border-white/40 border-t-white" />}
                  {t('signup.review.create')}
                </Button>
              </div>
            </>
          )}

          {step === 'done' && (
            <div className="py-2 text-center">
              <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-green-100 text-green">
                <MailIcon />
              </div>
              <h1 className="mt-5 text-[22px] font-bold tracking-[-0.02em]">{t('signup.success.title')}</h1>
              <p className="mx-auto mt-2 max-w-[34ch] text-sm text-ink-3">
                {t('signup.success.subtitle', { email })}
              </p>
              {resent && (
                <p className="mt-3 text-[13px] font-medium text-green">{t('signup.success.resent')}</p>
              )}
              <div className="mt-6 flex flex-col gap-2.5">
                <Button block size="lg" onClick={() => navigate('/login')}>
                  {t('signup.success.goLogin')}
                </Button>
                <Button variant="ghost" onClick={resend} disabled={resent}>
                  {t('signup.success.resend')}
                </Button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// ---- small presentational helpers -----------------------------------------

function Field({
  label,
  error,
  hint,
  children,
}: {
  label: string;
  error?: string | undefined;
  hint?: string | undefined;
  children: React.ReactNode;
}) {
  return (
    <div className="field">
      <label className="label">{label}</label>
      {children}
      {error ? (
        <span className="text-[12.5px] font-medium text-red">{error}</span>
      ) : hint ? (
        <span className="text-[12.5px] text-ink-4">{hint}</span>
      ) : null}
    </div>
  );
}

function Segmented<T extends string>({
  options,
  value,
  onChange,
  render,
}: {
  options: readonly T[];
  value: T;
  onChange: (v: T) => void;
  render?: (v: T) => string;
}) {
  return (
    <div className="flex gap-1 rounded-sm bg-stone-100 p-1">
      {options.map((opt) => (
        <button
          key={opt}
          type="button"
          onClick={() => onChange(opt)}
          className={`flex-1 rounded-[6px] py-2 text-[13.5px] font-semibold transition-colors ${
            value === opt ? 'bg-paper text-ink shadow-xs' : 'text-ink-3 hover:text-ink'
          }`}
        >
          {render ? render(opt) : opt}
        </button>
      ))}
    </div>
  );
}

function ReviewRow({ k, v, mono }: { k: string; v: React.ReactNode; mono?: boolean }) {
  return (
    <div className="flex items-center justify-between border-b border-stone-150 px-4 py-3 text-sm last:border-0">
      <span className="text-ink-4">{k}</span>
      <span className={`font-semibold ${mono ? 'font-mono tracking-[0.02em]' : ''}`}>{v}</span>
    </div>
  );
}

function StepDot({ n, cur, label }: { n: number; cur: number; label: string }) {
  const done = cur > n;
  const active = cur === n;
  return (
    <div className="flex flex-none items-center gap-2">
      <span
        className={`flex h-7 w-7 items-center justify-center rounded-full text-[13px] font-bold transition-colors ${
          done ? 'bg-green text-white' : active ? 'bg-clay text-white' : 'bg-stone-150 text-ink-4'
        }`}
      >
        {done ? '✓' : n}
      </span>
      <span className={`text-[13px] font-semibold ${active || done ? 'text-ink' : 'text-ink-4'}`}>{label}</span>
    </div>
  );
}

function StepLine({ done }: { done: boolean }) {
  return <span className={`mx-3 h-0.5 flex-1 rounded-full ${done ? 'bg-green' : 'bg-stone-200'}`} />;
}

function BoltIcon() {
  return (
    <svg viewBox="0 0 24 24" width={18} height={18} fill="none" stroke="currentColor" strokeWidth={1.7} strokeLinecap="round" strokeLinejoin="round">
      <path d="M13 3 5 13h5l-1 8 8-10h-5z" />
    </svg>
  );
}

function MailIcon() {
  return (
    <svg viewBox="0 0 24 24" width={30} height={30} fill="none" stroke="currentColor" strokeWidth={1.8} strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="5" width="18" height="14" rx="2" />
      <path d="m3 7 9 6 9-6" />
    </svg>
  );
}
