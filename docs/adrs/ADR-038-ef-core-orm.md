# ADR-038 · Entity Framework Core como ORM, mantendo PostgreSQL e RLS

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 01/08/2026 |
| **Decisores** | Tech Lead |
| **Substitui** | [ADR-005](ADR-005-prisma-orm.md) |
| **Relacionados** | ADR-004, ADR-019, ADR-036, ADR-037 |
| **Requisitos afetados** | RNF-MAN-04, RNF-MAN-05, RNF-IMP-01 |

---

## Contexto

O ADR-005 escolheu Prisma pela confiabilidade de `prisma migrate deploy` em um parque distribuído de servidores locais e pela tipagem forte derivada do schema. Nenhuma dessas exigências muda com a troca de stack (ADR-036): migrations ainda precisam ser reprodutíveis em N lojas sem intervenção manual, e a camada analítica (métricas, CMV, percentis) ainda precisa de SQL que nenhum ORM expressa bem.

A decisão do usuário do produto foi explícita: **manter PostgreSQL com Row Level Security** (ADR-004) em vez de migrar para SQL Server como no `seminarioteologico` — a garantia de isolamento "fail-closed" do RLS é considerada mais importante que replicar a stack de referência 1:1. Isso significa que o ORM escolhido precisa ter suporte maduro a Postgres e um caminho claro para injetar `SET LOCAL app.tenant_id` por transação.

## Forças em jogo

| Força | Descrição |
|---|---|
| Confiabilidade de migration em parque distribuído | Repete a força original do ADR-005 — não pode exigir intervenção manual por loja |
| Compatibilidade com RLS do PostgreSQL | O ORM precisa permitir interceptar a conexão/transação para setar o contexto de tenant |
| Alinhamento com o padrão de referência | `seminarioteologico` usa EF Core (contra SQL Server) — reaproveitar o ORM mantém o padrão de camadas (`AppDbContext`, `Configurations/`, `Migrations/`) mesmo trocando o provider para Npgsql |
| Consultas analíticas | Métricas, percentis e agregações pesadas continuam exigindo SQL fora do ORM |

## Decisão

**Entity Framework Core como ORM principal, com o provider Npgsql para PostgreSQL**, mantendo RLS como mecanismo de isolamento (ADR-004). SQL puro (`FromSqlRaw`/`ExecuteSqlRaw` ou Dapper pontual) para a camada analítica, no mesmo espírito do `$queryRaw` do ADR-005 original.

## Detalhamento

### Divisão clara

| Uso | Ferramenta |
|---|---|
| CRUD de domínio, relacionamentos, transações | `DbContext` do EF Core (LINQ) |
| Consultas analíticas, percentis, janelas, agregações | `FromSqlRaw` tipado ou Dapper, isolado em `Nexora.Infrastructure/Persistence/Analytics` |
| Migrations | `dotnet ef migrations add` / `dotnet ef database update` |
| Contexto de RLS | Interceptor de conexão do EF Core |

### Integração com RLS via interceptor

EF Core, assim como Prisma, usa pool de conexões — o contexto de tenant precisa ser local à transação, senão vaza entre requisições. A mecânica muda de um middleware que envolve o client (Prisma) para um `DbConnectionInterceptor`/`SaveChangesInterceptor` registrado no `DbContextOptionsBuilder`:

```csharp
public sealed class TenantConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ICurrentTenantContext _tenantContext;

    public TenantConnectionInterceptor(ICurrentTenantContext tenantContext)
        => _tenantContext = tenantContext;

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (_tenantContext.TenantId is { } tenantId)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT set_config('app.tenant_id', @tenantId, false)";
            cmd.Parameters.Add(new NpgsqlParameter("tenantId", tenantId.ToString()));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
```

Registrado uma vez, no `Program.cs` de cada API:

```csharp
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>());
});
```

Toda operação de negócio passa por essa injeção — uso de uma conexão que não passou pelo interceptor é proibido e verificado por `Nexora.ArchitectureTests` (equivalente ao `withTenant` obrigatório e ao lint do ADR-005 original).

### Migrations no edge

```bash
# durante a atualização do servidor local
dotnet ef database update --project Nexora.Infrastructure --startup-project Nexora.Api.Edge
```

Regras complementares em ADR-019 (compatibilidade do parque instalado) continuam válidas — a mecânica de "aplicar apenas o pendente, sem prompt, sem reset" é obtida com `dotnet ef database update`, equivalente a `prisma migrate deploy`.

### Estrutura do `DbContext`

Seguindo o padrão do `seminarioteologico`:

```
Nexora.Infrastructure/
└── Persistence/
    ├── AppDbContext.cs
    ├── Configurations/        uma classe IEntityTypeConfiguration<T> por entidade
    ├── Interceptors/          TenantConnectionInterceptor, AuditSaveChangesInterceptor
    ├── Migrations/            geradas por dotnet ef
    └── Analytics/             FromSqlRaw / Dapper para métricas e CMV
```

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Manter Prisma via um processo Node auxiliar só para migrations | Reaproveitaria ferramental já validado | Introduz uma segunda linguagem/runtime só para persistência — contraria o próprio motivo da migração para .NET | Duplica complexidade operacional sem ganho líquido |
| Dapper puro, sem EF Core | Controle total sobre SQL, sem overhead de tracking | Sem migrations geradas, sem tipagem de schema, mais código boilerplate para CRUD | Perde o ganho de produtividade e de schema-como-documentação que motivou o ADR-005 original |
| EF Core + SQL Server (replicar `seminarioteologico` 1:1) | Reaproveita a stack de referência sem adaptação | Perde a garantia de isolamento fail-closed do RLS nativo do Postgres (ADR-004) | Decisão explícita do usuário: manter PostgreSQL + RLS |

## Consequências

**Positivas**

- Migrations reprodutíveis via `dotnet ef database update`, mesma confiabilidade exigida pelo ADR-005 original
- Tipagem forte: mapeamento entidade↔tabela expresso em `IEntityTypeConfiguration<T>`, divergência falha no build
- Reaproveita o padrão de `AppDbContext`/`Configurations`/`Migrations` já validado no `seminarioteologico`
- RLS continua garantido pelo banco, não pela aplicação (ADR-004 permanece intacto)

**Negativas**

- Interceptor de RLS é peça de infraestrutura adicional a manter (equivalente ao `withTenant` do ADR-005 original, só que via `DbConnectionInterceptor` em vez de wrapper de client)
- Consultas analíticas saem do ORM (o que já era considerado desejável no ADR-005 original, não um problema)
- EF Core com Npgsql é uma combinação menos usada no ecossistema .NET que EF Core + SQL Server (o par usado no `seminarioteologico`) — exige atenção a particularidades do provider (ex.: mapeamento de `TIMESTAMPTZ`, enums nativos do Postgres)

**Mitigações**

- `TenantConnectionInterceptor` é a única porta de entrada; `Nexora.ArchitectureTests` bloqueia `AppDbContext` instanciado fora do DI configurado
- Migrations de **dados** rodam como job separado, nunca dentro de migration de schema (ADR-019, inalterado)
- Consultas analíticas ficam isoladas em `Persistence/Analytics`, com testes próprios

## Como validar

- Nenhuma instância de `AppDbContext` é criada fora da configuração de DI com o interceptor registrado
- `dotnet ef database update` aplicado sobre dump real (anonimizado) de produção em CI antes de liberar release do parque
- Teste de RLS (ADR-004) passa também nas consultas via `FromSqlRaw`
- `Nexora.IntegrationTests` roda contra PostgreSQL real via Testcontainers — nunca contra o provider InMemory do EF Core, que não teria como validar RLS

## Revisitar quando

- O provider Npgsql apresentar lacuna de funcionalidade que bloqueie um requisito (candidato a monitorar: suporte a novos recursos do PostgreSQL 16+)
- Volumetria de consultas analíticas justificar um data warehouse dedicado (ver ADR-012, decisão adiada)
