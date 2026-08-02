import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

function read(file) {
  return readFileSync(file, 'utf8');
}

test('pipeline de PR contém todas as travas bloqueantes da US-007', () => {
  const workflow = read('.github/workflows/pull-request.yml');

  assert.match(workflow, /pull_request:/);
  assert.match(workflow, /pnpm format:check/);
  assert.match(workflow, /pnpm lint/);
  assert.match(workflow, /pnpm typecheck/);
  assert.match(workflow, /pnpm test:coverage/);
  assert.match(workflow, /governance\.test\.mjs/);
  assert.match(workflow, /pnpm governance/);
  assert.match(workflow, /pnpm audit --audit-level=critical/);
  assert.match(workflow, /pnpm build/);

  // Cenário Gherkin "Suíte de isolamento obrigatória" — Nexora.IntegrationTests via
  // Testcontainers.PostgreSql (RLS real). Os runners ubuntu-latest do GitHub Actions já têm
  // Docker, então não há `services: postgres:` — Testcontainers sobe o próprio container.
  assert.match(workflow, /name:\s*Integração e RLS/);
  assert.match(workflow, /dotnet test backend\/Nexora\.slnx/);
  assert.match(workflow, /FullyQualifiedName~IntegrationTests/);

  // Cenário Gherkin "Quebra de contrato de API" — snapshot gerado por Nexora.Api.Cloud
  // (Swashbuckle) e comparado por infra/scripts/openapi-compatibility.ts.
  assert.match(workflow, /name:\s*Contrato OpenAPI/);
  assert.match(workflow, /--generate-openapi-snapshot/);
  assert.match(workflow, /openapi-compatibility\.ts/);
  assert.match(workflow, /infra\/cloud\/openapi\/v1\.snapshot\.json/);
});

test('merge em main executa E2E, staging e publica edge/cloud com versão semântica', () => {
  const workflow = read('.github/workflows/main.yml');

  assert.match(workflow, /push:/);
  assert.match(workflow, /branches:\s*\[main\]/);
  assert.match(workflow, /pnpm test:e2e/);
  assert.match(workflow, /environment:\s*staging/);
  assert.match(workflow, /application: \[api-edge, api-cloud\]/);
  assert.match(workflow, /infra\/cloud\/\$\{\{ matrix\.application \}\}\.Dockerfile/);
  assert.match(workflow, /release-version\.ts/);
  assert.match(workflow, /packages:\s*write/);
});

test('pipeline agendado separa qualidade noturna e carga semanal', () => {
  const workflow = read('.github/workflows/scheduled.yml');

  assert.match(workflow, /schedule:/);
  assert.match(workflow, /chaos-offline/);
  assert.match(workflow, /data-integrity/);
  assert.match(workflow, /metrics-recalculation/);
  assert.match(workflow, /load-test/);
  assert.match(workflow, /github\.event\.schedule/);
  assert.match(workflow, /node infra\/scripts\/chaos-offline\.mjs/);
  assert.match(workflow, /node infra\/scripts\/data-integrity\.mjs/);
  assert.match(workflow, /node infra\/scripts\/metrics-recalculation\.mjs/);
  assert.match(workflow, /node infra\/scripts\/load-smoke\.mjs/);
  assert.doesNotMatch(workflow, /pnpm test:(?:chaos|data-integrity|metrics-recalculation|load)/);
  assert.match(workflow, /postgres:16-alpine/);
});

test('proteção de branch exige os checks nomeados do PR', () => {
  const protection = JSON.parse(read('.github/branch-protection.json'));
  const contexts = protection.required_status_checks.contexts;

  assert.equal(protection.required_status_checks.strict, true);
  assert.equal(protection.enforce_admins, true);
  assert.deepEqual(
    new Set(contexts),
    new Set([
      'Qualidade',
      'Governança',
      'Integração e RLS',
      'Contrato OpenAPI',
      'Segurança',
      'Build',
    ]),
  );
  assert.equal(protection.required_pull_request_reviews.required_approving_review_count, 1);

  const installer = read('.github/scripts/apply-branch-protection.mjs');
  assert.match(installer, /allow_squash_merge:\s*true/);
  assert.match(installer, /allow_merge_commit:\s*false/);
  assert.match(installer, /allow_rebase_merge:\s*false/);
});

test('Dockerfiles fazem build multi-stage do .NET real, sem privilégio root', () => {
  const projectName = { 'api-edge': 'Nexora.Api.Edge', 'api-cloud': 'Nexora.Api.Cloud' };

  for (const [application, project] of Object.entries(projectName)) {
    const dockerfile = read(`infra/cloud/${application}.Dockerfile`);
    assert.match(dockerfile, /FROM mcr\.microsoft\.com\/dotnet\/sdk:10\.0 AS build/);
    assert.match(dockerfile, /FROM mcr\.microsoft\.com\/dotnet\/aspnet:10\.0 AS runtime/);
    assert.match(dockerfile, new RegExp(`dotnet publish backend/src/${project.replaceAll('.', '\\.')}/${project.replaceAll('.', '\\.')}\\.csproj`));
    assert.match(dockerfile, /USER app/);
    assert.match(dockerfile, new RegExp(`ENTRYPOINT \\["dotnet", "${project.replaceAll('.', '\\.')}\\.dll"\\]`));
  }

  // Só api-edge expõe um health check genérico de processo (InstallationController,
  // GET /v1/health). api-cloud não tem endpoint de liveness genérico hoje (só GET /v1/sync/health,
  // saúde de UMA instalação autenticada) — HEALTHCHECK apontando para um caminho inexistente
  // marcaria o container como unhealthy para sempre, um bug silencioso pior que não ter
  // healthcheck (ver comentário no próprio infra/cloud/api-cloud.Dockerfile).
  assert.match(read('infra/cloud/api-edge.Dockerfile'), /HEALTHCHECK/);
});
