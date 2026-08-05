import { expect, test, type Page } from '@playwright/test';

/**
 * US-033 (Cancelar item ou pedido com autorização) — E2E do fluxo completo do lado do garçom/POS
 * (`web-pos`, porta 49175, US-033 §12 "E2E: fluxo completo — garçom solicita, gerente autoriza no
 * mesmo dispositivo"): o garçom confirma um pedido, pede o cancelamento de um item já iniciado, o
 * servidor recusa com 403 `AUTHORIZATION_REQUIRED`, o gerente digita o PIN NO MESMO DISPOSITIVO
 * (`AuthorizationModal`/`PinPad` do design system) e o item some da lista. Arquivo NOVO — não edita
 * nenhum spec existente (mesma convenção de `order-composition.spec.ts`, para não colidir com
 * outro agente trabalhando em paralelo). Mock de rede via `page.route`, sem backend real.
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

test.describe('US-033 · cancelamento de item com autorização', () => {
  test.beforeEach(async ({ page }) => {
    // Mesmo cuidado de order-composition.spec.ts: sem isto, a animação de entrada
    // (`nx-anim-scale-in`) da tela de confirmação deixaria o botão "Cancelar item" temporariamente
    // fora do fluxo de acessibilidade — flakiness de teste, não bug de produto.
    await page.emulateMedia({ reducedMotion: 'reduce' });
  });

  test('garçom pede o cancelamento de item já iniciado, gerente autoriza no mesmo dispositivo, item some', async ({
    page,
  }) => {
    const sessionId = '0198aabb-7777-7000-8000-000000000007';
    const tableId = '0198aabb-8888-7000-8000-000000000008';
    const orderId = '0198aabb-1111-7000-8000-000000000011';
    const itemId = '0198aabb-2222-7000-8000-000000000022';

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
          permissions: ['order:create', 'order:cancel_queued'],
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
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          order: {
            id: orderId,
            shortCode: 'A50',
            status: 'PLACED',
            sessionId,
            channel: 'DineIn',
            total: '45.90',
            placedAt: new Date().toISOString(),
            items: [
              {
                id: itemId,
                orderId,
                variantId: '0198aabb-5555-7000-8000-000000000005',
                name: 'Pizza Margherita',
                quantity: 1,
                unitPrice: '45.90',
                totalPrice: '45.90',
                // Já em produção (US-033 §4, cenário "Cancelamento após início de produção") — o
                // status exato não é lido pelo cliente para decidir se pede token; quem decide é o
                // servidor (403 AUTHORIZATION_REQUIRED), o cliente só reage a esse código.
                status: 'FIRED',
                notes: null,
                stationId: null,
                repeatedFromItemId: null,
                modifiers: [],
                fractions: [],
              },
            ],
          },
          promisedAt: new Date(Date.now() + 15 * 60_000).toISOString(),
          estimatedMinutes: 15,
        }),
      });
    });

    // PATCH .../cancel: 1ª chamada (sem X-Authorization-Token) recusa com 403; 2ª chamada (com o
    // token emitido por /v1/auth/authorize) autoriza — mesmo mecanismo do backend real (ADR-023).
    let cancelAttempts = 0;
    await page.route(`**/v1/orders/${orderId}/items/${itemId}/cancel`, async (route) => {
      cancelAttempts += 1;
      const authorizationToken = route.request().headers()['x-authorization-token'];

      if (cancelAttempts === 1) {
        expect(authorizationToken).toBeFalsy();
        await route.fulfill({
          status: 403,
          contentType: 'application/json',
          body: JSON.stringify({
            code: 'AUTHORIZATION_REQUIRED',
            detail: 'Item já iniciado. É necessária autorização de perfil superior.',
            meta: { action: 'CANCEL_STARTED_ITEM', itemStatus: 'FIRED' },
          }),
        });
        return;
      }

      expect(authorizationToken).toBe('authz-token-e2e');
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          item: {
            id: itemId,
            status: 'CANCELLED',
            cancelledAt: new Date().toISOString(),
            reason: 'CUSTOMER_REQUEST',
            notes: null,
            wasStarted: true,
            authorizedBy: { id: '0198aabb-3333-7000-8000-000000000033', name: 'Gerente Bruno' },
          },
        }),
      });
    });

    await page.route('**/v1/auth/authorize', async (route) => {
      const body = route.request().postDataJSON() as { action: string; pin: string; context: { orderItemId: string } };
      expect(body.action).toBe('CANCEL_STARTED_ITEM');
      expect(body.context.orderItemId).toBe(itemId);
      // PIN do gerente validado localmente, no MESMO dispositivo do garçom (US-033 §9/§10).
      expect(body.pin).toBe('9911');

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          authorizationToken: 'authz-token-e2e',
          expiresIn: 120,
          authorizedBy: { id: '0198aabb-3333-7000-8000-000000000033', name: 'Gerente Bruno' },
        }),
      });
    });

    await page.goto('http://127.0.0.1:49175');

    await expect(page.getByRole('heading', { name: 'Autorizar dispositivo' })).toBeVisible();
    await page.getByLabel('Código de pareamento').fill('123456');
    await page.getByRole('button', { name: 'Autorizar' }).click();

    await expect(page.getByRole('heading', { name: 'Quem está operando?' })).toBeVisible();
    await page.getByRole('button', { name: '1', exact: true }).click();
    await page.getByRole('button', { name: '2', exact: true }).click();
    await page.getByRole('button', { name: '3', exact: true }).click();
    await page.getByRole('button', { name: '4', exact: true }).click();
    await page.getByRole('button', { name: 'Entrar' }).click();

    await expect(page.getByRole('button', { name: /Mesa 12/ })).toBeVisible();
    await page.getByRole('button', { name: 'Lançar pedido' }).click();

    await page.getByText('Pizza Margherita').click();
    await page.getByRole('button', { name: 'Adicionar ao pedido' }).click();
    await page.getByRole('button', { name: /Confirmar pedido/ }).click();

    // US-030 §10: código curto do pedido exibido após a confirmação — ponto de partida deste teste.
    await expect(page.getByText('A50')).toBeVisible();
    await expect(page.getByText('Pizza Margherita')).toBeVisible();

    // US-033 §10 — motivo obrigatório, escolhido de lista curta, antes de tentar cancelar.
    await page.getByRole('button', { name: 'Cancelar item' }).click();
    await page.getByLabel('Motivo').selectOption('CUSTOMER_REQUEST');
    await page.getByRole('button', { name: 'Confirmar cancelamento' }).click();

    // 403 AUTHORIZATION_REQUIRED (item já em produção) abre o diálogo de PIN do gerente, no MESMO
    // dispositivo do garçom — ADR-023, US-033 §10 ("modal de autorização sobre o contexto").
    await expect(page.getByRole('heading', { name: 'Autorização necessária' })).toBeVisible();
    await page.getByRole('button', { name: '9', exact: true }).click();
    await page.getByRole('button', { name: '9', exact: true }).click();
    await page.getByRole('button', { name: '1', exact: true }).click();
    await page.getByRole('button', { name: '1', exact: true }).click();
    await page.getByRole('button', { name: 'Autorizar' }).click();

    // Autorizado: o cancelamento é repetido com o token e o item some da lista (US-033 §4/§10).
    await expect(page.getByText('Pizza Margherita', { exact: true })).not.toBeVisible();
    await expect(page.getByRole('button', { name: 'Cancelar item' })).not.toBeVisible();
  });
});
