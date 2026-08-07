import { expect, test, type Page } from '@playwright/test';

/**
 * US-156 · Recuperação do provisionamento e token de instalação — cobre o cenário
 * "tenant existente → reemitir → copiar → sair → segredo não reaparece", a combinação central do
 * requisito de exibição única ("após sair da tela, o segredo desaparece e não pode ser reaberto").
 * Mesmo padrão de mock de rede (`page.route`) de `tests/e2e/tenant-ownership.spec.ts` (US-155).
 *
 * IMPORTANTE (documentado no relatório final da tarefa): `tenant-installation-credentials-section.tsx`
 * é um componente AUTOCONTIDO (recebe `tenantId` como prop e faz o próprio fetch via
 * `installation-credentials-api.ts`), mas ainda não está importado/renderizado por
 * `tenant-detail-page.tsx` — essa integração central é responsabilidade de uma tarefa posterior
 * (mesma decisão já tomada para `tenant-plan-section.tsx`/`tenant-ownership-section.tsx`, US-154/155).
 * Este spec assume o ponto de integração esperado (uma seção "Recuperação de provisionamento" na
 * ficha do estabelecimento, `/estabelecimentos/{id}`) e por isso NÃO passa ainda — falha ao não
 * encontrar a seção na página real. É sintaticamente válido e roda sob `playwright test --list`
 * normalmente; passará sem nenhuma mudança neste arquivo assim que a integração central acontecer.
 */

const adminSession = {
  accessToken: 'access-token-admin',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Admin de plataforma' },
  permissions: ['tenant:manage'],
};

const platformSummary = {
  tenants: { total: 1, active: 1, attention: 0 },
  installations: { healthy: 0, degraded: 0, offline: 0 },
  pendingInvites: 0,
  generatedAt: new Date().toISOString(),
};

const tenantId = '0198aabb-7777-7000-8000-000000000001';
const installationId = '0198aabb-7777-7000-8000-000000000002';
const credentialId = '0198aabb-7777-7000-8000-000000000003';
const rawToken = 'raw-install-token-reemitido-1234567890';

async function mockLogin(page: Page) {
  await page.route('**/v1/auth/login', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(adminSession),
    });
  });
}

async function mockPlatformSummary(page: Page) {
  await page.route('**/v1/platform/summary', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(platformSummary),
    });
  });
}

async function mockOverview(page: Page) {
  await page.route(`**/v1/platform/tenants/${tenantId}/overview`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        tenant: {
          id: tenantId,
          name: 'Pizzaria Dona Betinha',
          slug: 'dona-betinha',
          status: 'PROVISIONED',
          statusVersion: 1,
          availableTransitions: [],
          plan: 'COMPLETO',
          template: 'PIZZERIA',
          domain: null,
          createdAt: '2026-01-01T00:00:00Z',
          updatedAt: '2026-08-01T00:00:00Z',
        },
        owner: { name: 'Dona Betinha', email: 'dona.betinha@example.com', inviteStatus: 'PENDING' },
        stores: [
          {
            id: '0198aabb-7777-7000-8000-000000000004',
            name: 'Matriz',
            timezone: 'America/Sao_Paulo',
          },
        ],
        installations: [
          {
            id: installationId,
            label: 'Servidor local — Matriz',
            status: 'PENDING',
            health: 'UNKNOWN',
          },
        ],
        deployment: { completed: 1, total: 9, nextAction: 'BRANDING' },
        links: { publicMenu: null, admin: null, health: null },
      }),
    });
  });
}

/** Cenário Gherkin "Resposta de criação foi perdida" — tenant/loja/instalação existem, token original não consumido. */
async function mockDeploymentStatus(page: Page) {
  await page.route(`**/v1/platform/tenants/${tenantId}/deployment`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        completed: 1,
        total: 9,
        installation: { id: installationId, status: 'PENDING', canReissueToken: true },
        nextAction: 'BRANDING',
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
}

test.describe('Recuperação do provisionamento e token de instalação (US-156)', () => {
  test('tenant existente → reemitir → copiar → sair → segredo não reaparece', async ({
    page,
    context,
  }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);
    await mockOverview(page);
    await mockDeploymentStatus(page);

    await context.grantPermissions(['clipboard-read', 'clipboard-write']);

    let reissueCalls = 0;
    await page.route(`**/v1/platform/installations/${installationId}/tokens`, async (route) => {
      if (route.request().method() !== 'POST') return route.fallback();
      reissueCalls += 1;
      expect(route.request().headers()['idempotency-key']).toBeTruthy();
      const body = route.request().postDataJSON() as { reason: string; expiresInHours: number };
      expect(body.reason.trim().length).toBeGreaterThan(0);
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          credentialId,
          expiresAt: '2026-08-07T00:00:00Z',
          installToken: rawToken,
          installCommand: `./install.sh --tenant=${tenantId} --token=${rawToken}`,
        }),
      });
    });

    await login(page);
    await page.goto(`http://127.0.0.1:49174/estabelecimentos/${tenantId}`);
    await expect(page.getByRole('heading', { name: 'Pizzaria Dona Betinha' })).toBeVisible();

    // Seção de recuperação de provisionamento (US-156) — ver docstring do arquivo sobre a
    // integração central pendente.
    await expect(page.getByText('Recuperação de provisionamento')).toBeVisible();
    await expect(page.getByText('Ainda não registrada')).toBeVisible();
    await expect(page.getByText('Provisionamento incompleto')).toBeVisible();

    await page.getByRole('button', { name: 'Reemitir token de instalação' }).click();
    await expect(
      page.getByRole('heading', { name: 'Reemitir token de instalação?' }),
    ).toBeVisible();
    await expect(page.getByText('será invalidado imediatamente')).toBeVisible();

    await page.getByLabel('Motivo').fill('Comando original não foi exibido');
    await page.getByRole('button', { name: 'Sim, reemitir token' }).click();

    expect(reissueCalls).toBe(1);
    await expect(
      page.getByRole('heading', { name: 'Reemitir token de instalação?' }),
    ).not.toBeVisible();
    await expect(page.getByText('Token gerado')).toBeVisible();

    // Mascarado por padrão — exibição única exige confirmação explícita para revelar.
    await expect(page.getByText(rawToken, { exact: true })).toHaveCount(0);
    await page.getByRole('button', { name: 'Revelar token' }).click();
    await expect(page.getByText(rawToken, { exact: true })).toBeVisible();

    await page.getByRole('button', { name: /Copiar token/ }).click();
    // Escopado à mensagem de confirmação da própria cópia — o aviso de custódia permanente
    // (AlertBanner) também contém a mesma frase e tornaria o locator ambíguo.
    await expect(
      page.getByText('Token copiado. Guarde-o em local seguro — ele não será mostrado novamente.', {
        exact: true,
      }),
    ).toBeVisible();
    const clipboardText = await page.evaluate(() => navigator.clipboard.readText());
    expect(clipboardText).toBe(rawToken);

    // "Sair" — navega para fora da ficha e volta; a seção remonta do zero (novo `useState`), o
    // segredo em memória da visita anterior não sobrevive.
    await page.goto('http://127.0.0.1:49174/estabelecimentos');
    await page.goto(`http://127.0.0.1:49174/estabelecimentos/${tenantId}`);
    await expect(page.getByRole('heading', { name: 'Pizzaria Dona Betinha' })).toBeVisible();

    await expect(page.getByText('Token gerado')).not.toBeVisible();
    await expect(page.getByText(rawToken)).toHaveCount(0);
  });
});
