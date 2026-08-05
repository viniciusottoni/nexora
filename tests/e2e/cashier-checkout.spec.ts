import { expect, test } from '@playwright/test';

test.use({ baseURL: 'http://127.0.0.1:49175' });

/**
 * E-05 (Caixa e Pagamento) — US-052 (Múltiplas formas de pagamento) end-to-end pelo celular do
 * caixa (`web-pos`, porta 49175): mesa com conta solicitada → "Dividir a conta" → registra o
 * pagamento único que fecha a comanda. Arquivo NOVO (mesma convenção de `order-composition.spec.ts`)
 * — mock de rede via `page.route`, sem backend real, rodando contra o app de verdade (Vite dev
 * server).
 */
test.describe('US-052 · fechamento de conta com múltiplas formas de pagamento', () => {
  test.beforeEach(async ({ page }) => {
    // CLAUDE.md › "Motion e microinterações" — ver mesma nota em order-composition.spec.ts.
    await page.emulateMedia({ reducedMotion: 'reduce' });
  });

  test('caixa fecha a conta com pagamento em dinheiro e vê a confirmação', async ({ page }) => {
    const sessionId = '0198aabb-6666-7000-8000-000000000066';
    const tableId = '0198aabb-7777-7000-8000-000000000077';

    await page.route('**/v1/devices/pair', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          device: { id: '0198aabb-9999-7000-8000-000000000099', label: 'Caixa', kind: 'CASHIER' },
          deviceSecret: 'device-secret-e2e',
        }),
      }),
    );
    await page.route('**/v1/auth/pin', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: 'access-token-caixa',
          user: { id: '0198aabb-1010-7000-8000-000000000101', name: 'Caixa Ana' },
          permissions: ['table:manage', 'bill:manage', 'payment:register'],
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
              label: '7',
              area: 'Salão',
              status: 'BILL_REQUESTED',
              seats: 4,
              session: {
                openedAt: new Date(Date.now() - 40 * 60_000).toISOString(),
                minutesOpen: 40,
                total: '110.00',
                guestCount: 2,
                waiter: { id: '0198aabb-1111-7000-8000-000000000011', name: 'João' },
                sessionId,
              },
              flags: { waiterCalled: false, billRequested: true, itemsReadyToServe: 0, aboveAvgDuration: false },
            },
          ],
        }),
      }),
    );

    await page.route(`**/v1/sessions/${sessionId}/bill*`, (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [{ id: '0198aabb-2222-7000-8000-000000000022', name: 'Pizza Grande', total: '100.00', pending: false, assignedPerson: null }],
          subtotal: '100.00',
          serviceFee: '10.00',
          total: '110.00',
          splitMode: 'BY_PERSON',
          split: [{ person: 1, amount: '110.00', serviceFeeAmount: '10.00', serviceFeeWaived: false }],
          pendingItems: [],
          hasPendingItems: false,
          amountPaid: null,
          remainingAmount: null,
          unassignedItemIds: [],
          serviceFeeOptional: true,
          serviceFeeWaived: false,
        }),
      }),
    );

    await page.route(`**/v1/sessions/${sessionId}/payments`, async (route) => {
      const payload = route.request().postDataJSON() as { payments: Array<{ method: string; amount: number }> };
      expect(payload.payments).toHaveLength(1);
      expect(payload.payments[0]?.method).toBe('CASH');
      expect(payload.payments[0]?.amount).toBe(110);

      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          session: { status: 'CLOSED' },
          payments: [
            {
              id: '0198aabb-3333-7000-8000-000000000033',
              method: 'CASH',
              amount: '110.00',
              netAmount: '110.00',
              feeAmount: '0.00',
              changeAmount: '0.00',
              provider: null,
              providerRef: null,
              reconciliationStatus: 'NOTAPPLICABLE',
            },
          ],
          change: '0.00',
          receipt: { url: `/v1/sessions/${sessionId}/receipt` },
        }),
      });
    });

    await page.goto('/');

    await expect(page.getByRole('heading', { name: 'Autorizar dispositivo' })).toBeVisible();
    await page.getByLabel('Código de pareamento').fill('123456');
    await page.getByRole('button', { name: 'Autorizar' }).click();

    await expect(page.getByRole('heading', { name: 'Quem está operando?' })).toBeVisible();
    await page.getByRole('button', { name: '1', exact: true }).click();
    await page.getByRole('button', { name: '2', exact: true }).click();
    await page.getByRole('button', { name: '3', exact: true }).click();
    await page.getByRole('button', { name: '4', exact: true }).click();
    await page.getByRole('button', { name: 'Entrar' }).click();

    // US-050/US-023: mesa com conta solicitada em destaque no mapa.
    await expect(page.getByRole('button', { name: 'Dividir a conta' })).toBeVisible();
    await page.getByRole('button', { name: 'Dividir a conta' }).click();

    // US-052: painel de fechamento sempre visível, independente do modo de divisão escolhido.
    await expect(page.getByRole('heading', { name: 'Fechar conta' })).toBeVisible();
    await page.getByRole('spinbutton', { name: 'Valor', exact: true }).fill('110');

    const submitButton = page.getByRole('button', { name: 'Registrar pagamentos e fechar conta' });
    await expect(submitButton).toBeEnabled();
    await submitButton.click();

    await expect(page.getByRole('heading', { name: 'Conta fechada' })).toBeVisible();
    await expect(page.getByText(/CLOSED/)).toBeVisible();
  });
});
