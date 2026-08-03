import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: true,
  timeout: 30_000,
  use: {
    baseURL: 'http://127.0.0.1:49173',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    // Sem isso, `navigator.clipboard.writeText` (provision-tenant-page.tsx, "Copiar comando")
    // rejeita silenciosamente em Chromium headless por falta de permissão — o botão nunca
    // atualiza `copyStatus` e o teste trava esperando "Comando copiado.".
    permissions: ['clipboard-read', 'clipboard-write'],
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'pnpm --filter @nexora/web-admin dev --host 127.0.0.1 --port 49173',
      url: 'http://127.0.0.1:49173',
      reuseExistingServer: false,
    },
    {
      command: 'pnpm --filter @nexora/web-platform dev --host 127.0.0.1 --port 49174',
      url: 'http://127.0.0.1:49174',
      reuseExistingServer: false,
    },
    {
      command: 'pnpm --filter @nexora/web-pos dev --host 127.0.0.1 --port 49175',
      url: 'http://127.0.0.1:49175',
      reuseExistingServer: false,
    },
    {
      command: 'pnpm --filter @nexora/web-kds dev --host 127.0.0.1 --port 49176',
      url: 'http://127.0.0.1:49176',
      reuseExistingServer: false,
    },
    {
      command: 'pnpm --filter @nexora/web-menu dev --host 127.0.0.1 --port 49177',
      url: 'http://127.0.0.1:49177',
      reuseExistingServer: false,
    },
  ],
});
