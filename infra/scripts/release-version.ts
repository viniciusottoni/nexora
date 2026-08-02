import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

function argument(name: string, fallback?: string): string {
  const index = process.argv.indexOf(name);
  const value = index >= 0 ? process.argv[index + 1] : undefined;
  if (value) return value;
  if (fallback) return fallback;
  throw new Error(`Argumento obrigatório ausente: ${name}`);
}

function main(): void {
  const root = resolve(argument('--root', process.cwd()));
  const ref = argument('--ref', process.env.GITHUB_REF);
  const sha = argument('--sha', process.env.GITHUB_SHA ?? 'local0000000');
  const packageJson = JSON.parse(readFileSync(resolve(root, 'package.json'), 'utf8')) as {
    version?: unknown;
  };
  const version = packageJson.version;
  if (typeof version !== 'string' || !/^\d+\.\d+\.\d+$/.test(version)) {
    throw new Error('package.json deve declarar uma única versão semântica MAJOR.MINOR.PATCH.');
  }

  if (ref.startsWith('refs/tags/')) {
    const tag = ref.slice('refs/tags/'.length);
    if (tag !== `v${version}`) {
      throw new Error(
        `Tag ${tag} diverge do package.json (${version}). ADR-029 exige versão única.`,
      );
    }
    console.log(version);
    return;
  }

  const shortSha = sha.slice(0, 7).toLowerCase();
  if (!/^[0-9a-f]{7}$/.test(shortSha)) throw new Error('SHA inválido para gerar versão imutável.');
  console.log(`${version}-main.${shortSha}`);
}

try {
  main();
} catch (error) {
  console.error(error instanceof Error ? error.message : String(error));
  process.exitCode = 1;
}
