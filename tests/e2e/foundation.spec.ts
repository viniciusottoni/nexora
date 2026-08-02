import { expect, test, type Page } from '@playwright/test';

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Gestora' },
  permissions: ['user:read', 'user:write', 'tenant:manage'],
};

async function mockLogin(page: Page) {
  await page.route('**/v1/auth/login', async (route) => {
    expect(route.request().headers()['idempotency-key']).toBeTruthy();
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

test('gestora autentica e acessa papéis no admin', async ({ page }) => {
  await mockLogin(page);
  await page.route('**/v1/devices', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [] }),
    }),
  );
  await page.route('**/v1/roles', async (route) => {
    expect(route.request().headers().authorization).toBe('Bearer access-token');
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
  await expect(page.getByRole('heading', { name: 'Papéis e permissões' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Proprietário' })).toBeVisible();
});

test('admin de plataforma autentica e abre provisionamento', async ({ page }) => {
  await mockLogin(page);
  await page.goto('http://127.0.0.1:49174');
  await login(page);
  await expect(page.getByRole('heading', { name: 'Provisionar estabelecimento' })).toBeVisible();
});
