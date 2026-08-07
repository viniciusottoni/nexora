import { expect, test, type Page } from '@playwright/test';

/**
 * US-152 · Visão 360 e acesso aos módulos do estabelecimento — cobre os cenários Gherkin do §4:
 * "Cadastro saudável" (checklist concluído, atalhos do mesmo tenant), "Provisionamento incompleto"
 * (instalação pendente, próxima ação segura), "Tentativa de abrir dado de negócio" (inicia o fluxo
 * da US-145, nunca acesso implícito) e "Recurso inexistente" (404 sem detalhe sensível). Também
 * cobre a estratégia de teste do §12: "Diretório → detalhe → próxima ação → retorno preservado".
 * Mesmo padrão de mock de rede (`page.route`) de `tests/e2e/tenant-directory.spec.ts`.
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

async function login(page: Page) {
  await page.goto('http://127.0.0.1:49174');
  await page.getByLabel('E-mail').fill('admin@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByRole('heading', { name: 'Visão geral' })).toBeVisible();
}

test.describe('Visão 360 do estabelecimento (US-152)', () => {
  test('cadastro saudável: mostra metadados, checklist concluído e atalhos do mesmo tenant', async ({ page }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);

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
            plan: 'COMPLETO',
            template: 'PIZZERIA',
            domain: 'donabetinha.com.br',
            createdAt: '2026-01-01T00:00:00Z',
            updatedAt: '2026-08-01T00:00:00Z',
          },
          owner: { name: 'Betina Souza', email: 'betina@example.com', inviteStatus: 'ACCEPTED' },
          stores: [{ id: '0198aabb-4444-7000-8000-000000000001', name: 'Matriz', timezone: 'America/Sao_Paulo' }],
          installations: [
            { id: '0198aabb-5555-7000-8000-000000000001', label: 'Servidor local — Matriz', status: 'ACTIVE', health: 'OK' },
          ],
          deployment: { completed: 9, total: 9, nextAction: null },
          links: { publicMenu: 'https://dona-betinha.plataforma.com.br', admin: null, health: null },
        }),
      });
    });

    await login(page);
    await page.goto(`http://127.0.0.1:49174/estabelecimentos/${tenantId}`);

    await expect(page.getByRole('heading', { name: 'Pizzaria Dona Betinha' })).toBeVisible();
    await expect(page.getByText('Betina Souza')).toBeVisible();
    await expect(page.getByText('Convite aceito')).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Matriz', exact: true })).toBeVisible();
    await expect(page.getByText('Implantação concluída')).toBeVisible();
    await expect(page.getByText('9/9')).toBeVisible();

    // Atalho de saúde/instalações aponta para a MESMA plataforma (nunca para outro tenant) —
    // US-152 §10 "atalhos devem apontar para recursos daquele mesmo tenant".
    await page.getByRole('button', { name: 'Ver instalações' }).click();
    await expect(page.getByRole('heading', { name: 'Instalações' })).toBeVisible();
  });

  test('provisionamento incompleto: mostra instalação pendente e a próxima ação segura', async ({ page }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);

    await page.route(`**/v1/platform/tenants/${tenantId}/overview`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          tenant: {
            id: tenantId,
            name: 'Hamburgueria do Zé',
            slug: 'hamburgueria-do-ze',
            // US-153 renomeou TRIAL → PROVISIONED/INSTALLING — provisionamento incompleto (4/9)
            // com instalação ainda pendente é o cenário de INSTALLING (sem ação manual disponível).
            status: 'INSTALLING',
            statusVersion: 1,
            availableTransitions: [],
            plan: 'OPERACAO',
            template: 'HAMBURGUERIA',
            domain: null,
            createdAt: '2026-08-01T00:00:00Z',
            updatedAt: '2026-08-01T00:00:00Z',
          },
          owner: { name: 'José Silva', email: 'ze@example.com', inviteStatus: 'PENDING' },
          stores: [{ id: '0198aabb-4444-7000-8000-000000000002', name: 'Matriz', timezone: 'America/Sao_Paulo' }],
          installations: [
            { id: '0198aabb-5555-7000-8000-000000000002', label: 'Servidor local — Matriz', status: 'PENDING', health: 'UNKNOWN' },
          ],
          deployment: { completed: 4, total: 9, nextAction: 'EDGE_INSTALL' },
          links: { publicMenu: null, admin: null, health: null },
        }),
      });
    });

    await login(page);
    await page.goto(`http://127.0.0.1:49174/estabelecimentos/${tenantId}`);

    await expect(page.getByRole('heading', { name: 'Hamburgueria do Zé' })).toBeVisible();
    await expect(page.getByText('Convite pendente')).toBeVisible();
    await expect(page.getByText('Pendente', { exact: true })).toBeVisible();
    await expect(page.getByText('4/9')).toBeVisible();
    // §10 "próxima ação mais importante destacada" — rótulo de negócio (PT-BR) do passo pendente.
    await expect(page.getByText('Servidor local instalado')).toBeVisible();
  });

  test('tentativa de abrir dado de negócio: inicia o fluxo de suporte da US-145, nunca acesso implícito', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);

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
            plan: 'COMPLETO',
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

    await login(page);
    await page.goto(`http://127.0.0.1:49174/estabelecimentos/${tenantId}`);
    await expect(page.getByRole('heading', { name: 'Pizzaria Dona Betinha' })).toBeVisible();

    // Nenhum dado de negócio (pedido/caixa/estoque/financeiro) aparece nesta ficha — RN-015.
    for (const forbidden of ['Pedido', 'Caixa', 'Estoque', 'Financeiro']) {
      await expect(page.getByText(forbidden, { exact: true })).not.toBeVisible();
    }

    await page.getByRole('button', { name: 'Solicitar acesso de suporte' }).click();

    // A ficha só INICIA o fluxo auditado da US-145 — motivo/duração continuam exigidos ali (campo
    // "Motivo" obrigatório, vazio aqui), nunca acesso implícito concedido a partir do detalhe.
    await expect(page.getByRole('heading', { name: 'Solicitar acesso de suporte' })).toBeVisible();
    await expect(page.getByLabel('Estabelecimento (id)')).toHaveValue(tenantId);
    await expect(page.getByLabel('Motivo')).toHaveValue('');
  });

  test('recurso inexistente: id de tenant sem correspondência devolve 404 sem informação sensível', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);

    const missingId = '0198aabb-9999-7000-8000-000000000099';
    await page.route(`**/v1/platform/tenants/${missingId}/overview`, async (route) => {
      await route.fulfill({
        status: 404,
        contentType: 'application/problem+json',
        body: JSON.stringify({ detail: 'Estabelecimento não encontrado.', code: 'TENANT_NOT_FOUND' }),
      });
    });

    await login(page);
    await page.goto(`http://127.0.0.1:49174/estabelecimentos/${missingId}`);

    // 404 sem detalhe sensível adicional — o id só ecoa o que o próprio administrador já digitou
    // na URL (não é dado novo vazado pelo servidor); o que NUNCA pode aparecer é qualquer
    // metadado do cadastro (nome, dono, lojas) que só existiria se o recurso fosse real.
    await expect(page.getByText('Estabelecimento não encontrado')).toBeVisible();
    await expect(page.getByText('Betina Souza')).not.toBeVisible();
    await expect(page.getByRole('heading', { name: 'Checklist de implantação' })).not.toBeVisible();
  });

  test('diretório → detalhe → retorno preserva a busca e os filtros aplicados', async ({ page }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);

    const tenant = {
      id: tenantId,
      name: 'Pizzaria Dona Betinha',
      slug: 'dona-betinha',
      status: 'ACTIVE',
      plan: 'COMPLETO',
      ownerEmail: 'betina@example.com',
      storesCount: 1,
      installationsCount: 1,
      health: 'OK',
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-08-01T00:00:00Z',
    };

    await page.route('**/v1/platform/tenants**', async (route) => {
      if (route.request().method() !== 'GET') return route.fallback();
      const url = new URL(route.request().url());
      const query = url.searchParams.get('query') ?? '';
      const matches = !query || tenant.name.toLowerCase().includes(query.toLowerCase()) ? [tenant] : [];
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: matches,
          nextCursor: null,
          appliedFilters: {
            query,
            status: [],
            plan: [],
            template: [],
            health: [],
            createdFrom: null,
            createdTo: null,
            sort: 'attention',
            limit: 25,
          },
        }),
      });
    });

    await page.route(`**/v1/platform/tenants/${tenantId}/overview`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          tenant: {
            id: tenantId,
            name: tenant.name,
            slug: tenant.slug,
            status: tenant.status,
            statusVersion: 5,
            availableTransitions: ['SUSPENDED', 'CANCELLED'],
            plan: tenant.plan,
            template: 'PIZZERIA',
            domain: null,
            createdAt: tenant.createdAt,
            updatedAt: tenant.updatedAt,
          },
          owner: { name: 'Betina Souza', email: tenant.ownerEmail, inviteStatus: 'ACCEPTED' },
          stores: [],
          installations: [],
          deployment: { completed: 9, total: 9, nextAction: null },
          links: { publicMenu: null, admin: null, health: null },
        }),
      });
    });

    await login(page);
    await page.getByRole('button', { name: 'Estabelecimentos', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Estabelecimentos' })).toBeVisible();

    await page.getByLabel('Buscar').fill('betinha');
    await expect(page).toHaveURL(/query=betinha/);
    await expect(page.getByText('Pizzaria Dona Betinha')).toBeVisible();

    await page.getByText('Pizzaria Dona Betinha').click();
    await expect(page.getByRole('heading', { name: 'Pizzaria Dona Betinha' })).toBeVisible();

    await page.goBack();

    await expect(page.getByRole('heading', { name: 'Estabelecimentos' })).toBeVisible();
    await expect(page).toHaveURL(/query=betinha/);
    await expect(page.getByLabel('Buscar')).toHaveValue('betinha');
  });
});

test.describe('Ciclo de vida do estabelecimento (US-153)', () => {
  test('suspender, visualizar impacto e reativar (§12 "E2E: Suspender, visualizar impacto e reativar")', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);

    let overviewTenant: {
      status: string;
      statusVersion: number;
      availableTransitions: string[];
    } = { status: 'ACTIVE', statusVersion: 5, availableTransitions: ['SUSPENDED', 'CANCELLED'] };

    await page.route(`**/v1/platform/tenants/${tenantId}/overview`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          tenant: {
            id: tenantId,
            name: 'Pizzaria Dona Betinha',
            slug: 'dona-betinha',
            ...overviewTenant,
            plan: 'COMPLETO',
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

    // POST /v1/platform/tenants/{id}/status-transitions — muta o estado em memória do mock, que a
    // rota de overview acima devolve na próxima chamada (o front sempre recarrega após sucesso).
    await page.route(`**/v1/platform/tenants/${tenantId}/status-transitions`, async (route) => {
      const request = route.request();
      const body = request.postDataJSON() as { targetStatus: 'ACTIVE' | 'SUSPENDED' | 'CANCELLED'; reason: string };
      expect(request.headers()['if-match']).toBe(`"${overviewTenant.statusVersion}"`);
      expect(body.reason.trim().length).toBeGreaterThan(0);

      const previousStatus = overviewTenant.status;
      overviewTenant = {
        status: body.targetStatus,
        statusVersion: overviewTenant.statusVersion + 1,
        availableTransitions: body.targetStatus === 'SUSPENDED' ? ['ACTIVE', 'CANCELLED'] : ['SUSPENDED', 'CANCELLED'],
      };

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          tenantId,
          previousStatus,
          status: overviewTenant.status,
          version: overviewTenant.statusVersion,
          changedAt: new Date().toISOString(),
        }),
      });
    });

    await login(page);
    await page.goto(`http://127.0.0.1:49174/estabelecimentos/${tenantId}`);
    await expect(page.getByRole('heading', { name: 'Pizzaria Dona Betinha' })).toBeVisible();
    await expect(page.getByText('Ativo', { exact: true })).toBeVisible();

    // Suspender — §10 "modal mostra impacto antes da confirmação" + "motivo obrigatório".
    await page.getByRole('button', { name: 'Suspender', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Suspender estabelecimento?' })).toBeVisible();
    await expect(
      page.getByText('Novas sessões de gestão e operação seguirão a política de suspensão até a reativação.'),
    ).toBeVisible();

    const confirmSuspend = page.getByRole('button', { name: 'Sim, suspender estabelecimento' });
    await expect(confirmSuspend).toBeDisabled();
    await page.getByLabel('Motivo').fill('Inadimplência — chamado #482');
    await expect(confirmSuspend).toBeEnabled();
    await confirmSuspend.click();

    await expect(page.getByRole('heading', { name: 'Suspender estabelecimento?' })).not.toBeVisible();
    await expect(page.getByText('Suspenso', { exact: true })).toBeVisible();

    // Reativar — a partir de SUSPENDED, "Reativar" some a validar dependências técnicas.
    await page.getByRole('button', { name: 'Reativar', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Reativar estabelecimento?' })).toBeVisible();
    await expect(page.getByText('As dependências técnicas serão validadas antes de reativar.')).toBeVisible();

    const confirmReactivate = page.getByRole('button', { name: 'Sim, reativar estabelecimento' });
    await expect(confirmReactivate).toBeDisabled();
    await page.getByLabel('Motivo').fill('Pagamento regularizado com o cliente');
    await confirmReactivate.click();

    await expect(page.getByRole('heading', { name: 'Reativar estabelecimento?' })).not.toBeVisible();
    await expect(page.getByText('Ativo', { exact: true })).toBeVisible();
  });
});
