import assert from 'node:assert/strict';
import { execFileSync, spawnSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { afterEach, test } from 'node:test';

const script = resolve('infra/scripts/governance.ts');
const temporaryDirectories = [];

function fixture(files) {
  const root = mkdtempSync(join(tmpdir(), 'nexora-governance-'));
  temporaryDirectories.push(root);

  for (const [file, contents] of Object.entries(files)) {
    const target = join(root, file);
    mkdirSync(resolve(target, '..'), { recursive: true });
    writeFileSync(target, contents, 'utf8');
  }

  return root;
}

function run(root) {
  return spawnSync(process.execPath, ['--import', 'tsx', script, '--root', root], {
    cwd: process.cwd(),
    encoding: 'utf8',
  });
}

afterEach(() => {
  for (const directory of temporaryDirectories.splice(0)) {
    rmSync(directory, { recursive: true, force: true });
  }
});

test('ADR-013 reprova comparação literal de tenant e informa arquivo e linha', () => {
  const root = fixture({
    'apps/api-cloud/src/rule.ts':
      "const ok = true;\nif (tenant.slug === 'cliente-x') useSpecialRule();\n",
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /apps\/api-cloud\/src\/rule\.ts:2/);
  assert.match(result.stderr, /ADR-013/);
  assert.match(result.stderr, /configura[cç][aã]o/i);
});

test('ADR-010 reprova cor literal em componente e informa arquivo e linha', () => {
  const root = fixture({
    'packages/ui/src/button.tsx':
      "export const Button = () => <button style={{ color: '#C1121F' }} />;\n",
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /packages\/ui\/src\/button\.tsx:1/);
  assert.match(result.stderr, /ADR-010/);
  assert.match(result.stderr, /token/i);
});

test('ADR-010 reprova marca de cliente dentro de texto maior', () => {
  const root = fixture({
    'apps/api-cloud/src/openapi.ts': "const title = 'Dona Betinha Cloud API';\n",
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /openapi\.ts:1/);
  assert.match(result.stderr, /ADR-010/);
});

test('ADR-010 permite declarar valores neutros no módulo central de tokens', () => {
  const root = fixture({
    'packages/ui/src/theme/tokens.ts': "export const neutralTokens = { primary: '#174A3B' };\n",
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('ADR-010 reprova cor literal em pasta "branding" de um app — isenção vale só para packages/ui', () => {
  const root = fixture({
    // "branding" também é nome de domínio de feature legítimo (ex.: a tela de administração de
    // marca da US-003 em apps/web-admin/src/branding/) — um componente de página ali não declara
    // paleta/tema abstrato nenhum, então não deve herdar a isenção pensada para
    // packages/ui/src/(theme|branding)/, que é onde o design system central de fato vive.
    'apps/web-admin/src/branding/some-page.tsx':
      "export function SomePage() {\n  return <div style={{ color: '#123456' }} />;\n}\n",
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /some-page\.tsx:2/);
  assert.match(result.stderr, /ADR-010/);
});

test('ADR-010 reprova cor/marca de cliente-piloto embutida em arquivo .css', () => {
  const root = fixture({
    'packages/ui/src/tokens/tenant-fixture.css':
      '[data-tenant="dona-betinha"]{\n  --brand-primary:#C1121F;\n}\n',
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /tenant-fixture\.css/);
  assert.match(result.stderr, /ADR-010/);
});

test('ADR-010 reprova valor de marca do cliente-piloto mesmo em arquivo de tema com nome genérico', () => {
  const root = fixture({
    'packages/ui/src/branding/fallback-colors.ts':
      "export function createNeutralFallback() {\n  return { primary: '#C1121F' };\n}\n",
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /fallback-colors\.ts:2/);
  assert.match(result.stderr, /ADR-010/);
  assert.match(result.stderr, /cliente-piloto/i);
});

test('ADR-010 permite arquivo .css de tema genérico, sem marca nem cor de cliente-piloto', () => {
  const root = fixture({
    'packages/ui/src/tokens/generic-theme.css':
      ':root{\n  --brand-primary:#1E3446;\n  --brand-secondary:#B8965A;\n}\n',
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('ADR-010 não confunde "#" seguido de dígitos em comentário/JSDoc com cor literal', () => {
  const root = fixture({
    'packages/ui/src/components/order-ticket-like.tsx':
      "/** Origem: \"Mesa 12\", \"Delivery #4821\". */\nexport const x = 1;\n",
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('exceções de governança existem somente em plataforma e testes', () => {
  const root = fixture({
    'apps/web-platform/src/tenant-preview.ts': "if (tenant.id === 'tenant-demo') preview();\n",
    'apps/api-cloud/src/rule.test.ts': "if (tenant.id === 'tenant-fixture') expect(true);\n",
    'packages/ui/__tests__/theme.tsx': "const color = '#C1121F';\n",
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('ADR-013 reprova comparação literal de tenant em C# (backend/src)', () => {
  const root = fixture({
    'backend/src/Nexora.Application/Rules/SpecialRuleHandler.cs':
      'namespace Nexora.Application.Rules;\n\n' +
      'public sealed class SpecialRuleHandler\n{\n' +
      '    public void Handle(Tenant tenant)\n    {\n' +
      '        if (tenant.Slug == "dona-betinha") ApplySpecialRule();\n' +
      '    }\n}\n',
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /SpecialRuleHandler\.cs:7/);
  assert.match(result.stderr, /ADR-013/);
  assert.match(result.stderr, /configura[cç][aã]o/i);
});

test('ADR-013 permite comparação de tenant contra variável/propriedade em C# — só literal é proibido', () => {
  const root = fixture({
    'backend/src/Nexora.Application/Devices/ListDevicesQueryHandler.cs':
      'namespace Nexora.Application.Devices;\n\n' +
      'public sealed class ListDevicesQueryHandler\n{\n' +
      '    public void Handle(Guid tenantId)\n    {\n' +
      '        var rows = _db.Devices.Where(d => d.TenantId == tenantId);\n' +
      '    }\n}\n',
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('ADR-010 reprova cor de marca do cliente-piloto embutida em C#', () => {
  const root = fixture({
    'backend/src/Nexora.Infrastructure/Storage/DefaultTheme.cs':
      'namespace Nexora.Infrastructure.Storage;\n\n' +
      'public static class DefaultTheme\n{\n' +
      '    public const string Primary = "#C1121F";\n}\n',
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /DefaultTheme\.cs:5/);
  assert.match(result.stderr, /ADR-010/);
});

test('C#: isenção de ADR-013/ADR-010 existe só para backend/tests e módulo de plataforma', () => {
  const root = fixture({
    'backend/tests/Nexora.ArchitectureTests/TenantFixtureTests.cs':
      'namespace Nexora.ArchitectureTests;\n\n' +
      'public sealed class TenantFixtureTests\n{\n' +
      '    public void Fixture(Tenant tenant) { if (tenant.Slug == "tenant-fixture") { } }\n}\n',
    'backend/src/Nexora.Domain/Platform/PlatformTenantLookup.cs':
      'namespace Nexora.Domain.Platform;\n\n' +
      'public sealed class PlatformTenantLookup\n{\n' +
      '    public void Lookup(Tenant tenant) { if (tenant.Slug == "tenant-fixture") { } }\n}\n',
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('ADR-019 reprova operações destrutivas e orienta expand-contract', () => {
  const root = fixture({
    'packages/db/prisma/migrations/20260801000000_remove_legacy/migration.sql':
      'ALTER TABLE order_item DROP COLUMN legacy_price;\n',
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /migration\.sql:1/);
  assert.match(result.stderr, /ADR-019/);
  assert.match(result.stderr, /expans[aã]o e contra[cç][aã]o/i);
});

test('ADR-019 detecta operação destrutiva quebrada em múltiplas linhas', () => {
  const root = fixture({
    'packages/db/prisma/migrations/20260801000000_remove_legacy/migration.sql':
      'ALTER TABLE order_item\n  ALTER COLUMN legacy_price\n  TYPE NUMERIC(12, 2);\n',
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /migration\.sql:2/);
  assert.match(result.stderr, /ADR-019/);
});

// A partir de ADR-036/038/039, a migration real é um arquivo .cs do EF Core em
// backend/src/Nexora.Infrastructure/Persistence/Migrations — não mais um .sql solto em
// packages/db/prisma/migrations (pasta que não existe mais neste repositório). Ver
// EF_MIGRATIONS_ROOT/tablesWithTenantIdInEfMigration em governance.ts.
function efCreateTableFixture(tableName, extraColumnsCs = '') {
  return `
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nexora.Infrastructure.Persistence.Migrations
{
    public partial class AddTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "${tableName}",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ${extraColumnsCs}
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_${tableName}", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "${tableName}");
        }
    }
}
`;
}

test('RLS reprova tabela de negócio sem política completa (migration EF Core)', () => {
  const root = fixture({
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000000_AddTable.cs':
      efCreateTableFixture('ticket'),
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /Migrations/);
  assert.match(result.stderr, /RLS/);
  assert.match(result.stderr, /ticket/);
  assert.match(result.stderr, /ENABLE.*FORCE.*POLICY/is);
});

test('migration EF Core aditiva com RLS completa (padrão explícito) passa', () => {
  const root = fixture({
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000000_AddTable.cs':
      efCreateTableFixture('ticket', 'note = table.Column<string>(type: "text", nullable: true)'),
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000100_EnableRls.cs': `
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nexora.Infrastructure.Persistence.Migrations
{
    public partial class EnableRls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE ticket ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE ticket FORCE ROW LEVEL SECURITY;");
            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation ON ticket
                  USING (tenant_id = current_tenant_id())
                  WITH CHECK (tenant_id = current_tenant_id());
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
`,
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('migration EF Core aditiva com RLS completa (padrão dinâmico em massa) passa', () => {
  const root = fixture({
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000000_AddTable.cs':
      efCreateTableFixture('ticket'),
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000100_EnableRls.cs': `
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nexora.Infrastructure.Persistence.Migrations
{
    public partial class EnableRls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                DECLARE t text;
                BEGIN
                  FOREACH t IN ARRAY ARRAY['ticket'] LOOP
                    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', t);
                    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', t);
                    EXECUTE format(
                      'CREATE POLICY tenant_isolation ON %I USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id())',
                      t);
                  END LOOP;
                END $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
`,
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('RLS ignora arquivo .Designer.cs/ModelSnapshot.cs (não são a migration real)', () => {
  const root = fixture({
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000000_AddTable.Designer.cs':
      efCreateTableFixture('ticket'),
  });

  const result = run(root);

  // Nenhuma migration real (.cs que não seja Designer/Snapshot) declara a tabela -> nenhuma
  // tabela com tenant_id é encontrada -> sem violação de RLS a reportar (mas isso não significa
  // aprovação de verdade: é o comportamento esperado de "não achei nada para checar").
  assert.equal(result.status, 0, result.stderr);
});

function efMigrationFixture(className, upBody, downBody = '') {
  return `
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nexora.Infrastructure.Persistence.Migrations
{
    public partial class ${className} : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ${upBody}
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ${downBody}
        }
    }
}
`;
}

test('ADR-019 reprova DropColumn direto (API típada do EF Core) sem depreciação anterior', () => {
  const root = fixture({
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000000_RemoveLegacyPrice.cs':
      efMigrationFixture(
        'RemoveLegacyPrice',
        'migrationBuilder.DropColumn(name: "legacy_price", table: "order_item");',
      ),
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /RemoveLegacyPrice\.cs/);
  assert.match(result.stderr, /ADR-019/);
  assert.match(result.stderr, /expandir e contrair|governance:column-deprecated/i);
});

test('ADR-019 permite DropColumn típado quando uma migration anterior marcou a coluna obsoleta', () => {
  const root = fixture({
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000000_DeprecateLegacyPrice.cs':
      efMigrationFixture(
        'DeprecateLegacyPrice',
        '// governance:column-deprecated: order_item.legacy_price\n            migrationBuilder.Sql("SELECT 1;");',
      ),
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260802000000_RemoveLegacyPrice.cs':
      efMigrationFixture(
        'RemoveLegacyPrice',
        'migrationBuilder.DropColumn(name: "legacy_price", table: "order_item");',
      ),
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('marcador de depreciação não vale retroativamente para migration ANTERIOR a ele', () => {
  const root = fixture({
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000000_RemoveLegacyPrice.cs':
      efMigrationFixture(
        'RemoveLegacyPrice',
        'migrationBuilder.DropColumn(name: "legacy_price", table: "order_item");',
      ),
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260802000000_DeprecateLegacyPrice.cs':
      efMigrationFixture(
        'DeprecateLegacyPrice',
        '// governance:column-deprecated: order_item.legacy_price\n            migrationBuilder.Sql("SELECT 1;");',
      ),
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /ADR-019/);
});

test('ADR-019 reprova RenameColumn direto (API típada do EF Core)', () => {
  const root = fixture({
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000000_RenameColumn.cs':
      efMigrationFixture(
        'RenameColumn',
        'migrationBuilder.RenameColumn(name: "old_name", table: "order_item", newName: "new_name");',
      ),
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /ADR-019/);
  assert.match(result.stderr, /backfill/i);
});

test('ADR-019 reprova AlterColumn nullable:false sem defaultValue (API típada do EF Core)', () => {
  const root = fixture({
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000000_AlterColumn.cs':
      efMigrationFixture(
        'AlterColumn',
        'migrationBuilder.AlterColumn<string>(name: "note", table: "order_item", type: "text", nullable: false);',
      ),
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /ADR-019/);
  assert.match(result.stderr, /defaultValue/i);
});

test('AlterColumn nullable:false COM defaultValue passa (expansão compatível)', () => {
  const root = fixture({
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000000_AlterColumn.cs':
      efMigrationFixture(
        'AlterColumn',
        'migrationBuilder.AlterColumn<string>(name: "note", table: "order_item", type: "text", nullable: false, defaultValue: "");',
      ),
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('DropColumn dentro de Down() é rollback normal, não remoção destrutiva — não reprova', () => {
  const root = fixture({
    'backend/src/Nexora.Infrastructure/Persistence/Migrations/20260801000000_AddColumn.cs':
      efMigrationFixture(
        'AddColumn',
        'migrationBuilder.AddColumn<string>(name: "note", table: "order_item", type: "text", nullable: true);',
        'migrationBuilder.DropColumn(name: "note", table: "order_item");',
      ),
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('índice sem CONCURRENTLY é seguro quando a tabela nasce vazia na mesma migration', () => {
  const root = fixture({
    'packages/db/prisma/migrations/20260801000000_add_lookup/migration.sql': `
CREATE TABLE public_lookup (id UUID PRIMARY KEY, code TEXT NOT NULL);
CREATE UNIQUE INDEX uq_public_lookup_code ON public_lookup (code);
`,
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('bootstrap inicial marcado permite índice Prisma sem CONCURRENTLY', () => {
  const root = fixture({
    'packages/db/prisma/migrations/20260731001500_bootstrap/migration.sql': `
-- governance:foundation-bootstrap
CREATE INDEX idx_existing_name ON existing_table (name);
`,
  });

  const result = run(root);

  assert.equal(result.status, 0, result.stderr);
});

test('marcador de bootstrap não abre exceção em migrations futuras', () => {
  const root = fixture({
    'packages/db/prisma/migrations/20260801000000_unsafe/migration.sql': `
-- governance:foundation-bootstrap
CREATE INDEX idx_existing_name ON existing_table (name);
`,
  });

  const result = run(root);

  assert.equal(result.status, 1);
  assert.match(result.stderr, /ADR-019/);
});
