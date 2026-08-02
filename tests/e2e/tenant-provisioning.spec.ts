import { expect, test, type Page } from '@playwright/test';

/**
 * US-002 · Provisionar novo estabelecimento — cobre os dois cenários Gherkin ("Criação de tenant
 * a partir de modelo" e "Slug duplicado") do fluxo web-platform. Arquivo NOVO (não edita
 * tests/e2e/foundation.spec.ts) para não colidir com outro agente que possa estar tocando nele em
 * paralelo — mesma convenção de mock de rede (`page.route`) já usada ali.
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
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(session),
    });
  });
}

async function loginAndOpenProvisioning(page: Page) {
  await mockLogin(page);
  await page.goto('http://127.0.0.1:49174');
  await page.getByLabel('E-mail').fill('admin@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByRole('heading', { name: 'Provisionar estabelecimento' })).toBeVisible();
}

async function fillProvisioningForm(page: Page) {
  await page.getByLabel('Nome do estabelecimento').fill('Pizzaria Dona Betinha');
  await page.getByLabel('Nome do proprietário').fill('Dona Betinha');
  await page.getByLabel('E-mail do proprietário').fill('betinha@example.com');
  await page.getByLabel('Nome da loja').fill('Matriz');
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

test.describe('Provisionamento de estabelecimento (US-002)', () => {
  test('criação a partir de modelo até comando de instalação copiável', async ({ page }) => {
    await loginAndOpenProvisioning(page);

    await page.route('**/v1/platform/tenants/slug-availability*', async (route) => {
      const url = new URL(route.request().url());
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ slug: url.searchParams.get('slug'), available: true }),
      });
    });

    let provisionCalls = 0;
    await page.route('**/v1/platform/tenants', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.fallback();
        return;
      }
      provisionCalls += 1;
      expect(route.request().headers()['idempotency-key']).toBeTruthy();
      expect(route.request().headers().authorization).toBe('Bearer access-token');

      const payload = route.request().postDataJSON() as { slug: string; owner: { email: string } };
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          tenant: {
            id: '0198aabb-2222-7000-8000-000000000001',
            slug: payload.slug,
            status: 'PROVISIONED',
          },
          store: { id: '0198aabb-2222-7000-8000-000000000002', name: 'Matriz' },
          installToken: 'raw-install-token-de-uso-unico-1234567890',
          installCommand:
            './install.sh --tenant=0198aabb-2222-7000-8000-000000000001 --token=raw-install-token-de-uso-unico-1234567890',
          ownerInviteSentTo: payload.owner.email,
          checklist,
        }),
      });
    });

    await fillProvisioningForm(page);

    // Aguarda a checagem de disponibilidade do slug sugerido automaticamente antes de submeter —
    // o botão fica desabilitado enquanto o status é CHECKING/TAKEN (provision-tenant-page.tsx).
    await expect(page.getByText('Endereço disponível.')).toBeVisible();

    await page.getByRole('button', { name: 'Criar estabelecimento' }).click();

    await expect(
      page.getByRole('heading', { name: 'Pizzaria Dona Betinha está pronto para implantação' }),
    ).toBeVisible();
    expect(provisionCalls).toBe(1);

    // Token mascarado por padrão (secure-note) — só aparece por completo após "Revelar token".
    await expect(page.locator('.install-command')).toContainText('••••');
    await page.getByRole('button', { name: 'Revelar token' }).click();
    await expect(page.locator('.install-command')).toContainText('raw-install-token-de-uso-unico-1234567890');

    // Checklist dos 9 passos da Visão Geral §8.5 — 7 concluídos, 2 pendentes (edge/cardápio).
    await expect(page.getByRole('heading', { name: 'Checklist de lançamento' })).toBeVisible();
    await expect(page.getByText('7/9')).toBeVisible();
    await expect(page.getByText('Servidor local instalado')).toBeVisible();

    await page.getByRole('button', { name: 'Copiar comando' }).click();
    await expect(page.getByText('Comando copiado. Guarde-o em local seguro.')).toBeVisible();
  });

  test('slug duplicado mostra erro e bloqueia a criação', async ({ page }) => {
    await loginAndOpenProvisioning(page);

    await page.route('**/v1/platform/tenants/slug-availability*', async (route) => {
      const url = new URL(route.request().url());
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ slug: url.searchParams.get('slug'), available: false }),
      });
    });

    let provisionCalls = 0;
    await page.route('**/v1/platform/tenants', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.fallback();
        return;
      }
      provisionCalls += 1;
      await route.fulfill({ status: 422, contentType: 'application/json', body: '{}' });
    });

    await fillProvisioningForm(page);

    await expect(page.getByText('Este endereço já está em uso.')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Criar estabelecimento' })).toBeDisabled();

    // Botão desabilitado é a própria proteção da tela — nenhuma chamada de criação deve ocorrer.
    expect(provisionCalls).toBe(0);
  });
});
