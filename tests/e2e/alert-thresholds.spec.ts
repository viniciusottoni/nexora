import { expect, test, type Page } from '@playwright/test';

/**
 * US-080 (Motor de alertas com limiares configuráveis) — E2E do fluxo de configuração de
 * limiares no painel web-admin (porta 49173). Mock de rede via `page.route`, sem backend real —
 * mesmo padrão de `audit-log.spec.ts` (login + boot do painel via `mockAdminShell`) e
 * `catalog-pricing.spec.ts`. Arquivo NOVO — não edita nenhum spec existente.
 *
 * Cobre os cenários centrais do §10 da US: a tela carrega os valores atuais em campos com
 * explicação em linguagem de negócio, editar um único campo dispara um PATCH parcial (só o campo
 * alterado, nunca os 14), e uma falha ao carregar mostra o estado de erro em vez de um formulário
 * quebrado.
 */

const session = {
  accessToken: 'access-token-e2e',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Gestora' },
  permissions: ['config:write'],
};

const thresholds = {
  orderWarnMinutes: 12,
  orderCriticalMinutes: 18,
  itemInWindowMinutes: 5,
  tableIdleMinutes: 30,
  cashDivergenceAlert: '5.00',
  cmvDivergencePercent: 3,
  syncDelayMinutes: 5,
  dineInPromiseMinutes: 25,
  deliveryPromiseMinutes: 40,
  avgTimeAboveTargetPercent: 20,
  cancellationCountThreshold: 3,
  cancellationWindowMinutes: 60,
  discountAboveThresholdPercent: 15,
  discountWindowMinutes: 60,
};

async function mockLogin(page: Page) {
  await page.route('**/v1/auth/login', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(session),
    });
  });
}

/** Recursos que o boot do CloudAdmin busca sempre, independente da seção aberta (app.tsx). Os
 * limiares NÃO estão nessa lista — a tela busca sob demanda, como a trilha de auditoria. */
async function mockAdminShell(page: Page) {
  await page.route('**/v1/devices', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) }),
  );
  await page.route('**/v1/roles', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [], permissionCatalog: [] }),
    }),
  );
  await page.route('**/v1/catalog/stations', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) }),
  );
  await page.route('**/v1/catalog/modifier-groups', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) }),
  );
  await page.route('**/v1/catalog/categories', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) }),
  );
  await page.route('**/v1/catalog/products', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) }),
  );
  // Notificações do sino no TopBar (polling) — resposta vazia para não interferir no fluxo.
  await page.route('**/v1/notifications*', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ alerts: [], nextCursor: null }) }),
  );
}

async function login(page: Page) {
  await page.getByLabel('E-mail').fill('gestora@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
}

test.describe('US-080 · configuração de limiares de alerta', () => {
  test.beforeEach(async ({ page }) => {
    // Sem isto, a animação de entrada (`nx-anim-in`) deixaria elementos temporariamente fora do
    // fluxo de acessibilidade — flakiness de teste, não bug de produto (mesmo cuidado de
    // audit-log.spec.ts / order-cancellation.spec.ts).
    await page.emulateMedia({ reducedMotion: 'reduce' });
  });

  test('carrega os limiares atuais, edita um campo e salva só o PATCH do que mudou', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockAdminShell(page);

    let getCalls = 0;
    const patchBodies: unknown[] = [];
    await page.route('**/v1/tenant/thresholds', async (route) => {
      const request = route.request();
      if (request.method() === 'GET') {
        getCalls += 1;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(thresholds),
        });
        return;
      }
      if (request.method() === 'PATCH') {
        const body = JSON.parse(request.postData() ?? '{}') as Record<string, unknown>;
        patchBodies.push(body);
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ ...thresholds, ...body }),
        });
        return;
      }
      await route.continue();
    });

    await page.goto('http://127.0.0.1:49173');
    await login(page);

    await page.getByRole('button', { name: 'Limiares de alerta' }).click();

    // Campo em linguagem de negócio (US-080 §10), não o nome técnico — e já preenchido com o valor
    // atual vindo da API.
    const criticalField = page.getByLabel('Pedido atrasado (crítico)');
    await expect(criticalField).toHaveValue('18');
    await expect(page.getByLabel('Divergência de caixa')).toHaveValue('5,00');
    expect(getCalls).toBeGreaterThanOrEqual(1);

    await expect(page.getByRole('button', { name: 'Salvar limiares' })).toBeDisabled();

    await criticalField.fill('22');
    await expect(page.getByRole('button', { name: 'Salvar limiares' })).toBeEnabled();
    await page.getByRole('button', { name: 'Salvar limiares' }).click();

    await expect.poll(() => patchBodies.length).toBeGreaterThanOrEqual(1);
    // Só o campo alterado viaja no PATCH — os outros 13 não fazem parte do corpo (RN-016: configuração,
    // não reenvio cego de tudo que a tela carregou).
    expect(patchBodies[0]).toEqual({ orderCriticalMinutes: 22 });

    await expect(page.getByText('Limiares de alerta atualizados.')).toBeVisible();
  });

  test('falha ao carregar os limiares mostra o estado de erro, não um formulário quebrado', async ({
    page,
  }) => {
    await mockLogin(page);
    await mockAdminShell(page);

    await page.route('**/v1/tenant/thresholds', async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ detail: 'Falha ao consultar os limiares.' }),
      });
    });

    await page.goto('http://127.0.0.1:49173');
    await login(page);
    await page.getByRole('button', { name: 'Limiares de alerta' }).click();

    await expect(page.getByText('Falha ao consultar os limiares.')).toBeVisible();
    await expect(page.getByLabel('Pedido atrasado (crítico)')).toHaveCount(0);
  });
});
