import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import test from 'node:test';
import assert from 'node:assert/strict';

const edge = resolve(import.meta.dirname, '..');
const backend = resolve(edge, '../../backend');

// Remove linhas de comentário (YAML/Dockerfile/shell usam "#") antes de checar ausência de
// referência à stack Node/Prisma morta — os arquivos legitimamente MENCIONAM essa stack em
// comentários explicando por que ela foi substituída (ver docker-compose.yml/Dockerfiles), o que
// não deve contar como "referência viva" para efeito destes testes.
const stripComments = (source) =>
  source
    .split('\n')
    .filter((line) => !/^\s*#/.test(line))
    .join('\n');

// US-006, gap P0-1: até esta correção, docker-compose.yml e os Dockerfiles de infra/cloud ainda
// apontavam para a stack Node/Prisma anterior (pnpm/prisma:deploy, apps/api-edge/apps/api-cloud),
// que não existe mais no monorepo — a ponte para o backend .NET real (Nexora.Api.Edge/
// Nexora.Api.Cloud, ADR-036/037) estava quebrada mesmo com a imagem certa configurada. Estes
// testes passaram a validar a topologia NOVA em vez de reafirmar a antiga.

test('compose declara topologia local completa e nao publica banco ou redis', async () => {
  const compose = await readFile(resolve(edge, 'docker-compose.yml'), 'utf8');

  for (const service of ['postgres:', 'redis:', 'migrator:', 'api-edge:', 'web:', 'watchtower:']) {
    assert.match(compose, new RegExp(`^  ${service}`, 'm'));
  }

  // sync-worker e bootstrap-import deixaram de ser containers próprios: a sincronização
  // periódica agora é o BackgroundService SyncOutboxWorker embutido no processo do api-edge, e a
  // carga inicial é importada via endpoint HTTP do próprio api-edge — não processos Node
  // separados. Ver comentário em docker-compose.yml.
  assert.doesNotMatch(compose, /^ {2}sync-worker:/m);
  assert.doesNotMatch(compose, /^ {2}bootstrap-import:/m);

  assert.doesNotMatch(compose, /5432:5432|6379:6379/);
  assert.match(compose, /postgres:16/);

  // Stack Node/Prisma morta: nenhum resquício EXECUTÁVEL de pnpm/prisma/node dist (comentários
  // explicando a migração, que mencionam esses termos em prosa, não contam — ver stripComments).
  assert.doesNotMatch(stripComments(compose), /prisma|pnpm|apps\/api-edge|apps\/api-cloud/i);

  // Migração real do EF Core (ADR-038) contra o Postgres do compose, na mesma imagem do api-edge.
  assert.match(compose, /--migrate/);
  assert.match(compose, /ConnectionStrings__DefaultConnection/);

  assert.doesNotMatch(compose, /\bbuild:/);
  assert.match(compose, /EDGE_API_IMAGE/);
  assert.match(compose, /EDGE_WEB_IMAGE/);
  assert.match(compose, /EDGE_DB_RUNTIME_USER/);

  // Convenção de configuração hierárquica do ASP.NET Core (Section__Key) — não os nomes de env
  // var flat da stack Node anterior.
  assert.match(compose, /Edge__Installation__InstallationId/);
  assert.match(compose, /Auth__Jwt__Secret/);
});

test('instalador registra antes de subir containers e nunca persiste token', async () => {
  const install = await readFile(resolve(edge, 'install.sh'), 'utf8');
  const register = install.indexOf('register_installation');
  const containers = install.indexOf('start_containers');
  assert.ok(register >= 0 && containers > register);
  assert.doesNotMatch(install, /INSTALL_TOKEN=.*>>|token=.*\.env/);
  assert.match(install, /chmod 600/);
  assert.match(install, /EDGE_CONTAINER_GID/);
  assert.match(install, /chmod 640/);
  assert.match(install, /backup\.sh daily/);
  assert.match(install, /hasMore/);
  assert.match(install, /nextCursor/);
  assert.match(install, /X-Installation-Timestamp/);
  assert.match(install, /X-Installation-Nonce/);
  assert.match(install, /X-Installation-Signature/);
  assert.match(install, /printf '%s\\n%s\\n%s\\n%s'/);
  assert.doesNotMatch(install, /up -d --build/);
});

test('imagem web publica builds reais de POS, KDS, menu e administracao local', async () => {
  const dockerfile = await readFile(resolve(edge, 'web.Dockerfile'), 'utf8');
  const nginx = await readFile(resolve(edge, 'nginx.conf'), 'utf8');
  for (const app of ['web-pos', 'web-kds', 'web-menu', 'web-admin'])
    assert.match(dockerfile, new RegExp(app));
  for (const route of ['/pos/', '/kds/', '/menu/', '/admin/'])
    assert.match(nginx, new RegExp(route));
  for (const route of ['pos', 'kds', 'menu', 'admin'])
    assert.match(dockerfile, new RegExp(`--base=/${route}/`));
  assert.doesNotMatch(dockerfile, /build -- --base/);
  assert.doesNotMatch(nginx, /usr\/share\/nginx\/html:ro/);
});

test('backup usa dump custom, cifra upload e restore valida arquivo', async () => {
  const backup = await readFile(resolve(edge, 'backup.sh'), 'utf8');
  const restore = await readFile(resolve(edge, 'restore.sh'), 'utf8');
  assert.match(backup, /pg_dump.*--format=custom/);
  assert.match(backup, /openssl enc/);
  assert.match(backup, /\/proc\/sys\/kernel\/random\/uuid/);
  assert.doesNotMatch(backup, /Idempotency-Key: \$request_nonce/);
  assert.doesNotMatch(backup, /find .*?-printf/);
  assert.match(restore, /pg_restore --list/);
  assert.match(restore, /--verify/);
  // restore.sh só para/sobe os containers que ainda existem de fato (sync-worker foi absorvido
  // pelo api-edge — ver docker-compose.yml).
  assert.doesNotMatch(restore, /sync-worker/);
});

test('doctor verifica containers, banco, redis, TLS, sync, disco e backup', async () => {
  const doctor = await readFile(resolve(edge, 'doctor.sh'), 'utf8');
  for (const check of ['containers', 'postgres', 'redis', 'tls', 'sync', 'disk', 'backup']) {
    assert.match(doctor, new RegExp(`${check}`));
  }
  assert.doesNotMatch(doctor, /find .*?-printf/);
  assert.doesNotMatch(doctor, /sync-worker/);
});

test('Dockerfiles de infra/cloud buildam o backend .NET real, nao a stack Node/Prisma morta', async () => {
  const apiEdgeDockerfile = await readFile(resolve(edge, '../cloud/api-edge.Dockerfile'), 'utf8');
  const apiCloudDockerfile = await readFile(resolve(edge, '../cloud/api-cloud.Dockerfile'), 'utf8');

  for (const dockerfile of [apiEdgeDockerfile, apiCloudDockerfile]) {
    const code = stripComments(dockerfile);
    assert.doesNotMatch(code, /FROM node/i);
    assert.doesNotMatch(code, /pnpm|prisma|apps\/api-edge|apps\/api-cloud/i);
    assert.match(dockerfile, /FROM mcr\.microsoft\.com\/dotnet\/sdk:/);
    assert.match(dockerfile, /FROM mcr\.microsoft\.com\/dotnet\/aspnet:/);
    assert.match(dockerfile, /dotnet (restore|publish)/);
  }

  assert.match(apiEdgeDockerfile, /Nexora\.Api\.Edge\.csproj/);
  assert.match(apiEdgeDockerfile, /ENTRYPOINT.*Nexora\.Api\.Edge\.dll/);
  assert.match(apiCloudDockerfile, /Nexora\.Api\.Cloud\.csproj/);
  assert.match(apiCloudDockerfile, /ENTRYPOINT.*Nexora\.Api\.Cloud\.dll/);
});

test('sync worker (.NET) mantem processo ativo para novos ciclos', async () => {
  // Porta de sync-worker.ts (apps/api-edge, removido) — hoje é o BackgroundService
  // SyncOutboxWorker, registrado via AddHostedService no processo do api-edge (ver
  // Nexora.Api.Edge/Program.cs e docker-compose.yml).
  const worker = await readFile(
    resolve(backend, 'src/Nexora.Infrastructure/Installation/SyncOutboxWorker.cs'),
    'utf8',
  );
  const authHandler = await readFile(
    resolve(backend, 'src/Nexora.Api.Cloud/Infrastructure/Auth/InstallationAuthenticationHandler.cs'),
    'utf8',
  );
  const signatureVerifier = await readFile(
    resolve(backend, 'src/Nexora.Infrastructure/Installations/InstallationSignatureVerifier.cs'),
    'utf8',
  );
  const edgeProgram = await readFile(resolve(backend, 'src/Nexora.Api.Edge/Program.cs'), 'utf8');

  assert.match(worker, /class SyncOutboxWorker\s*:\s*BackgroundService/);
  assert.match(worker, /while\s*\(!stoppingToken\.IsCancellationRequested\)/);
  assert.match(worker, /Task\.Delay\(/);
  assert.match(edgeProgram, /AddHostedService<SyncOutboxWorker>/);

  assert.match(authHandler, /X-Installation-Timestamp/);
  assert.match(authHandler, /X-Installation-Nonce/);
  assert.match(authHandler, /X-Installation-Signature/);
  assert.match(signatureVerifier, /VerifySignature\(/);
});
