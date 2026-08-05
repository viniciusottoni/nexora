import { expect, test, type Page } from '@playwright/test';

/**
 * US-014 · Preço por canal de venda — cobre o fluxo principal do painel web-admin: tabela de
 * preço por canal editada em linha (§10) e reajuste em massa por categoria com pré-visualização
 * antes de confirmar. Arquivo NOVO (não edita catalog-categories-products.spec.ts,
 * catalog-stations.spec.ts, foundation.spec.ts nem tenant-provisioning.spec.ts).
 *
 * `PriceTablePage` é plugada via `PricingSection` (apps/web-admin/src/pricing/pricing-section.tsx),
 * que deixa a gestora escolher produto → variação antes de editar a tabela de preços (o componente
 * opera sobre UMA variação por vez) — por isso, além dos endpoints de preço, este spec mocka
 * `categories`/`products`/`products/:id/variants`, que alimentam os seletores.
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Gestora' },
  permissions: ['catalog:read', 'catalog:write'],
};

const variantId = '0198aabb-5555-7000-8000-000000000001';
const productId = '0198aabb-4444-7000-8000-000000000001';
const categoryId = '0198aabb-3333-7000-8000-000000000001';

let priceTable = {
  variantId,
  productId,
  channels: [
    { channel: 'DineIn', amount: '45.00', isInherited: false, validFrom: '2026-01-01T00:00:00Z' },
    { channel: 'Delivery', amount: '52.00', isInherited: false, validFrom: '2026-01-01T00:00:00Z' },
    { channel: 'Takeout', amount: '45.00', isInherited: true, validFrom: '2026-01-01T00:00:00Z' },
    {
      channel: 'Marketplace',
      amount: '45.00',
      isInherited: true,
      validFrom: '2026-01-01T00:00:00Z',
    },
  ],
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
  await page.route('**/v1/catalog/stations', (route) =>
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
  await page.route('**/v1/catalog/categories', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        items: [
          {
            id: categoryId,
            name: 'Pizzas Salgadas',
            description: null,
            position: 0,
            isActive: true,
            productCount: 1,
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
            categoryId,
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
            currentPrice: '45.00',
            currentPriceChannel: 'DineIn',
          },
        ],
      }),
    }),
  );
}

async function login(page: Page) {
  await page.getByLabel('E-mail').fill('gestora@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
}

async function openPricingForVariant(page: Page) {
  await page.getByRole('button', { name: 'Preços' }).click();
  await page.getByLabel('Produto').selectOption(productId);
  await expect(page.getByLabel('Variação')).toHaveValue(variantId);
}

test('gestora ve o preco distinto do delivery, herda o preco base no balcao e recebe aviso de delivery mais barato', async ({
  page,
}) => {
  await mockLogin(page);
  await mockAdminShell(page);

  await page.route(`**/v1/catalog/variants/${variantId}/prices`, async (route) => {
    const request = route.request();

    if (request.method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(priceTable),
      });
      return;
    }

    if (request.method() === 'PUT') {
      expect(request.headers()['idempotency-key']).toBeTruthy();
      const payload = request.postDataJSON() as {
        prices: Array<{ channel: string; amount: string }>;
      };
      priceTable = {
        ...priceTable,
        channels: priceTable.channels.map((row) => {
          const changed = payload.prices.find((p) => p.channel === row.channel);
          return changed ? { ...row, amount: changed.amount, isInherited: false } : row;
        }),
      };
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(priceTable),
      });
      return;
    }

    await route.fallback();
  });

  await page.goto('http://127.0.0.1:49173');
  await login(page);

  // Cenário Gherkin "Preço distinto no delivery" (US-014 §4).
  await openPricingForVariant(page);
  await expect(page.getByLabel('Preço do canal Salão')).toHaveValue('45,00');
  await expect(page.getByLabel('Preço do canal Delivery')).toHaveValue('52,00');

  // Cenário Gherkin "Herança do preço base" (US-014 §4) — balcão sem preço próprio usa o do salão.
  await expect(page.getByLabel('Preço do canal Balcão')).toHaveValue('45,00');
  await expect(page.getByText('Herdado do salão')).toHaveCount(2);

  // §10 — aviso quando o preço de delivery cai abaixo do salão, sem bloquear a edição.
  await page.getByLabel('Preço do canal Delivery').fill('4000');
  await expect(page.getByText('Delivery mais barato que o salão')).toBeVisible();

  await page.getByRole('button', { name: 'Salvar preços' }).click();
  await expect(page.getByText('Preços atualizados', { exact: true })).toBeVisible();
});

test('gestora pre-visualiza e confirma reajuste em massa por categoria', async ({ page }) => {
  await mockLogin(page);
  await mockAdminShell(page);
  await page.route(`**/v1/catalog/variants/${variantId}/prices`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(priceTable),
    }),
  );

  await page.route('**/v1/catalog/prices/bulk-adjust', async (route) => {
    expect(route.request().headers()['idempotency-key']).toBeTruthy();
    const payload = route.request().postDataJSON() as {
      categoryId: string;
      channel: string;
      percent: number;
    };
    expect(payload.categoryId).toBe(categoryId);
    expect(payload.channel).toBe('Delivery');
    expect(payload.percent).toBe(8);
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ updated: 20, effectiveFrom: new Date().toISOString() }),
    });
  });

  await page.goto('http://127.0.0.1:49173');
  await login(page);

  await openPricingForVariant(page);

  // Cenário Gherkin "Reajuste em massa" (US-014 §4) — pré-visualização antes de confirmar (§10).
  await page.getByLabel('Percentual').fill('8');
  await page.getByRole('button', { name: 'Pré-visualizar' }).click();
  await expect(page.getByRole('button', { name: 'Confirmar reajuste' })).toBeEnabled();

  await page.getByRole('button', { name: 'Confirmar reajuste' }).click();
  await expect(page.getByText(/20 preço\(s\) atualizado\(s\)/)).toBeVisible();
});
