#!/usr/bin/env node

import { spawnSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const repoRoot = fileURLToPath(new URL('../..', import.meta.url));
const devComposePath = fileURLToPath(new URL('../dev/docker-compose.yml', import.meta.url));

function readDevPostgresPassword() {
  const compose = readFileSync(devComposePath, 'utf8');
  const match = compose.match(/^\s*POSTGRES_PASSWORD:\s*(.+)\s*$/m);
  if (!match) {
    throw new Error('POSTGRES_PASSWORD nao encontrado em infra/dev/docker-compose.yml');
  }

  return match[1].trim().replace(/^['"]|['"]$/g, '');
}

function buildConnection(database) {
  const host = process.env.NEXORA_DEV_DB_HOST ?? 'localhost';
  const port = process.env.NEXORA_DEV_DB_PORT ?? '5432';
  const user = process.env.NEXORA_DEV_DB_USER ?? 'donabetinha';
  const password = process.env.NEXORA_DEV_DB_PASSWORD ?? readDevPostgresPassword();

  return [`Host=${host}`, `Port=${port}`, `Database=${database}`, `Username=${user}`, `Password=${password}`].join(';');
}

const edgeConnection = buildConnection('donabetinha_edge_dev');

console.log('==> Renovando o codigo inicial da gestao local...');

const result = spawnSync(
  'dotnet',
  [
    'run',
    '--project',
    'backend/src/Nexora.DevSeeder',
    '--',
    '--connection',
    edgeConnection,
    '--mode',
    'edge',
  ],
  { cwd: repoRoot, stdio: 'inherit' },
);

if (result.error?.code === 'ENOENT') {
  console.error('Comando nao encontrado: dotnet');
  process.exitCode = 1;
} else if (result.error) {
  throw result.error;
} else if (result.status !== 0) {
  console.error(`O DevSeeder terminou com o codigo ${result.status}.`);
  process.exitCode = result.status ?? 1;
} else {
  console.log('\nAbra http://localhost:5173/admin e use o codigo exibido acima.');
}
