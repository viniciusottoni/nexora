# 13 — Mapeamento EF Core

| | |
|---|---|
| **ADRs** | [038](../ADRs/ADR-038-ef-core-orm.md), [036](../ADRs/ADR-036-dotnet-solution-clean-architecture.md), [004](../ADRs/ADR-004-postgresql-rls-multitenancy.md), [019](../ADRs/ADR-019-migrations-e-compatibilidade.md) |
| **Substitui** | [13-Mapeamento-Prisma.md](13-Mapeamento-Prisma.md) (ver ADR-038) |

> O Entity Framework Core (provider Npgsql) é o ORM principal, mas **o DDL destes documentos continua sendo a fonte da verdade** — ele não muda com a troca de ORM (ADR-038). Alguns recursos usados aqui — RLS, particionamento, triggers, domínios de tipo, funções — não nascem de `dotnet ef migrations add` e vivem editados à mão dentro da migration gerada.

---

## 1. O que o EF Core expressa e o que não expressa

| Recurso | EF Core (Npgsql) | Onde vive |
|---|---|---|
| Tabelas, colunas, relações | ✅ | `IEntityTypeConfiguration<T>` |
| Enums | ✅ (nativo do Postgres via `HasPostgresEnum` + `HasConversion`) | `Configurations/` + `OnModelCreating` |
| Índices simples e únicos | ✅ | `IEntityTypeConfiguration<T>` (`HasIndex`) |
| **Índices parciais** (`WHERE` simples) | ✅ (`.HasFilter("sql")`) | `IEntityTypeConfiguration<T>`, filtro complexo revisado à mão na migration |
| **Row Level Security** | ❌ | Migration editada à mão (`migrationBuilder.Sql(...)`) |
| **Particionamento** | ❌ | Migration editada à mão |
| **Colunas geradas** (`GENERATED ALWAYS ... STORED`) | ✅ (`.HasComputedColumnSql(sql, stored: true)`) | `IEntityTypeConfiguration<T>`, expressão SQL revisada à mão |
| **Domínios de tipo** (`money_amount`, `qty_amount`...) | Parcial — propriedade mapeada via `.HasColumnType("money_amount")`, mas o `CREATE DOMAIN` em si não é gerado pelo EF Core | Migration inicial editada à mão (documento 00) |
| **Triggers e funções** | ❌ | Migration editada à mão |
| **Views** | Parcial — leitura via `ToView()` em entidade *keyless*; criação da view não é gerada | Migration editada à mão |
| **Constraints CHECK** | ✅ (`.ToTable(b => b.HasCheckConstraint(name, sql))`, EF Core 7+) | `IEntityTypeConfiguration<T>` |

Fluxo prático:

```bash
dotnet ef migrations add AddOrder --project Nexora.Infrastructure --startup-project Nexora.Api.Edge
# editar o .cs gerado em Migrations/: acrescentar RLS, particionamento, triggers, CREATE DOMAIN
dotnet ef database update --project Nexora.Infrastructure --startup-project Nexora.Api.Edge
```

Diferença relevante em relação ao Prisma: a migration gerada é um **arquivo C#** (`Up(MigrationBuilder)` / `Down(MigrationBuilder)`), não um `.sql` solto — o SQL manual entra como `migrationBuilder.Sql("...")` dentro do método `Up`, na mesma migration ou em uma migration vazia dedicada (`dotnet ef migrations add AddOrderRls --no-build` seguido de edição manual).

---

## 2. Configuração

```csharp
// Nexora.Infrastructure/Persistence/AppDbContext.cs
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemFraction> OrderItemFractions => Set<OrderItemFraction>();
    // ... um DbSet por agregado — ver documentos 01-12

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasPostgresExtension("btree_gist");

        modelBuilder.HasPostgresEnum<OrderItemStatus>();
        modelBuilder.HasPostgresEnum<OrderStatus>();
        modelBuilder.HasPostgresEnum<Channel>();

        // aplica todas as classes IEntityTypeConfiguration<T> do assembly de uma vez —
        // equivalente a um schema.prisma único, só que uma classe por entidade
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

Registro em `Program.cs`, com nomenclatura `snake_case` (pacote `EFCore.NamingConventions`, já que o padrão do time em C# é `PascalCase` e o do banco é `snake_case` — documento 00):

```csharp
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsql =>
            npgsql.MapEnum<OrderItemStatus>("order_item_status"))
        .UseSnakeCaseNamingConvention()
        .AddInterceptors(
            sp.GetRequiredService<TenantConnectionInterceptor>(),
            sp.GetRequiredService<AuditSaveChangesInterceptor>());
});
```

---

## 3. Trecho representativo — `IEntityTypeConfiguration<T>` por entidade

Ao contrário do `schema.prisma` (um arquivo único e declarativo), o EF Core espera **uma classe por entidade**, em `Nexora.Infrastructure/Persistence/Configurations/`. É Fluent API — nunca solta dentro de `OnModelCreating`.

```csharp
// Nexora.Infrastructure/Persistence/Configurations/TenantConfiguration.cs
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenant");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever(); // UUIDv7 na origem — ADR-016

        builder.Property(t => t.Slug).HasColumnName("slug").HasColumnType("slug").IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").HasDefaultValue(TenantStatus.Trial);
        builder.Property(t => t.Timezone).HasColumnName("timezone").HasDefaultValue("America/Sao_Paulo");
        builder.Property(t => t.Currency).HasColumnName("currency").HasColumnType("char(3)").HasDefaultValue("BRL");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(t => t.Slug).IsUnique().HasDatabaseName("uq_tenant_slug");

        builder.HasOne(t => t.Config).WithOne(c => c.Tenant).HasForeignKey<TenantConfig>(c => c.TenantId);
        builder.HasMany(t => t.Stores).WithOne().HasForeignKey(s => s.TenantId);
    }
}
```

```csharp
// Nexora.Infrastructure/Persistence/Configurations/OrderConfiguration.cs
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("order"); // "order" é palavra reservada — aspas resolvidas pelo provider Npgsql automaticamente

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.TenantId).HasColumnName("tenant_id");
        builder.Property(o => o.StoreId).HasColumnName("store_id");
        builder.Property(o => o.SessionId).HasColumnName("session_id");
        builder.Property(o => o.Channel).HasColumnName("channel");
        builder.Property(o => o.ShortCode).HasColumnName("short_code").HasMaxLength(8);
        builder.Property(o => o.BusinessDay).HasColumnName("business_day").HasColumnType("date");
        builder.Property(o => o.Status).HasColumnName("status").HasDefaultValue(OrderStatus.Draft);

        // carimbos de tempo — origem da métrica
        builder.Property(o => o.PlacedAt).HasColumnName("placed_at").HasColumnType("timestamptz");
        builder.Property(o => o.FirstFiredAt).HasColumnName("first_fired_at").HasColumnType("timestamptz");
        builder.Property(o => o.ReadyAt).HasColumnName("ready_at").HasColumnType("timestamptz");
        builder.Property(o => o.DispatchedAt).HasColumnName("dispatched_at").HasColumnType("timestamptz");
        builder.Property(o => o.ServedAt).HasColumnName("served_at").HasColumnType("timestamptz");
        builder.Property(o => o.PromisedAt).HasColumnName("promised_at").HasColumnType("timestamptz");

        // dinheiro — domínio money_amount, tipo C# decimal (ADR-017)
        builder.Property(o => o.Subtotal).HasColumnName("subtotal").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(o => o.DiscountAmount).HasColumnName("discount_amount").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(o => o.DeliveryFee).HasColumnName("delivery_fee").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(o => o.Total).HasColumnName("total").HasColumnType("money_amount").HasDefaultValue(0m);

        builder.HasOne(o => o.Session).WithMany().HasForeignKey(o => o.SessionId);
        builder.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.StoreId, o.BusinessDay, o.ShortCode })
            .IsUnique()
            .HasDatabaseName("uq_order_short_code");

        builder.HasIndex(o => new { o.TenantId, o.BusinessDay, o.Channel });

        builder.HasIndex(o => new { o.TenantId, o.PlacedAt })
            .HasDatabaseName("idx_order_placed_desc")
            .IsDescending(false, true);
    }
}
```

```csharp
// Nexora.Infrastructure/Persistence/Configurations/OrderItemConfiguration.cs
internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_item");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.Property(i => i.OrderId).HasColumnName("order_id");
        builder.Property(i => i.VariantId).HasColumnName("variant_id");
        builder.Property(i => i.StationId).HasColumnName("station_id");

        builder.Property(i => i.Quantity).HasColumnName("quantity").HasColumnType("smallint").HasDefaultValue((short)1);
        builder.Property(i => i.UnitPrice).HasColumnName("unit_price").HasColumnType("money_amount");
        builder.Property(i => i.TotalPrice).HasColumnName("total_price").HasColumnType("money_amount");
        builder.Property(i => i.UnitCost).HasColumnName("unit_cost").HasColumnType("money_amount");

        builder.Property(i => i.Status).HasColumnName("status").HasDefaultValue(OrderItemStatus.Queued);

        builder.Property(i => i.PlacedAt).HasColumnName("placed_at").HasColumnType("timestamptz");
        builder.Property(i => i.FireAt).HasColumnName("fire_at").HasColumnType("timestamptz");
        builder.Property(i => i.FiredAt).HasColumnName("fired_at").HasColumnType("timestamptz");
        builder.Property(i => i.OvenInAt).HasColumnName("oven_in_at").HasColumnType("timestamptz");
        builder.Property(i => i.OvenOutAt).HasColumnName("oven_out_at").HasColumnType("timestamptz");
        builder.Property(i => i.ReadyAt).HasColumnName("ready_at").HasColumnType("timestamptz");
        builder.Property(i => i.ServedAt).HasColumnName("served_at").HasColumnType("timestamptz");

        builder.HasOne(i => i.Variant).WithMany().HasForeignKey(i => i.VariantId);
        builder.HasMany(i => i.Fractions).WithOne(f => f.Item).HasForeignKey(f => f.OrderItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.Modifiers).WithOne().HasForeignKey("order_item_id").OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.TenantId, i.StationId, i.Status, i.PlacedAt })
            .HasDatabaseName("idx_item_queue")
            .HasFilter("status IN ('QUEUED','FIRED','IN_OVEN','OUT_OF_OVEN')"); // índice parcial — ADR-038 §1

        builder.HasIndex(i => i.OrderId);
    }
}
```

```csharp
// Nexora.Infrastructure/Persistence/Configurations/OrderItemFractionConfiguration.cs
internal sealed class OrderItemFractionConfiguration : IEntityTypeConfiguration<OrderItemFraction>
{
    public void Configure(EntityTypeBuilder<OrderItemFraction> builder)
    {
        builder.ToTable("order_item_fraction");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.TenantId).HasColumnName("tenant_id");
        builder.Property(f => f.OrderItemId).HasColumnName("order_item_id");
        builder.Property(f => f.VariantId).HasColumnName("variant_id");
        builder.Property(f => f.Weight).HasColumnName("weight").HasColumnType("fraction_weight"); // NUMERIC(5,4)
        builder.Property(f => f.UnitPrice).HasColumnName("unit_price").HasColumnType("money_amount");
        builder.Property(f => f.SortOrder).HasColumnName("sort_order").HasColumnType("smallint").HasDefaultValue((short)0);

        builder.HasIndex(f => f.OrderItemId);
    }
}
```

Enums seguem os do documento 00, mapeados como enum nativo do Postgres (`Npgsql.MapEnum`), não como `VARCHAR` com `CHECK`:

```csharp
public enum OrderItemStatus
{
    Queued, Fired, InOven, OutOfOven, Ready, Served, Cancelled
}

// convenção snake_case aplicada também aos valores do enum nativo, via UseSnakeCaseNamingConvention()
```

---

## 4. Integração com RLS

Toda operação de negócio passa pelo `TenantConnectionInterceptor` documentado em detalhe no [ADR-038](../ADRs/ADR-038-ef-core-orm.md#integração-com-rls-via-interceptor). Diferente do wrapper explícito `withTenant()` do Prisma, aqui a injeção do contexto é implícita: o `TenantBehavior` do pipeline MediatR (ADR-037) resolve o tenant do JWT e popula `ICurrentTenantContext` (escopo por requisição); o interceptor lê esse valor no evento `ConnectionOpenedAsync` e executa `SET LOCAL app.tenant_id`.

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

Acesso de plataforma (equivalente ao `withPlatform()` do Prisma) usa uma segunda connection string, conectada com o papel `platform_admin` (`BYPASSRLS`, documento 10), e passa sempre por auditoria obrigatória via `AuditSaveChangesInterceptor` — nunca por `AppDbContext` fora de DI:

```csharp
builder.Services.AddDbContext<PlatformDbContext>((sp, options) =>
{
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("PlatformConnection"))
        .AddInterceptors(sp.GetRequiredService<PlatformAuditSaveChangesInterceptor>());
});
```

`Nexora.ArchitectureTests` bloqueia qualquer `AppDbContext`/`PlatformDbContext` instanciado fora da configuração de DI — o equivalente ao lint do ADR-005 original, só que verificado por teste de arquitetura (NetArchTest.Rules), não por regra de ESLint.

---

## 5. Dinheiro: `decimal` em toda a fronteira

```csharp
// Nexora.Domain/Common/Money.cs
public readonly record struct Money
{
    private readonly decimal _value;

    private Money(decimal value) => _value = value;

    // arredondamento half-up (ADR-017) — MidpointRounding.AwayFromZero é o equivalente C# de ROUND_HALF_UP
    public static Money From(decimal value) =>
        new(Math.Round(value, 2, MidpointRounding.AwayFromZero));

    public static Money Zero => new(0m);

    public override string ToString() => _value.ToString("F2", CultureInfo.InvariantCulture);
}
```

Serialização de API — `decimal` do C# vira `number` em JSON por padrão no `System.Text.Json`, o que reintroduziria exatamente o problema que o ADR-017 proíbe. Um `JsonConverter` dedicado força string em toda fronteira HTTP:

```csharp
// Nexora.Contracts/Serialization/MoneyJsonConverter.cs
public sealed class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => Money.From(decimal.Parse(reader.GetString()!, CultureInfo.InvariantCulture));

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString()); // nunca number — ADR-017
}
```

O tipo de coluna `money_amount` (domínio Postgres `NUMERIC(12,2)`, documento 00) é mapeado como `decimal` com `.HasColumnType("money_amount")` nas `Configurations/` (§3) — o EF Core não precisa conhecer o domínio, só o nome do tipo de coluna, igual ao Prisma original tratava como `Decimal`.

> **Nunca** serializar dinheiro como `number` em JSON. O `MoneyJsonConverter` é obrigatório em `Nexora.Contracts` — sua ausência em um DTO novo é o mesmo defeito que o ADR-017 já proibia no stack anterior.

---

## 6. Migrations que exigem SQL manual

```
Nexora.Infrastructure/Persistence/Migrations/
├── 20260801000000_Init.cs                    tabelas, colunas, FKs, enums, índices simples/parciais/CHECK — gerado por dotnet ef
├── 20260801000100_RlsPolicies.cs              RLS em todas as tabelas — Sql() manual
├── 20260801000200_TypeDomains.cs               CREATE DOMAIN (money_amount, qty_amount, percent_amount, fraction_weight) — Sql() manual
├── 20260801000300_EventPartitioning.cs         particionamento de domain_event — Sql() manual
├── 20260801000400_Functions.cs                 business_day, variant_cost, next_short_code — Sql() manual
├── 20260801000500_Triggers.cs                  updated_at, catalog_version, stock guard — Sql() manual
└── 20260801000600_Views.cs                     v_kds_queue, v_table_map, ... — Sql() manual
```

Cada migration editada à mão usa `migrationBuilder.Sql("...")` dentro de `Up()`, com o `Down()` correspondente (`DROP POLICY`, `DROP DOMAIN`, etc.) — o EF Core não gera o `Down` de SQL cru automaticamente, diferente das colunas/tabelas que ele controla.

Regras de compatibilidade do parque em [ADR-019](../ADRs/ADR-019-migrations-e-compatibilidade.md) continuam válidas — `dotnet ef database update` aplica apenas o pendente, sem prompt, sem reset, o mesmo comportamento exigido de `prisma migrate deploy` no ADR-005 original.

---

## 7. Verificação no CI

```yaml
- name: Schema em dia
  run: dotnet ef migrations has-pending-model-changes
                            --project Nexora.Infrastructure
                            --startup-project Nexora.Api.Edge   # falha se o modelo divergir das migrations commitadas

- name: RLS cobre todas as tabelas
  run: psql $DATABASE_URL -f scripts/check-rls-coverage.sql

- name: Migration contra dump de produção
  run: ./scripts/test-migration-against-prod-dump.sh
```

A primeira verificação é o equivalente funcional do `prisma migrate diff --exit-code` do stack anterior, usando o comando `dotnet ef migrations has-pending-model-changes` (EF Core 8+). A segunda usa a consulta do documento 10, §7: tabela nova com `tenant_id` e sem RLS **falha o build** — inalterada pela troca de ORM.

---

*Substitui [13-Mapeamento-Prisma.md](13-Mapeamento-Prisma.md). Ver [ADR-038](../ADRs/ADR-038-ef-core-orm.md).*
