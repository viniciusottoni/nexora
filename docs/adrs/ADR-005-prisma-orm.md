# ADR-005 · Prisma como ORM

| | |
|---|---|
| **Status** | Substituído por ADR-038 |
| **Data** | 31/07/2026 (substituído em 01/08/2026) |
| **Decisores** | Tech Lead |
| **Substituído por** | [ADR-038](ADR-038-ef-core-orm.md) |
| **Relacionados** | ADR-004, ADR-019 |
| **Requisitos afetados** | RNF-MAN-04, RNF-MAN-05, RNF-IMP-01 |

> ⚠ **Substituído em 01/08/2026.** O ORM passou a ser Entity Framework Core (provider Npgsql), mantendo PostgreSQL e RLS. Ver [ADR-038](ADR-038-ef-core-orm.md). Conteúdo abaixo mantido como registro histórico.

---

## Contexto

O sistema precisa aplicar migrations em um **parque distribuído** de servidores locais, potencialmente desatualizados, sem intervenção manual (RNF-IMP-02). Isso muda o peso relativo dos critérios: confiabilidade e reprodutibilidade de migration valem mais que elegância de API.

Ao mesmo tempo, a camada analítica (métricas, CMV, percentis) exige SQL que nenhum ORM expressa bem.

## Decisão

**Prisma como ORM principal, com SQL puro via `$queryRaw` para a camada analítica.**

## Detalhamento

### Divisão clara

| Uso | Ferramenta |
|---|---|
| CRUD de domínio, relacionamentos, transações | Prisma Client |
| Consultas analíticas, percentis, janelas, agregações | `$queryRaw` tipado |
| Migrations | `prisma migrate` |
| Contexto de RLS | `$executeRaw` no middleware |

### Integração com RLS

Prisma usa pool de conexões, o que exige cuidado: o contexto do tenant precisa ser **local à transação**, senão vaza entre requisições.

```ts
export async function withTenant<T>(tenantId: string, fn: (tx) => Promise<T>) {
  return prisma.$transaction(async (tx) => {
    await tx.$executeRaw`SELECT set_config('app.tenant_id', ${tenantId}, true)`;
    return fn(tx);
  });
}
```

Toda operação de negócio passa por esse wrapper. Uso direto do `prisma` global em código de domínio é proibido e verificado por lint.

### Migrations no edge

```bash
# durante a atualização do servidor local
prisma migrate deploy      # aplica apenas o pendente, sem prompt, sem reset
```

Regras complementares em ADR-019.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| TypeORM | Maduro; Active Record e Data Mapper | Histórico de instabilidade em migrations | Migration não confiável é inaceitável em parque distribuído |
| Drizzle | Leve; SQL-first; tipagem excelente | Ferramental de migration menos maduro | Risco alto no cenário de parque; reavaliar no futuro |
| Kysely | Query builder tipado, ótimo para analytics | Sem migrations nem modelagem | Exigiria outra ferramenta para migration |
| Knex + SQL puro | Controle total | Sem tipagem derivada do schema; produtividade baixa | Custo de manutenção alto para o time |

## Consequências

**Positivas**

- Tipos gerados diretamente do schema — divergência entre modelo e código falha no build
- `prisma migrate deploy` é confiável e idempotente, ideal para o parque
- Schema declarativo funciona como documentação viva do modelo (doc. 03)
- Introspecção facilita conferência entre ambientes

**Negativas**

- RLS exige `$executeRaw` encapsulado — abstração adicional a manter
- Consultas analíticas saem do ORM (o que consideramos desejável, não problema)
- Prisma adiciona peso ao container do edge (~50 MB)
- Migrations do Prisma não expressam bem operações de dados complexas

**Mitigações**

- `withTenant` é a única porta de entrada; lint bloqueia uso direto do client
- Migrations de **dados** rodam como job separado, nunca dentro de migration de schema (ADR-019)
- Consultas analíticas ficam em `packages/metrics`, com testes próprios

## Como validar

- Nenhum uso de `prisma.` fora de `withTenant` em código de domínio (regra de lint)
- `prisma migrate deploy` aplicado sobre dump real de produção em CI antes de liberar release do parque
- Teste de RLS (ADR-004) passa também nas consultas via `$queryRaw`

## Revisitar quando

- Drizzle amadurecer o ferramental de migration a ponto de superar o Prisma em confiabilidade no cenário distribuído
- O peso do Prisma no edge tornar-se restritivo em hardware mais modesto
