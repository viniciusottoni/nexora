# ADR-004 · PostgreSQL com Row Level Security para multi-tenancy

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO |
| **Relacionados** | ADR-005, ADR-013, ADR-023 |
| **Requisitos afetados** | RF-PLT-01, RN-015, RNF-SEG-07, RNF-SEG-08 |

---

## Contexto

A diretriz de produto é categórica (RN-015):

> Nenhum dado de um estabelecimento é acessível a outro, em nenhuma circunstância.

Em um produto multi-cliente, vazamento entre tenants é o incidente de maior gravidade possível — destrói a confiança comercial de forma irreversível e tem implicações legais sob a LGPD.

O modo clássico de falhar é banal: um desenvolvedor escreve uma query nova e esquece o `WHERE tenant_id = ?`. O código passa em revisão, passa nos testes funcionais (que rodam com um único tenant) e vaza em produção. **Nenhuma quantidade de disciplina elimina esse risco** — ele precisa ser eliminado por construção.

## Forças em jogo

| Força | Descrição |
|---|---|
| Isolamento | Erro humano não pode causar vazamento |
| Custo operacional | Migrations, backup e monitoramento não podem crescer linearmente com o número de clientes |
| Simplicidade de deploy | Fase 5 pressupõe implantar novo cliente em minutos |
| Consultas de plataforma | A Replay precisa de visão agregada do parque |

## Decisão

**Banco único, schema compartilhado, `tenant_id` em toda tabela de negócio, com isolamento imposto pelo PostgreSQL Row Level Security.**

O isolamento **não** é responsabilidade da aplicação. É do banco.

## Detalhamento

### Habilitação

```sql
ALTER TABLE "order" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "order" FORCE  ROW LEVEL SECURITY;   -- vale inclusive para o dono da tabela

CREATE POLICY tenant_isolation ON "order"
  USING      (tenant_id = current_setting('app.tenant_id', true)::uuid)
  WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid);
```

`USING` filtra leitura; `WITH CHECK` impede gravar linha de outro tenant. Ambos são obrigatórios.

### Definição do contexto

Interceptor de conexão do EF Core, antes de qualquer query, dentro da mesma conexão/transação (ver ADR-038 para a implementação completa do `TenantConnectionInterceptor`):

```csharp
await using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT set_config('app.tenant_id', @tenantId, true)";
cmd.Parameters.Add(new NpgsqlParameter("tenantId", tenantId.ToString()));
await cmd.ExecuteNonQueryAsync(cancellationToken);
```

O terceiro parâmetro `true` de `set_config` torna a configuração **local à transação** — essencial com pool de conexões, senão o contexto vaza entre requisições. O `tenantId` chega ao interceptor via `ICurrentTenantContext`, resolvido a partir do claim `tid` do JWT (ver ADR-037).

### Comportamento sem contexto

Se `app.tenant_id` não estiver definido, `current_setting(..., true)` retorna `NULL`, a comparação resulta em `NULL` e **nenhuma linha é retornada**. Falha fechada, que é o comportamento seguro.

### Papel de plataforma

```sql
CREATE ROLE platform_admin BYPASSRLS;
```

Usado **exclusivamente** nas rotas de `PlatformModule`, sempre com registro em `audit_log` e emissão de `EVT-074 support.access.granted`, visível ao cliente.

### Tabelas fora do RLS

`unit_of_measure`, `tenant`, `migration_history` e demais tabelas globais não possuem `tenant_id` e não recebem política.

## Alternativas consideradas

| Alternativa | Isolamento | Custo operacional | Por que foi descartada |
|---|---|---|---|
| Banco por tenant | Máximo | Alto | N migrations, N backups, N monitoramentos; inviabiliza a Fase 5 e o suporte em escala |
| Schema por tenant | Alto | Médio | Migrations ainda se multiplicam; pool de conexões e EF Core ficam complicados |
| Filtro apenas na aplicação | Frágil | Baixo | Um `WHERE` esquecido vaza dados; risco permanente e inaceitável |
| Interceptor do EF Core que injeta o filtro via LINQ | Médio | Baixo | Não cobre `FromSqlRaw`/Dapper, que usaremos em toda a camada analítica |

## Consequências

**Positivas**

- Vazamento por esquecimento torna-se **impossível por construção**
- Uma migration serve todo o parque
- Custo por cliente adicional é marginal
- `FromSqlRaw`/Dapper também ficam protegidos — diferencial relevante frente a um filtro aplicado só na camada LINQ do ORM

**Negativas**

- Toda conexão precisa definir o contexto — esquecer significa "nenhum dado", que é confuso ao depurar
- Rotas de plataforma exigem `BYPASSRLS`, criando uma superfície privilegiada a auditar
- Consultas cross-tenant só existem na camada de plataforma
- Pequeno custo de avaliação da política em cada query (desprezível na volumetria atual)

**Mitigações**

- `TenantConnectionInterceptor` obrigatório, registrado no DI de `Api.Edge` e `Api.Cloud`; ausência de contexto lança erro explícito em desenvolvimento
- Teste automatizado de isolamento **bloqueante em todo PR** (doc. 10, §8.1), varrendo todas as tabelas de negócio
- Toda rota com `BYPASSRLS` exige atributo explícito `[PlatformScope]` e gera auditoria

## Como validar

```csharp
[Theory]
[MemberData(nameof(AllBusinessTables))]
public async Task Tabela_Nao_Vaza_Entre_Tenants(string table)
{
    var tenantA = await CreateTenantWithDataAsync();
    await CreateTenantWithDataAsync();                       // tenant B

    await WithTenantAsync(tenantA.Id, async () =>
    {
        var rows = await _dbContext.Database
            .SqlQueryRaw<TenantRow>($"SELECT tenant_id FROM {table}")
            .ToListAsync();

        rows.Should().OnlyContain(r => r.TenantId == tenantA.Id);
    });
}
```

Complementos: acesso direto por ID de outro tenant retorna 404 e gera auditoria; query sem contexto retorna zero linhas.

## Revisitar quando

- Um cliente exigir isolamento físico por cláusula contratual — nesse caso, banco dedicado apenas para ele, mantendo o mesmo schema
- O parque ultrapassar a capacidade de uma instância de banco (muito além do horizonte atual)
