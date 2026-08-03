import { expect, test, type Page } from '@playwright/test';

/**
 * US-031 (Roteamento simultâneo para cozinha e caixa) — frontend. Cobre o cenário Gherkin central
 * ("Chegada ao KDS": do pedido confirmado ao cartão aparecer na fila) e "Pedido de delivery" (canal
 * visualmente distinguível). Arquivo NOVO — não edita nenhum spec existente (mesma convenção de
 * `order-composition.spec.ts`/`catalog-categories-products.spec.ts`, para não colidir com outro
 * agente trabalhando em paralelo nesta mesma onda).
 *
 * Mock de rede via `page.route`, sem backend real (mesmo padrão de `order-composition.spec.ts`) —
 * `web-kds` roda contra o Vite dev server de verdade (porta 49176, `playwright.config.ts`). Como
 * não há host de API real no ambiente de teste, a conexão SignalR de `/hubs/kds` nunca completa o
 * handshake e `KdsRealtimeClient` cai no fallback de polling (ADR-011) — o PRÓPRIO mecanismo que o
 * cenário Gherkin "Queda do WebSocket no KDS" exige ("deve exibi-lo em no máximo 5 segundos, via
 * polling") acaba sendo exercitado de ponta a ponta por este teste, não apenas simulado.
 */

/** JWT sintático (sem assinatura verificável) só para o payload decodificado no cliente (claim `stn`) — ver `decode-station-claim.ts`. */
function fakeAccessToken(payload: Record<string, unknown>): string {
  const base64url = (value: object) =>
    Buffer.from(JSON.stringify(value)).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${base64url({ alg: 'HS256', typ: 'JWT' })}.${base64url(payload)}.assinatura-nao-verificada-no-cliente`;
}

const OVEN_STATION_ID = '0198aabb-1111-7000-8000-000000000050';

async function pairAndLoginKds(page: Page, accessToken: string) {
  await page.route('**/v1/devices/pair', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        device: { id: '0198aabb-9999-7000-8000-000000000099', label: 'Cozinha - Forno', kind: 'KDS' },
        deviceSecret: 'device-secret-e2e-kds',
      }),
    }),
  );
  await page.route('**/v1/auth/pin', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        accessToken,
        user: { id: '0198aabb-1010-7000-8000-000000000098', name: 'Pizzaiolo' },
        permissions: ['kds:advance', 'kds:read'],
      }),
    }),
  );
  // web-kds também consulta a marca/tema em runtime (RuntimeBrandingProvider) antes de exibir a
  // tela — sem mockar, cai no fallback neutro (createNeutralBrandingResponse), suficiente aqui.
  await page.route('**/v1/local/branding', (route) => route.fulfill({ status: 404, body: '' }));

  await page.goto('http://127.0.0.1:49176');

  await expect(page.getByRole('heading', { name: 'Autorizar dispositivo' })).toBeVisible();
  await page.getByLabel('Código de pareamento').fill('654321');
  await page.getByRole('button', { name: 'Autorizar' }).click();

  await expect(page.getByRole('heading', { name: 'Quem está operando?' })).toBeVisible();
  await page.getByRole('button', { name: '1', exact: true }).click();
  await page.getByRole('button', { name: '2', exact: true }).click();
  await page.getByRole('button', { name: '3', exact: true }).click();
  await page.getByRole('button', { name: '4', exact: true }).click();
  await page.getByRole('button', { name: 'Entrar' }).click();
}

test.describe('US-031 · roteamento para o KDS', () => {
  test.beforeEach(async ({ page }) => {
    // Ver comentário equivalente em order-composition.spec.ts — animação de entrada não pode
    // deixar o cartão temporariamente fora da árvore de acessibilidade durante o teste.
    await page.emulateMedia({ reducedMotion: 'reduce' });
  });

  test('do pedido confirmado ao cartão aparecer na fila do KDS, via fallback de polling (ADR-011)', async ({ page }) => {
    const accessToken = fakeAccessToken({ sub: 'user-1', stn: OVEN_STATION_ID, roles: ['KITCHEN'] });

    // Começa sem nenhum pedido — o "pedido confirmado" só passa a existir depois (variável mutável
    // lida pelo handler a cada chamada, mesmo padrão de "atualizar o mock no meio do teste").
    let orderConfirmed = false;
    await page.route('**/v1/kds/queue**', (route) => {
      const url = new URL(route.request().url());
      expect(url.searchParams.get('stationId')).toBe(OVEN_STATION_ID);

      const items = orderConfirmed
        ? [
            {
              orderItemId: '0198aabb-1111-7000-8000-000000000060',
              orderCode: 'A47',
              productName: 'Pizza Calabresa Grande',
              quantity: 1,
              modifiers: ['sem cebola'],
              notes: 'bem assada',
              status: 'QUEUED',
              placedAt: new Date().toISOString(),
              elapsedSeconds: 2,
              table: '12',
              channel: 'DineIn',
            },
          ]
        : [];

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items, lastEventId: new Date().toISOString() }),
      });
    });

    await pairAndLoginKds(page, accessToken);

    await expect(page.getByText('Cozinha em dia')).toBeVisible();
    // Indicador de modo degradado (ADR-011 §"sinalização visual do modo degradado") — o WebSocket
    // nunca conecta neste ambiente de teste, então a tela deve mostrar "Sync atrasada", nunca
    // "Sincronizado" (que sugeriria falsamente tempo real, o risco de "falha silenciosa" do ADR).
    await expect(page.getByText('Sync atrasada')).toBeVisible();

    // "O pedido é confirmado agora" — a próxima consulta de polling (ADR-011: a cada 5s) passa a
    // devolver o item novo.
    orderConfirmed = true;

    // Cenário Gherkin "Queda do WebSocket no KDS": em no máximo 5 segundos, via polling — margem
    // generosa contra o timeout padrão do Playwright (30s, playwright.config.ts).
    await expect(page.getByText('A47')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText('Pizza Calabresa Grande')).toBeVisible();
    await expect(page.getByText('Mesa 12')).toBeVisible();
  });

  test('canal delivery chega ao KDS e é visualmente distinguível do salão (critério de aceite)', async ({ page }) => {
    const accessToken = fakeAccessToken({ sub: 'user-2', stn: OVEN_STATION_ID, roles: ['KITCHEN'] });

    await page.route('**/v1/kds/queue**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              orderItemId: '0198aabb-1111-7000-8000-000000000061',
              orderCode: 'A48',
              productName: 'Pizza Marguerita',
              quantity: 1,
              modifiers: [],
              notes: null,
              status: 'QUEUED',
              placedAt: new Date().toISOString(),
              elapsedSeconds: 5,
              table: '5',
              channel: 'DineIn',
            },
            {
              orderItemId: '0198aabb-1111-7000-8000-000000000062',
              orderCode: 'A49',
              productName: 'Pizza Portuguesa',
              quantity: 1,
              modifiers: [],
              notes: null,
              status: 'QUEUED',
              placedAt: new Date().toISOString(),
              elapsedSeconds: 3,
              table: null,
              channel: 'Delivery',
            },
          ],
          lastEventId: new Date().toISOString(),
        }),
      }),
    );

    await pairAndLoginKds(page, accessToken);

    const tickets = page.getByTestId('kds-ticket');
    await expect(tickets).toHaveCount(2);

    const dineInTicket = page.locator('[data-testid="kds-ticket"][data-channel="DineIn"]');
    const deliveryTicket = page.locator('[data-testid="kds-ticket"][data-channel="Delivery"]');
    await expect(dineInTicket).toBeVisible();
    await expect(deliveryTicket).toBeVisible();

    await expect(dineInTicket.getByText('Salão')).toBeVisible();
    await expect(deliveryTicket.getByText('Delivery').first()).toBeVisible();
  });
});
