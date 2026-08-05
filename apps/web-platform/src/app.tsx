import { useState } from 'react';
import {
  BrandMark,
  Button,
  clearCloudSession,
  CloudLoginScreen,
  CreatedByFooter,
  hasCloudSession,
  Icon,
  SideNav,
  ThemeProvider,
  TopBar,
  type SideNavItem,
} from '@nexora/ui';

import { BusinessTemplateManagementPage } from './features/business-templates/business-template-management-page.js';
import { createBusinessTemplatesApi } from './features/business-templates/business-templates-api.js';
import { InstallationsPanelPage } from './features/installations/installations-panel-page.js';
import { OnboardingChecklistPage } from './features/onboarding/onboarding-checklist-page.js';
import { PublishReleasePage } from './features/releases/publish-release-page.js';
import { GrantSupportAccessPage } from './features/support-access/grant-support-access-page.js';
import { ProvisionTenantPage } from './features/tenants/provision-tenant-page.js';

type PlatformSection = 'provision' | 'installations' | 'templates' | 'support-access' | 'releases';

const PLATFORM_NAV_ITEMS: readonly SideNavItem[] = [
  { group: 'Plataforma' },
  { id: 'provision', label: 'Provisionar estabelecimento', icon: 'add_business' },
  { id: 'installations', label: 'Instalações', icon: 'dns' },
  { id: 'templates', label: 'Modelos de negócio', icon: 'category' },
  { id: 'support-access', label: 'Solicitar acesso de suporte', icon: 'support_agent' },
  { id: 'releases', label: 'Versões', icon: 'system_update_alt' },
];

const businessTemplatesApi = createBusinessTemplatesApi();

/**
 * Navegação da plataforma. Cada item leva para uma tela real já implementada.
 */
export function App() {
  const pathname = globalThis.location?.pathname ?? '/';
  const [authenticated, setAuthenticated] = useState(() => hasCloudSession());
  const onboardingTenantId = getOnboardingTenantId(pathname);

  return (
    <ThemeProvider>
      {!authenticated ? (
        <CloudLoginScreen onAuthenticated={() => setAuthenticated(true)} />
      ) : onboardingTenantId ? (
        <OnboardingChecklistPage tenantId={onboardingTenantId} />
      ) : (
        <PlatformShell
          onLogout={() => {
            clearCloudSession();
            setAuthenticated(false);
          }}
        />
      )}
    </ThemeProvider>
  );
}

function PlatformShell({ onLogout }: Readonly<{ onLogout: () => void }>) {
  const [section, setSection] = useState<PlatformSection>('provision');

  return (
    <div className="db-console">
      <SideNav
        variant="dark"
        brand={<BrandMark inverse subtitle="Plataforma" />}
        items={PLATFORM_NAV_ITEMS}
        activeId={section}
        onSelect={(id) => setSection(id as PlatformSection)}
        footer={<span className="db-sidenav__foot-note">Replay Studio · plataforma</span>}
      />
      <div className="db-console__main">
        <TopBar
          left={
            <nav className="db-crumbs" aria-label="Trilha de navegação">
              <span>Plataforma</span>
              <Icon name="chevron_right" size={16} />
              <span className="db-crumbs__current">{sectionLabel(section)}</span>
            </nav>
          }
          right={
            <Button type="button" variant="ghost" size="sm" onClick={onLogout}>
              Sair
            </Button>
          }
        />
        <div className="db-console__content">
          <div className="db-console__column">
            {section === 'provision' ? <ProvisionTenantPage /> : null}
            {section === 'installations' ? <InstallationsPanelPage /> : null}
            {section === 'templates' ? <BusinessTemplateManagementPage api={businessTemplatesApi} /> : null}
            {section === 'support-access' ? <GrantSupportAccessPage /> : null}
            {section === 'releases' ? <PublishReleasePage /> : null}
            <CreatedByFooter />
          </div>
        </div>
      </div>
    </div>
  );
}

function getOnboardingTenantId(pathname: string): string | undefined {
  const match = pathname.match(/^\/tenants\/([^/]+)\/onboarding\/?$/);
  return match?.[1];
}

function sectionLabel(section: PlatformSection): string {
  const item = PLATFORM_NAV_ITEMS.find((candidate) => candidate.id === section);
  return typeof item?.label === 'string' ? item.label : 'Plataforma';
}
