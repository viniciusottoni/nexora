import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    globals: true,
    environment: 'node',
    setupFiles: ['./vitest.setup.ts', './tests/setup-storage.ts'],
    // Backend TypeScript (apps/api-edge, apps/api-cloud, packages/db|domain|events|metrics) foi
    // removido — porte .NET completo em backend/ (ADR-036 a ADR-039). Este config agora cobre só
    // o frontend (apps/web-*) e os pacotes compartilhados restantes (packages/contracts, packages/ui).
    include: ['packages/**/*.test.{ts,tsx}', 'apps/**/*.test.{ts,tsx}'],
    exclude: ['**/node_modules/**'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json-summary'],
      include: ['packages/*/src/**/*.{ts,tsx}', 'apps/*/src/**/*.{ts,tsx}'],
      exclude: [
        '**/*.test.{ts,tsx}',
        '**/index.ts',
        '**/main.ts',
        '**/main.tsx',
        '**/app.tsx',
        '**/cloud-auth.tsx',
        '**/provision-tenant-page.tsx',
        '**/*-provider.tsx',
      ],
      thresholds: { lines: 70, functions: 70, branches: 70, statements: 70 },
    },
  },
});
