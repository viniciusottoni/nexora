import { expect, test, type Page } from '@playwright/test';

/**
 * US-154 · Gestão de planos e configuração comercial — mesmo padrão de mock de rede
 * (`page.route`) de `tests/e2e/tenant-detail.spec.ts`. Cobre a estratégia de teste do §12
 * ("E2E: Alterar plano, confirmar impacto e visualizar histórico"): consulta do plano atual,
 * upgrade com vigência imediata (comparação antes/depois + motivo obrigatório), agendamento com
 * vigência futura, plano desconhecido (422) e divergência com reconciliação.
 *
 * ESTADO CONHECIDO (ver relatório final da tarefa): `TenantPlanSection` (o componente que estas
 * rotas exercitam) ainda não está renderizado dentro de `tenant-detail-page.tsx` — essa integração
 * é central e vem depois desta tarefa (o componente foi entregue autocontido, de propósito, para
 * não editar `tenant-detail-page.tsx`/`app.tsx`, hotspots compartilhados com US-155/US-156). Até
 * essa integração acontecer, os testes abaixo ficam VERMELHOS ao navegar para a ficha real do
 * estabelecimento (a seção de plano simplesmente não existe na página ainda) — não é um bug desta
 * tarefa, é a lacuna documentada.
 */

const adminSession = {
  accessToken: 'access-token-admin',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Admin de plataforma' },
  permissions: ['tenant:manage'],
};

const platformSummary = {
  tenants: { total: 1, active: 1, attention: 0 },
  installations: { healthy: 1, degraded: 0, offline: 0 },
  pendingInvites: 0,
  generatedAt: new Date().toISOString(),
};

const tenantId = '0198aabb-3333-7000-8000-000000000001';

const catalog = {
  data: [
    { code: 'STANDARD', name: 'Standard', active: true, capabilities: ['online_ordering', 'kds'] },
    { code: 'GESTAO', name: 'Gestão', active: true, capabilities: ['online_ordering', 'kds', 'inventory'] },
    { code: 'COMPLETO', name: 'Completo', active: true, capabilities: ['online_ordering', 'kds', 'inventory', 'multi_store'] },
  ],
};

async function mockLogin(page: Page) {
  await page.route('**/v1/auth/login', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(adminSession) });
  });
}

async function mockPlatformSummary(page: Page) {
  await page.route('**/v1/platform/summary', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(platformSummary) });
  });
}

async function mockTenantOverview(page: Page) {
  await page.route(`**/v1/platform/tenants/${tenantId}/overview`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        tenant: {
          id: tenantId,
          name: 'Pizzaria Dona Betinha',
          slug: 'dona-betinha',
          status: 'ACTIVE',
          statusVersion: 5,
          availableTransitions: ['SUSPENDED', 'CANCELLED'],
          plan: 'GESTAO',
          template: 'PIZZERIA',
          domain: null,
          createdAt: '2026-01-01T00:00:00Z',
          updatedAt: '2026-08-01T00:00:00Z',
        },
        owner: { name: 'Betina Souza', email: 'betina@example.com', inviteStatus: 'ACCEPTED' },
        stores: [],
        installations: [],
        deployment: { completed: 9, total: 9, nextAction: null },
        links: { publicMenu: null, admin: null, health: null },
      }),
    });
  });
}

async function mockPlanCatalog(page: Page) {
  await page.route('**/v1/platform/plans', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(catalog) });
  });
}

async function login(page: Page) {
  await page.goto('http://127.0.0.1:49174');
  await page.getByLabel('E-mail').fill('admin@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByRole('heading', { name: 'Visão geral' })).toBeVisible();
}

test.describe('Plano comercial do estabelecimento (US-154)', () => {
  test('consulta o plano atual, capacidades efetivas e altera com vigência imediata (comparação antes/depois + motivo obrigatório)', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);
    await mockTenantOverview(page);
    await mockPlanCatalog(page);

    let currentPlan: { current: string; scheduled: unknown; consistent: boolean; version: number } = {
      current: 'GESTAO',
      scheduled: null,
      consistent: true,
      version: 3,
    };

    await page.route(`**/v1/platform/tenants/${tenantId}/plan`, async (route) => {
      const request = route.request();
      if (request.method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            current: currentPlan.current,
            effectiveCapabilities: catalog.data.find((p) => p.code === currentPlan.current)?.capabilities ?? [],
            scheduled: currentPlan.scheduled,
            consistent: currentPlan.consistent,
            version: currentPlan.version,
          }),
        });
        return;
      }

      if (request.method() === 'PUT') {
        const body = request.postDataJSON() as { plan: string; effectiveAt?: string; reason: string };
        expect(request.headers()['if-match']).toBe(`"${currentPlan.version}"`);
        expect(body.reason.trim().length).toBeGreaterThan(0);

        currentPlan = { current: body.plan, scheduled: null, consistent: true, version: currentPlan.version + 1 };
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ current: currentPlan.current, scheduled: null, version: currentPlan.version }),
        });
        return;
      }

      await route.fallback();
    });

    await login(page);
    await page.goto(`http://127.0.0.1:49174/estabelecimentos/${tenantId}`);
    await expect(page.getByRole('heading', { name: 'Pizzaria Dona Betinha' })).toBeVisible();

    await expect(page.getByText('GESTAO')).toBeVisible();

    await page.getByRole('button', { name: /Alterar plano/ }).click();
    await expect(page.getByRole('heading', { name: 'Alterar plano' })).toBeVisible();

    const confirmButton = page.getByRole('button', { name: 'Confirmar mudança' });
    await expect(confirmButton).toBeDisabled();

    await page.getByLabel(/Novo plano/).selectOption('COMPLETO');
    await expect(page.getByText('Antes (GESTAO)')).toBeVisible();
    await expect(page.getByText('Depois (Completo)')).toBeVisible();

    await page.getByLabel(/Data de vigência/).fill('2026-01-01T00:00');
    await expect(confirmButton).toBeDisabled();

    await page.getByLabel(/Motivo/).fill('Aditivo contratual #32');
    await expect(confirmButton).toBeEnabled();
    await confirmButton.click();

    await expect(page.getByRole('heading', { name: 'Alterar plano' })).not.toBeVisible();
    await expect(page.getByText('COMPLETO')).toBeVisible();
  });

  test('agendar mudança com vigência futura mantém o plano atual e mostra o agendamento', async ({ page }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);
    await mockTenantOverview(page);
    await mockPlanCatalog(page);

    await page.route(`**/v1/platform/tenants/${tenantId}/plan`, async (route) => {
      const request = route.request();
      if (request.method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            current: 'GESTAO',
            effectiveCapabilities: ['online_ordering', 'kds', 'inventory'],
            scheduled: { plan: 'COMPLETO', effectiveAt: '2027-01-01T00:00:00Z' },
            consistent: true,
            version: 3,
          }),
        });
        return;
      }

      if (request.method() === 'PUT') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            current: 'GESTAO',
            scheduled: { plan: 'COMPLETO', effectiveAt: '2027-01-01T00:00:00Z' },
            version: 4,
          }),
        });
        return;
      }

      await route.fallback();
    });

    await login(page);
    await page.goto(`http://127.0.0.1:49174/estabelecimentos/${tenantId}`);

    await expect(page.getByText(/COMPLETO a partir de/)).toBeVisible();
    await expect(page.getByText('GESTAO')).toBeVisible();
  });

  test('plano desconhecido: 422 PLAN_NOT_AVAILABLE aparece no modal e não fecha o diálogo', async ({ page }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);
    await mockTenantOverview(page);
    await mockPlanCatalog(page);

    await page.route(`**/v1/platform/tenants/${tenantId}/plan`, async (route) => {
      const request = route.request();
      if (request.method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            current: 'GESTAO',
            effectiveCapabilities: ['online_ordering', 'kds', 'inventory'],
            scheduled: null,
            consistent: true,
            version: 3,
          }),
        });
        return;
      }

      if (request.method() === 'PUT') {
        await route.fulfill({
          status: 422,
          contentType: 'application/problem+json',
          body: JSON.stringify({ detail: 'Plano comercial não disponível.', code: 'PLAN_NOT_AVAILABLE' }),
        });
        return;
      }

      await route.fallback();
    });

    await login(page);
    await page.goto(`http://127.0.0.1:49174/estabelecimentos/${tenantId}`);

    await page.getByRole('button', { name: /Alterar plano/ }).click();
    await page.getByLabel(/Novo plano/).selectOption('COMPLETO');
    await page.getByLabel(/Data de vigência/).fill('2026-01-01T00:00');
    await page.getByLabel(/Motivo/).fill('Teste');
    await page.getByRole('button', { name: 'Confirmar mudança' }).click();

    await expect(page.getByText('Plano comercial não disponível.')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Alterar plano' })).toBeVisible();
  });

  test('divergência detectada: alerta administrativo com ação de reconciliar (§4, "sem correção automática silenciosa")', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);
    await mockTenantOverview(page);
    await mockPlanCatalog(page);

    let reconciled = false;

    await page.route(`**/v1/platform/tenants/${tenantId}/plan`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          current: 'GESTAO',
          effectiveCapabilities: reconciled ? ['online_ordering', 'kds', 'inventory'] : [],
          scheduled: null,
          consistent: reconciled,
          version: 3,
        }),
      });
    });

    await page.route(`**/v1/platform/tenants/${tenantId}/plan/reconciliations`, async (route) => {
      reconciled = true;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          current: 'GESTAO',
          effectiveCapabilities: ['online_ordering', 'kds', 'inventory'],
          consistent: true,
          changed: true,
        }),
      });
    });

    await login(page);
    await page.goto(`http://127.0.0.1:49174/estabelecimentos/${tenantId}`);

    await expect(page.getByText('Divergência entre plano e configuração')).toBeVisible();
    await page.getByRole('button', { name: 'Reconciliar agora' }).click();

    await expect(page.getByText('Divergência entre plano e configuração')).not.toBeVisible();
  });
});
