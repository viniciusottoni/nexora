import { expect, test, type Page } from '@playwright/test';

/**
 * US-151 · Diretório de estabelecimentos com busca e filtros — cobre os cenários Gherkin do §4:
 * "Listagem inicial" (linha abre o detalhe), "Busca combinada com filtros" (URL reflete os dois),
 * "Base grande" (paginação por cursor sem repetir/omitir) e "Usuário sem autorização global"
 * (403, sem vazamento de dado). Mesmo padrão de mock de rede (`page.route`) dos demais specs de
 * web-platform (ver tests/e2e/platform-shell.spec.ts).
 *
 * A rota de detalhe (`/estabelecimentos/{id}`) tem conteúdo próprio desde a US-152 — a cobertura
 * completa da ficha administrativa (US-152 §4 Gherkin) vive em `tests/e2e/tenant-detail.spec.ts`;
 * aqui o teste "listagem inicial" só confirma que a linha do diretório navega para o destino real.
 */

const adminSession = {
  accessToken: 'access-token-admin',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Admin de plataforma' },
  permissions: ['tenant:manage'],
};

const nonAdminSession = {
  accessToken: 'access-token-sem-permissao',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000002', name: 'Sem permissão de plataforma' },
  permissions: [],
};

const platformSummary = {
  tenants: { total: 2, active: 1, attention: 1 },
  installations: { healthy: 2, degraded: 0, offline: 0 },
  pendingInvites: 0,
  generatedAt: new Date().toISOString(),
};

const donaBetinha = {
  id: '0198aabb-3333-7000-8000-000000000001',
  name: 'Pizzaria Dona Betinha',
  slug: 'dona-betinha',
  status: 'ACTIVE',
  plan: 'COMPLETO',
  ownerEmail: 'dono@example.com',
  storesCount: 1,
  installationsCount: 1,
  health: 'OK',
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-08-01T00:00:00Z',
};

const hamburgueriaDoZe = {
  id: '0198aabb-3333-7000-8000-000000000002',
  name: 'Hamburgueria do Zé',
  slug: 'hamburgueria-do-ze',
  // US-153 · Ciclo de vida do estabelecimento renomeou TRIAL → PROVISIONED e acrescentou
  // INSTALLING ao enum canônico — este tenant segue "em implantação" (0 instalações ainda).
  status: 'INSTALLING',
  plan: 'OPERACAO',
  ownerEmail: 'ze@example.com',
  storesCount: 1,
  installationsCount: 0,
  health: 'UNKNOWN',
  createdAt: '2026-02-01T00:00:00Z',
  updatedAt: '2026-07-01T00:00:00Z',
};

function appliedFilters(overrides: Record<string, unknown> = {}) {
  return {
    query: '',
    status: [],
    plan: [],
    template: [],
    health: [],
    createdFrom: null,
    createdTo: null,
    sort: 'attention',
    limit: 25,
    ...overrides,
  };
}

async function mockLogin(page: Page, session: typeof adminSession) {
  await page.route('**/v1/dev/auth/otp', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ otp: '123456', expiresInSeconds: 19 }),
    });
  });
  await page.route('**/v1/auth/login', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(session),
    });
  });
}

async function mockPlatformSummary(page: Page, status = 200) {
  await page.route('**/v1/platform/summary', async (route) => {
    await route.fulfill({
      status,
      contentType: status === 200 ? 'application/json' : 'application/problem+json',
      body: JSON.stringify(
        status === 200
          ? platformSummary
          : { detail: 'Acesso restrito à administração da plataforma.', code: 'FORBIDDEN' },
      ),
    });
  });
}

async function mockTenantOverview(page: Page) {
  const unavailable = {
    detail: 'Recurso indisponível neste cenário de diretório.',
    code: 'NOT_FOUND',
  };
  for (const path of [
    '**/v1/platform/plans',
    `**/v1/platform/tenants/${donaBetinha.id}/plan`,
    `**/v1/platform/tenants/${donaBetinha.id}/ownership`,
    `**/v1/platform/tenants/${donaBetinha.id}/deployment`,
    `**/v1/platform/tenants/${donaBetinha.id}/administrative-timeline`,
  ]) {
    await page.route(path, async (route) => {
      await route.fulfill({
        status: 404,
        contentType: 'application/problem+json',
        body: JSON.stringify(unavailable),
      });
    });
  }

  await page.route(`**/v1/platform/tenants/${donaBetinha.id}/overview`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        tenant: {
          id: donaBetinha.id,
          name: donaBetinha.name,
          slug: donaBetinha.slug,
          status: donaBetinha.status,
          statusVersion: 1,
          availableTransitions: ['SUSPENDED', 'CANCELLED'],
          plan: donaBetinha.plan,
          template: 'PIZZERIA',
          domain: null,
          createdAt: donaBetinha.createdAt,
          updatedAt: donaBetinha.updatedAt,
        },
        owner: { name: 'Betina Souza', email: donaBetinha.ownerEmail, inviteStatus: 'ACCEPTED' },
        stores: [
          {
            id: '0198aabb-4444-7000-8000-000000000001',
            name: 'Matriz',
            timezone: 'America/Sao_Paulo',
          },
        ],
        installations: [
          {
            id: '0198aabb-5555-7000-8000-000000000001',
            label: 'Servidor local — Matriz',
            status: 'ACTIVE',
            health: 'OK',
          },
        ],
        deployment: { completed: 9, total: 9, nextAction: null },
        links: { publicMenu: null, admin: null, health: null },
      }),
    });
  });
}

async function login(page: Page) {
  await page.goto('http://127.0.0.1:49174');
  await page.getByLabel('E-mail').fill('admin@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByRole('heading', { name: 'Visão geral' })).toBeVisible();
  await page.getByRole('button', { name: 'Estabelecimentos', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Estabelecimentos' })).toBeVisible();
}

test.describe('Diretório de estabelecimentos com busca e filtros (US-151)', () => {
  test('listagem inicial ordenada por criticidade, e cada linha abre o detalhe do estabelecimento', async ({
    page,
  }) => {
    await mockLogin(page, adminSession);
    await mockPlatformSummary(page);

    await page.route('**/v1/platform/tenants**', async (route) => {
      if (route.request().method() !== 'GET') return route.fallback();
      const url = new URL(route.request().url());
      // Só a listagem em si — sub-recursos como .../plan, .../ownership, .../deployment e
      // .../administrative-timeline (US-154/155/156/157) também combinam com o glob amplo
      // `**/v1/platform/tenants**` e têm forma de resposta diferente; devolvê-los aqui quebraria
      // as seções novas da ficha do estabelecimento sem relação com este teste.
      if (url.pathname !== '/v1/platform/tenants') return route.fallback();
      // Critério de aceite "ordenada por criticidade e atualização" — `attention` é o default
      // enviado pelo diretório sem que o administrador precise escolher ordenação nenhuma.
      expect(url.searchParams.get('sort')).toBe('attention');
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: [donaBetinha, hamburgueriaDoZe],
          nextCursor: null,
          appliedFilters: appliedFilters(),
        }),
      });
    });
    await mockTenantOverview(page);

    await login(page);

    await expect(page.getByText('Pizzaria Dona Betinha')).toBeVisible();
    await expect(page.getByText('Hamburgueria do Zé')).toBeVisible();

    const tenantAction = page.getByRole('button', { name: 'Pizzaria Dona Betinha', exact: true });
    await tenantAction.focus();
    await expect(tenantAction).toBeFocused();
    await page.keyboard.press('Enter');

    await expect(page.getByRole('heading', { name: 'Pizzaria Dona Betinha' })).toBeVisible();
    await expect(page).toHaveURL(new RegExp(`/estabelecimentos/${donaBetinha.id}$`));
  });

  test('busca combinada com filtro de status: só correspondências ativas aparecem, URL reflete os dois', async ({
    page,
  }) => {
    await mockLogin(page, adminSession);
    await mockPlatformSummary(page);
    await mockTenantOverview(page);

    await page.route('**/v1/platform/tenants**', async (route) => {
      if (route.request().method() !== 'GET') return route.fallback();
      const url = new URL(route.request().url());
      // Só a listagem em si — sub-recursos como .../plan, .../ownership, .../deployment e
      // .../administrative-timeline (US-154/155/156/157) também combinam com o glob amplo
      // `**/v1/platform/tenants**` e têm forma de resposta diferente; devolvê-los aqui quebraria
      // as seções novas da ficha do estabelecimento sem relação com este teste.
      if (url.pathname !== '/v1/platform/tenants') return route.fallback();
      const query = url.searchParams.get('query') ?? '';
      const statusFilters = url.searchParams.getAll('status');

      const matches = [donaBetinha, hamburgueriaDoZe].filter((tenant) => {
        const matchesQuery = !query || tenant.name.toLowerCase().includes(query.toLowerCase());
        const matchesStatus = statusFilters.length === 0 || statusFilters.includes(tenant.status);
        return matchesQuery && matchesStatus;
      });

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: matches,
          nextCursor: null,
          appliedFilters: appliedFilters({ query, status: statusFilters }),
        }),
      });
    });

    await login(page);
    await expect(page.getByText('Pizzaria Dona Betinha')).toBeVisible();
    await expect(page.getByText('Hamburgueria do Zé')).toBeVisible();

    await page.getByLabel('Buscar').fill('betinha');
    await page.getByLabel('Status').selectOption('ACTIVE');

    await expect(page.getByText('Pizzaria Dona Betinha')).toBeVisible();
    await expect(page.getByText('Hamburgueria do Zé')).not.toBeVisible();

    await expect(page).toHaveURL(/query=betinha/);
    await expect(page).toHaveURL(/status=ACTIVE/);

    await page.getByText('Pizzaria Dona Betinha').click();
    await expect(page.getByRole('heading', { name: 'Pizzaria Dona Betinha' })).toBeVisible();
    await expect(page).toHaveURL(
      /\/estabelecimentos\/0198aabb-3333-7000-8000-000000000001\?query=betinha&status=ACTIVE$/,
    );

    await page.getByRole('button', { name: /Voltar ao diretório/ }).click();
    await expect(page.getByRole('heading', { name: 'Estabelecimentos' })).toBeVisible();
    await expect(page.getByLabel('Buscar')).toHaveValue('betinha');
    await expect(page).toHaveURL(/\/estabelecimentos\?query=betinha&status=ACTIVE$/);
  });

  test('base grande: paginação por cursor avança sem repetir nem omitir registros', async ({
    page,
  }) => {
    await mockLogin(page, adminSession);
    await mockPlatformSummary(page);

    const pageTwoTenant = {
      ...hamburgueriaDoZe,
      id: '0198aabb-3333-7000-8000-000000000003',
      name: 'Restaurante Bom Sabor',
      slug: 'bom-sabor',
    };

    await page.route('**/v1/platform/tenants**', async (route) => {
      if (route.request().method() !== 'GET') return route.fallback();
      const url = new URL(route.request().url());
      // Só a listagem em si — sub-recursos como .../plan, .../ownership, .../deployment e
      // .../administrative-timeline (US-154/155/156/157) também combinam com o glob amplo
      // `**/v1/platform/tenants**` e têm forma de resposta diferente; devolvê-los aqui quebraria
      // as seções novas da ficha do estabelecimento sem relação com este teste.
      if (url.pathname !== '/v1/platform/tenants') return route.fallback();
      const cursor = url.searchParams.get('cursor');

      if (!cursor) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            data: [donaBetinha],
            nextCursor: 'cursor-pagina-2',
            appliedFilters: appliedFilters(),
          }),
        });
        return;
      }
      if (cursor === 'cursor-pagina-2') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            data: [pageTwoTenant],
            nextCursor: null,
            appliedFilters: appliedFilters(),
          }),
        });
        return;
      }
      await route.fulfill({ status: 400, contentType: 'application/problem+json', body: '{}' });
    });

    await login(page);
    await expect(page.getByText('Pizzaria Dona Betinha')).toBeVisible();
    await expect(page.getByText('Restaurante Bom Sabor')).not.toBeVisible();

    await page.getByRole('button', { name: /Próxima página/ }).click();

    await expect(page.getByText('Restaurante Bom Sabor')).toBeVisible();
    await expect(page.getByText('Pizzaria Dona Betinha')).not.toBeVisible();

    await page.getByRole('button', { name: /Página anterior/ }).click();

    await expect(page.getByText('Pizzaria Dona Betinha')).toBeVisible();
    await expect(page.getByText('Restaurante Bom Sabor')).not.toBeVisible();
  });

  test('usuário sem policy PlatformAdmin recebe 403 e nenhum dado do diretório é exposto', async ({
    page,
  }) => {
    await mockLogin(page, nonAdminSession);
    await mockPlatformSummary(page, 403);
    let directoryRequested = false;
    await page.route('**/v1/platform/tenants**', async (route) => {
      directoryRequested = true;
      await route.fulfill({
        status: 403,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          detail: 'Acesso restrito à administração da plataforma.',
          code: 'FORBIDDEN',
        }),
      });
    });

    await page.goto('http://127.0.0.1:49174');
    await page.getByLabel('E-mail').fill('admin@example.com');
    await page.getByLabel('Senha').fill('senha-segura');
    await page.getByRole('button', { name: 'Entrar' }).click();

    await expect(page.getByText('Acesso negado')).toBeVisible();
    await expect(page.getByText('Pizzaria Dona Betinha')).not.toBeVisible();
    // A sonda de plataforma nega antes de o shell renderizar qualquer rota — o diretório nunca chega a pedir dado.
    expect(directoryRequested).toBe(false);
  });
});
