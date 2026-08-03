import { expect, test, type Page } from '@playwright/test';

/**
 * US-091 (Consulta e filtro da trilha) — E2E do fluxo de consulta/filtro da trilha de auditoria no
 * painel web-admin (porta 49173). Mock de rede via `page.route`, sem backend real — mesmo padrão de
 * `catalog-pricing.spec.ts` (login + boot do painel) e `order-cancellation.spec.ts` (estrutura do
 * describe/beforeEach). Arquivo NOVO — não edita nenhum spec existente.
 *
 * Cobre os cenários centrais do Gherkin da US:
 * - "Filtro por autor e período"/"Filtro por tipo de ação"/"Filtro por valor": aplicar um filtro
 *   dispara uma nova consulta com os query params certos.
 * - Resumo em português por linha, nunca `before`/`after` cru.
 * - Paginação por cursor: "Carregar mais" busca a próxima página com `meta.nextCursor`.
 */

const session = {
  accessToken: 'access-token-e2e',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Gestora' },
  permissions: ['audit:read'],
};

const entryDiscount = {
  id: '0198aabb-2222-7000-8000-000000000001',
  action: 'DISCOUNT_APPLIED',
  summary: 'Desconto de 10% (R$ 19,80) aplicado',
  actor: { id: '0198aabb-2222-7000-8000-000000000002', name: 'Carlos' },
  authorizedBy: { id: '0198aabb-2222-7000-8000-000000000003', name: 'Ana' },
  device: { id: '0198aabb-2222-7000-8000-000000000004', label: 'Caixa 1' },
  occurredAt: '2026-08-01T20:00:00Z',
  target: { type: 'order', id: '0198aabb-2222-7000-8000-000000000005', label: 'Pedido A47' },
  before: { discount: 0 },
  after: { discount: 1980 },
  reason: 'cortesia',
  traceId: null,
};

const entryPriceChange = {
  id: '0198aabb-2222-7000-8000-000000000006',
  action: 'PRICE_CHANGED',
  summary: 'Preço da Pizza Grande alterado de R$ 45,00 para R$ 48,00',
  actor: { id: '0198aabb-2222-7000-8000-000000000002', name: 'Carlos' },
  authorizedBy: null,
  device: null,
  occurredAt: '2026-07-30T09:00:00Z',
  target: { type: 'price', id: '0198aabb-2222-7000-8000-000000000007', label: 'Pizza Grande' },
  before: null,
  after: null,
  reason: null,
  traceId: null,
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

/** Recursos que o boot do CloudAdmin busca sempre, independente da seção aberta (app.tsx). A
 * trilha de auditoria NÃO está nessa lista — ela é buscada sob demanda pela própria página. */
async function mockAdminShell(page: Page) {
  await page.route('**/v1/devices', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) }),
  );
  await page.route('**/v1/roles', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [], permissionCatalog: [] }),
    }),
  );
  await page.route('**/v1/catalog/stations', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) }),
  );
  await page.route('**/v1/catalog/modifier-groups', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) }),
  );
  await page.route('**/v1/catalog/categories', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) }),
  );
  await page.route('**/v1/catalog/products', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) }),
  );
}

async function login(page: Page) {
  await page.getByLabel('E-mail').fill('gestora@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
}

test.describe('US-091 · consulta e filtro da trilha de auditoria', () => {
  test.beforeEach(async ({ page }) => {
    // Sem isto, a animação de entrada (`nx-anim-in`) da tela e da tabela deixaria elementos
    // temporariamente fora do fluxo de acessibilidade — flakiness de teste, não bug de produto
    // (mesmo cuidado de order-cancellation.spec.ts).
    await page.emulateMedia({ reducedMotion: 'reduce' });
  });

  test('lista o resumo legível de cada registro, filtra por ação/operador/valor e pagina por cursor', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockAdminShell(page);

    const queriesSeen: URLSearchParams[] = [];
    await page.route('**/v1/audit*', async (route) => {
      const url = new URL(route.request().url());
      queriesSeen.push(url.searchParams);

      if (url.searchParams.get('cursor') === 'cursor-abc') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            data: [entryPriceChange],
            meta: { nextCursor: null, hasMore: false },
          }),
        });
        return;
      }

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: [entryDiscount],
          meta: { nextCursor: 'cursor-abc', hasMore: true },
        }),
      });
    });

    await page.goto('http://127.0.0.1:49173');
    await login(page);

    await page.getByRole('button', { name: 'Trilha de auditoria' }).click();

    // Resumo pronto em português — nunca before/after cru (Gherkin "Antes e depois legíveis").
    await expect(page.getByText('Desconto de 10% (R$ 19,80) aplicado')).toBeVisible();
    await expect(page.getByText('Carlos')).toBeVisible();
    await expect(page.getByText('Ana')).toBeVisible();
    await expect(page.getByText('Pedido A47')).toBeVisible();

    // Cenário "Filtro por tipo de ação"/"Filtro por autor e período"/"Filtro por valor" — os três
    // filtros combinados disparam UMA consulta nova com os parâmetros escolhidos.
    await page.getByLabel('Tipo de ação').selectOption('DISCOUNT_APPLIED');
    await page.getByLabel('Operador (ID)').fill('0198aabb-2222-7000-8000-000000000002');
    await page.getByLabel('Valor mínimo').fill('5000');
    await page.getByRole('button', { name: 'Filtrar' }).click();

    await expect.poll(() => queriesSeen.length).toBeGreaterThanOrEqual(2);
    const filterQuery = queriesSeen[queriesSeen.length - 1]!;
    expect(filterQuery.get('action')).toBe('DISCOUNT_APPLIED');
    expect(filterQuery.get('actorId')).toBe('0198aabb-2222-7000-8000-000000000002');
    expect(filterQuery.get('minAmount')).toBe('50.00');

    // Paginação por cursor: "Carregar mais" usa meta.nextCursor da resposta anterior e concatena
    // o novo registro ao final (modo append) — o primeiro continua na tela.
    await page.getByRole('button', { name: 'Carregar mais' }).click();
    await expect(page.getByText(/Preço da Pizza Grande/)).toBeVisible();
    await expect(page.getByText('Desconto de 10% (R$ 19,80) aplicado')).toBeVisible();

    const paginationQuery = queriesSeen[queriesSeen.length - 1]!;
    expect(paginationQuery.get('cursor')).toBe('cursor-abc');

    // Sem mais páginas (hasMore: false) — o botão desaparece.
    await expect(page.getByRole('button', { name: 'Carregar mais' })).toHaveCount(0);
  });
});
