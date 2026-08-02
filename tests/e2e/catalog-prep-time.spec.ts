import { expect, test, type Page } from '@playwright/test';

/**
 * US-016 · Tempo de preparo e praça por produto — cobre os cenários Gherkin "Limiar específico
 * do produto" (tempo/limiares editáveis em linha) e "Comparativo estimado versus real" (painel de
 * divergência com sugestão de ajuste) na tela `PrepTimePage` (web-admin), plugada via
 * `PrepTimeSection` (apps/web-admin/src/prep-time/prep-time-section.tsx).
 *
 * `PrepTimeSection` monta a lista de variações client-side (produtos → variações de cada produto
 * → análise de tempo de preparo de cada uma), já que não existe endpoint de listagem agregada —
 * por isso este spec mocka `products`/`products/:id/variants`/`variants/:id/prep-time-analysis`
 * além das rotas reais de escrita (`PATCH .../prep-time`, `PATCH .../station`).
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Gestora Dona Betinha' },
  permissions: ['catalog:read', 'catalog:write'],
};

const variantId = '0198aabb-3333-7000-8000-000000000001';
const productId = '0198aabb-3333-7000-8000-000000000002';
const stationId = '0198aabb-3333-7000-8000-000000000003';

async function mockLogin(page: Page) {
  await page.route('**/v1/auth/login', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(session),
    });
  });
}

async function mockAdminShell(page: Page) {
  await page.route('**/v1/devices', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [] }),
    }),
  );
  await page.route('**/v1/roles', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [], permissionCatalog: [] }),
    }),
  );
  await page.route('**/v1/catalog/categories', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [] }),
    }),
  );
  await page.route('**/v1/catalog/modifier-groups', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [] }),
    }),
  );
  await page.route('**/v1/catalog/stations', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        items: [
          {
            id: stationId,
            code: 'FORNO',
            name: 'Forno',
            color: 'red',
            capacitySlots: null,
            isBottleneck: true,
            position: 0,
            isActive: true,
            linkedProductCount: 1,
          },
        ],
      }),
    }),
  );
  await page.route('**/v1/catalog/products', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        items: [
          {
            id: productId,
            categoryId: '0198aabb-3333-7000-8000-000000000009',
            categoryName: 'Pizzas Salgadas',
            stationId: null,
            stationName: null,
            name: 'Pizza Mussarela',
            description: null,
            ingredientsText: null,
            allergens: [],
            imageUrl: null,
            position: 0,
            isActive: true,
            isAvailable: true,
            allowsFractions: false,
            maxFractions: 1,
          },
        ],
      }),
    }),
  );
  await page.route(`**/v1/catalog/products/${productId}/variants`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        items: [
          {
            id: variantId,
            productId,
            name: 'Grande',
            sku: null,
            sizeCode: 'G',
            prepMinutes: 12,
            isDefault: true,
            isActive: true,
            currentPrice: null,
            currentPriceChannel: null,
          },
        ],
      }),
    }),
  );
  await page.route(`**/v1/catalog/variants/${variantId}/prep-time-analysis`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        variantId,
        configuredMinutes: 12,
        effectiveWarnMinutes: 15,
        warnMinutesInherited: true,
        effectiveCriticalMinutes: 25,
        criticalMinutesInherited: true,
        actualAvgMinutes: 16.4,
        actualP90Minutes: null,
        sampleSize: 340,
        suggestion: 16,
        note: null,
      }),
    }),
  );
}

async function loginAndOpenPrepTime(page: Page) {
  await mockLogin(page);
  await mockAdminShell(page);
  await page.goto('/');
  await page.getByLabel('E-mail').fill('gestora@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await page.getByRole('button', { name: 'Tempo e praça' }).click();
  await expect(
    page.getByRole('heading', { name: 'Tempo de preparo e praça por produto' }),
  ).toBeVisible();
  await expect(page.getByText('Pizza Mussarela')).toBeVisible();
}

test.describe('Tempo de preparo e praça por produto (US-016)', () => {
  test('gestor define tempo de preparo e limiares de uma variação', async ({ page }) => {
    await loginAndOpenPrepTime(page);

    let patchBody: unknown;
    await page.route('**/v1/catalog/variants/*/prep-time', async (route) => {
      expect(route.request().headers()['idempotency-key']).toBeTruthy();
      patchBody = route.request().postDataJSON();
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ variantId, prepMinutes: 14, warnMinutes: 15, criticalMinutes: 20 }),
      });
    });

    await page.getByLabel('Preparo (min)').fill('14');
    await page.getByLabel('Atenção (min)').fill('15');
    await page.getByLabel('Crítico (min)').fill('20');
    await page.getByRole('button', { name: 'Salvar tempo de preparo' }).click();

    await expect
      .poll(() => patchBody)
      .toEqual({ prepMinutes: 14, warnMinutes: 15, criticalMinutes: 20 });
  });

  test('cenário Gherkin "Roteamento pela praça": gestor vincula o produto a uma praça', async ({
    page,
  }) => {
    await loginAndOpenPrepTime(page);

    await page.route('**/v1/catalog/products/*/station', async (route) => {
      expect(route.request().headers()['idempotency-key']).toBeTruthy();
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ productId, stationId, stationCode: 'FORNO', stationName: 'Forno' }),
      });
    });

    await page.getByLabel('Praça de produção').selectOption(stationId);

    await expect(page.locator('.prep-time-station-tag')).toHaveText('Forno');
  });

  test('cenário Gherkin "Comparativo estimado versus real": divergência exibida com sugestão de ajuste', async ({
    page,
  }) => {
    await loginAndOpenPrepTime(page);

    await page.getByRole('button', { name: 'Ver comparativo estimado x real' }).click();

    await expect(page.getByText(/considere ajustar para 16 min/i)).toBeVisible();
    await expect(page.getByText('340 pedido(s)')).toBeVisible();
  });
});
