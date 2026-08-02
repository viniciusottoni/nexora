import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: true,
  timeout: 30_000,
  use: { trace: 'retain-on-failure', screenshot: 'only-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'pnpm --filter @db/web-admin dev --host 127.0.0.1 --port 49173',
      url: 'http://127.0.0.1:49173',
      reuseExistingServer: false,
    },
    {
      command: 'pnpm --filter @db/web-platform dev --host 127.0.0.1 --port 49174',
      url: 'http://127.0.0.1:49174',
      reuseExistingServer: false,
    },
  ],
});
