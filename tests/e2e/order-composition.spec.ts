import { expect, test, type Page } from '@playwright/test';

/**
 * US-030 (Criar pedido com itens, modificadores e frações) — frontend. Cobre os dois caminhos
 * centrais do §12 ("E2E: cliente monta e envia pelo QR; garçom lança pelo celular; ambos chegam
 * iguais"): o cliente pela mesa via QR (`web-menu`, porta 49177, `POST /v1/public/orders`) e o
 * garçom pelo celular (`web-pos`, porta 49175, `POST /v1/orders`). Arquivo NOVO — não edita
 * nenhum spec existente, para não colidir com outro agente trabalhando em paralelo (mesma
 * convenção de `catalog-categories-products.spec.ts`). Mock de rede via `page.route`, sem
 * backend real — os dois testes rodam de ponta a ponta contra os apps de verdade (Vite dev
 * server), só a rede é dublada.
 */

const menuFixture = {
  tenantId: '0198aabb-1111-7000-8000-000000000001',
  tenantName: 'Dona Betinha',
  categories: [
    {
      id: '0198aabb-4444-7000-8000-000000000004',
      name: 'Pizzas',
      description: null,
      position: 0,
      products: [
        {
          id: '0198aabb-5555-7000-8000-000000000005',
          name: 'Pizza Margherita',
          description: 'Molho, mussarela, manjericão',
          ingredientsText: null,
          allergens: [],
          imageUrl: null,
          position: 0,
          fromPrice: '45.90',
        },
      ],
    },
  ],
};

async function mockMenu(page: Page) {
  await page.route('**/v1/public/menu**', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(menuFixture) }),
  );
}

test.describe('US-030 · composição de pedido', () => {
  test.beforeEach(async ({ page }) => {
    // CLAUDE.md › "Motion e microinterações": os tokens já zeram `--dur-*` sob
    // `prefers-reduced-motion: reduce` — emulado aqui para que a entrada animada
    // (`nx-anim-in`) de uma tela nova não deixe o `transform` da animação temporariamente no ar
    // (criando um contexto de empilhamento que, por um instante, esconderia o CTA principal por
    // baixo do grupo flutuante "Chamar garçom"/"Pedir a conta" — flakiness de teste, não bug de
    // produto: no dispositivo real a animação termina em `--dur-slow`, bem antes de qualquer toque).
    await page.emulateMedia({ reducedMotion: 'reduce' });
  });

  test('cliente monta e envia o pedido pelo QR (web-menu)', async ({ page }) => {
    const qrToken = 'token-mesa-9';
    const sessionId = '0198aabb-2222-7000-8000-000000000002';
    const tableId = '0198aabb-3333-7000-8000-000000000003';

    await page.route('**/v1/public/table/**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          table: { id: tableId, label: '9', areaName: 'Salão' },
          session: {
            id: sessionId,
            tableId,
            tableLabel: '9',
            status: 'OPEN',
            openedAt: new Date().toISOString(),
            guestCount: 2,
            guestCountConfirmed: true,
            waiterId: null,
            source: 'QR',
            currentItems: [],
            total: '0.00',
          },
          sessionToken: 'jwt-sessao-mesa-9',
        }),
      }),
    );
    await mockMenu(page);

    await page.route('**/v1/public/orders', async (route) => {
      const request = route.request();
      expect(request.headers().authorization).toBe('Bearer jwt-sessao-mesa-9');
      expect(request.headers()['idempotency-key']).toBeTruthy();
      const payload = request.postDataJSON() as { items: Array<{ notes: string | null }> };
      expect(payload.items).toHaveLength(1);
      expect(payload.items[0]?.notes).toBe('sem cebola, por favor');

      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          order: {
            id: '0198aabb-6666-7000-8000-000000000006',
            shortCode: 'A47',
            status: 'PLACED',
            sessionId,
            channel: 'DineIn',
            total: '45.90',
            placedAt: new Date().toISOString(),
            items: [],
          },
          promisedAt: new Date(Date.now() + 15 * 60_000).toISOString(),
          estimatedMinutes: 15,
        }),
      });
    });

    await page.goto(`http://127.0.0.1:49177/?t=${qrToken}`);

    // Zero fricção (US-021 §10) — o cardápio aparece direto; a composição vive na aba "Meu pedido".
    await expect(page.getByRole('heading', { name: 'Dona Betinha' })).toBeVisible();
    await page.getByRole('button', { name: 'Meu pedido' }).click();

    await page.getByText('Pizza Margherita').click();
    await page
      .getByPlaceholder('Ex.: bem assada, sem cebola')
      .fill('sem cebola, por favor');
    await page.getByRole('button', { name: 'Adicionar ao pedido' }).click();

    // US-030 §10: preço total sempre visível durante a montagem.
    await expect(page.getByText(/Total: R\$\s*45,90/)).toBeVisible();

    await page.getByRole('button', { name: /Confirmar pedido/ }).click();

    // US-030 §10: código curto do pedido exibido após a confirmação.
    await expect(page.getByText('A47')).toBeVisible();
  });

  test('garçom lança o pedido pelo celular (web-pos)', async ({ page }) => {
    const sessionId = '0198aabb-7777-7000-8000-000000000007';
    const tableId = '0198aabb-8888-7000-8000-000000000008';

    await page.route('**/v1/devices/pair', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          device: { id: '0198aabb-9999-7000-8000-000000000009', label: 'Caixa', kind: 'CASHIER' },
          deviceSecret: 'device-secret-e2e',
        }),
      }),
    );
    await page.route('**/v1/auth/pin', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: 'access-token-garcom',
          user: { id: '0198aabb-1010-7000-8000-000000000010', name: 'Ana' },
          permissions: ['order:create'],
        }),
      }),
    );
    await page.route('**/v1/tables?*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          tables: [
            {
              id: tableId,
              label: '12',
              area: 'Salão',
              status: 'OCCUPIED',
              seats: 4,
              session: {
                openedAt: new Date().toISOString(),
                minutesOpen: 5,
                total: '0.00',
                guestCount: 2,
                waiter: null,
                sessionId,
              },
              flags: { waiterCalled: false, billRequested: false, itemsReadyToServe: 0, aboveAvgDuration: false },
            },
          ],
        }),
      }),
    );
    await mockMenu(page);

    await page.route('**/v1/orders', async (route) => {
      const request = route.request();
      expect(request.headers().authorization).toBe('Bearer access-token-garcom');
      const payload = request.postDataJSON() as { channel: string; sessionId: string; items: unknown[] };
      expect(payload.channel).toBe('DineIn');
      expect(payload.sessionId).toBe(sessionId);
      expect(payload.items).toHaveLength(1);

      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          order: {
            id: '0198aabb-1111-7000-8000-000000000011',
            shortCode: 'A48',
            status: 'PLACED',
            sessionId,
            channel: 'DineIn',
            total: '45.90',
            placedAt: new Date().toISOString(),
            items: [],
          },
          promisedAt: new Date(Date.now() + 12 * 60_000).toISOString(),
          estimatedMinutes: 12,
        }),
      });
    });

    await page.goto('http://127.0.0.1:49175');

    await expect(page.getByRole('heading', { name: 'Autorizar dispositivo' })).toBeVisible();
    await page.getByLabel('Código de pareamento').fill('123456');
    await page.getByRole('button', { name: 'Autorizar' }).click();

    // PinScreen — qualquer PIN de 4+ dígitos (o mock de POST /v1/auth/pin aceita qualquer corpo).
    await expect(page.getByRole('heading', { name: 'Quem está operando?' })).toBeVisible();
    await page.getByRole('button', { name: '1', exact: true }).click();
    await page.getByRole('button', { name: '2', exact: true }).click();
    await page.getByRole('button', { name: '3', exact: true }).click();
    await page.getByRole('button', { name: '4', exact: true }).click();
    await page.getByRole('button', { name: 'Entrar' }).click();

    // US-030 §7, cenário "Pedido pelo celular do garçom" — lança o pedido a partir da mesa ocupada.
    await expect(page.getByRole('button', { name: /Mesa 12/ })).toBeVisible();
    await page.getByRole('button', { name: 'Lançar pedido' }).click();

    await page.getByText('Pizza Margherita').click();
    await page.getByRole('button', { name: 'Adicionar ao pedido' }).click();

    await expect(page.getByText(/Total: R\$\s*45,90/)).toBeVisible();

    await page.getByRole('button', { name: /Confirmar pedido/ }).click();

    // US-030 §10: código curto do pedido exibido após a confirmação — mesmo comportamento do cliente.
    await expect(page.getByText('A48')).toBeVisible();
  });
});
