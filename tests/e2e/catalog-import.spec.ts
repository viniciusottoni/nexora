import { expect, test, type Page } from '@playwright/test';

/**
 * US-144 · Importação de cardápio por planilha — cobre o fluxo principal do web-admin: baixar o
 * modelo, enviar um arquivo com erro (mostra linha/coluna/mensagem), corrigir e confirmar a
 * importação (pré-visualização -> confirmação -> resultado com contagem por tipo). Arquivo NOVO
 * (mesma convenção de mock de rede via `page.route` de `catalog-categories-products.spec.ts`) para
 * não colidir com outro agente tocando os specs existentes.
 *
 * A seção está plugada em `apps/web-admin/src/app.tsx` com o id `catalog-import`, label
 * "Importar planilha" (renomeado de "Importar cardápio" na integração final do E-14 — o rótulo
 * original colidia em substring com o botão pré-existente "Cardápio" nos specs
 * `catalog-categories-products.spec.ts`/`catalog-variants-pricing.spec.ts`, que usam
 * `getByRole('button', { name: 'Cardápio' })` sem `exact: true`).
 */

const session = {
  accessToken: 'access-token',
  refreshToken: 'refresh-token-with-more-than-thirty-two-characters',
  user: { id: '0198aabb-1111-7000-8000-000000000001', name: 'Gestora' },
  permissions: ['catalog:read', 'catalog:write'],
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

function fakeXlsx(name: string) {
  return { name, mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', buffer: Buffer.from('conteudo-fake') };
}

test('gestora baixa o modelo, corrige uma planilha com erro e confirma a importação', async ({ page }) => {
  await mockLogin(page);

  await page.route('**/v1/catalog/import/template', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      headers: { 'Content-Disposition': 'attachment; filename="modelo-importacao-cardapio.xlsx"' },
      body: Buffer.from('modelo-fake'),
    });
  });

  let validateCallCount = 0;
  await page.route('**/v1/catalog/import/validate', async (route) => {
    validateCallCount += 1;
    expect(route.request().headers()['idempotency-key']).toBeTruthy();

    if (validateCallCount === 1) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          valid: false,
          errors: [{ row: 3, column: 'preco', message: 'Valor inválido' }],
          preview: {
            toCreate: { categories: 0, products: 0, variants: 0 },
            toUpdate: { categories: 0, products: 0, variants: 0 },
          },
        }),
      });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        valid: true,
        errors: [],
        preview: {
          toCreate: { categories: 1, products: 1, variants: 1 },
          toUpdate: { categories: 0, products: 0, variants: 0 },
        },
      }),
    });
  });

  await page.route('**/v1/catalog/import', async (route) => {
    expect(route.request().headers()['idempotency-key']).toBeTruthy();
    await route.fulfill({
      status: 201,
      contentType: 'application/json',
      body: JSON.stringify({
        valid: true,
        errors: [],
        created: { categories: 1, products: 1, variants: 1 },
        updated: { categories: 0, products: 0, variants: 0 },
        skipped: 0,
      }),
    });
  });

  await page.goto('http://127.0.0.1:49173');
  await login(page);

  await page.getByRole('button', { name: 'Importar planilha' }).click();
  await expect(page.getByRole('heading', { name: 'Importar cardápio por planilha' })).toBeVisible();

  const downloadPromise = page.waitForEvent('download');
  await page.getByRole('button', { name: /Baixar modelo/ }).click();
  const download = await downloadPromise;
  expect(download.suggestedFilename()).toBe('modelo-importacao-cardapio.xlsx');

  // Cenário "Erros por linha" (US-144 §4).
  await page.getByLabel('Selecionar arquivo .xlsx', { exact: false }).setInputFiles(fakeXlsx('cardapio-com-erro.xlsx'));
  await page.getByRole('button', { name: 'Validar planilha' }).click();

  await expect(page.getByText('Valor inválido')).toBeVisible();
  await expect(page.getByText('preco')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Confirmar importação' })).toHaveCount(0);

  // Corrige (nova seleção de arquivo) e revalida.
  await page.getByRole('button', { name: 'Escolher outra planilha' }).click();
  await page.getByLabel('Selecionar arquivo .xlsx', { exact: false }).setInputFiles(fakeXlsx('cardapio-corrigido.xlsx'));
  await page.getByRole('button', { name: 'Validar planilha' }).click();

  // Cenário "Pré-visualização" (US-144 §4) — nada gravado ainda, mostra o que seria criado.
  await expect(page.getByText('2. Confira antes de importar')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Confirmar importação' })).toBeVisible();

  // Cenário "Importação completa" (US-144 §4) — confirma e mostra a contagem por tipo.
  await page.getByRole('button', { name: 'Confirmar importação' }).click();
  await expect(page.getByText('Importação concluída')).toBeVisible();
  await expect(page.getByText(/1 categoria\(s\), 1 produto\(s\)/)).toBeVisible();
});
