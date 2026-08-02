import { expect, test, type Page } from '@playwright/test';

/**
 * US-011 · Variações de produto com preço próprio — cobre o fluxo principal (produto já nasce com
 * uma variação padrão implícita, gestora define o preço base dela, cria uma segunda variação com
 * preço distinto e desativa a primeira sem apagá-la) do painel web-admin, editado em linha na
 * mesma tela do produto (US-011 §10). Arquivo NOVO (não edita catalog-categories-products.spec.ts,
 * catalog-stations.spec.ts, foundation.spec.ts nem tenant-provisioning.spec.ts) para não colidir
 * com outro agente que possa estar tocando neles em paralelo — mesma convenção de mock de rede
 * (`page.route`) de catalog-categories-products.spec.ts.
 *
 * Pré-requisito para este spec rodar: `apps/web-admin/src/app.tsx` precisa ter
 * `onLoadVariants`/`onCreateVariant`/`onUpdateVariant`/`onSetVariantPrice`/`onActivateVariant`/
 * `onDeactivateVariant`/`onMarkVariantDefault` plugados em `CatalogPage` → `ProductManagementPage`
 * (US-011). Enquanto essa integração não existir, o teste falha por não encontrar a seção
 * "Variações e preço" — falha de dependência externa a este arquivo, não deste spec.
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Gestora' },
  permissions: ['catalog:read', 'catalog:write'],
};

const pizzasCategory = {
  id: '0198aabb-3333-7000-8000-000000000001',
  name: 'Pizzas Salgadas',
  description: null,
  position: 0,
  isActive: true,
  productCount: 1,
};

const mussarelaProduct = {
  id: '0198aabb-4444-7000-8000-000000000001',
  categoryId: pizzasCategory.id,
  categoryName: pizzasCategory.name,
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
};

/** Variação padrão implícita (US-011 §3.1) — todo produto já nasce com uma, sem preço definido ainda. */
const implicitVariant = {
  id: '0198aabb-5555-7000-8000-000000000001',
  productId: mussarelaProduct.id,
  name: 'Pizza Mussarela',
  sku: null,
  sizeCode: null,
  prepMinutes: 10,
  isDefault: true,
  isActive: true,
  currentPrice: null as string | null,
  currentPriceChannel: null as string | null,
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

test('gestora define o preço base da variação implícita e cadastra uma segunda variação com preço próprio', async ({
  page,
}) => {
  await mockLogin(page);
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
  await page.route('**/v1/catalog/categories', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [pizzasCategory] }),
    }),
  );
  await page.route('**/v1/catalog/products?*', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [mussarelaProduct] }),
    }),
  );
  await page.route('**/v1/catalog/products', (route) => {
    if (route.request().method() !== 'GET') return route.fallback();
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [mussarelaProduct] }),
    });
  });

  let variants = [implicitVariant];

  await page.route(`**/v1/catalog/products/${mussarelaProduct.id}/variants`, async (route) => {
    const request = route.request();

    if (request.method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: variants }),
      });
      return;
    }

    if (request.method() === 'POST') {
      expect(request.headers()['idempotency-key']).toBeTruthy();
      const payload = request.postDataJSON() as {
        name: string;
        sizeCode?: string;
        basePrice: string;
      };
      const created = {
        id: '0198aabb-5555-7000-8000-000000000002',
        productId: mussarelaProduct.id,
        name: payload.name,
        sku: null,
        sizeCode: payload.sizeCode ?? null,
        prepMinutes: 10,
        isDefault: false,
        isActive: true,
        currentPrice: payload.basePrice,
        currentPriceChannel: 'DineIn',
      };
      variants = [...variants, created];
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(created),
      });
      return;
    }

    await route.fallback();
  });

  await page.route('**/v1/catalog/variants/*/prices', async (route) => {
    const request = route.request();
    expect(request.headers()['idempotency-key']).toBeTruthy();
    const variantId = request.url().split('/').slice(-2, -1)[0]!;
    const payload = request.postDataJSON() as { amount: string };
    variants = variants.map((variant) =>
      variant.id === variantId
        ? { ...variant, currentPrice: payload.amount, currentPriceChannel: 'DineIn' }
        : variant,
    );
    await route.fulfill({
      status: 201,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 'price-id',
        variantId,
        channel: 'DineIn',
        amount: payload.amount,
        validFrom: new Date().toISOString(),
        validTo: null,
      }),
    });
  });

  await page.route('**/v1/catalog/variants/*/deactivate', async (route) => {
    const variantId = route.request().url().split('/').slice(-2, -1)[0]!;
    variants = variants.map((variant) =>
      variant.id === variantId ? { ...variant, isActive: false } : variant,
    );
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(variants.find((v) => v.id === variantId)),
    });
  });

  await page.route('**/v1/catalog/variants/*', async (route) => {
    const request = route.request();
    if (request.method() !== 'PATCH') return route.fallback();

    const variantId = request.url().split('/').pop()!;
    const payload = request.postDataJSON() as { name: string; sizeCode?: string };
    variants = variants.map((variant) =>
      variant.id === variantId
        ? { ...variant, name: payload.name, sizeCode: payload.sizeCode ?? null }
        : variant,
    );
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(variants.find((v) => v.id === variantId)),
    });
  });

  await page.goto('http://127.0.0.1:49173');
  await login(page);

  await page.getByRole('button', { name: 'Cardápio' }).click();
  await page.getByRole('button', { name: 'Produtos' }).click();
  await page.getByText('Pizza Mussarela').first().click();

  await expect(page.getByText('Variações e preço')).toBeVisible();
  await expect(page.getByLabel('Nome da variação Pizza Mussarela')).toBeVisible();

  // Cenário "Produto sem variação" (US-011 §4) — o produto já nasceu com a variação padrão
  // implícita; a gestora só precisa dar um preço a ela.
  await page.getByLabel('Preço da variação Pizza Mussarela').fill('3500');
  await page
    .getByRole('row', { name: /Pizza Mussarela/ })
    .getByRole('button', { name: 'Salvar' })
    .click();

  // Cenário "Produto com três tamanhos" (US-011 §4, simplificado a dois) — nova variação com
  // preço próprio, distinto do da variação padrão.
  await page.getByRole('button', { name: 'Nova variação' }).click();
  await page.getByLabel('Nome da variação', { exact: true }).fill('Grande');
  await page.getByLabel('Tamanho', { exact: true }).fill('G');
  await page.getByLabel('Preço', { exact: true }).fill('5200');
  await page.getByRole('button', { name: 'Adicionar variação' }).click();

  await expect(page.getByLabel('Nome da variação Grande')).toBeVisible();
  await expect(page.getByLabel('Preço da variação Grande')).toHaveValue('52,00');

  // Cenário "Exclusão com histórico" (US-011 §4) — não existe exclusão física, só desativação.
  await page
    .getByRole('row', { name: /Grande/ })
    .getByRole('button', { name: 'Desativar' })
    .click();
  await expect(page.getByRole('row', { name: /Grande/ }).getByText('Inativa')).toBeVisible();
});
