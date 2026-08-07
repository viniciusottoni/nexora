import { expect, test, type Page } from '@playwright/test';

/**
 * US-150 · Estrutura e navegação do painel de plataforma — cobre o percurso do §12 ("E2E: Login →
 * raiz → lista → novo estabelecimento → retorno à lista") e os cenários Gherkin de acesso negado e
 * recurso inexistente. Mesmo padrão de mock de rede (`page.route`) dos demais specs de
 * web-platform (ver tests/e2e/tenant-provisioning.spec.ts).
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
  tenants: { total: 1, active: 1, attention: 0 },
  installations: { healthy: 1, degraded: 0, offline: 0 },
  pendingInvites: 0,
  generatedAt: new Date().toISOString(),
};

const provisionedTenant = {
  id: '0198aabb-2222-7000-8000-000000000001',
  slug: 'pizzaria-nova',
  name: 'Pizzaria Nova',
  plan: 'COMPLETO',
  status: 'ACTIVE',
  createdAt: new Date().toISOString(),
};

// US-151 · Diretório de estabelecimentos — payload de `GET /v1/platform/tenants` do diretório
// (mesmo endpoint do provisionamento mínimo da US-150, agora com busca/filtro/paginação).
function directoryItem(tenant: typeof provisionedTenant) {
  return {
    id: tenant.id,
    name: tenant.name,
    slug: tenant.slug,
    status: tenant.status,
    plan: tenant.plan,
    ownerEmail: 'dono@example.com',
    storesCount: 1,
    installationsCount: 1,
    health: 'OK',
    createdAt: tenant.createdAt,
    updatedAt: tenant.createdAt,
  };
}

const checklist = [
  { code: 'TENANT_CREATED', label: 'Tenant criado', status: 'COMPLETED' },
  { code: 'CONFIG_APPLIED', label: 'Configuração padrão aplicada', status: 'COMPLETED' },
  { code: 'STORE_CREATED', label: 'Loja inicial criada', status: 'COMPLETED' },
  { code: 'ROLES_CREATED', label: 'Papéis padrão criados', status: 'COMPLETED' },
  { code: 'STATIONS_CREATED', label: 'Praças de produção criadas', status: 'COMPLETED' },
  { code: 'OWNER_INVITED', label: 'Convite do proprietário enviado', status: 'COMPLETED' },
  { code: 'INSTALL_TOKEN_ISSUED', label: 'Token de instalação emitido', status: 'COMPLETED' },
  { code: 'EDGE_INSTALLED', label: 'Servidor local instalado', status: 'PENDING' },
  { code: 'MENU_LOADED', label: 'Cardápio carregado', status: 'PENDING' },
];

async function mockLogin(page: Page, session: typeof adminSession) {
  await page.route('**/v1/auth/login', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(session) });
  });
}

async function login(page: Page) {
  await page.goto('http://127.0.0.1:49174');
  await page.getByLabel('E-mail').fill('admin@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
}

test.describe('Estrutura e navegação do painel de plataforma (US-150)', () => {
  test('login → raiz (visão geral) → lista → novo estabelecimento → retorno à lista', async ({ page }) => {
    await mockLogin(page, adminSession);
    await page.route('**/v1/platform/summary', async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(platformSummary) });
    });

    let tenantList = [] as Array<typeof provisionedTenant>;
    // US-151 · `GET /v1/platform/tenants` (com querystring de busca/filtro/ordenação/paginação) é
    // o mesmo endpoint do provisionamento mínimo da US-150 — um único handler cobre GET (diretório)
    // e POST (criação), casando com ou sem querystring.
    await page.route('**/v1/platform/tenants**', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            data: tenantList.map(directoryItem),
            nextCursor: null,
            appliedFilters: {
              query: '',
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
        return;
      }
      if (route.request().method() === 'POST') {
        const payload = route.request().postDataJSON() as { slug: string; owner: { email: string } };
        tenantList = [{ ...provisionedTenant, slug: payload.slug }];
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            tenant: { id: provisionedTenant.id, slug: payload.slug, status: 'PROVISIONED' },
            store: { id: '0198aabb-2222-7000-8000-000000000002', name: 'Matriz' },
            installToken: 'raw-install-token-de-uso-unico-1234567890',
            installCommand: `./install.sh --tenant=${provisionedTenant.id} --token=raw-install-token-de-uso-unico-1234567890`,
            ownerInviteSentTo: payload.owner.email,
            checklist,
          }),
        });
        return;
      }
      await route.fallback();
    });
    await page.route('**/v1/platform/tenants/slug-availability*', async (route) => {
      const url = new URL(route.request().url());
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ slug: url.searchParams.get('slug'), available: true }),
      });
    });

    // Login → raiz: visão geral, nunca o formulário de criação (US-150 §3.1/§4).
    await login(page);
    await expect(page.getByRole('heading', { name: 'Visão geral' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Estabelecimentos', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Instalações', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Auditoria e suporte', exact: true })).toBeVisible();

    // → lista (diretório, ainda vazio).
    await page.getByRole('button', { name: 'Estabelecimentos', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Estabelecimentos' })).toBeVisible();
    await expect(page.getByText('Nenhum estabelecimento provisionado ainda')).toBeVisible();

    // → novo estabelecimento (CTA do diretório).
    await page.getByRole('button', { name: 'Novo estabelecimento' }).first().click();
    await expect(page.getByRole('heading', { name: 'Provisionar estabelecimento' })).toBeVisible();

    await page.getByLabel('Nome do estabelecimento').fill('Pizzaria Nova');
    await page.getByLabel('Nome do proprietário').fill('Dona Nova');
    await page.getByLabel('E-mail do proprietário').fill('nova@example.com');
    await page.getByLabel('Nome da loja').fill('Matriz');
    await expect(page.getByText('Endereço disponível.')).toBeVisible();
    await page.getByRole('button', { name: 'Criar estabelecimento' }).click();

    await expect(page.getByRole('heading', { name: 'Pizzaria Nova está pronto para implantação' })).toBeVisible();

    // → retorno à lista: o novo estabelecimento aparece no diretório.
    await page.getByRole('button', { name: 'Voltar aos estabelecimentos' }).click();
    await expect(page.getByRole('heading', { name: 'Estabelecimentos' })).toBeVisible();
    await expect(page.getByText('Pizzaria Nova')).toBeVisible();
  });

  test('acesso direto a uma rota protegida sem policy PlatformAdmin nega acesso sem revelar dado de plataforma', async ({
    page,
  }) => {
    await mockLogin(page, nonAdminSession);
    let summaryRequested = false;
    await page.route('**/v1/platform/summary', async (route) => {
      summaryRequested = true;
      await route.fulfill({
        status: 403,
        contentType: 'application/problem+json',
        body: JSON.stringify({ detail: 'Acesso restrito à administração da plataforma.', code: 'FORBIDDEN' }),
      });
    });

    await login(page);

    await expect(page.getByText('Acesso negado')).toBeVisible();
    // A tentativa é observável no backend: a sonda de autorização foi de fato chamada (403 real, não decisão só de UI).
    expect(summaryRequested).toBe(true);
    await expect(page.getByText(String(platformSummary.tenants.total))).not.toBeVisible();
    await expect(page.getByRole('navigation')).toHaveCount(0);
  });

  test('rota desconhecida mostra recurso inexistente mantendo a navegação', async ({ page }) => {
    await mockLogin(page, adminSession);
    await page.route('**/v1/platform/summary', async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(platformSummary) });
    });

    await page.goto('http://127.0.0.1:49174');
    await page.getByLabel('E-mail').fill('admin@example.com');
    await page.getByLabel('Senha').fill('senha-segura');
    await page.getByRole('button', { name: 'Entrar' }).click();
    await expect(page.getByRole('heading', { name: 'Visão geral' })).toBeVisible();

    await page.goto('http://127.0.0.1:49174/rota-que-nao-existe');

    await expect(page.getByRole('heading', { name: 'Página não encontrada' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Estabelecimentos', exact: true })).toBeVisible();
  });
});
