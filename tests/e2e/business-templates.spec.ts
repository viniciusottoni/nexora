import { expect, test, type Page } from '@playwright/test';

/**
 * US-142 · Modelos por tipo de negócio — cobre o cenário Gherkin "Aplicação do modelo" do lado do
 * web-platform: o seletor de modelo da tela de provisionamento deixou de ser um valor travado em
 * "Pizzaria" (US-002) e passa a listar o catálogo real (`GET /v1/platform/templates`), incluindo o
 * código escolhido no corpo de `POST /v1/platform/tenants`. Arquivo NOVO (mesma convenção de mock
 * de rede via `page.route` de tests/e2e/tenant-provisioning.spec.ts, para não colidir com outro
 * agente tocando aquele arquivo em paralelo).
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Admin de plataforma' },
  permissions: ['tenant:manage'],
};

const templates = [
  { code: 'PIZZERIA', name: 'Pizzaria', version: 3 },
  { code: 'HAMBURGUERIA', name: 'Hamburgueria', version: 2 },
  { code: 'RESTAURANTE', name: 'Restaurante', version: 1 },
  { code: 'LANCHONETE', name: 'Lanchonete', version: 1 },
];

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

async function mockLogin(page: Page) {
  await page.route('**/v1/auth/login', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(session) });
  });
}

/** US-150 — sonda de autorização do shell (`GET /v1/platform/summary`), chamada antes de qualquer rota renderizar. */
async function mockPlatformSummary(page: Page) {
  await page.route('**/v1/platform/summary', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        tenants: { total: 1, active: 1, attention: 0 },
        installations: { healthy: 0, degraded: 0, offline: 0 },
        pendingInvites: 0,
        generatedAt: new Date().toISOString(),
      }),
    });
  });
}

async function mockTemplates(page: Page) {
  await page.route('**/v1/platform/templates', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.fallback();
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: templates }),
    });
  });
}

async function loginAndOpenProvisioning(page: Page) {
  await mockLogin(page);
  await mockPlatformSummary(page);
  await mockTemplates(page);
  await page.goto('http://127.0.0.1:49174');
  await page.getByLabel('E-mail').fill('admin@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByRole('heading', { name: 'Visão geral' })).toBeVisible();
  await page.getByRole('button', { name: 'Novo estabelecimento' }).first().click();
  await expect(page.getByRole('heading', { name: 'Provisionar estabelecimento' })).toBeVisible();
}

test.describe('Catálogo de modelos de negócio (US-142)', () => {
  test('seletor de modelo lista o catálogo real, e a escolha vai no corpo do provisionamento', async ({
    page,
  }) => {
    await loginAndOpenProvisioning(page);

    // As 4 opções do catálogo (não mais só "Pizzaria" travada) precisam estar disponíveis.
    const templateSelect = page.getByLabel('Modelo de negócio');
    await expect(templateSelect.locator('option')).toHaveCount(4);
    for (const template of templates) {
      await expect(templateSelect.locator(`option[value="${template.code}"]`)).toHaveText(template.name);
    }

    await templateSelect.selectOption('HAMBURGUERIA');

    await page.route('**/v1/platform/tenants/slug-availability*', async (route) => {
      const url = new URL(route.request().url());
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ slug: url.searchParams.get('slug'), available: true }),
      });
    });

    let capturedTemplate: string | undefined;
    await page.route('**/v1/platform/tenants', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.fallback();
        return;
      }
      const payload = route.request().postDataJSON() as { slug: string; template: string; owner: { email: string } };
      capturedTemplate = payload.template;
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          tenant: { id: '0198aabb-2222-7000-8000-000000000001', slug: payload.slug, status: 'PROVISIONED' },
          store: { id: '0198aabb-2222-7000-8000-000000000002', name: 'Matriz' },
          installToken: 'raw-install-token-de-uso-unico-1234567890',
          installCommand:
            './install.sh --tenant=0198aabb-2222-7000-8000-000000000001 --token=raw-install-token-de-uso-unico-1234567890',
          ownerInviteSentTo: payload.owner.email,
          checklist,
        }),
      });
    });

    await page.getByLabel('Nome do estabelecimento').fill('Lanchonete do Zé');
    await page.getByLabel('Nome do proprietário').fill('José');
    await page.getByLabel('E-mail do proprietário').fill('jose@example.com');
    await page.getByLabel('Nome da loja').fill('Matriz');

    await expect(page.getByText('Endereço disponível.')).toBeVisible();
    await page.getByRole('button', { name: 'Criar estabelecimento' }).click();

    await expect(
      page.getByRole('heading', { name: 'Lanchonete do Zé está pronto para implantação' }),
    ).toBeVisible();
    expect(capturedTemplate).toBe('HAMBURGUERIA');
  });

  test('catálogo indisponível mantém a pizzaria como opção de reserva', async ({ page }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);
    await page.route('**/v1/platform/templates', async (route) => {
      await route.abort('failed');
    });
    await page.goto('http://127.0.0.1:49174');
    await page.getByLabel('E-mail').fill('admin@example.com');
    await page.getByLabel('Senha').fill('senha-segura');
    await page.getByRole('button', { name: 'Entrar' }).click();
    await expect(page.getByRole('heading', { name: 'Visão geral' })).toBeVisible();
    await page.getByRole('button', { name: 'Novo estabelecimento' }).first().click();

    const templateSelect = page.getByLabel('Modelo de negócio');
    await expect(templateSelect.locator('option[value="PIZZERIA"]')).toHaveText('Pizzaria');
  });
});
