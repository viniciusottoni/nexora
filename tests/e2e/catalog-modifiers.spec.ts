import { expect, test, type Page } from '@playwright/test';

/**
 * E2E da US-012 (Grupos de modificadores) — cenário Gherkin "Modificador obrigatório": o cliente
 * não consegue avançar com um grupo obrigatório pendente, e a UI destaca isso antes da tentativa
 * (§10, §12 "E2E: Cliente não consegue enviar item com grupo obrigatório pendente").
 *
 * PENDENTE DE INTEGRAÇÃO (ver relatório da tarefa): este spec assume que alguém já plugou
 * `ModifierGroupManagementPage` em `apps/web-admin/src/app.tsx` (import, instância de
 * `ModifierGroupsApi`, estado, fetch inicial, bloco de render e uma entrada
 * `{ value: 'modifiers', label: 'Grupos de modificadores' }` em `ADMIN_SECTIONS`/`AdminSection`) —
 * este worktree isolado não pôde tocar `app.tsx` (arquivo proibido, compartilhado por 4 agentes em
 * paralelo). Enquanto isso não acontecer, o clique em "Grupos de modificadores" não encontra o
 * botão de navegação e o teste falha — comportamento esperado até a integração manual. Não faz
 * parte da verificação desta tarefa rodar Playwright (bug pré-existente de config, fora de escopo).
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Gestora' },
  permissions: ['catalog:read', 'catalog:write'],
};

const tamanhoGroup = {
  id: '0198aabb-2222-7000-8000-000000000001',
  name: 'Tamanho',
  minSelect: 1,
  maxSelect: 1,
  isRequired: true,
  sortOrder: 0,
  productIds: [],
  modifiers: [
    {
      id: '0198aabb-2222-7000-8000-000000000011',
      groupId: '0198aabb-2222-7000-8000-000000000001',
      name: 'Pequena',
      priceDelta: '0.00',
      ingredientId: null,
      quantity: null,
      isAvailable: true,
      sortOrder: 0,
    },
    {
      id: '0198aabb-2222-7000-8000-000000000012',
      groupId: '0198aabb-2222-7000-8000-000000000001',
      name: 'Grande',
      priceDelta: '10.00',
      ingredientId: null,
      quantity: null,
      isAvailable: true,
      sortOrder: 1,
    },
  ],
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

async function login(page: Page) {
  await page.getByLabel('E-mail').fill('gestora@example.com');
  await page.getByLabel('Senha').fill('senha-segura');
  await page.getByRole('button', { name: 'Entrar' }).click();
}

test('gestora vê grupo obrigatório destacado antes de tentar avançar', async ({ page }) => {
  await mockLogin(page);
  await page.route('**/v1/devices', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [] }),
    }),
  );
  await page.route('**/v1/roles', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [], permissionCatalog: [] }),
    }),
  );
  await page.route('**/v1/catalog/modifier-groups', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [tamanhoGroup] }),
    }),
  );

  await page.goto('http://127.0.0.1:49173');
  await login(page);

  await page.getByRole('button', { name: 'Grupos de modificadores' }).click();
  await expect(page.getByRole('heading', { name: 'Grupos de modificadores' })).toBeVisible();
  await expect(page.getByText(/Escolha pendente: este grupo é obrigatório/)).toBeVisible();

  await page.getByRole('checkbox', { name: 'Grande' }).check();
  await expect(page.getByText(/Escolha pendente/)).toHaveCount(0);
});
