import { expect, test, type Page } from '@playwright/test';

/**
 * US-140 · Painel de instalações com saúde — mesmo padrão de mock de rede (`page.route`) de
 * tests/e2e/tenant-provisioning.spec.ts. Arquivo NOVO (não edita foundation.spec.ts nem
 * tenant-provisioning.spec.ts) para não colidir com outro agente em paralelo.
 *
 * DEPENDÊNCIA: o item de navegação "Instalações" (id `installations`) ainda não foi adicionado a
 * `apps/web-platform/src/app.tsx` — por convenção desta tarefa (US-140), a navegação central é
 * wireada por outro passo para não colidir com os demais agentes que também adicionam item de
 * menu à mesma tela. Este spec assume esse item (rótulo "Instalações", exatamente como sugerido no
 * relatório da história) já wireado; até lá, o teste falha no passo de navegação — não é um
 * defeito da tela em si (que os testes de componente em installations-panel-page.test.tsx já
 * cobrem isoladamente).
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Admin de plataforma' },
  permissions: ['tenant:manage'],
};

const okInstallation = {
  installationId: '0198aabb-3333-7000-8000-000000000001',
  tenantId: '0198aabb-3333-7000-8000-000000000002',
  tenantName: 'Pizzaria Dona Betinha',
  storeName: 'Matriz',
  version: '1.4.2',
  expectedVersion: '1.4.2',
  lastSeenAt: new Date().toISOString(),
  syncLagSeconds: 4,
  pendingEvents: 0,
  openAlerts: 0,
  health: 'OK',
};

const downInstallation = {
  installationId: '0198aabb-4444-7000-8000-000000000001',
  tenantId: '0198aabb-4444-7000-8000-000000000002',
  tenantName: 'Pizzaria do Zé',
  storeName: 'Centro',
  version: '1.3.0',
  expectedVersion: '1.4.2',
  lastSeenAt: new Date(Date.now() - 30 * 60_000).toISOString(),
  syncLagSeconds: 1800,
  pendingEvents: 42,
  openAlerts: 1,
  health: 'DOWN',
};

const degradedInstallation = {
  ...downInstallation,
  installationId: '0198aabb-5555-7000-8000-000000000001',
  tenantName: 'Pizzaria Degradada',
  health: 'DEGRADED',
};

async function mockLogin(page: Page) {
  await page.route('**/v1/auth/login', async (route) => {
    expect(route.request().headers()['idempotency-key']).toBeTruthy();
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(session) });
  });
}

async function loginAndOpenInstallations(page: Page) {
  await mockLogin(page);
  await page.goto('http://127.0.0.1:49174');
  await page.getByLabel('E-mail').fill('admin@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await page.getByRole('link', { name: 'Instalações' }).or(page.getByRole('button', { name: 'Instalações' })).click();
  await expect(page.getByRole('heading', { name: 'Instalações' })).toBeVisible();
}

test.describe('Painel de instalações com saúde (US-140)', () => {
  test('visão consolidada mostra versão, último contato, atraso de sincronização e saúde', async ({ page }) => {
    await page.route('**/v1/platform/installations', async (route) => {
      expect(route.request().headers().authorization).toBe('Bearer access-token');
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [downInstallation, okInstallation] }),
      });
    });

    await loginAndOpenInstallations(page);

    await expect(page.getByText('Pizzaria Dona Betinha')).toBeVisible();
    await expect(page.getByText('Pizzaria do Zé')).toBeVisible();
    await expect(page.getByText('Saudável')).toBeVisible();
    await expect(page.getByText('Fora do ar')).toBeVisible();
    await expect(page.getByText('desatualizada')).toBeVisible();
  });

  test('instalação degradada aparece distinta de fora do ar', async ({ page }) => {
    await page.route('**/v1/platform/installations', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [downInstallation, degradedInstallation] }),
      });
    });

    await loginAndOpenInstallations(page);

    await expect(page.getByText('Fora do ar', { exact: true })).toBeVisible();
    await expect(page.getByText('Degradada', { exact: true })).toBeVisible();
  });

  test('lista vazia mostra estado vazio', async ({ page }) => {
    await page.route('**/v1/platform/installations', async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ data: [] }) });
    });

    await loginAndOpenInstallations(page);

    await expect(page.getByText('Nenhuma instalação ativa')).toBeVisible();
  });

  test('abrir uma instalação mostra diagnóstico e histórico de incidentes sem sair do painel', async ({ page }) => {
    await page.route('**/v1/platform/installations', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [downInstallation] }),
      });
    });

    await page.route('**/v1/platform/installations/*/diagnostics', async (route) => {
      expect(route.request().headers().authorization).toBe('Bearer access-token');
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          healthCheck: { postgres: 'OK', redis: null, sync: 'DOWN', checkedAt: new Date().toISOString() },
          recentLogs: [
            { occurredAt: new Date().toISOString(), level: 'ERROR', message: 'Instalação detectou queda da própria conectividade com a nuvem.' },
          ],
          diskUsagePercent: 58,
          lastBackupAt: null,
        }),
      });
    });

    await page.route('**/v1/platform/installations/*/incidents', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: [
            {
              id: '0198aabb-6666-7000-8000-000000000001',
              type: 'OFFLINE',
              startedAt: new Date(Date.now() - 30 * 60_000).toISOString(),
              resolvedAt: null,
              cause: 'Sem contato com a nuvem há 30 minutos.',
              durationSeconds: 1800,
            },
          ],
        }),
      });
    });

    await loginAndOpenInstallations(page);

    await page.getByText('Pizzaria do Zé').click();

    await expect(page.getByRole('dialog')).toBeVisible();
    await expect(page.getByText('Instalação detectou queda da própria conectividade com a nuvem.')).toBeVisible();
    await expect(page.getByText('Sem contato com a nuvem há 30 minutos.')).toBeVisible();
    // RN-015: nada no diálogo de diagnóstico deve parecer dado de negócio (pedido/pagamento/cliente).
    await expect(page.getByRole('dialog')).not.toContainText('R$');
  });
});
