import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { relative, resolve, sep } from 'node:path';

type Violation = {
  file: string;
  line: number;
  rule: 'ADR-010' | 'ADR-013' | 'ADR-019' | 'RLS';
  message: string;
};

// .css/.scss entraram na varredura depois que uma auditoria achou a paleta REAL do
// cliente-piloto (Pizzaria Dona Betinha) embutida em packages/ui/src/tokens/tenants.css
// (seletor `[data-tenant="dona-betinha"]`) — arquivo que nunca tinha sido lido por este script.
// Não existe .scss no projeto hoje (só CSS puro com custom properties, ver packages/ui/src/tokens);
// a extensão está na lista para não exigir outra mudança neste arquivo no dia em que alguém migrar.
const SOURCE_EXTENSIONS = new Set([
  '.js', '.jsx', '.mjs', '.cjs', '.ts', '.tsx', '.mts', '.cts', '.css', '.scss',
]);
const TEST_SEGMENTS = new Set(['__tests__', 'test', 'tests', '__fixtures__', 'fixtures']);

// Paleta REAL do cliente-piloto (Pizzaria Dona Betinha, doc 02 §5.2) — módulo-escopo porque tanto
// sourceViolations() (TS/JS/CSS) quanto csharpViolations() (C#, backend/src) precisam do mesmo
// bloqueio permanente. Ver comentário original de knownClientColor em sourceViolations() para o
// motivo de serem só os 3 valores-base, sem os tons derivados.
const KNOWN_CLIENT_BRAND_COLORS = ['#C1121F', '#669BBC', '#FDF0D5'];

function argument(name: string): string | undefined {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : undefined;
}

function normalizedPath(root: string, file: string): string {
  return relative(root, file).split(sep).join('/');
}

function extension(file: string): string {
  const dot = file.lastIndexOf('.');
  return dot < 0 ? '' : file.slice(dot);
}

function filesBelow(directory: string): string[] {
  if (!existsSync(directory)) return [];

  const result: string[] = [];
  for (const entry of readdirSync(directory)) {
    if (
      entry === 'node_modules' ||
      entry === 'dist' ||
      entry === 'coverage' ||
      entry === '.turbo' ||
      entry === 'bin' ||
      entry === 'obj'
    ) {
      continue;
    }

    const file = resolve(directory, entry);
    if (statSync(file).isDirectory()) result.push(...filesBelow(file));
    else result.push(file);
  }
  return result;
}

function isException(file: string): boolean {
  const segments = file.toLowerCase().split('/');
  const basename = segments.at(-1) ?? '';
  const isTest =
    segments.some((segment) => TEST_SEGMENTS.has(segment)) ||
    /\.(?:test|spec)\.[cm]?[jt]sx?$/.test(basename);
  const isPlatform = segments.some(
    (segment) => segment === 'platform' || segment === 'web-platform' || segment === 'api-platform',
  );
  return isTest || isPlatform;
}

function lineNumber(contents: string, index: number): number {
  return contents.slice(0, index).split('\n').length;
}

function sourceViolations(root: string): Violation[] {
  const roots = [resolve(root, 'apps'), resolve(root, 'packages')];
  const files = roots.flatMap(filesBelow).filter((file) => SOURCE_EXTENSIONS.has(extension(file)));
  const violations: Violation[] = [];

  const tenantExpression = String.raw`(?:tenant(?:Id|_id)?|tenant\s*(?:\.|\?\.)\s*(?:id|slug)|tenant\[['"](?:id|slug)['"]\])`;
  const quote = '[\'"`]';
  const tenantLiteralComparisons = [
    new RegExp(`${tenantExpression}\\s*(?:===|==|!==|!=)\\s*${quote}`, 'gi'),
    new RegExp(`${quote}[^'\"\\x60]+${quote}\\s*(?:===|==|!==|!=)\\s*${tenantExpression}`, 'gi'),
  ];
  const embeddedClient =
    /['"`][^'"`\r\n]*(?:dona[\s_-]*betinha|pizzaria[\s_-]*dona[\s_-]*betinha)[^'"`\r\n]*['"`]/gi;

  // Regra de cor literal (ADR-010, "nenhum componente usa cor literal") — exige contexto de
  // atribuição CSS/JS (precedida por ":" de propriedade ou "=" de variável) para não disparar
  // dentro de texto livre. Antes bastava um "#" seguido de 3-8 hex em QUALQUER lugar do arquivo —
  // inclusive dentro de comentário/JSDoc, o que confundia "Delivery #4821" (exemplo de código de
  // pedido em order-ticket.tsx) com uma cor. Comentários de exemplo como esse não têm ":"/"="
  // imediatamente antes do "#", então já não casam; código real (`color: '#fff'`,
  // `--brand-primary:#C1121F`, `const x = '#C1121F'`) continua casando.
  const literalColor = /[:=]\s*['"`]?(?:#[\da-f]{3,8}\b|(?:rgb|hsl)a?\s*\()/gi;

  // Paleta REAL do cliente-piloto (Pizzaria Dona Betinha, doc 02 §5.2) — bloqueio permanente,
  // independente de pasta ou nome de variável. Motivo: o gap que a auditoria encontrou
  // (packages/ui/src/branding/runtime-branding.ts, função já chamada
  // `createNeutralBrandingResponse`) mostrou que um nome de variável/arquivo genérico NÃO é
  // garantia de que o valor embutido também é genérico — só um valor real do cliente escapou
  // com um nome que parecia neutro. Por isso este bloqueio de valores conhecidos NÃO aceita a
  // isenção de "arquivo de token/tema abstrato" abaixo (isTokenDeclaration): ele vale em
  // qualquer arquivo de código-fonte, css incluso, só respeitando a isenção padrão de
  // teste/fixture/plataforma (isException) — um teste de regressão pode legitimamente precisar
  // do valor real para verificar o branding vindo da API.
  // Só os 3 valores-base canônicos (documentados no doc 02 §5.2 e no exemplo de contrato de
  // US-003, GET /v1/public/branding) — deliberadamente SEM os tons derivados (hover/active/subtle)
  // que existiam em tokens/tenants.css: um deles, o tom "subtle" (#FBE0E1), colide por
  // coincidência com --nx-danger-100 do design system institucional (packages/ui/src/tokens/
  // colors.css) — um tom de vermelho genérico do próprio produto, não uma marca de cliente. Um
  // valor derivado tem mais chance de colidir com outro tom qualquer da paleta; os 3 valores-base
  // são específicos o suficiente (e documentados como sendo exatamente os do cliente-piloto) para
  // este bloqueio permanente valer a pena.
  const knownClientColor = new RegExp(
    String.raw`(?:${KNOWN_CLIENT_BRAND_COLORS.join('|')})\b`,
    'gi',
  );

  // Declaração (function/const/let/var) mais próxima antes de um índice — usada para decidir se
  // um arquivo de token/tema abstrato (isTokenDeclaration) está de fato nomeando algo genérico
  // (neutralTheme, createNeutralBrandingResponse, defaultPalette...) e não, por exemplo,
  // `donaBetinhaTheme`. Substitui a antiga isenção "qualquer arquivo dentro de theme/branding/"
  // (que perdoava até valor real de cliente, ver comentário de knownClientColor acima) por uma
  // isenção por trecho de código.
  const declarationName = /(?:export\s+)?(?:function|const|let|var)\s+([A-Za-z_$][\w$]*)/g;
  const genericTokenName = /neutral|default|fallback|placeholder|institucional|platform/i;

  function nearestDeclarationName(source: string, index: number): string {
    let name = '';
    for (const match of source.matchAll(declarationName)) {
      if (match.index === undefined || match.index > index) break;
      name = match[1] ?? name;
    }
    return name;
  }

  for (const absoluteFile of files) {
    const file = normalizedPath(root, absoluteFile);
    if (isException(file)) continue;
    const contents = readFileSync(absoluteFile, 'utf8');

    for (const pattern of tenantLiteralComparisons) {
      for (const match of contents.matchAll(pattern)) {
        violations.push({
          file,
          line: lineNumber(contents, match.index),
          rule: 'ADR-013',
          message:
            'código condicional por tenant. Transforme a diferença em configuração disponível para todos os tenants.',
        });
      }
    }

    for (const match of contents.matchAll(embeddedClient)) {
      violations.push({
        file,
        line: lineNumber(contents, match.index),
        rule: 'ADR-010',
        message:
          'marca de cliente embutida. Identidade visual e textos devem vir da configuração de branding.',
      });
    }

    for (const match of contents.matchAll(knownClientColor)) {
      violations.push({
        file,
        line: lineNumber(contents, match.index),
        rule: 'ADR-010',
        message:
          'valor de marca do cliente-piloto (Pizzaria Dona Betinha) embutido em código compartilhado. Identidade visual é dado de tenant, nunca literal — mesmo em arquivo com nome genérico.',
      });
    }

    // Escopado a packages/ui/src/(theme|branding)/ — não "qualquer pasta chamada branding em
    // qualquer app". "branding" também é um nome de domínio de feature legítimo (ex.:
    // apps/web-admin/src/branding/, a tela de administração de marca da US-003): um componente
    // de página ali não declara paleta/tema abstrato nenhum, só CONSOME a marca via API, então
    // não deve herdar a isenção pensada para os arquivos que efetivamente definem tokens no
    // design system central.
    const isTokenDeclaration =
      /^packages\/ui\/src\/(?:theme|branding)\//.test(file) ||
      /(?:^|\/)(?:theme|tokens)(?:\.[^.]+)?\.[cm]?[jt]sx?$/.test(file);
    const isWebComponent =
      /^(?:apps\/web-[^/]+|packages\/ui)\//.test(file) &&
      /\.[cm]?[jt]sx?$/.test(file);
    if (isWebComponent) {
      for (const match of contents.matchAll(literalColor)) {
        if (isTokenDeclaration && genericTokenName.test(nearestDeclarationName(contents, match.index))) {
          continue;
        }
        violations.push({
          file,
          line: lineNumber(contents, match.index),
          rule: 'ADR-010',
          message: 'cor literal em componente. Use um token CSS --brand-* do design system.',
        });
      }
    }
  }

  return violations;
}

// ADR-013 nunca varria C# — só apps/packages (TS/JS/CSS) acima. O grep bloqueante que o próprio
// ADR-013 prescreve (`grep -rEn "(tenant(Id|\.Slug)?\s*==\s*[\"'])" backend/src --include=*.cs
// --exclude-dir=Platform --exclude-dir=*Tests`) nunca tinha sido conectado a este script — esta
// função fecha esse gap, adaptando a MESMA heurística de tenantLiteralComparisons/embeddedClient/
// knownClientColor usada acima para a sintaxe de C#.
const CSHARP_ROOTS = ['backend/src', 'backend/tests'];

/**
 * Isenção específica de C# — mirror do próprio exemplo de CI do ADR-013
 * (`--exclude-dir=Platform --exclude-dir=*Tests`), não a isenção geral de `isException()` (que foi
 * desenhada para apps/packages e não entende a convenção de nome de projeto do backend, ex.:
 * `Nexora.ArchitectureTests`, `Nexora.IntegrationTests`). Testes legítimos (Nexora.ArchitectureTests
 * comparando tenant IDs de fixture, ex.: US-001) e o módulo de plataforma (que legitimamente opera
 * sobre tenants, ex.: Nexora.Domain.Platform/Nexora.Infrastructure.Platform) ficam de fora — qualquer
 * OUTRA pasta de `backend/src` continua sujeita à regra.
 */
function isCsharpException(file: string): boolean {
  const segments = file.split('/');
  if (segments[0] === 'backend' && segments[1] === 'tests') return true;
  return segments.some(
    (segment) => segment.toLowerCase() === 'platform' || /tests$/i.test(segment),
  );
}

function csharpFiles(root: string): string[] {
  return CSHARP_ROOTS.flatMap((relative) => filesBelow(resolve(root, relative))).filter(
    (file) =>
      file.endsWith('.cs') &&
      !file.endsWith('.Designer.cs') &&
      !file.endsWith('.g.cs') &&
      !file.endsWith('AssemblyInfo.cs') &&
      !file.endsWith('GlobalUsings.g.cs'),
  );
}

function csharpViolations(root: string): Violation[] {
  const files = csharpFiles(root);
  const violations: Violation[] = [];

  // Identificador de tenant em C#: `tenantId`/`TenantId` (variável, parâmetro ou propriedade) OU
  // acesso a membro `tenant.Id`/`tenant.Slug` (qualquer capitalização — convenção de C# varia entre
  // parâmetro camelCase e propriedade PascalCase). Comparar contra OUTRA variável/propriedade
  // (`x.TenantId == tenantId`, `ICurrentTenantContext.TenantId == id`) é o padrão CORRETO e onipresente
  // do produto (RLS/filtro por tenant do próprio request) — só comparar contra um LITERAL (string ou
  // Guid.Parse("...")) é o que o ADR-013 proíbe, por isso as duas regras abaixo exigem aspas (ou
  // `Guid.Parse(`) imediatamente após o operador, nunca outro identificador.
  const tenantExpressionCs = String.raw`(?:\btenant(?:Id)?\b|\btenant\b\s*(?:\.|\?\.)\s*(?:Id|Slug)\b)`;
  const literalOrGuidParseCs = String.raw`(?:"|Guid\s*\.\s*Parse\s*\(\s*")`;
  const tenantLiteralComparisonsCs = [
    new RegExp(String.raw`${tenantExpressionCs}\s*(?:==|!=)\s*${literalOrGuidParseCs}`, 'gi'),
    new RegExp(
      String.raw`(?:"[^"\r\n]*"|Guid\s*\.\s*Parse\s*\(\s*"[^"\r\n]*"\s*\))\s*(?:==|!=)\s*${tenantExpressionCs}`,
      'gi',
    ),
  ];
  const embeddedClientCs =
    /"[^"\r\n]*(?:dona[\s_-]*betinha|pizzaria[\s_-]*dona[\s_-]*betinha)[^"\r\n]*"/gi;

  // Mesmos 3 valores-base canônicos de knownClientBrandColors (ver comentário acima) — exigindo
  // aspas ao redor (string literal de C#) para não disparar em comentário/XML doc (`<c>...</c>`,
  // sem aspas) citando o valor como exemplo do que NÃO fazer.
  const knownClientColorCs = new RegExp(
    String.raw`"[^"\r\n]*(?:${KNOWN_CLIENT_BRAND_COLORS.join('|')})[^"\r\n]*"`,
    'gi',
  );

  for (const absoluteFile of files) {
    const file = normalizedPath(root, absoluteFile);
    if (isCsharpException(file)) continue;
    const contents = readFileSync(absoluteFile, 'utf8');

    for (const pattern of tenantLiteralComparisonsCs) {
      for (const match of contents.matchAll(pattern)) {
        violations.push({
          file,
          line: lineNumber(contents, match.index),
          rule: 'ADR-013',
          message:
            'código condicional por tenant (comparação literal em C#). Transforme a diferença em configuração disponível para todos os tenants.',
        });
      }
    }

    for (const match of contents.matchAll(embeddedClientCs)) {
      violations.push({
        file,
        line: lineNumber(contents, match.index),
        rule: 'ADR-010',
        message:
          'marca de cliente embutida em C#. Identidade visual e textos devem vir da configuração de branding.',
      });
    }

    for (const match of contents.matchAll(knownClientColorCs)) {
      violations.push({
        file,
        line: lineNumber(contents, match.index),
        rule: 'ADR-010',
        message:
          'valor de marca do cliente-piloto (Pizzaria Dona Betinha) embutido em C#. Identidade visual é dado de tenant, nunca literal.',
      });
    }
  }

  return violations;
}

function unquotedIdentifier(identifier: string): string {
  return identifier.split('.').at(-1)!.replaceAll('"', '').toLowerCase();
}

function hasRls(sql: string, tableIdentifier: string): boolean {
  const table = unquotedIdentifier(tableIdentifier);
  const escaped = table.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const explicitEnable = new RegExp(
    `ALTER\\s+TABLE\\s+(?:"?[a-z_][\\w$]*"?\\.)?"?${escaped}"?\\s+ENABLE\\s+ROW\\s+LEVEL\\s+SECURITY`,
    'i',
  ).test(sql);
  const explicitForce = new RegExp(
    `ALTER\\s+TABLE\\s+(?:"?[a-z_][\\w$]*"?\\.)?"?${escaped}"?\\s+FORCE\\s+ROW\\s+LEVEL\\s+SECURITY`,
    'i',
  ).test(sql);
  const explicitPolicy = new RegExp(
    `CREATE\\s+POLICY\\s+tenant_isolation\\s+ON\\s+(?:"?[a-z_][\\w$]*"?\\.)?"?${escaped}"?[\\s\\S]*?USING\\s*\\([\\s\\S]*?tenant_id[\\s\\S]*?\\)[\\s\\S]*?WITH\\s+CHECK\\s*\\([\\s\\S]*?tenant_id`,
    'i',
  ).test(sql);

  const listedForDynamicPolicy = new RegExp(`['"]${escaped}['"]`, 'i').test(sql);
  const dynamicEnable = /EXECUTE\s+format\s*\(\s*'ALTER TABLE %I ENABLE ROW LEVEL SECURITY'/i.test(
    sql,
  );
  const dynamicForce = /EXECUTE\s+format\s*\(\s*'ALTER TABLE %I FORCE ROW LEVEL SECURITY'/i.test(
    sql,
  );
  const dynamicPolicy =
    /EXECUTE\s+format\s*\(\s*'CREATE POLICY tenant_isolation ON %I[\s\S]*?USING[\s\S]*?WITH CHECK/i.test(
      sql,
    );

  return (
    (explicitEnable && explicitForce && explicitPolicy) ||
    (listedForDynamicPolicy && dynamicEnable && dynamicForce && dynamicPolicy)
  );
}

// Caminho real das migrations desde a migração para .NET/EF Core (ADR-036/038/039) — a stack
// anterior gravava migration como arquivo .sql solto em packages/db/prisma/migrations, pasta que
// não existe mais neste repositório. A migration do EF Core é um arquivo .cs
// (Up(MigrationBuilder)/Down(MigrationBuilder)); tabela/coluna nascem de chamadas típadas
// (migrationBuilder.CreateTable(...)), e só RLS/domínios/funções — que o EF Core não expressa
// nativamente (Docs/Domain/13-Mapeamento-EFCore.md §1) — entram como SQL cru dentro de
// migrationBuilder.Sql("..."). Por isso a extração abaixo tem duas metades: uma que entende a
// API do MigrationBuilder (para achar "quais tabelas têm tenant_id") e outra que reaproveita
// hasRls() sobre o texto cru dos arquivos (que já sabe reconhecer o padrão
// "EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY'...)" usado pela migration
// EnableRowLevelSecurity, gerado em massa a partir de um array de nomes de tabela).
const EF_MIGRATIONS_ROOT = 'backend/src/Nexora.Infrastructure/Persistence/Migrations';

/** Tabelas (nome sem aspas) criadas por `migrationBuilder.CreateTable(name: "...", columns: ...)` que têm coluna `tenant_id`. */
function tablesWithTenantIdInEfMigration(content: string): Set<string> {
  const tables = new Set<string>();
  const createTablePattern = /migrationBuilder\.CreateTable\(\s*name:\s*"([a-z0-9_]+)"/gi;
  const matches = [...content.matchAll(createTablePattern)];

  for (let i = 0; i < matches.length; i++) {
    const match = matches[i]!;
    const blockStart = match.index! + match[0].length;
    const blockEnd = i + 1 < matches.length ? matches[i + 1]!.index! : content.length;
    const block = content.slice(blockStart, blockEnd);

    if (/\btenant_id\s*=\s*table\.Column\b/i.test(block)) {
      tables.add(match[1]!);
    }
  }

  return tables;
}

function migrationViolations(root: string): Violation[] {
  const migrationsRoot = resolve(root, EF_MIGRATIONS_ROOT);
  const files = filesBelow(migrationsRoot).filter(
    (file) =>
      file.endsWith('.cs') && !file.endsWith('.Designer.cs') && !file.endsWith('ModelSnapshot.cs'),
  );
  const violations: Violation[] = [];

  // hasRls() varre texto cru em busca do padrão de RLS (explícito OU o DO $$ ... EXECUTE
  // format(...) em massa) — concatenar todas as migrations dá a ele o mesmo texto que teria se
  // ainda fossem .sql soltos, sem precisar reescrever a função.
  const allMigrationsText = files.map((file) => readFileSync(file, 'utf8')).join('\n');

  const tenantIdTables = new Set<string>();
  for (const absoluteFile of files) {
    const contents = readFileSync(absoluteFile, 'utf8');
    for (const table of tablesWithTenantIdInEfMigration(contents)) {
      tenantIdTables.add(table);
    }
  }

  for (const table of tenantIdTables) {
    if (hasRls(allMigrationsText, table)) continue;

    violations.push({
      file: normalizedPath(root, files[0] ?? migrationsRoot),
      line: 1,
      rule: 'RLS',
      message: `tabela ${table} possui tenant_id sem ENABLE + FORCE ROW LEVEL SECURITY e POLICY tenant_isolation com USING/WITH CHECK (verificado em ${EF_MIGRATIONS_ROOT}).`,
    });
  }

  return [
    ...violations,
    ...legacySqlMigrationViolations(root),
    ...efCoreTypedMigrationViolations(files, root),
  ];
}

/**
 * ADR-019 para a API TÍPADA do EF Core (`migrationBuilder.DropColumn`/`RenameColumn`/`AlterColumn`)
 * — gap fechado pela US-007: `legacySqlMigrationViolations()` abaixo só entende texto SQL cru
 * (`DROP COLUMN`, `RENAME COLUMN`, `ALTER COLUMN ... TYPE`), então nunca disparava para estas
 * chamadas C#, que não contêm essas palavras como texto literal.
 *
 * Cobre, dentro do corpo de `Up()` (nunca `Down()` — `DropColumn` no rollback de uma migration que
 * fez `CreateTable`/`AddColumn` no próprio `Up()` é o comportamento normal, não uma remoção
 * destrutiva):
 *   - `DropColumn` direto: sempre reprovado, A MENOS que uma migration CRONOLOGICAMENTE ANTERIOR
 *     (arquivos ordenados pelo prefixo de timestamp do nome, `AAAAMMDDHHmmss_Nome.cs`) contenha o
 *     marcador `// governance:column-deprecated: tabela.coluna` — heurística escolhida para
 *     representar o padrão de expandir/contrair em duas etapas do ADR-019 (primeiro marca a coluna
 *     como obsoleta e para de escrever nela; só depois, em versão posterior, remove) sem exigir
 *     que o script entenda o histórico real de uso da coluna pela aplicação.
 *   - `RenameColumn` direto: sempre reprovado — o padrão correto é ADD (coluna nova) + backfill
 *     fora da migration + DROP (coluna antiga) em versão posterior, nunca RENAME.
 *   - `AlterColumn` com `nullable: false` sem `defaultValue`/`defaultValueSql`: mesmo risco do
 *     equivalente SQL (`SET NOT NULL` sem default) — a versão anterior do app pode gravar sem essa
 *     coluna, e o EF Core não tem como saber que já existe dado sem backfill.
 */
function efCoreTypedMigrationViolations(migrationFiles: string[], root: string): Violation[] {
  const files = [...migrationFiles].sort(); // prefixo de timestamp -> ordem cronológica real
  const violations: Violation[] = [];
  const deprecatedBeforeThisFile = new Set<string>();

  const argValue = (args: string, key: string): string | undefined =>
    args.match(new RegExp(String.raw`\b${key}\s*:\s*"([^"]*)"`, 'i'))?.[1];
  const hasDefaultValue = (args: string): boolean => /\bdefaultValue(?:Sql)?\s*:/i.test(args);
  const isNullableFalse = (args: string): boolean => /\bnullable\s*:\s*false\b/i.test(args);

  for (const absoluteFile of files) {
    const file = normalizedPath(root, absoluteFile);
    const contents = readFileSync(absoluteFile, 'utf8');

    const upStart = contents.search(/protected\s+override\s+void\s+Up\s*\(/);
    const downStart = contents.search(/protected\s+override\s+void\s+Down\s*\(/);
    const upBody = upStart < 0 ? '' : contents.slice(upStart, downStart >= 0 ? downStart : contents.length);

    for (const match of upBody.matchAll(/migrationBuilder\.DropColumn\(([\s\S]*?)\);/gi)) {
      const args = match[1] ?? '';
      const column = argValue(args, 'name');
      const table = argValue(args, 'table');
      const key = table && column ? `${table}.${column}`.toLowerCase() : undefined;
      if (key && deprecatedBeforeThisFile.has(key)) continue;

      violations.push({
        file,
        line: lineNumber(contents, upStart + (match.index ?? 0)),
        rule: 'ADR-019',
        message:
          'DropColumn direto em migration do EF Core (remoção de coluna em uma etapa). Marque a coluna obsoleta em uma migration ANTERIOR (comentário "// governance:column-deprecated: tabela.coluna") e só remova depois, em versão posterior — expandir e contrair.',
      });
    }

    for (const match of upBody.matchAll(/migrationBuilder\.RenameColumn\(([\s\S]*?)\);/gi)) {
      violations.push({
        file,
        line: lineNumber(contents, upStart + (match.index ?? 0)),
        rule: 'ADR-019',
        message:
          'RenameColumn direto em migration do EF Core (renomeação em uma etapa). Adicione a coluna nova, faça backfill fora da migration e remova a antiga em versão posterior.',
      });
    }

    for (const match of upBody.matchAll(/migrationBuilder\.AlterColumn(?:<[^>]*>)?\(([\s\S]*?)\);/gi)) {
      const args = match[1] ?? '';
      if (isNullableFalse(args) && !hasDefaultValue(args)) {
        violations.push({
          file,
          line: lineNumber(contents, upStart + (match.index ?? 0)),
          rule: 'ADR-019',
          message:
            'AlterColumn com nullable: false sem defaultValue/defaultValueSql. A expansão deve aceitar a versão anterior (dado já existente sem backfill).',
        });
      }
    }

    // Marcadores de depreciação valem para migrations FUTURAS (por isso só entram no conjunto
    // depois de processar este arquivo) — podem estar em qualquer lugar do arquivo, não só em Up().
    for (const match of contents.matchAll(/\/\/\s*governance:column-deprecated:\s*([a-z0-9_]+)\.([a-z0-9_]+)/gi)) {
      deprecatedBeforeThisFile.add(`${match[1]}.${match[2]}`.toLowerCase());
    }
  }

  return violations;
}

/**
 * Checagens de compatibilidade de migration (ADR-019) que pressupõem SQL cru (`DROP COLUMN`,
 * `RENAME COLUMN`, `ALTER COLUMN ... TYPE` como texto) — mantidas apontando para o caminho antigo
 * por compatibilidade (retornam vazio hoje, pasta não existe). A API TÍPADA do MigrationBuilder
 * (`migrationBuilder.DropColumn`/`RenameColumn`/`AlterColumn`, sem essas palavras como texto) é
 * coberta por `efCoreTypedMigrationViolations()` acima, não por esta função — ela só protege
 * contra alguém colar SQL bruto perigoso dentro de um `migrationBuilder.Sql("...")` manual.
 */
function legacySqlMigrationViolations(root: string): Violation[] {
  const migrationsRoot = resolve(root, 'packages/db/prisma/migrations');
  const files = filesBelow(migrationsRoot).filter((file) => file.endsWith('.sql'));
  const violations: Violation[] = [];

  const destructivePatterns: Array<[RegExp, string]> = [
    [/\bDROP\s+COLUMN\b/i, 'remoção de coluna em uma etapa'],
    [/\bRENAME\s+COLUMN\b/i, 'renomeação direta de coluna'],
    [/\bALTER\s+COLUMN\b[\s\S]*?\bTYPE\b/i, 'alteração direta de tipo'],
    [/\bDROP\s+VALUE\b/i, 'remoção direta de valor de enum'],
  ];

  for (const absoluteFile of files) {
    const file = normalizedPath(root, absoluteFile);
    const sql = readFileSync(absoluteFile, 'utf8');
    const createdTables = new Set(
      [...sql.matchAll(/CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?([^\s(]+)/gi)].map((match) =>
        unquotedIdentifier(match[1]!),
      ),
    );

    for (const statementMatch of sql.matchAll(/[^;]+(?:;|$)/g)) {
      const rawStatement = statementMatch[0];
      const foundationBootstrap =
        file.includes('/migrations/20260731') &&
        /--\s*governance:foundation-bootstrap\b/i.test(rawStatement);
      const statement = rawStatement.replace(/--[^\r\n]*/g, (comment) =>
        ' '.repeat(comment.length),
      );
      if (!statement.trim()) continue;
      const statementOffset = statementMatch.index;

      for (const [pattern, description] of destructivePatterns) {
        const match = statement.match(pattern);
        if (match?.index !== undefined && !foundationBootstrap) {
          violations.push({
            file,
            line: lineNumber(sql, statementOffset + match.index),
            rule: 'ADR-019',
            message: `${description}. Use expansão e contração em duas etapas, mantendo compatibilidade com a versão anterior.`,
          });
        }
      }

      const setNotNull = statement.match(/\bALTER\s+COLUMN\b[\s\S]*?\bSET\s+NOT\s+NULL\b/i);
      if (setNotNull?.index !== undefined) {
        violations.push({
          file,
          line: lineNumber(sql, statementOffset + setNotNull.index),
          rule: 'ADR-019',
          message:
            'NOT NULL aplicado diretamente. Adicione default/coluna compatível, faça backfill fora da migration e contraia em uma versão posterior.',
        });
      }

      const indexStatement = statement.match(
        /\bCREATE\s+(?:UNIQUE\s+)?INDEX\s+[^\s]+\s+ON\s+(?:ONLY\s+)?([^\s(]+)/i,
      );
      const indexTarget = indexStatement?.[1];
      const indexesExistingTable =
        indexTarget && !createdTables.has(unquotedIdentifier(indexTarget));
      if (indexesExistingTable && !/\bCONCURRENTLY\b/i.test(statement) && !foundationBootstrap) {
        violations.push({
          file,
          line: lineNumber(sql, statementOffset + (indexStatement?.index ?? 0)),
          rule: 'ADR-019',
          message:
            'índice novo sem CONCURRENTLY; a criação não pode bloquear uma loja em operação.',
        });
      }

      const addColumn = statement.match(/\bADD\s+COLUMN\b[\s\S]*?\bNOT\s+NULL\b/i);
      if (addColumn?.index !== undefined && !/\bDEFAULT\b/i.test(statement)) {
        violations.push({
          file,
          line: lineNumber(sql, statementOffset + addColumn.index),
          rule: 'ADR-019',
          message: 'coluna NOT NULL sem DEFAULT. A expansão deve aceitar a versão anterior.',
        });
      }

      const constraintStatement = statement.match(
        /\bALTER\s+TABLE\s+(?:ONLY\s+)?([^\s]+)[\s\S]*?\bADD\s+CONSTRAINT\b/i,
      );
      const constraintTarget = constraintStatement?.[1];
      const constrainsExistingTable =
        constraintTarget && !createdTables.has(unquotedIdentifier(constraintTarget));
      if (constrainsExistingTable && !/\bNOT\s+VALID\b/i.test(statement)) {
        violations.push({
          file,
          line: lineNumber(sql, statementOffset + (constraintStatement?.index ?? 0)),
          rule: 'ADR-019',
          message: 'constraint adicionada sem NOT VALID; valide em uma etapa posterior.',
        });
      }
    }

    // Checagem de RLS por CREATE TABLE literal removida daqui — vive agora em
    // migrationViolations()/tablesWithTenantIdInEfMigration(), que entende migrations do EF Core
    // (ver comentário na definição de EF_MIGRATIONS_ROOT). Este laço é só compatibilidade com um
    // .sql solto manual, caso algum apareça de novo.
  }

  return violations;
}

function main(): void {
  const root = resolve(argument('--root') ?? process.cwd());
  const violations = [
    ...sourceViolations(root),
    ...csharpViolations(root),
    ...migrationViolations(root),
  ];

  if (violations.length === 0) {
    console.log('Governança: ADR-010, ADR-013, ADR-019 e RLS validados.');
    return;
  }

  for (const violation of violations) {
    console.error(
      `::error file=${violation.file},line=${violation.line},title=${violation.rule}::${violation.file}:${violation.line} — ${violation.rule}: ${violation.message}`,
    );
  }
  process.exitCode = 1;
}

main();
