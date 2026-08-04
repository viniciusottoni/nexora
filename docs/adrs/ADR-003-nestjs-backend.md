# ADR-003 · NestJS como framework de backend

| | |
|---|---|
| **Status** | Substituído por ADR-037 |
| **Data** | 31/07/2026 (substituído em 01/08/2026) |
| **Decisores** | Tech Lead |
| **Substituído por** | [ADR-037](ADR-037-aspnet-core-backend.md) |
| **Relacionados** | ADR-002, ADR-015, ADR-021, ADR-023 |
| **Requisitos afetados** | RNF-MAN-03, RNF-MAN-06 |

> ⚠ **Substituído em 01/08/2026.** O framework de backend passou a ser ASP.NET Core (.NET 10), com Clean Architecture + CQRS/MediatR em vez de módulos NestJS. Ver [ADR-037](ADR-037-aspnet-core-backend.md). Conteúdo abaixo mantido como registro histórico.

---

## Contexto

O mesmo código-base precisa rodar em dois contextos com responsabilidades diferentes:

- **`api-edge`** — na loja: pedido, mesa, KDS, caixa, outbox de sincronização
- **`api-cloud`** — na nuvem: recepção de sync, métricas, financeiro, catálogo, plataforma

Precisamos de um mecanismo de composição que permita montar as duas aplicações a partir dos mesmos módulos, sem duplicar código e sem carregar na loja o que ela não usa.

Além disso, o sistema tem exigências transversais fortes: contexto de tenant em toda requisição (ADR-004), idempotência (ADR-020), autorização com elevação (ADR-023), auditoria (RF-AUD) e correlação de traces (ADR-022). Esses aspectos precisam de um lugar arquitetural claro para viver — não espalhados por controllers.

## Decisão

**NestJS (Node 22 LTS) como framework de backend nas duas aplicações**, com módulos por domínio e composição diferente por aplicação.

## Detalhamento

```ts
// apps/api-edge/src/app.module.ts
@Module({
  imports: [
    CoreModule,            // tenant context, auth, audit, errors
    CatalogReadModule,     // catálogo somente leitura (réplica local)
    OrderModule,
    TableModule,
    KdsModule,
    CashModule,
    StockDeductionModule,
    SyncOutboxModule,
    RealtimeModule,        // WebSocket
  ],
})
export class AppModule {}

// apps/api-cloud/src/app.module.ts
@Module({
  imports: [
    CoreModule,
    CatalogWriteModule,
    RecipeModule,
    StockModule,
    FinanceModule,
    MetricsModule,
    SyncGatewayModule,
    DeliveryModule,
    PlatformModule,
  ],
})
export class AppModule {}
```

### Onde vive cada aspecto transversal

| Aspecto | Mecanismo do Nest |
|---|---|
| Contexto de tenant (`SET LOCAL app.tenant_id`) | Middleware + AsyncLocalStorage |
| Autenticação | `AuthGuard` |
| Autorização RBAC e elevação | `PermissionsGuard` + decorator `@RequirePermission()` |
| Idempotência | `IdempotencyInterceptor` |
| Auditoria | `AuditInterceptor` |
| Erros padronizados | `ExceptionFilter` global |
| Correlação e tracing | `TracingMiddleware` |
| Emissão de evento no outbox | Serviço de domínio dentro da transação (não interceptor) |
| Validação de entrada | `ZodValidationPipe` |
| Documentação | `@nestjs/swagger` → OpenAPI 3.1 |

> Nota deliberada: **a emissão de evento não é interceptor.** Ela precisa ocorrer dentro da transação de negócio (ADR-007) e depende de dados que só o serviço de domínio conhece.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Fastify puro | Mais rápido; sem overhead | Sem estrutura imposta | Em time que cresce, cada desenvolvedor inventa uma organização; aspectos transversais viram cópia-e-cola |
| Express | Onipresente; simples | Nenhuma opinião arquitetural | Mesmo problema, agravado |
| Hono / Elysia | Modernos e leves | Ecossistema imaturo para DI, guards e OpenAPI | Risco alto em sistema de longa vida |
| Encore / Wasp | Muito produtivos | Opinião forte demais; lock-in | Não queremos acoplar a arquitetura a um framework opinado sobre infraestrutura |

## Consequências

**Positivas**

- Injeção de dependência real: domínio testável sem infraestrutura
- Módulos são a unidade natural para compor edge e cloud de forma diferente
- Guards, interceptors e filters dão lugar arquitetural claro aos aspectos transversais
- OpenAPI gerado automaticamente, sustentando o contrato versionado (doc. 05)

**Negativas**

- Curva inicial de decorators e do sistema de módulos
- Overhead de boot (~1 s) — irrelevante em serviço de longa duração
- Facilita construir camadas demais se o time não tiver disciplina

**Mitigações**

- Fronteiras entre camadas definidas em ADR-015 e verificadas no CI
- Regra de time: controller não contém regra de negócio — apenas orquestra
- Domínio permanece em `packages/domain`, fora do Nest

## Como validar

- Módulos de domínio importáveis por ambas as aplicações sem alteração
- Nenhum `import` de `@nestjs/*` dentro de `packages/domain`
- OpenAPI gerado e versionado no CI (teste de contrato)

## Revisitar quando

- O tempo de boot passar a importar (função serverless, por exemplo)
- O ecossistema de um framework mais leve amadurecer a ponto de oferecer DI, guards e OpenAPI equivalentes
