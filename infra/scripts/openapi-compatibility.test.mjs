import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { afterEach, test } from 'node:test';

const script = resolve('infra/scripts/openapi-compatibility.ts');
const temporaryDirectories = [];

function document(required = ['id', 'name']) {
  return {
    openapi: '3.0.3',
    info: { title: 'API', version: '1.0.0' },
    paths: {
      '/v1/tenants': {
        get: {
          responses: {
            200: {
              description: 'ok',
              content: {
                'application/json': { schema: { $ref: '#/components/schemas/Tenant' } },
              },
            },
          },
        },
      },
    },
    components: {
      schemas: {
        Tenant: {
          type: 'object',
          required,
          properties: { id: { type: 'string' }, name: { type: 'string' } },
        },
      },
    },
  };
}

function run(before, after) {
  const root = mkdtempSync(join(tmpdir(), 'nexora-openapi-'));
  temporaryDirectories.push(root);
  const baseline = join(root, 'baseline.json');
  const current = join(root, 'current.json');
  writeFileSync(baseline, JSON.stringify(before), 'utf8');
  writeFileSync(current, JSON.stringify(after), 'utf8');

  return spawnSync(
    process.execPath,
    ['--import', 'tsx', script, '--baseline', baseline, '--current', current],
    {
      cwd: process.cwd(),
      encoding: 'utf8',
    },
  );
}

afterEach(() => {
  for (const directory of temporaryDirectories.splice(0)) {
    rmSync(directory, { recursive: true, force: true });
  }
});

test('remoção de campo obrigatório publicado reprova e exige /v2', () => {
  const result = run(document(), document(['id']));

  assert.equal(result.status, 1);
  assert.match(result.stderr, /Tenant\.name/);
  assert.match(result.stderr, /campo obrigat[oó]rio/i);
  assert.match(result.stderr, /\/v2/);
});

test('remoção de operação publicada reprova', () => {
  const current = document();
  delete current.paths['/v1/tenants'].get;

  const result = run(document(), current);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /GET \/v1\/tenants/);
});

test('novo campo obrigatório em schema publicado reprova consumidores antigos', () => {
  const current = document();
  current.components.schemas.Tenant.properties.document = { type: 'string' };
  current.components.schemas.Tenant.required.push('document');

  const result = run(document(), current);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /Tenant\.document/);
  assert.match(result.stderr, /novo campo obrigat[oó]rio/i);
});

test('remoção de resposta publicada reprova', () => {
  const current = document();
  delete current.paths['/v1/tenants'].get.responses[200];

  const result = run(document(), current);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /GET \/v1\/tenants.*200/);
});

test('mudança aditiva preserva compatibilidade', () => {
  const current = document();
  current.components.schemas.Tenant.properties.nickname = { type: 'string' };

  const result = run(document(), current);

  assert.equal(result.status, 0, result.stderr);
});
