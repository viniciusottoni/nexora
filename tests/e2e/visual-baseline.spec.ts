import { expect, test, type Page } from '@playwright/test';

/**
 * Rede de segurança visual para o refresh de design system (ver CLAUDE.md › "Motion e
 * microinterações"). Não existia nenhuma captura de tela nos specs de e2e antes deste arquivo —
 * `toHaveScreenshot` grava o baseline no primeiro `--update-snapshots` e depois compara.
 * Cobre 1 tela representativa por app; não substitui revisão manual no navegador.
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Gestora' },
  permissions: ['user:read', 'user:write', 'tenant:manage'],
};

async function mockLogin(page: Page) {
  await page.route('**/v1/auth/login', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(session),
    });
  });
}

async function login(page: Page) {
  await page.getByLabel('E-mail').fill('gestora@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
}

test.describe('visual baseline', () => {
  test('web-admin: papéis e permissões', async ({ page }) => {
    await mockLogin(page);
    await page.route('**/v1/devices', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [] }),
      }),
    );
    await page.route('**/v1/roles', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: '0198aabb-1111-7000-8000-000000000010',
              code: 'OWNER',
              name: 'Proprietário',
              permissions: ['*'],
              system: true,
              userCount: 1,
            },
          ],
          permissionCatalog: [],
        }),
      });
    });
    await page.goto('http://127.0.0.1:49173');
    await login(page);
    await page.getByRole('button', { name: 'Papéis e permissões' }).click();
    await expect(page.getByRole('heading', { name: 'Proprietário' })).toBeVisible();
    await page.getByRole('main').evaluate((element) => {
      const scrollContainer = element.closest<HTMLElement>('.db-console__content');
      if (scrollContainer) scrollContainer.scrollTop = 0;
    });
    await expect(page).toHaveScreenshot('web-admin-roles.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });

  test('web-platform: provisionar estabelecimento', async ({ page }) => {
    await mockLogin(page);
    await page.route('**/v1/platform/summary', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          tenants: { total: 1, active: 1, attention: 0 },
          installations: { healthy: 0, degraded: 0, offline: 0 },
          pendingInvites: 0,
          generatedAt: new Date().toISOString(),
        }),
      });
    });
    await page.route('**/v1/platform/templates', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: [{ code: 'PIZZERIA', name: 'Pizzaria', version: 1 }],
        }),
      });
    });
    await page.goto('http://127.0.0.1:49174');
    await login(page);
    // Raiz (US-150) é a visão geral; "Novo estabelecimento" leva ao mesmo formulário fotografado aqui.
    await page.getByRole('button', { name: 'Novo estabelecimento' }).first().click();
    await expect(page.getByRole('heading', { name: 'Provisionar estabelecimento' })).toBeVisible();
    await expect(page.getByLabel('Modelo de negócio').locator('option')).toHaveCount(1);
    await page.getByRole('main').evaluate((element) => {
      const scrollContainer = element.closest<HTMLElement>('.db-console__content');
      if (scrollContainer) scrollContainer.scrollTop = 0;
    });
    await expect(page).toHaveScreenshot('web-platform-provision.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });

  test('web-pos: pareamento de dispositivo', async ({ page }) => {
    await page.goto('http://127.0.0.1:49175');
    await expect(page.getByRole('heading', { name: 'Autorizar dispositivo' })).toBeVisible();
    await expect(page).toHaveScreenshot('web-pos-device-pairing.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });

  test('web-kds: pareamento de dispositivo', async ({ page }) => {
    await page.goto('http://127.0.0.1:49176');
    await expect(page.getByRole('heading', { name: 'Autorizar dispositivo' })).toBeVisible();
    await expect(page).toHaveScreenshot('web-kds-device-pairing.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });

  test('web-menu: vitrine do cardápio', async ({ page }) => {
    await page.goto('http://127.0.0.1:49177');
    await expect(page.getByRole('heading', { name: 'Nosso cardápio' })).toBeVisible();
    await expect(page).toHaveScreenshot('web-menu-home.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });
});
