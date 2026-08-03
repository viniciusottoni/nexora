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

import { ProvisionTenantPage } from './features/tenants/provision-tenant-page.js';

/**
 * Navegação da plataforma. O kit `ui_kits/admin-nexora` prevê também Instâncias, Saúde do parque,
 * Versões e Auditoria — nenhuma dessas telas existe em código ainda, e item de menu que não leva a
 * lugar nenhum é desonesto com quem opera. Entram aqui quando existirem.
 */
const PLATFORM_NAV_ITEMS: readonly SideNavItem[] = [
  { group: 'Plataforma' },
  { id: 'provision', label: 'Provisionar estabelecimento', icon: 'add_business' },
];

export function App() {
  const [authenticated, setAuthenticated] = useState(() => hasCloudSession());

  if (!authenticated) {
    return (
      <ThemeProvider>
        <CloudLoginScreen onAuthenticated={() => setAuthenticated(true)} />
      </ThemeProvider>
    );
  }

  return (
    <ThemeProvider>
      <div className="db-console">
        <SideNav
          variant="dark"
          brand={<BrandMark inverse subtitle="Plataforma" />}
          items={PLATFORM_NAV_ITEMS}
          activeId="provision"
          footer={<span className="db-sidenav__foot-note">Replay Studio · plataforma</span>}
        />
        <div className="db-console__main">
          <TopBar
            left={
              <nav className="db-crumbs" aria-label="Trilha de navegação">
                <span>Plataforma</span>
                <Icon name="chevron_right" size={16} />
                <span className="db-crumbs__current">Provisionar estabelecimento</span>
              </nav>
            }
            right={
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  clearCloudSession();
                  setAuthenticated(false);
                }}
              >
                Sair
              </Button>
            }
          />
          <div className="db-console__content">
            <div className="db-console__column">
              <ProvisionTenantPage />
              <CreatedByFooter />
            </div>
          </div>
        </div>
      </div>
    </ThemeProvider>
  );
}
