import { expect, test, type Page } from '@playwright/test';

/**
 * US-017 · Cadastro de praças de produção — cobre o fluxo principal (criar praça, marcar como
 * gargalo) do painel web-admin. Arquivo NOVO (não edita tests/e2e/foundation.spec.ts nem
 * tenant-provisioning.spec.ts) para não colidir com outro agente que possa estar tocando neles em
 * paralelo — mesma convenção de mock de rede (`page.route`) já usada ali (ver comentário no topo
 * de tenant-provisioning.spec.ts).
 *
 * Pré-requisito para este spec rodar: a seção "Praças de produção" precisa estar plugada em
 * `apps/web-admin/src/app.tsx` (`AdminNavigation`/`CloudAdmin`, ver `ADMIN_SECTIONS` e o `switch`
 * de `section`) — este spec assume o rótulo de navegação "Praças de produção" e o import de
 * `StationManagementPage`/`StationsApi` de `./stations/*`, exatamente como reportado na tarefa que
 * criou `apps/web-admin/src/stations/*`. Enquanto essa integração não for feita, o teste falha por
 * não encontrar o botão de navegação — falha de dependência externa a este arquivo, não deste spec.
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Gestora' },
  permissions: ['catalog:read', 'catalog:write'],
};

const ovenStation = {
  id: '0198aabb-3333-7000-8000-000000000001',
  code: 'OVEN',
  name: 'Forno',
  color: 'amber',
  capacitySlots: 5,
  isBottleneck: false,
  position: 1,
  isActive: true,
  linkedProductCount: 0,
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

test('gestora cria uma praça e a marca como gargalo', async ({ page }) => {
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

  let stations: Array<typeof ovenStation> = [];

  await page.route('**/v1/catalog/stations', async (route) => {
    const request = route.request();

    if (request.method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: stations }),
      });
      return;
    }

    if (request.method() === 'POST') {
      expect(request.headers().authorization).toBe('Bearer access-token');
      expect(request.headers()['idempotency-key']).toBeTruthy();

      const payload = request.postDataJSON() as {
        code: string;
        name: string;
        isBottleneck: boolean;
      };
      const created = {
        ...ovenStation,
        code: payload.code,
        name: payload.name,
        isBottleneck: payload.isBottleneck,
      };
      stations = [...stations, created];

      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(created),
      });
      return;
    }

    await route.fallback();
  });

  await page.route('**/v1/catalog/stations/*', async (route) => {
    const request = route.request();
    if (request.method() !== 'PATCH') {
      await route.fallback();
      return;
    }

    expect(request.headers()['idempotency-key']).toBeTruthy();
    const payload = request.postDataJSON() as { isBottleneck?: boolean };
    stations = stations.map((station) =>
      payload.isBottleneck === undefined
        ? station
        : { ...station, isBottleneck: payload.isBottleneck },
    );

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(stations[0]),
    });
  });

  await page.goto('/');
  await login(page);

  await page.getByRole('button', { name: 'Praças de produção' }).click();
  await expect(page.getByRole('heading', { name: 'Praças de produção' })).toBeVisible();

  await page.getByRole('button', { name: 'Nova praça' }).click();
  const dialog = page.getByRole('dialog', { name: 'Criar praça' });
  await dialog.getByLabel('Nome').fill('Forno');
  await dialog.getByRole('textbox', { name: /Código/ }).fill('OVEN');
  await dialog.getByRole('button', { name: 'Criar praça' }).click();

  await expect(page.getByRole('cell', { name: 'Forno' })).toBeVisible();

  // Marca a praça recém-criada como gargalo — só uma praça pode ser o gargalo por vez (US-017 §10).
  await page.getByLabel('Marcar como gargalo').check();
  await page.getByRole('button', { name: 'Salvar praça' }).click();

  await expect(page.getByText('Alterações salvas.')).toBeVisible();
});
