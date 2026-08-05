import { expect, test, type Page } from '@playwright/test';

/**
 * US-146 · Atualização controlada do parque — cobre publicação de release + consulta de progresso
 * de rollout na tela `apps/web-platform/src/features/releases/publish-release-page.tsx`. Mesmo
 * padrão de mock de rede (`page.route`) de tests/e2e/tenant-provisioning.spec.ts/
 * platform-installations.spec.ts. Arquivo NOVO (não edita foundation.spec.ts nem os specs
 * existentes) para não colidir com outro agente em paralelo.
 *
 * DEPENDÊNCIA: o item de navegação "Versões" (rótulo sugerido no relatório desta história) ainda
 * não foi adicionado a `apps/web-platform/src/app.tsx` — mesma convenção já registrada por
 * platform-installations.spec.ts (US-140): a navegação central é wireada por outro passo para não
 * colidir com os demais agentes que também adicionam item de menu à mesma tela. Este spec assume
 * esse item já wireado; até lá, falha no passo de navegação — não é defeito da tela em si (os
 * testes de componente em publish-release-page.test.tsx já cobrem isso isoladamente).
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Admin de plataforma' },
  permissions: ['tenant:manage'],
};

async function mockLogin(page: Page) {
  await page.route('**/v1/auth/login', async (route) => {
    expect(route.request().headers()['idempotency-key']).toBeTruthy();
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(session) });
  });
}

async function loginAndOpenReleases(page: Page) {
  await mockLogin(page);
  await page.goto('http://127.0.0.1:49174');
  await page.getByLabel('E-mail').fill('admin@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await page.getByRole('link', { name: 'Versões' }).or(page.getByRole('button', { name: 'Versões' })).click();
  await expect(page.getByRole('heading', { name: 'Versões e liberação gradual' })).toBeVisible();
}

test.describe('Atualização controlada do parque (US-146)', () => {
  test('publicar uma versão nova mostra o progresso da liberação', async ({ page }) => {
    await loginAndOpenReleases(page);

    let publishCalls = 0;
    await page.route('**/v1/platform/releases', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.fallback();
        return;
      }
      publishCalls += 1;
      expect(route.request().headers()['idempotency-key']).toBeTruthy();
      expect(route.request().headers().authorization).toBe('Bearer access-token');

      const payload = route.request().postDataJSON() as { version: string; rolloutPercent: number };
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          release: {
            id: '0198aabb-7777-7000-8000-000000000001',
            version: payload.version,
            rolloutPercent: payload.rolloutPercent,
            notes: null,
            publishedAt: new Date().toISOString(),
            publishedBy: null,
          },
        }),
      });
    });

    await page.route('**/v1/platform/releases/1.5.0/rollout', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ total: 12, updated: 3, failed: 0, pending: 9 }),
      });
    });

    await page.getByLabel('Versão', { exact: true }).fill('1.5.0');
    await page.getByLabel('Percentual de liberação', { exact: true }).fill('10');
    await page.getByRole('button', { name: 'Publicar release' }).click();

    await expect(page.getByText('3 de 12')).toBeVisible();
    expect(publishCalls).toBe(1);
    await expect(page.getByText('Total elegível')).toBeVisible();
  });

  test('republicar com percentual menor mostra o erro de liberação que nunca reduz', async ({ page }) => {
    await loginAndOpenReleases(page);

    await page.route('**/v1/platform/releases', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 422,
        contentType: 'application/json',
        body: JSON.stringify({ code: 'RELEASE_ROLLOUT_CANNOT_DECREASE', detail: 'A liberação gradual nunca reduz.' }),
      });
    });

    await page.getByLabel('Versão', { exact: true }).fill('1.5.0');
    await page.getByLabel('Percentual de liberação', { exact: true }).fill('5');
    await page.getByRole('button', { name: 'Publicar release' }).click();

    await expect(page.getByText(/a liberação gradual nunca reduz/i)).toBeVisible();
  });

  test('consultar rollout com falhas mostra o alerta de rollback', async ({ page }) => {
    await loginAndOpenReleases(page);

    await page.route('**/v1/platform/releases/2.0.0/rollout', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ total: 10, updated: 6, failed: 2, pending: 2 }),
      });
    });

    await page.getByLabel('Versão para consultar').fill('2.0.0');
    await page.getByRole('button', { name: 'Consultar' }).click();

    await expect(page.getByText('Rollbacks nesta versão')).toBeVisible();
  });
});
