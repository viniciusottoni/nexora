# ADR-037 · ASP.NET Core com CQRS/MediatR como padrão de aplicação

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 01/08/2026 |
| **Decisores** | Tech Lead |
| **Substitui** | [ADR-003](ADR-003-nestjs-backend.md) |
| **Relacionados** | ADR-001, ADR-036, ADR-038, ADR-020, ADR-021, ADR-022, ADR-023 |
| **Requisitos afetados** | RNF-MAN-03, RNF-MAN-06 |

---

## Contexto

O ADR-003 escolheu NestJS por dar estrutura explícita (módulos, guards, interceptors, DI) a um código-base que precisa rodar em dois contextos — `api-edge` e `api-cloud` — com aspectos transversais fortes: contexto de tenant em toda requisição (ADR-004), idempotência (ADR-020), autorização com elevação (ADR-023), auditoria e correlação de tracing (ADR-022).

Com a mudança de stack para .NET (ADR-036), a mesma necessidade de estrutura explícita continua — só que agora a referência é o padrão já em produção no `seminarioteologico`: **ASP.NET Core Web API com Clean Architecture e CQRS via MediatR**, documentado em detalhe em `backend/ARCHITECTURE.md` daquele projeto.

## Forças em jogo

| Força | Descrição |
|---|---|
| Controllers finos | Regra de negócio não pode viver em controller — problema que o ADR-003 já resolvia com módulos/guards/interceptors |
| Separação leitura/escrita | Comandos e queries têm perfis de performance e de risco diferentes (queries usam `AsNoTracking`, commands passam por transação) |
| Aspectos transversais | Tenant, idempotência, autorização, auditoria, tracing precisam de um lugar arquitetural único, não espalhado |
| Padrão já validado | `seminarioteologico` já resolveu esse desenho em produção — reduz risco de reinventar |
| Dois pontos de entrada | `Api.Edge` e `Api.Cloud` precisam compor handlers de módulos diferentes (equivalente aos módulos NestJS distintos por app do ADR-003) |

## Decisão

**ASP.NET Core Web API (.NET 10) como framework de backend, com CQRS via MediatR como padrão obrigatório para casos de uso**, seguindo o pipeline de behaviors e a separação Command/Query do `seminarioteologico`.

## Detalhamento

### Fluxo padrão

```
Controller → ISender.Send(command|query)
  → ValidationBehavior (FluentValidation)
  → LoggingBehavior (Serilog estruturado, com traceId — ADR-022)
  → TenantBehavior (resolve tenant do JWT via ICurrentTenantContext, aplica SET LOCAL — ADR-004)
  → IdempotencyBehavior (só em commands — ADR-020)
  → TransactionBehavior (SaveChangesAsync único por command)
  → Handler → Domain / Repository / DbContext
  → Result<T> → Controller → ProblemDetails (erro) ou 2xx (sucesso)
```

> Nota herdada do ADR-003 original: a emissão do evento de domínio (ADR-006) **não é um behavior separado** — ela acontece dentro do próprio handler, na mesma transação do estado, porque depende de dados que só o handler conhece no momento da escrita.

### Onde vive cada aspecto transversal

| Aspecto | Mecanismo no ASP.NET Core |
|---|---|
| Contexto de tenant (`SET LOCAL app.tenant_id`) | `ICurrentTenantContext` (Application) + interceptor de conexão do EF Core (Infrastructure) — ver ADR-004 e ADR-038 |
| Autenticação | `AddAuthentication().AddJwtBearer(...)` |
| Autorização RBAC e elevação | `[Authorize(Roles = "...")]` + `IAuthorizationHandler` customizado para elevação pontual — ver ADR-023 |
| Idempotência | Middleware `IdempotencyMiddleware` (ASP.NET Core) + tabela `idempotency_key` — ver ADR-020 |
| Auditoria | `SaveChangesInterceptor` do EF Core, grava em `audit_log` na mesma transação |
| Erros padronizados | `ProblemDetails` nativo (`AddProblemDetails()`) — já é RFC 7807 por padrão, sem exception filter customizado — ver ADR-021 |
| Correlação e tracing | Middleware de correlação + OpenTelemetry — ver ADR-022 |
| Validação de entrada | `FluentValidation` + `ValidationBehavior<TRequest,TResponse>` do MediatR |
| Documentação | `Microsoft.AspNetCore.OpenApi`/Swashbuckle → OpenAPI 3.1 |

### Composição diferente por aplicação

```csharp
// Nexora.Api.Edge/Program.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateOrderCommand>();
    cfg.RegisterServicesFromAssemblyContaining<UpdateKdsStatusCommand>();
    cfg.RegisterServicesFromAssemblyContaining<OpenCashSessionCommand>();
    cfg.RegisterServicesFromAssemblyContaining<PushOutboxBatchCommand>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(TenantBehavior<,>));
    cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
});
builder.Services.AddHostedService<SyncOutboxWorker>();   // BackgroundService — equivalente ao SyncOutboxModule do Nest

// Nexora.Api.Cloud/Program.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<AcceptSyncBatchCommand>();
    cfg.RegisterServicesFromAssemblyContaining<UpsertCatalogCommand>();
    cfg.RegisterServicesFromAssemblyContaining<GetOwnerDashboardQuery>();
    cfg.RegisterServicesFromAssemblyContaining<CloseFinancialPeriodCommand>();
    // ...mesmos behaviors
});
```

Cada aplicação referencia (`ProjectReference`) apenas os módulos de `Application` que precisa — o equivalente direto à composição por `@Module({ imports: [...] })` do ADR-003 substituído, só que a fronteira agora é imposta pelo grafo de projetos, não por decorator.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Minimal APIs sem MediatR | Menos código de infraestrutura, boot mais rápido | Sem separação clara de commands/queries; controllers tendem a acumular lógica | Repete o problema que motivou o ADR-003 original — sem estrutura, cada desenvolvedor organiza diferente |
| Manter NestJS (ADR-003 original) | Já documentado e avaliado pela equipe | Fora do padrão de referência da Replay; duas stacks de backend entre projetos | Superado pela decisão de padronização em ADR-036 |
| CQRS sem MediatR (handlers resolvidos manualmente) | Menos dependência externa | Perde o pipeline de behaviors pronto (validation/logging/transaction); reimplementação desnecessária | MediatR é o padrão já validado no `seminarioteologico`, com pipeline testado |

## Consequências

**Positivas**

- Controllers ficam finos: recebem request, montam command/query, chamam `ISender.Send`, traduzem `Result` em resposta HTTP
- Separação commands/queries reduz risco de query pesada rodar dentro de transação de escrita
- `ProblemDetails` nativo do ASP.NET Core cobre RFC 7807 sem exception filter customizado — simplificação real em relação ao NestJS
- Pipeline de behaviors dá lugar único para tenant, validação, logging e transação — mesmo ganho do ADR-003 original, com garantias de tipo mais fortes (C# genérico vs. decorators TS)

**Negativas**

- Curva inicial de MediatR e do padrão Result para quem só conhece controllers tradicionais
- Debugging de pipeline de behaviors pode ser menos direto que uma chamada de método explícita
- Overhead de reflexão do MediatR em alto volume — irrelevante na volumetria da Dona Betinha (doc. 03, §14)

**Mitigações**

- `ARCHITECTURE.md` do `seminarioteologico` serve como guia de referência para o time
- Regra de time: handler não conhece HTTP, controller não conhece regra de negócio (mesma regra do ADR-003 original)
- `Nexora.ArchitectureTests` bloqueia handler que retorna `IActionResult` ou controller que injeta `AppDbContext` diretamente

## Como validar

- Nenhum controller injeta `AppDbContext`/repositório diretamente — só `ISender`
- Todo command/query tem um validator ou está explicitamente isento (documentado)
- `Nexora.ArchitectureTests` verifica que handlers estão marcados `internal sealed` (não vazam para fora de `Application`)
- OpenAPI gerado e versionado no CI (teste de contrato, doc. 05)

## Revisitar quando

- O tempo de boot ou o overhead do MediatR se tornar mensuravelmente relevante em produção
- Um caso de uso não se encaixar bem no padrão Command/Query (ex.: operações em lote muito específicas) — avaliar exceção documentada, não abandono da regra
