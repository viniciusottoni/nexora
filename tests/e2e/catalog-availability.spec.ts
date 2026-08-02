import { expect, test, type Page } from '@playwright/test';

/**
 * US-015 · Marcar produto indisponível com propagação imediata — cobre os cenários Gherkin
 * "Propagação imediata" e "Retorno à disponibilidade" do lado do painel de gestão (web-admin).
 * Arquivo NOVO (não edita tests/e2e/foundation.spec.ts/tenant-provisioning.spec.ts) — mesma
 * convenção de mock de rede (`page.route`) já usada nos dois.
 *
 * `UnavailableListPage` já está plugada em `apps/web-admin/src/app.tsx` (seção "Indisponíveis" de
 * `ADMIN_SECTIONS`) — integração feita na consolidação da E-01. O cenário "Cliente com o item no
 * carrinho" (US-015 §4) é do cardápio do cliente (`web-menu`, ainda uma casca) e continua fora
 * desta suíte.
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Gestor' },
  permissions: ['catalog:read', 'catalog:write'],
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

async function loginToAdmin(page: Page) {
  await mockLogin(page);
  await page.goto('http://127.0.0.1:49173');
  await page.getByLabel('E-mail').fill('gestor@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
}

test.describe('Indisponibilidade de produto no painel de gestão (US-015)', () => {
  test('Cenário "Propagação imediata": produto marcado indisponível pela cozinha some do cardápio e aparece na lista do gestor', async ({
    page,
  }) => {
    await loginToAdmin(page);

    // Rota registrada ANTES de abrir a seção — `UnavailableListPage` dispara a busca inicial no
    // próprio mount (useEffect), então o mock precisa existir antes do clique, não depois.
    await page.route('**/v1/catalog/products/unavailable', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              productId: '0198aabb-3333-7000-8000-000000000001',
              productName: 'Pizza Calabresa',
              isAvailable: false,
              unavailableReason: 'Acabou a calabresa',
              unavailableSince: '2026-08-02T20:00:00.000Z',
            },
          ],
        }),
      });
    });

    await page.getByRole('button', { name: 'Indisponíveis' }).click();

    await expect(page.getByText('Pizza Calabresa')).toBeVisible();
    await expect(page.getByText('Acabou a calabresa')).toBeVisible();
  });

  test('Cenário "Retorno à disponibilidade, manual": gestor marca disponível e o item some da lista', async ({
    page,
  }) => {
    await loginToAdmin(page);

    // Estado mutável: o fallback de polling da própria tela (US-015 §9, WebSocket sem servidor
    // real neste teste) reconsulta `unavailable` periodicamente — se o GET continuasse sempre
    // devolvendo o item, o polling o reinseriria na lista logo depois do "Marcar disponível".
    let stillUnavailable = true;

    await page.route('**/v1/catalog/products/unavailable', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: stillUnavailable
            ? [
                {
                  productId: '0198aabb-3333-7000-8000-000000000001',
                  productName: 'Pizza Calabresa',
                  isAvailable: false,
                  unavailableReason: 'Acabou a calabresa',
                  unavailableSince: '2026-08-02T20:00:00.000Z',
                },
              ]
            : [],
        }),
      });
    });

    await page.route('**/v1/catalog/products/*/availability', async (route) => {
      expect(route.request().headers()['idempotency-key']).toBeTruthy();
      const payload = route.request().postDataJSON() as { isAvailable: boolean };
      expect(payload.isAvailable).toBe(true);
      stillUnavailable = false;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          productId: '0198aabb-3333-7000-8000-000000000001',
          productName: 'Pizza Calabresa',
          isAvailable: true,
          unavailableReason: null,
          unavailableSince: null,
        }),
      });
    });

    await page.getByRole('button', { name: 'Indisponíveis' }).click();

    await expect(page.getByText('Pizza Calabresa')).toBeVisible();
    await page.getByRole('button', { name: 'Marcar disponível' }).click();

    await expect(page.getByText('Nenhum item indisponível')).toBeVisible();
  });
});
