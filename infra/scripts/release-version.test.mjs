import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { afterEach, test } from 'node:test';

const directories = [];
const script = resolve('infra/scripts/release-version.ts');

function run(ref, sha = 'abcdef1234567890') {
  const root = mkdtempSync(join(tmpdir(), 'nexora-version-'));
  directories.push(root);
  writeFileSync(join(root, 'package.json'), JSON.stringify({ version: '1.4.2' }), 'utf8');
  return spawnSync(
    process.execPath,
    ['--import', 'tsx', script, '--root', root, '--ref', ref, '--sha', sha],
    {
      cwd: process.cwd(),
      encoding: 'utf8',
    },
  );
}

afterEach(() => {
  for (const directory of directories.splice(0))
    rmSync(directory, { recursive: true, force: true });
});

test('tag deve coincidir com a versão única do monorepo', () => {
  const result = run('refs/tags/v1.4.2');
  assert.equal(result.status, 0, result.stderr);
  assert.equal(result.stdout.trim(), '1.4.2');
});

test('merge em main produz versão semântica de pré-release imutável', () => {
  const result = run('refs/heads/main');
  assert.equal(result.status, 0, result.stderr);
  assert.equal(result.stdout.trim(), '1.4.2-main.abcdef1');
});

test('tag divergente reprova a release', () => {
  const result = run('refs/tags/v1.5.0');
  assert.equal(result.status, 1);
  assert.match(result.stderr, /package\.json.*1\.4\.2/);
});
