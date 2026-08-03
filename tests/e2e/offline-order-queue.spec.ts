import { expect, test, type Page } from '@playwright/test';

/**
 * US-034 (Operar pedido integralmente offline) — frontend, cenário "Queda momentânea da rede
 * local" (§4/§7): simula a LAN caindo bem no toque em "Confirmar pedido" (via `page.route(...,
 * route.abort('failed'))`, que faz o `fetch` do navegador rejeitar com `TypeError` — a MESMA
 * condição que `isNetworkFailure` (`packages/ui/src/offline/action-queue.ts`) reconhece).
 * Confirma que a tela nunca mostra erro bloqueante (§10: "o operador não pode ser interrompido"),
 * e que, ao "voltar" a rede (`window.dispatchEvent(new Event('online'))` — o MESMO sinal que o
 * app escuta em produção, nenhum mecanismo inventado só para o teste), o pedido é reenviado
 * automaticamente com a MESMA `Idempotency-Key` da tentativa original (ADR-020, "não duplica").
 *
 * Arquivo NOVO — não edita nenhum spec existente (mesma convenção de `order-composition.spec.ts`),
 * para não colidir com o agente de backend trabalhando em paralelo nesta mesma onda. Mock de rede
 * via `page.route`, sem backend real — os dois testes rodam de ponta a ponta contra os apps de
 * verdade (Vite dev server), só a rede é dublada.
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

function orderResponseFixture(sessionId: string | null, shortCode: string) {
  return {
    order: {
      id: '0198aabb-6666-7000-8000-000000000006',
      shortCode,
      status: 'PLACED',
      sessionId,
      channel: 'DineIn',
      total: '45.90',
      placedAt: new Date().toISOString(),
      items: [],
    },
    promisedAt: new Date(Date.now() + 15 * 60_000).toISOString(),
    estimatedMinutes: 15,
  };
}

test.describe('US-034 · fila offline de pedidos (queda momentânea da rede local)', () => {
  test.beforeEach(async ({ page }) => {
    // CLAUDE.md › "Motion e microinterações" — mesmo motivo de order-composition.spec.ts: a
    // entrada animada do toast (`nx-anim-toast-in`) não pode deixar o texto momentaneamente
    // ilegível/fora do lugar por causa da animação em andamento no instante do assert.
    await page.emulateMedia({ reducedMotion: 'reduce' });
  });

  test('cliente pela mesa (web-menu): pedido cai na fila sem erro, e reenvia com a mesma Idempotency-Key ao reconectar', async ({
    page,
  }) => {
    const qrToken = 'token-mesa-offline';
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
          sessionToken: 'jwt-sessao-mesa-offline',
        }),
      }),
    );
    await mockMenu(page);

    const idempotencyKeysSeen: string[] = [];
    let attempt = 0;
    await page.route('**/v1/public/orders', async (route) => {
      attempt += 1;
      idempotencyKeysSeen.push(route.request().headers()['idempotency-key'] ?? '');
      if (attempt === 1) {
        // A LAN cai bem no toque em "Confirmar pedido" — o fetch do navegador nem chega a voltar
        // uma Response (rejeita com TypeError), condição distinta de uma resposta HTTP de erro.
        await route.abort('failed');
        return;
      }
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(orderResponseFixture(sessionId, 'A47')),
      });
    });

    await page.goto(`http://127.0.0.1:49177/?t=${qrToken}`);

    await expect(page.getByRole('heading', { name: 'Dona Betinha' })).toBeVisible();
    await page.getByRole('button', { name: 'Meu pedido' }).click();
    await page.getByText('Pizza Margherita').click();
    await page.getByRole('button', { name: 'Adicionar ao pedido' }).click();
    await expect(page.getByText(/Total: R\$\s*45,90/)).toBeVisible();

    await page.getByRole('button', { name: /Confirmar pedido/ }).click();

    // US-034 §10: nunca erro bloqueante — o aviso é discreto ("recebido, sincronizando"), sem
    // jargão técnico, e nenhum `role="alert"` chega a aparecer.
    await expect(page.getByText('Pedido recebido — sincronizando quando a conexão voltar.')).toBeVisible();
    await expect(page.getByRole('alert')).toHaveCount(0);
    // Envio otimista: o carrinho volta ao estado vazio (a ação já está garantida na fila local).
    await expect(page.getByText('Nenhum item adicionado ainda.')).toBeVisible();

    // A rede volta — o MESMO evento que o app escuta em produção (window 'online'), disparado no
    // browser via page.evaluate (não um mecanismo inventado só para o teste).
    await page.evaluate(() => window.dispatchEvent(new Event('online')));

    await expect.poll(() => attempt, { message: 'reenvio automático de /v1/public/orders ao reconectar' }).toBe(2);
    expect(idempotencyKeysSeen[0]).toBeTruthy();
    expect(idempotencyKeysSeen[1]).toBe(idempotencyKeysSeen[0]); // ADR-020: mesma chave, não duplica
  });

  test('garçom pelo celular (web-pos): pedido cai na fila sem erro, e reenvia com a mesma Idempotency-Key ao reconectar', async ({
    page,
  }) => {
    const sessionId = '0198aabb-7777-7000-8000-000000000007';
    const tableId = '0198aabb-8888-7000-8000-000000000008';

    await page.route('**/v1/devices/pair', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          device: { id: '0198aabb-9999-7000-8000-000000000009', label: 'Caixa', kind: 'CASHIER' },
          deviceSecret: 'device-secret-e2e-offline',
        }),
      }),
    );
    await page.route('**/v1/auth/pin', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: 'access-token-garcom-offline',
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

    const idempotencyKeysSeen: string[] = [];
    let attempt = 0;
    await page.route('**/v1/orders', async (route) => {
      attempt += 1;
      idempotencyKeysSeen.push(route.request().headers()['idempotency-key'] ?? '');
      if (attempt === 1) {
        await route.abort('failed');
        return;
      }
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(orderResponseFixture(sessionId, 'A48')),
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
    await expect(page.getByText(/Total: R\$\s*45,90/)).toBeVisible();

    await page.getByRole('button', { name: /Confirmar pedido/ }).click();

    await expect(page.getByText('Pedido recebido — sincronizando quando a conexão voltar.')).toBeVisible();
    await expect(page.getByRole('alert')).toHaveCount(0);
    await expect(page.getByText('Nenhum item adicionado ainda.')).toBeVisible();

    // US-034 §10: indicador discreto e PERMANENTE (SyncStatus) no shell autenticado — nunca
    // modal/pop-up — mostra "1 registro aguardando envio" enquanto a ação está na fila.
    await expect(page.getByText(/Trabalhando sem internet · 1 registro aguardando envio\./)).toBeVisible();

    await page.evaluate(() => window.dispatchEvent(new Event('online')));

    await expect.poll(() => attempt, { message: 'reenvio automático de /v1/orders ao reconectar' }).toBe(2);
    expect(idempotencyKeysSeen[0]).toBeTruthy();
    expect(idempotencyKeysSeen[1]).toBe(idempotencyKeysSeen[0]);

    // O indicador permanente desaparece depois do reenvio bem-sucedido (fila esvaziada).
    await expect(page.getByText(/Trabalhando sem internet/)).toHaveCount(0);
  });
});
