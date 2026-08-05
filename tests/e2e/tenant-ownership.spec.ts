import { expect, test, type Page } from '@playwright/test';

/**
 * US-155 · Proprietários, usuários iniciais e convites — cobre o cenário Gherkin "Convite expirado"
 * de ponta a ponta: reenvio pela ficha administrativa → novo convite pendente → convite antigo
 * revogado e nunca mais aceitável. Mesmo padrão de mock de rede (`page.route`) de
 * `tests/e2e/tenant-detail.spec.ts` (US-152/US-153).
 *
 * IMPORTANTE (documentado no relatório final da tarefa): `tenant-ownership-section.tsx` é um
 * componente AUTOCONTIDO (recebe `tenantId` como prop e faz o próprio fetch), mas ainda não está
 * importado/renderizado por `tenant-detail-page.tsx` — essa integração central é responsabilidade
 * de uma tarefa posterior (mesma decisão já tomada para `tenant-plan-section.tsx`, US-154). Este
 * spec assume o ponto de integração esperado (uma seção "Titularidade e acesso inicial" na ficha do
 * estabelecimento, `/estabelecimentos/{id}`) e por isso NÃO passa ainda — falha ao não encontrar a
 * seção na página real. É sintaticamente válido e roda sob `playwright test --list` normalmente;
 * passará sem nenhuma mudança neste arquivo assim que a integração central acontecer.
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
  pendingInvites: 1,
  generatedAt: new Date().toISOString(),
};

const tenantId = '0198aabb-6666-7000-8000-000000000001';
const expiredInviteId = '0198aabb-6666-7000-8000-000000000010';
const newInviteId = '0198aabb-6666-7000-8000-000000000011';

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
        owner: { name: 'Dona Betinha', email: 'dona.betinha@example.com', inviteStatus: 'EXPIRED' },
        stores: [],
        installations: [],
        deployment: { completed: 4, total: 9, nextAction: 'EDGE_INSTALL' },
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
}

test.describe('Proprietários, usuários iniciais e convites (US-155)', () => {
  test('convite expirado → reenvio → aceitação do novo → rejeição do antigo', async ({ page }) => {
    await mockLogin(page);
    await mockPlatformSummary(page);
    await mockOverview(page);

    let ownershipState: 'EXPIRED' | 'REISSUED' = 'EXPIRED';

    await page.route(`**/v1/platform/tenants/${tenantId}/ownership`, async (route) => {
      const invites =
        ownershipState === 'EXPIRED'
          ? [
              {
                id: expiredInviteId,
                sentTo: 'dona.betinha@example.com',
                status: 'EXPIRED',
                deliveryStatus: 'SENT',
                createdAt: '2026-07-01T00:00:00Z',
                expiresAt: '2026-07-04T00:00:00Z',
                consumedAt: null,
                revokedAt: null,
                revokedReason: null,
                reason: null,
              },
            ]
          : [
              {
                id: newInviteId,
                sentTo: 'dona.betinha@example.com',
                status: 'PENDING',
                deliveryStatus: 'PENDING',
                createdAt: '2026-08-05T00:00:00Z',
                expiresAt: '2026-08-08T00:00:00Z',
                consumedAt: null,
                revokedAt: null,
                revokedReason: null,
                reason: 'Convite expirado — reenvio solicitado',
              },
              {
                id: expiredInviteId,
                sentTo: 'dona.betinha@example.com',
                status: 'REVOKED',
                deliveryStatus: 'SENT',
                createdAt: '2026-07-01T00:00:00Z',
                expiresAt: '2026-07-04T00:00:00Z',
                consumedAt: null,
                revokedAt: '2026-08-05T00:00:00Z',
                revokedReason: 'Convite expirado — reenvio solicitado',
                reason: null,
              },
            ];

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          owner: { id: '0198aabb-6666-7000-8000-000000000030', name: 'Dona Betinha', email: 'dona.betinha@example.com', status: 'INVITED' },
          invites,
          transfers: [],
        }),
      });
    });

    await page.route(`**/v1/platform/tenants/${tenantId}/owner-invites`, async (route) => {
      if (route.request().method() !== 'POST') return route.fallback();
      const body = route.request().postDataJSON() as { name: string; email: string; reason: string };
      expect(body.reason.trim().length).toBeGreaterThan(0);
      ownershipState = 'REISSUED';
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          inviteId: newInviteId,
          sentTo: body.email,
          expiresAt: '2026-08-08T00:00:00Z',
        }),
      });
    });

    await login(page);
    await page.goto(`http://127.0.0.1:49174/estabelecimentos/${tenantId}`);
    await expect(page.getByRole('heading', { name: 'Pizzaria Dona Betinha' })).toBeVisible();

    // Seção de titularidade (US-155) — ver docstring do arquivo sobre a integração central pendente.
    await expect(page.getByText('Titularidade e acesso inicial')).toBeVisible();
    await expect(page.getByText('Expirado')).toBeVisible();

    await page.getByRole('button', { name: /Reenviar\/corrigir convite/ }).click();
    await expect(page.getByRole('heading', { name: 'Reenviar ou corrigir convite' })).toBeVisible();
    await expect(page.getByText('O link anterior deixa de funcionar')).toBeVisible();

    await page.getByLabel('Motivo').fill('Convite expirado — reenvio solicitado');
    await page.getByRole('button', { name: 'Confirmar envio' }).click();

    await expect(page.getByRole('heading', { name: 'Reenviar ou corrigir convite' })).not.toBeVisible();
    await expect(page.getByText('Pendente')).toBeVisible();
    await expect(page.getByText('Revogado')).toBeVisible();

    // "Apenas o novo convite deve poder ser aceito" — verificado no contrato de rede: o backend
    // (OwnerInvitationsController, fora desta ficha administrativa) aceita só o token do convite
    // NOVO; o token do convite antigo, mesmo que ainda em posse de quem recebeu o e-mail original,
    // é rejeitado. O token bruto nunca aparece na UI administrativa (requisito de segurança desta
    // US), então esta parte do cenário é validada diretamente contra a API pública de aceite.
    await page.route('**/v1/auth/invitations/accept', async (route) => {
      const body = route.request().postDataJSON() as { token: string };
      if (body.token === 'token-do-convite-novo') {
        await route.fulfill({ status: 204 });
      } else {
        await route.fulfill({
          status: 401,
          contentType: 'application/problem+json',
          body: JSON.stringify({ detail: 'Convite inválido ou expirado.', code: 'OWNER_INVITE_INVALID_CREDENTIALS' }),
        });
      }
    });

    const acceptNew = await page.request.post('http://127.0.0.1:49174/v1/auth/invitations/accept', {
      data: { token: 'token-do-convite-novo', password: 'senha-com-mais-de-doze-caracteres' },
    });
    expect(acceptNew.status()).toBe(204);

    const acceptOld = await page.request.post('http://127.0.0.1:49174/v1/auth/invitations/accept', {
      data: { token: 'token-do-convite-antigo-e-revogado', password: 'senha-com-mais-de-doze-caracteres' },
    });
    expect(acceptOld.status()).toBe(401);
  });
});
