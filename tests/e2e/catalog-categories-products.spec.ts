import { expect, test, type Page } from '@playwright/test';

/**
 * US-010 · Cadastrar categorias e produtos — cobre o fluxo principal (criar categoria, criar
 * produto vinculado, desativar produto sem apagá-lo) do painel web-admin. Arquivo NOVO (não edita
 * tests/e2e/foundation.spec.ts, tenant-provisioning.spec.ts nem catalog-stations.spec.ts) para não
 * colidir com outro agente que possa estar tocando neles em paralelo — mesma convenção de mock de
 * rede (`page.route`) já usada em catalog-stations.spec.ts.
 *
 * Pré-requisito para este spec rodar: a seção "Cardápio" precisa estar plugada em
 * `apps/web-admin/src/app.tsx` (`ADMIN_SECTIONS`/`CatalogPage`) — assume os rótulos de navegação
 * "Cardápio", "Categorias" e "Produtos" exatamente como implementado em
 * `apps/web-admin/src/catalog/*`. Enquanto essa integração não existir, o teste falha por não
 * encontrar o botão de navegação — falha de dependência externa a este arquivo, não deste spec.
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
  productCount: 0,
};

const mussarelaProduct = {
  id: '0198aabb-4444-7000-8000-000000000001',
  categoryId: pizzasCategory.id,
  categoryName: pizzasCategory.name,
  stationId: null,
  stationName: null,
  name: 'Pizza Mussarela',
  description: null,
  ingredientsText: 'molho, mussarela, orégano',
  allergens: [],
  imageUrl: null,
  position: 0,
  isActive: true,
  isAvailable: true,
  allowsFractions: false,
  maxFractions: 1,
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

test('gestora cria uma categoria, cria um produto vinculado e o desativa sem apaga-lo', async ({
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

  let categories: Array<typeof pizzasCategory> = [];
  let products: Array<typeof mussarelaProduct> = [];

  await page.route('**/v1/catalog/categories', async (route) => {
    const request = route.request();

    if (request.method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: categories }),
      });
      return;
    }

    if (request.method() === 'POST') {
      expect(request.headers().authorization).toBe('Bearer access-token');
      expect(request.headers()['idempotency-key']).toBeTruthy();

      const payload = request.postDataJSON() as { name: string; position: number };
      const created = { ...pizzasCategory, name: payload.name, position: payload.position };
      categories = [...categories, created];

      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(created),
      });
      return;
    }

    await route.fallback();
  });

  await page.route('**/v1/catalog/products?*', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: products }),
    });
  });

  await page.route('**/v1/catalog/products', async (route) => {
    const request = route.request();

    if (request.method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: products }),
      });
      return;
    }

    if (request.method() === 'POST') {
      expect(request.headers()['idempotency-key']).toBeTruthy();

      const payload = request.postDataJSON() as { name: string; categoryId: string };
      const created = { ...mussarelaProduct, name: payload.name, categoryId: payload.categoryId };
      products = [...products, created];

      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(created),
      });
      return;
    }

    await route.fallback();
  });

  await page.route('**/v1/catalog/products/*/deactivate', async (route) => {
    products = products.map((product) => ({ ...product, isActive: false }));
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(products[0]),
    });
  });

  await page.goto('http://127.0.0.1:49173');
  await login(page);

  await page.getByRole('button', { name: 'Cardápio' }).click();
  await expect(page.getByRole('heading', { name: 'Categorias' })).toBeVisible();

  // Cenário "Criação de produto completo" (US-010 §4) começa por uma categoria já existente.
  await page.getByRole('button', { name: 'Nova categoria' }).click();
  const categoryDialog = page.getByRole('dialog', { name: 'Criar categoria' });
  await categoryDialog.getByLabel('Nome').fill('Pizzas Salgadas');
  await categoryDialog.getByRole('button', { name: 'Criar categoria' }).click();
  await expect(page.getByText('Categoria criada.')).toBeVisible();

  await page.getByRole('button', { name: 'Produtos' }).click();
  await page.getByRole('button', { name: 'Novo produto' }).click();
  const productDialog = page.getByRole('dialog', { name: 'Criar produto' });
  await productDialog.getByLabel('Nome').fill('Pizza Mussarela');
  await productDialog.getByRole('button', { name: 'Criar produto' }).click();

  await expect(page.getByText('Produto criado.')).toBeVisible();
  await expect(page.getByText('Pizza Mussarela').first()).toBeVisible();

  // Cenário "Desativação de produto" (US-010 §4) — o produto sai dos canais, mas nunca é excluído fisicamente.
  await page.getByRole('button', { name: 'Desativar produto' }).click();
  await expect(page.getByText('Produto desativado.')).toBeVisible();
  await expect(page.getByText('Inativo')).toBeVisible();
});
