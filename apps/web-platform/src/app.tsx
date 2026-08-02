import { useState } from 'react';
import { CloudLoginScreen, hasCloudSession, ThemeProvider } from '@nexora/ui';

import { ProvisionTenantPage } from './features/tenants/provision-tenant-page.js';

export function App() {
  const [authenticated, setAuthenticated] = useState(() => hasCloudSession());
  return (
    <ThemeProvider>
      {authenticated ? (
        <ProvisionTenantPage />
      ) : (
        <CloudLoginScreen onAuthenticated={() => setAuthenticated(true)} />
      )}
    </ThemeProvider>
  );
}
