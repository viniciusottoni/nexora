import { expect, test, type Page } from '@playwright/test';

/**
 * US-157 · Central operacional, auditoria e atalhos de suporte — mesmo padrão de mock de rede
 * (`page.route`) de tests/e2e/platform-installations.spec.ts. Cobre o caminho "card global → lista
 * filtrada → detalhe → link de diagnóstico/suporte" pedido pela DoD: da visão geral (US-150) até a
 * central de atenção (US-157), e dela até o painel de instalações (US-140)/auditoria de suporte
 * (US-145) — nunca criando um token de suporte silenciosamente (RN-015).
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Admin de plataforma' },
  permissions: ['tenant:manage'],
};

const TENANT_ID = '0198aabb-7000-7000-8000-000000000002';
const WEB_PLATFORM_BASE_URL = process.env.WEB_PLATFORM_BASE_URL ?? 'http://127.0.0.1:49174';

const offlineItem = {
  id: `INSTALLATION_OFFLINE|${TENANT_ID}|0198aabb-7000-7000-8000-000000000003`,
  tenantId: TENANT_ID,
  tenantName: 'Pizzaria Dona Betinha',
  type: 'INSTALLATION_OFFLINE',
  severity: 'CRITICAL',
  since: new Date(Date.now() - 90 * 60_000).toISOString(),
  reason: 'Sem contato há 1 h 30 min',
  action: { kind: 'OPEN_DIAGNOSTICS', href: '/instalacoes' },
};

const inviteItem = {
  id: `INVITE_EXPIRED|${TENANT_ID}|0198aabb-7000-7000-8000-000000000004`,
  tenantId: TENANT_ID,
  tenantName: 'Pizzaria do Zé',
  type: 'INVITE_EXPIRED',
  severity: 'MEDIUM',
  since: new Date(Date.now() - 5 * 86_400_000).toISOString(),
  reason: 'Convite expirado há 5 dias, proprietário ainda sem acesso',
  action: { kind: 'OPEN_TENANT', href: `/estabelecimentos/${TENANT_ID}` },
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

/** US-150 — sonda de autorização do shell (`GET /v1/platform/summary`), chamada antes de qualquer rota renderizar. */
async function mockPlatformSummary(page: Page, attention = 1) {
  await page.route('**/v1/platform/summary', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        tenants: { total: 5, active: 4, attention },
        installations: { healthy: 3, degraded: 0, offline: 1 },
        pendingInvites: 1,
        generatedAt: new Date().toISOString(),
      }),
    });
  });
}

async function mockAttentionQueue(page: Page, data: unknown[] = [offlineItem, inviteItem]) {
  await page.route('**/v1/platform/attention*', async (route) => {
    expect(route.request().headers().authorization).toBe('Bearer access-token');
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        data,
        nextCursor: null,
        meta: { collectedAt: new Date().toISOString(), unavailableSources: [] },
      }),
    });
  });
}

async function mockEmptyInstallations(page: Page) {
  await page.route('**/v1/platform/installations', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: [] }),
    });
  });
}

async function login(page: Page) {
  await page.goto(WEB_PLATFORM_BASE_URL);
  await page.getByLabel('E-mail').fill('admin@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
}

test.describe('Central de atenção (US-157)', () => {
  test('card da visão geral leva à lista filtrada, que leva ao diagnóstico da instalação', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);
    await mockAttentionQueue(page);
    await mockEmptyInstallations(page);

    await login(page);
    await expect(page.getByRole('heading', { name: 'Visão geral' })).toBeVisible();

    // Card de resumo → lista filtrada (US-157 "Cards de resumo levam diretamente à lista filtrada").
    await page.getByRole('button', { name: 'Ver fila de atenção' }).click();

    await expect(page.getByRole('heading', { name: 'Central de atenção' })).toBeVisible();
    await expect(page.getByText('Pizzaria Dona Betinha')).toBeVisible();
    await expect(page.getByText('Sem contato há 1 h 30 min')).toBeVisible();
    await expect(page.getByText('Pizzaria do Zé')).toBeVisible();
    await expect(
      page.getByText('Convite expirado há 5 dias, proprietário ainda sem acesso'),
    ).toBeVisible();

    // Lista → diagnóstico (US-140), sem sair da central antes de decidir.
    await page.getByRole('button', { name: 'Ver diagnóstico' }).click();
    await expect(page.getByRole('heading', { name: 'Instalações' })).toBeVisible();
  });

  test('atalho de suporte encaminha ao fluxo autorizado da US-145 sem criar token silenciosamente', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);
    await mockAttentionQueue(page, [offlineItem]);

    let supportAccessGranted = false;
    await page.route('**/v1/platform/tenants/*/support-access', async (route) => {
      supportAccessGranted = true;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({}),
      });
    });

    await login(page);
    await page.getByRole('button', { name: 'Central de atenção', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Central de atenção' })).toBeVisible();

    await page.getByRole('button', { name: /Solicitar suporte/ }).click();

    await expect(page.getByRole('heading', { name: 'Solicitar acesso de suporte' })).toBeVisible();
    await expect(page.getByLabel('Estabelecimento (id)')).toHaveValue(TENANT_ID);
    expect(supportAccessGranted).toBe(false);
  });

  test('priorização explicável: severidade, motivo e ordenação por criticidade sem esconder itens menos graves', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);
    await mockAttentionQueue(page);

    await login(page);
    await page.getByRole('button', { name: 'Central de atenção', exact: true }).click();

    const items = page.getByRole('listitem');
    await expect(items).toHaveCount(2);
    // O item CRITICAL aparece antes do MEDIUM — sem esconder o menos grave.
    await expect(items.nth(0)).toContainText('Pizzaria Dona Betinha');
    await expect(items.nth(1)).toContainText('Pizzaria do Zé');
  });

  test('falha parcial: fonte indisponível não derruba a central, e o horário da última coleta continua visível', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);
    await page.route('**/v1/platform/attention*', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: [inviteItem],
          nextCursor: null,
          meta: {
            collectedAt: new Date().toISOString(),
            unavailableSources: ['INSTALLATION_HEALTH'],
          },
        }),
      });
    });

    await login(page);
    await page.getByRole('button', { name: 'Central de atenção', exact: true }).click();

    await expect(page.getByText('Pizzaria do Zé')).toBeVisible();
    await expect(
      page.getByText(/Fontes indisponíveis nesta coleta: INSTALLATION_HEALTH/),
    ).toBeVisible();
    await expect(page.getByText(/Última coleta:/)).toBeVisible();
  });
});
