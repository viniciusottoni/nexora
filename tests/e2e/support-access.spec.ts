import { expect, test, type Page } from '@playwright/test';

/**
 * US-145 · Acesso de suporte auditado — cobre os dois lados do fluxo: concessão pela plataforma
 * (web-platform, :49174) e histórico/revogação pelo cliente (web-admin, :49173). Arquivo NOVO
 * (mesma convenção de `tests/e2e/tenant-provisioning.spec.ts`, para não colidir com outro agente
 * tocando `foundation.spec.ts` em paralelo) e mesmo padrão de mock de rede (`page.route`).
 *
 * DEPENDÊNCIA DE INTEGRAÇÃO AINDA PENDENTE (ver relatório da tarefa): nenhum dos dois apps navega
 * por URL (é estado de componente em `app.tsx`, não roteador) — só é possível chegar a estas
 * telas clicando no item de navegação correspondente. Este agente foi instruído a NÃO editar
 * `apps/web-platform/src/app.tsx` nem `apps/web-admin/src/app.tsx` diretamente (arquivos
 * compartilhados com outro trabalho em andamento no mesmo épico) — só a reportar os itens de nav
 * a adicionar. Os dois testes abaixo pressupõem exatamente os rótulos reportados:
 *   - web-platform: item "Solicitar acesso de suporte" (id `support-access`)
 *   - web-admin: item "Acessos de suporte" (id `support-access`)
 * Ficam VERMELHOS até essa integração (fora do escopo desta tarefa) acontecer — não é um defeito
 * do código novo, é o mesmo tipo de gap documentado para os códigos de erro não mapeados ainda em
 * ResultExtensions (ver SupportAccessResultMappingTests.cs).
 */

const platformSession = {
  accessToken: 'access-token-platform',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Admin de plataforma' },
  permissions: ['tenant:manage'],
};

const adminSession = {
  accessToken: 'access-token-admin',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000002', name: 'Gestor da loja' },
  permissions: ['*'],
};

async function mockLogin(page: Page, session: typeof platformSession) {
  await page.route('**/v1/auth/login', async (route) => {
    expect(route.request().headers()['idempotency-key']).toBeTruthy();
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(session),
    });
  });
}

test.describe('Concessão de acesso de suporte pela plataforma (US-145)', () => {
  test('solicita acesso, gera token de escopo especial e notifica o cliente', async ({ page }) => {
    await mockLogin(page, platformSession);
    await page.goto('http://127.0.0.1:49174');
    await page.getByLabel('E-mail').fill('admin@example.com');
    await page.getByLabel('Senha').fill('senha-segura');
    await page.getByRole('button', { name: 'Entrar' }).click();

    await page.getByRole('button', { name: 'Solicitar acesso de suporte' }).click();
    await expect(page.getByRole('heading', { name: 'Solicitar acesso de suporte' })).toBeVisible();

    let grantCalls = 0;
    const tenantId = '0198aabb-2222-7000-8000-000000000009';
    await page.route(`**/v1/platform/tenants/${tenantId}/support-access`, async (route) => {
      grantCalls += 1;
      expect(route.request().method()).toBe('POST');
      expect(route.request().headers()['idempotency-key']).toBeTruthy();
      expect(route.request().headers().authorization).toBe('Bearer access-token-platform');

      const payload = route.request().postDataJSON() as { reason: string; durationMinutes: number };
      expect(payload.reason).toBe('Investigação de divergência de caixa — chamado #482');
      expect(payload.durationMinutes).toBe(60);

      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          token: 'raw-support-token-de-uso-unico-1234567890',
          expiresAt: '2026-08-04T13:00:00Z',
          notifiedCustomer: true,
        }),
      });
    });

    await page.getByLabel('Estabelecimento (id)').fill(tenantId);
    await page
      .getByLabel('Motivo')
      .fill('Investigação de divergência de caixa — chamado #482');
    await page.getByLabel('Duração').selectOption('60');
    await page.getByRole('button', { name: 'Solicitar acesso', exact: true }).click();

    await expect(page.getByRole('heading', { name: 'Acesso concedido' })).toBeVisible();
    expect(grantCalls).toBe(1);

    // Token mascarado por padrão — só aparece por completo após "Revelar token".
    await expect(page.locator('.support-access-token')).toContainText('••••');
    await page.getByRole('button', { name: 'Revelar token' }).click();
    await expect(page.locator('.support-access-token')).toContainText(
      'raw-support-token-de-uso-unico-1234567890',
    );
  });
});

test.describe('Histórico e revogação de acesso de suporte pelo cliente (US-145)', () => {
  test('lista o histórico e revoga um acesso ativo em um clique', async ({ page }) => {
    await mockLogin(page, adminSession);
    await page.goto('http://127.0.0.1:49173');

    const grantId = '0198aabb-4444-7000-8000-000000000001';
    await page.route('**/v1/tenant/support-access-history', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: [
            {
              id: grantId,
              tenantId: '0198aabb-2222-7000-8000-000000000009',
              tenantName: null,
              grantedTo: null,
              reason: 'Investigação de divergência de caixa — chamado #482',
              durationMinutes: 60,
              grantedAt: '2026-08-04T12:00:00Z',
              expiresAt: '2026-08-04T13:00:00Z',
              revokedAt: null,
              revokedBy: null,
              lastUsedAt: null,
              isActive: true,
            },
          ],
        }),
      });
    });

    await page.getByLabel('E-mail').fill('gestor@example.com');
    await page.getByLabel('Senha').fill('senha-segura');
    await page.getByRole('button', { name: 'Entrar' }).click();

    await page.getByRole('button', { name: 'Acessos de suporte' }).click();
    await expect(page.getByRole('heading', { name: 'Acessos de suporte' })).toBeVisible();
    await expect(
      page.getByText('Investigação de divergência de caixa — chamado #482'),
    ).toBeVisible();

    let revokeCalls = 0;
    await page.route(`**/v1/tenant/support-access/${grantId}`, async (route) => {
      revokeCalls += 1;
      expect(route.request().method()).toBe('DELETE');
      expect(route.request().headers()['idempotency-key']).toBeTruthy();
      expect(route.request().headers().authorization).toBe('Bearer access-token-admin');
      await route.fulfill({ status: 204 });
    });

    await page.getByRole('button', { name: 'Revogar' }).click();

    await expect(
      page.getByText('Acesso de suporte revogado. Ele deixa de valer imediatamente.'),
    ).toBeVisible();
    expect(revokeCalls).toBe(1);
  });
});
