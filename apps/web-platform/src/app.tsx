import { useEffect, useState } from 'react';
import {
  BrandMark,
  Button,
  clearCloudSession,
  CloudLoginScreen,
  CreatedByFooter,
  hasCloudSession,
  Icon,
  readCloudSessionClaims,
  SideNav,
  ThemeProvider,
  TopBar,
  type SideNavItem,
} from '@nexora/ui';
import type { PlatformSummaryResponse } from '@nexora/contracts';

import { BusinessTemplateManagementPage } from './features/business-templates/business-template-management-page.js';
import { createBusinessTemplatesApi } from './features/business-templates/business-templates-api.js';
import { InstallationsPanelPage } from './features/installations/installations-panel-page.js';
import { OnboardingChecklistPage } from './features/onboarding/onboarding-checklist-page.js';
import { PlatformOverviewPage } from './features/overview/platform-overview-page.js';
import { createPlatformSummaryApi } from './features/overview/platform-summary-api.js';
import { PublishReleasePage } from './features/releases/publish-release-page.js';
import { GrantSupportAccessPage } from './features/support-access/grant-support-access-page.js';
import { TenantDetailPage } from './features/tenants/tenant-detail-page.js';
import { ProvisionTenantPage } from './features/tenants/provision-tenant-page.js';
import { TenantsDirectoryPage } from './features/tenants/tenants-directory-page.js';
import { AccessDeniedPage } from './routing/access-denied-page.js';
import { reportAccessDenied, reportSessionExpired } from './routing/observability.js';
import { NotFoundPage } from './routing/not-found-page.js';
import {
  matchRoute,
  navItemIdForRoute,
  pathForRoute,
  pathForTenantDetail,
  type PlatformRouteId,
  type PlatformRouteMatch,
} from './routing/route-map.js';
import { usePathname } from './routing/use-pathname.js';

const PLATFORM_NAV_ITEMS: readonly SideNavItem[] = [
  { group: 'Plataforma' },
  { id: 'overview', label: 'Visão geral', icon: 'space_dashboard' },
  { id: 'tenants', label: 'Estabelecimentos', icon: 'storefront' },
  { id: 'installations', label: 'Instalações', icon: 'dns' },
  { id: 'support-access', label: 'Auditoria e suporte', icon: 'verified_user' },
  { group: 'Configuração da plataforma' },
  { id: 'business-templates', label: 'Modelos de negócio', icon: 'category' },
  { id: 'releases', label: 'Versões', icon: 'system_update_alt' },
];

const ROUTE_LABEL: Record<PlatformRouteId, string> = {
  overview: 'Visão geral',
  tenants: 'Estabelecimentos',
  'tenants-new': 'Novo estabelecimento',
  'tenant-detail': 'Estabelecimento',
  installations: 'Instalações',
  'support-access': 'Auditoria e suporte',
  'business-templates': 'Modelos de negócio',
  releases: 'Versões',
  onboarding: 'Implantação',
};

const summaryApi = createPlatformSummaryApi();
const businessTemplatesApi = createBusinessTemplatesApi();

/**
 * US-150 "Estrutura e navegação do painel de plataforma" — raiz do `web-platform`. Login sempre
 * primeiro (qualquer rota exige uma sessão). Só depois de autenticado é que o checklist de
 * implantação (US-141) desvia do shell administrativo: é uma rota de TENANT (o dono recém-
 * convidado com a PRÓPRIA sessão, não um administrador de plataforma), então nunca passa pela
 * policy `PlatformAdmin` nem pela sonda de `PlatformAdminGate`.
 */
export function App() {
  const [pathname] = usePathname();
  const onboardingTenantId = getOnboardingTenantId(pathname);
  const [authenticated, setAuthenticated] = useState(() => hasCloudSession());

  return (
    <ThemeProvider>
      {!authenticated ? (
        <CloudLoginScreen onAuthenticated={() => setAuthenticated(true)} />
      ) : onboardingTenantId ? (
        <OnboardingChecklistPage tenantId={onboardingTenantId} />
      ) : (
        <PlatformAdminGate
          onSignOut={() => {
            clearCloudSession();
            setAuthenticated(false);
          }}
        />
      )}
    </ThemeProvider>
  );
}

type GateState =
  | { readonly kind: 'checking' }
  | { readonly kind: 'denied' }
  | { readonly kind: 'ready'; readonly summary: PlatformSummaryResponse | undefined };

/**
 * US-150 §4, cenários "Acesso direto a uma rota protegida" e "Sessão expirada" — a ÚNICA fonte de
 * verdade sobre `PlatformAdmin` é a resposta do backend (nunca a claim decodificada isolada, ver
 * docstring de `PlatformSummaryApi`): uma sonda a `GET /v1/platform/summary` roda uma vez por
 * sessão, cobre qualquer rota (não só a atual) e decide entre renderizar o shell, negar acesso ou
 * voltar ao login — sem revelar nenhum dado antes disso (RN-015).
 */
function PlatformAdminGate({ onSignOut }: Readonly<{ onSignOut: () => void }>) {
  const [pathname, navigate] = usePathname();
  const [state, setState] = useState<GateState>({ kind: 'checking' });

  useEffect(() => {
    let cancelled = false;
    summaryApi
      .get()
      .then((summary) => {
        if (!cancelled) setState({ kind: 'ready', summary });
      })
      .catch((reason: unknown) => {
        if (cancelled) return;
        const status = (reason as { status?: number }).status;

        if (status === 401) {
          reportSessionExpired({ pathname });
          onSignOut();
          return;
        }

        if (status === 403) {
          reportAccessDenied({ pathname });
          setState({ kind: 'denied' });
          return;
        }

        // Sem `status` = falha de rede, não de autorização (US-150 §9 "sem conexão, mantém
        // apenas a estrutura visual"). A claim do token já decodificada localmente é o único
        // sinal disponível offline — nunca mais permissivo do que a resposta do backend seria.
        const claims = readCloudSessionClaims();
        setState(claims?.isPlatformAdmin ? { kind: 'ready', summary: undefined } : { kind: 'denied' });
      });
    return () => {
      cancelled = true;
    };
    // A sonda roda uma única vez por sessão autenticada — cobre toda rota, não só a de entrada.
  }, []);

  if (state.kind === 'checking') {
    return (
      <p className="db-loading" role="status">
        <span className="nx-spinner" aria-hidden="true" />
        Preparando a plataforma…
      </p>
    );
  }

  if (state.kind === 'denied') {
    return <AccessDeniedPage onSignOut={onSignOut} />;
  }

  return (
    <PlatformShell
      summary={state.summary}
      pathname={pathname}
      navigate={navigate}
      onSignOut={onSignOut}
    />
  );
}

function PlatformShell({
  summary,
  pathname,
  navigate,
  onSignOut,
}: Readonly<{
  summary: PlatformSummaryResponse | undefined;
  pathname: string;
  navigate: (path: string) => void;
  onSignOut: () => void;
}>) {
  const matched = matchRoute(pathname);
  const activeNavId = matched ? navItemIdForRoute(matched.routeId) : undefined;

  return (
    <div className="db-console">
      <SideNav
        variant="dark"
        brand={<BrandMark inverse subtitle="Plataforma" />}
        items={PLATFORM_NAV_ITEMS}
        {...(activeNavId ? { activeId: activeNavId } : {})}
        onSelect={(id) => navigate(pathForRoute(id as Exclude<PlatformRouteId, 'onboarding' | 'tenant-detail'>))}
        footer={<span className="db-sidenav__foot-note">Replay Studio · plataforma</span>}
      />
      <div className="db-console__main">
        <TopBar
          left={
            <nav className="db-crumbs" aria-label="Trilha de navegação">
              <span>Plataforma</span>
              <Icon name="chevron_right" size={16} />
              <span className="db-crumbs__current">
                {matched ? ROUTE_LABEL[matched.routeId] : 'Página não encontrada'}
              </span>
            </nav>
          }
          right={
            <Button type="button" variant="ghost" size="sm" onClick={onSignOut}>
              Sair
            </Button>
          }
        />
        <div className="db-console__content">
          <div className="db-console__column">
            {renderRoute(matched, { summary, navigate })}
            <CreatedByFooter />
          </div>
        </div>
      </div>
    </div>
  );
}

function renderRoute(
  matched: PlatformRouteMatch | undefined,
  ctx: { readonly summary: PlatformSummaryResponse | undefined; readonly navigate: (path: string) => void },
) {
  if (!matched) {
    return <NotFoundPage onGoToOverview={() => ctx.navigate(pathForRoute('overview'))} />;
  }

  switch (matched.routeId) {
    case 'overview':
      return (
        <PlatformOverviewPage
          summary={ctx.summary}
          onCreateTenant={() => ctx.navigate(pathForRoute('tenants-new'))}
          onOpenTenants={() => ctx.navigate(pathForRoute('tenants'))}
          onOpenInstallations={() => ctx.navigate(pathForRoute('installations'))}
          onOpenSupportAccess={() => ctx.navigate(pathForRoute('support-access'))}
        />
      );
    case 'tenants':
      return (
        <TenantsDirectoryPage
          onCreateTenant={() => ctx.navigate(pathForRoute('tenants-new'))}
          onOpenTenant={(tenantId) => ctx.navigate(pathForTenantDetail(tenantId))}
        />
      );
    case 'tenants-new':
      return <ProvisionTenantPage onDone={() => ctx.navigate(pathForRoute('tenants'))} />;
    case 'tenant-detail':
      return (
        <TenantDetailPage
          tenantId={matched.params.tenantId ?? ''}
          onBack={() => ctx.navigate(pathForRoute('tenants'))}
          onRequestSupportAccess={(tenantId) =>
            ctx.navigate(`${pathForRoute('support-access')}?tenantId=${encodeURIComponent(tenantId)}`)
          }
          onOpenInstallations={() => ctx.navigate(pathForRoute('installations'))}
          onOpenBusinessTemplates={() => ctx.navigate(pathForRoute('business-templates'))}
        />
      );
    case 'installations':
      return <InstallationsPanelPage />;
    case 'support-access':
      return <GrantSupportAccessPage />;
    case 'business-templates':
      return <BusinessTemplateManagementPage api={businessTemplatesApi} />;
    case 'releases':
      return <PublishReleasePage />;
    case 'onboarding':
      return null;
  }
}

function getOnboardingTenantId(pathname: string): string | undefined {
  const matched = matchRoute(pathname);
  return matched?.routeId === 'onboarding' ? matched.params.tenantId : undefined;
}
