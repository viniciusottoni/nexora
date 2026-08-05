import { expect, test, type Page } from '@playwright/test';

/**
 * US-141 · Provisionamento autoatendido — cobre os cenários Gherkin "Checklist de implantação" e
 * "Validação antes da ativação" do lado do painel da Replay (web-platform). Arquivo NOVO (não edita
 * tests/e2e/tenant-provisioning.spec.ts/foundation.spec.ts) — mesma convenção de mock de rede
 * (`page.route`) já usada nos dois.
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Admin de plataforma' },
  permissions: ['tenant:manage'],
};

const TENANT_ID = '0198aabb-2222-7000-8000-000000000001';

async function mockLogin(page: Page) {
  await page.route('**/v1/auth/login', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(session),
    });
  });
}

function mockOnboardingStatus(page: Page, overrides: Partial<Record<string, string>> = {}) {
  const statuses: Record<string, string> = {
    TENANT_CREATED: 'DONE',
    BRANDING: 'DONE',
    MENU: 'IN_PROGRESS',
    TABLES: 'PENDING',
    EDGE_INSTALL: 'PENDING',
    PAYMENT_CONFIG: 'PENDING',
    TRAINING: 'PENDING',
    PILOT: 'PENDING',
    ACTIVATION: 'PENDING',
    ...overrides,
  };

  return page.route(`**/v1/platform/tenants/${TENANT_ID}/onboarding`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        steps: Object.entries(statuses).map(([key, status]) => ({
          key,
          status,
          ...(key === 'MENU' ? { progress: { products: 44, expected: 60 } } : {}),
        })),
        startedAt: '2026-08-01T09:00:00.000Z',
        elapsedBusinessDays: 2,
      }),
    });
  });
}

test.describe('Checklist de implantação (US-141)', () => {
  test('Cenário "Checklist de implantação": mostra os nove passos com progresso quantificado', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockOnboardingStatus(page);

    await page.goto(`http://127.0.0.1:49174/tenants/${TENANT_ID}/onboarding`);
    await page.getByLabel('E-mail').fill('admin@example.com');
    await page.getByLabel('Senha').fill('senha-segura');
    await page.getByRole('button', { name: 'Entrar' }).click();

    await expect(page.getByRole('heading', { name: 'Checklist de implantação' })).toBeVisible();
    await expect(page.getByText('2/9')).toBeVisible();
    await expect(page.getByText('44 de 60 produtos')).toBeVisible();
    await expect(page.getByText(/2 dias úteis decorridos/)).toBeVisible();
  });

  test('Cenário "Validação antes da ativação": ativação bloqueada lista os passos pendentes', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockOnboardingStatus(page);

    await page.route(`**/v1/platform/tenants/${TENANT_ID}/activate`, async (route) => {
      await route.fulfill({
        status: 422,
        contentType: 'application/json',
        body: JSON.stringify({
          code: 'ONBOARDING_INCOMPLETE',
          detail: 'Ainda há passos pendentes no roteiro de implantação.',
          meta: { pendingItems: ['MENU', 'TABLES', 'EDGE_INSTALL', 'PAYMENT_CONFIG', 'TRAINING', 'PILOT'] },
        }),
      });
    });

    await page.goto(`http://127.0.0.1:49174/tenants/${TENANT_ID}/onboarding`);
    await page.getByLabel('E-mail').fill('admin@example.com');
    await page.getByLabel('Senha').fill('senha-segura');
    await page.getByRole('button', { name: 'Entrar' }).click();

    await page.getByRole('button', { name: 'Ativar estabelecimento' }).click();

    await expect(page.getByText('Ativação bloqueada')).toBeVisible();
    await expect(page.getByText('Ainda há passos pendentes no roteiro de implantação.')).toBeVisible();
    const banner = page.getByRole('status');
    await expect(banner.getByText('Cardápio', { exact: true })).toBeVisible();
    await expect(banner.getByText('Mesas', { exact: true })).toBeVisible();
  });
});
