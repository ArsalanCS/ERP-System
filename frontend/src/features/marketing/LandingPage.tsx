import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useSession } from '@/shared/rbac/session';
import { LanguageSwitch } from '@/components/shell/LanguageSwitch';

interface NamedDesc {
  name: string;
  desc: string;
}
interface Layer {
  name: string;
  nodes: string[];
}
interface Stat {
  value: string;
  label: string;
}

/**
 * Public marketing landing page (built from the Design Kickoff `Landing.html`).
 * Bilingual AR/EN + RTL via i18n; CTAs route to signup / login. Reachable at "/".
 */
export function LandingPage() {
  const { t } = useTranslation();
  const { status } = useSession();
  const authed = status === 'authenticated';

  const modules = t('landing.modules.items', { returnObjects: true }) as NamedDesc[];
  const features = t('landing.platform.features', { returnObjects: true }) as NamedDesc[];
  const layers = t('landing.platform.layers', { returnObjects: true }) as Layer[];
  const chips = t('landing.compliance.chips', { returnObjects: true }) as string[];
  const stats = t('landing.stats.items', { returnObjects: true }) as Stat[];
  const layerTone = ['bg-green-100 text-green', 'bg-violet-100 text-violet', 'bg-amber-100 text-amber', 'bg-blue-100 text-blue'];
  const modTone = [
    'bg-clay-100 text-clay',
    'bg-blue-100 text-blue',
    'bg-green-100 text-green',
    'bg-violet-100 text-violet',
    'bg-amber-100 text-amber',
    'bg-clay-100 text-clay',
  ];

  return (
    <div className="min-h-screen bg-paper">
      {/* NAV */}
      <header className="sticky top-0 z-50 border-b border-stone-150 bg-paper/80 backdrop-blur-md">
        <div className="mx-auto flex max-w-[1180px] items-center gap-7 px-7 py-3.5">
          <Link to="/" className="logo">
            <span className="logo-mark"><Bolt /></span>
            {t('app.brand')}
          </Link>
          <nav className="ms-2 hidden gap-6 md:flex">
            <a href="#modules" className="text-sm font-medium text-ink-2 hover:text-clay">{t('landing.nav.modules')}</a>
            <a href="#platform" className="text-sm font-medium text-ink-2 hover:text-clay">{t('landing.nav.platform')}</a>
            <a href="#compliance" className="text-sm font-medium text-ink-2 hover:text-clay">{t('landing.nav.compliance')}</a>
          </nav>
          <div className="ms-auto flex items-center gap-2">
            <LanguageSwitch />
            {authed ? (
              <Link to="/admin/overview" className="btn btn-primary">{t('nav.overview')}<ArrowRight /></Link>
            ) : (
              <>
                <Link to="/login" className="btn btn-ghost">{t('landing.nav.login')}</Link>
                <Link to="/signup" className="btn btn-primary">{t('landing.nav.startTrial')}</Link>
              </>
            )}
          </div>
        </div>
      </header>

      {/* HERO */}
      <section className="relative overflow-hidden">
        <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(60%_50%_at_50%_-8%,rgba(201,100,66,0.07),transparent_70%)]" />
        <div className="relative mx-auto max-w-[1180px] px-7 pb-8 pt-16">
          <span className="inline-flex items-center gap-2 rounded-full border border-clay-200 bg-clay-100 py-1.5 pe-3.5 ps-2 text-[13px] font-semibold text-clay-active">
            <span className="rounded-full bg-clay px-2 py-0.5 text-[11px] font-bold text-white">{t('landing.hero.tag')}</span>
            {t('landing.hero.eyebrow')}
          </span>
          <h1 className="mt-5 max-w-[15ch] font-serif text-[42px] font-medium leading-[1.04] tracking-[-0.02em] sm:text-[58px]">
            {t('landing.hero.title')}
          </h1>
          <p className="mt-5 max-w-[52ch] text-[18px] leading-relaxed text-ink-3">{t('landing.hero.lead')}</p>
          <div className="mt-7 flex flex-wrap gap-3">
            <Link to="/signup" className="btn btn-primary btn-lg">{t('landing.hero.ctaPrimary')}<ArrowRight /></Link>
            <Link to="/login" className="btn btn-outline btn-lg">{t('landing.hero.ctaSecondary')}</Link>
          </div>
          <div className="mt-7 flex items-center gap-3.5 text-[13.5px] text-ink-4">
            <div className="flex">
              {['SM', 'NT', 'TF', 'QC'].map((s, i) => (
                <span
                  key={s}
                  className="avatar -ms-2.5 h-[30px] w-[30px] border-2 border-paper text-[11px] first:ms-0"
                  style={{ background: ['#3b6ea5', '#4f7a55', '#c96442', '#6b5ca5'][i] }}
                >
                  {s}
                </span>
              ))}
            </div>
            <span>{t('landing.hero.trust')}</span>
          </div>

          {/* product mock */}
          <div className="mt-12 overflow-hidden rounded-2xl border border-stone-200 bg-paper shadow-xl">
            <div className="flex h-10 items-center gap-1.5 border-b border-stone-150 bg-stone-50 px-4">
              <span className="h-2.5 w-2.5 rounded-full bg-stone-300" />
              <span className="h-2.5 w-2.5 rounded-full bg-stone-300" />
              <span className="h-2.5 w-2.5 rounded-full bg-stone-300" />
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-[200px_1fr]">
              <aside className="hidden border-e border-stone-150 bg-paper p-3 sm:block">
                {['Dashboard', 'Finance', 'Invoicing', 'Inventory', 'Help Desk'].map((m, i) => (
                  <div
                    key={m}
                    className={`mb-0.5 flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-[12.5px] font-medium ${
                      i === 0 ? 'bg-clay-100 text-clay-active' : 'text-ink-3'
                    }`}
                  >
                    <span className="h-3.5 w-3.5 rounded-[4px] bg-current opacity-70" />
                    {m}
                  </div>
                ))}
              </aside>
              <div className="bg-canvas p-5">
                <div className="mb-3.5 text-[13px] font-bold text-ink-3">{t('landing.hero.mockTitle')}</div>
                <div className="grid grid-cols-3 gap-3">
                  {[
                    { l: t('landing.hero.mockRevenue'), v: '4.82M' },
                    { l: t('landing.hero.mockCash'), v: '11.3M' },
                    { l: t('landing.hero.mockReceivables'), v: '2.1M' },
                  ].map((s) => (
                    <div key={s.l} className="rounded-xl border border-stone-200 bg-paper p-3.5">
                      <div className="text-[11px] font-medium text-ink-4">{s.l}</div>
                      <div className="mt-1.5 text-[19px] font-bold tracking-[-0.02em]">
                        <span className="text-[11px] font-semibold text-ink-4">SAR</span> {s.v}
                      </div>
                    </div>
                  ))}
                </div>
                <div className="mt-3.5 rounded-xl border border-stone-200 bg-paper p-4">
                  <div className="flex justify-between text-[11px]">
                    <b className="font-bold">{t('landing.hero.mockChart')}</b>
                  </div>
                  <div className="mt-3.5 flex h-[150px] items-end gap-2.5">
                    {[44, 52, 40, 62, 48, 70, 56].map((base, i) => (
                      <div key={i} className="flex flex-1 flex-col justify-end gap-1">
                        <span className="rounded-t bg-stone-200" style={{ height: base }} />
                        <span className="rounded-t bg-clay" style={{ height: [34, 46, 50, 58, 64, 72, 80][i] }} />
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* LOGOS BAND */}
      <div className="border-y border-stone-150 py-10">
        <div className="mx-auto max-w-[1180px] px-7">
          <p className="text-center text-[12.5px] font-semibold uppercase tracking-[0.08em] text-ink-4">{t('landing.band')}</p>
          <div className="mt-5 flex flex-wrap items-center justify-center gap-x-11 gap-y-4 opacity-70">
            {['Najd Logistics', 'Saudi Modern Industries', 'Tahweel Foods', 'Qassim Cement', 'Riyadh Power'].map((n) => (
              <span key={n} className="text-[17px] font-bold tracking-[-0.02em] text-ink-3">{n}</span>
            ))}
          </div>
        </div>
      </div>

      {/* MODULES */}
      <section id="modules" className="py-20">
        <div className="mx-auto max-w-[1180px] px-7">
          <div className="mx-auto max-w-[640px] text-center">
            <div className="text-[13px] font-bold uppercase tracking-[0.06em] text-clay">{t('landing.modules.eyebrow')}</div>
            <h2 className="mx-auto mt-3 font-serif text-[34px] font-medium leading-tight tracking-[-0.02em]">{t('landing.modules.title')}</h2>
            <p className="mx-auto mt-4 text-[17px] leading-relaxed text-ink-3">{t('landing.modules.subtitle')}</p>
          </div>
          <div className="mt-11 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {modules.map((m, i) => (
              <div key={m.name} className="rounded-lg border border-stone-200 bg-paper p-5.5 transition hover:-translate-y-0.5 hover:border-stone-300 hover:shadow-md">
                <div className={`flex h-10 w-10 items-center justify-center rounded-[11px] ${modTone[i % modTone.length]}`}>
                  <Square />
                </div>
                <h3 className="mt-4 text-[16px] font-semibold">{m.name}</h3>
                <p className="mt-1.5 text-[13.5px] leading-relaxed text-ink-3">{m.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* PLATFORM */}
      <section id="platform" className="border-y border-stone-150 bg-stone-50 py-20">
        <div className="mx-auto grid max-w-[1180px] items-center gap-14 px-7 lg:grid-cols-2">
          <div>
            <div className="text-[13px] font-bold uppercase tracking-[0.06em] text-clay">{t('landing.platform.eyebrow')}</div>
            <h2 className="mt-3 font-serif text-[32px] font-medium leading-tight tracking-[-0.02em]">{t('landing.platform.title')}</h2>
            <p className="mt-4 text-[17px] leading-relaxed text-ink-3">{t('landing.platform.subtitle')}</p>
            <div className="mt-6 flex flex-col gap-4">
              {features.map((f) => (
                <div key={f.name} className="flex gap-3">
                  <span className="flex h-7 w-7 flex-none items-center justify-center rounded-lg bg-clay-100 text-clay"><Check /></span>
                  <div>
                    <h4 className="text-[15px] font-semibold">{f.name}</h4>
                    <p className="mt-0.5 text-[13.5px] text-ink-3">{f.desc}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
          <div className="rounded-xl bg-stone-900 p-7">
            {layers.map((layer, i) => (
              <div key={layer.name} className="mb-3 rounded-[13px] border border-white/10 bg-white/[0.05] p-4 last:mb-0">
                <div className="flex items-center gap-2.5 text-[12px] font-bold uppercase tracking-[0.05em]">
                  <span className={`flex h-5.5 w-5.5 items-center justify-center rounded-[7px] text-[11px] ${layerTone[i]}`}>{i + 1}</span>
                  <span className="text-white/90">{layer.name}</span>
                </div>
                <div className="mt-3 flex flex-wrap gap-1.5">
                  {layer.nodes.map((node) => (
                    <span key={node} className="rounded-[7px] border border-white/10 bg-white/[0.08] px-2.5 py-1 text-[12px] font-medium text-white/85">{node}</span>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* COMPLIANCE */}
      <section id="compliance" className="py-20">
        <div className="mx-auto max-w-[1180px] px-7">
          <div className="grid items-center gap-12 rounded-2xl border border-clay-200 bg-gradient-to-br from-clay-tint to-clay-100 p-8 sm:p-12 lg:grid-cols-[1.2fr_1fr]">
            <div>
              <span className="inline-flex items-center gap-2 rounded-full border border-clay-200 bg-paper px-3.5 py-1.5 text-[13px] font-semibold text-clay-active shadow-xs"><Shield />{t('landing.compliance.badge')}</span>
              <h2 className="mt-4 font-serif text-[30px] font-medium leading-tight tracking-[-0.02em]">{t('landing.compliance.title')}</h2>
              <p className="mt-4 text-[16px] leading-relaxed text-ink-3">{t('landing.compliance.subtitle')}</p>
              <div className="mt-6 flex flex-wrap gap-2.5">
                {chips.map((c) => (
                  <span key={c} className="chip chip-clay"><span className="dot" />{c}</span>
                ))}
              </div>
            </div>
            <div className="overflow-hidden rounded-lg border border-stone-200 bg-paper shadow-lg">
              <div className="flex items-center justify-between border-b border-stone-150 px-4 py-3.5">
                <div>
                  <div className="text-[11px] font-semibold text-ink-4">{t('landing.compliance.invoiceLabel')}</div>
                  <div className="text-[15px] font-bold">INV-2026-0418</div>
                </div>
                <span className="chip chip-green"><span className="dot" />{t('landing.compliance.invoiceStatus')}</span>
              </div>
              <div className="px-4 py-4">
                <Row k={t('landing.compliance.invoiceCustomer')} v="Najd Logistics Co." />
                <Row k={t('landing.compliance.invoiceSubtotal')} v="SAR 86,000.00" />
                <Row k={t('landing.compliance.invoiceVat')} v="SAR 12,900.00" />
                <div className="my-2 border-t border-stone-150" />
                <Row k={t('landing.compliance.invoiceTotal')} v="SAR 98,900.00" strong />
                <div className="mt-3.5 flex items-center gap-3.5 border-t border-dashed border-stone-200 pt-3.5">
                  <div className="h-[68px] w-[68px] rounded-lg border-4 border-white shadow-[0_0_0_1px_var(--color-stone-200)] [background:repeating-conic-gradient(var(--color-stone-900)_0%_25%,#fff_0%_50%)_50%/8px_8px]" />
                  <div className="text-[11.5px] leading-relaxed text-ink-4">{t('landing.compliance.invoiceScan')}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* STATS */}
      <section className="pb-8">
        <div className="mx-auto grid max-w-[1180px] grid-cols-2 gap-7 px-7 text-center lg:grid-cols-4">
          {stats.map((s) => (
            <div key={s.label}>
              <div className="font-serif text-[40px] font-medium tracking-[-0.02em] text-ink">{s.value}</div>
              <div className="mt-1 text-[13.5px] text-ink-3">{s.label}</div>
            </div>
          ))}
        </div>
      </section>

      {/* CTA */}
      <section className="py-12">
        <div className="mx-auto max-w-[1180px] px-7">
          <div className="relative overflow-hidden rounded-[28px] bg-stone-900 px-8 py-16 text-center">
            <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(50%_90%_at_50%_120%,rgba(201,100,66,0.4),transparent_70%)]" />
            <h2 className="relative font-serif text-[32px] font-medium tracking-[-0.02em] text-white sm:text-[40px]">{t('landing.cta.title')}</h2>
            <p className="relative mt-3.5 text-[18px] text-white/60">{t('landing.cta.subtitle')}</p>
            <div className="relative mt-7 flex flex-wrap justify-center gap-3">
              <Link to="/signup" className="btn btn-primary btn-lg">{t('landing.cta.primary')}</Link>
              <Link to="/login" className="btn btn-lg border border-white/20 bg-white/10 text-white hover:bg-white/15">{t('landing.cta.secondary')}</Link>
            </div>
          </div>
        </div>
      </section>

      {/* FOOTER */}
      <footer className="mt-12 border-t border-stone-150 py-12">
        <div className="mx-auto max-w-[1180px] px-7">
          <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-[1.6fr_1fr_1fr_1fr]">
            <div>
              <Link to="/" className="logo text-[18px]"><span className="logo-mark"><Bolt /></span>{t('app.brand')}</Link>
              <p className="mt-3.5 max-w-[30ch] text-sm leading-relaxed text-ink-3">{t('landing.footer.tagline')}</p>
            </div>
            <FootCol title={t('landing.footer.product')} links={[t('landing.nav.modules'), t('landing.nav.platform'), t('landing.nav.compliance'), t('landing.nav.pricing')]} />
            <FootCol title={t('landing.footer.company')} links={[t('landing.footer.about'), t('landing.footer.customers'), t('landing.footer.careers'), t('landing.footer.contact')]} />
            <FootCol title={t('landing.footer.resources')} links={[t('landing.footer.documentation'), t('landing.footer.apiReference'), t('landing.footer.status'), t('landing.footer.security')]} />
          </div>
          <div className="mt-11 flex flex-col items-center justify-between gap-3 border-t border-stone-150 pt-6 text-[13px] text-ink-4 sm:flex-row">
            <span>© 2026 {t('app.brand')}. {t('landing.footer.rights')}</span>
            <span className="flex gap-5">
              <a href="#" onClick={(e) => e.preventDefault()} className="hover:text-clay">{t('landing.footer.privacy')}</a>
              <a href="#" onClick={(e) => e.preventDefault()} className="hover:text-clay">{t('landing.footer.terms')}</a>
            </span>
          </div>
        </div>
      </footer>
    </div>
  );
}

function FootCol({ title, links }: { title: string; links: string[] }) {
  return (
    <div>
      <h5 className="text-[12px] font-bold uppercase tracking-[0.06em] text-ink-4">{title}</h5>
      {links.map((l) => (
        <a key={l} href="#" onClick={(e) => e.preventDefault()} className="mt-2.5 block text-sm text-ink-2 hover:text-clay">{l}</a>
      ))}
    </div>
  );
}

function Row({ k, v, strong }: { k: string; v: string; strong?: boolean }) {
  return (
    <div className={`flex justify-between py-1.5 text-[13px] ${strong ? 'text-[15px] font-bold' : 'text-ink-2'}`}>
      <span className={strong ? '' : 'text-ink-4'}>{k}</span>
      <span className={strong ? '' : 'tnum'}>{v}</span>
    </div>
  );
}

// ---- inline icons ----------------------------------------------------------
function Bolt() {
  return <svg viewBox="0 0 24 24" width={18} height={18} fill="none" stroke="currentColor" strokeWidth={1.7} strokeLinecap="round" strokeLinejoin="round"><path d="M13 3 5 13h5l-1 8 8-10h-5z" /></svg>;
}
function ArrowRight() {
  return <svg viewBox="0 0 24 24" width={17} height={17} fill="none" stroke="currentColor" strokeWidth={1.8} strokeLinecap="round" strokeLinejoin="round" className="rtl:-scale-x-100"><path d="M5 12h14M13 6l6 6-6 6" /></svg>;
}
function Check() {
  return <svg viewBox="0 0 24 24" width={16} height={16} fill="none" stroke="currentColor" strokeWidth={2}><path d="m5 12 5 5 9-10" /></svg>;
}
function Square() {
  return <svg viewBox="0 0 24 24" width={20} height={20} fill="none" stroke="currentColor" strokeWidth={1.7}><rect x="4" y="4" width="16" height="16" rx="3" /><path d="M9 9h6M9 13h6" /></svg>;
}
function Shield() {
  return <svg viewBox="0 0 24 24" width={16} height={16} fill="none" stroke="currentColor" strokeWidth={1.7}><path d="M12 3 5 6v5c0 4.5 3 7.8 7 9 4-1.2 7-4.5 7-9V6z" /><path d="M9.5 12l1.8 1.8 3.2-3.4" /></svg>;
}
