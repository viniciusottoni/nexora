# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## O que este repositório é hoje

**Pacote de especificação + monorepo em implementação (E-00, Fundação da Plataforma).** O projeto 004_DonaBetinha (Replay Studio) é o ecossistema de gestão e operação para estabelecimentos de alimentação, com a Pizzaria Dona Betinha como cliente-piloto e primeira instância de um **produto replicável**, não de um software sob medida.

```
004_DonaBetinha/
├── Assets/    briefing original (.docx) e infográfico do ecossistema
├── Docs/      o pacote inteiro de especificação (ver hierarquia abaixo)
└── Git/       monorepo (backend .NET + frontend React/Vite) — ver Build/lint/testes abaixo
```

O diretório de trabalho raiz **não** é um repositório git. Dentro de `Git/` existe a solution .NET (`backend/Nexora.slnx`, camadas do ADR-039) e o monorepo pnpm/turbo do frontend, com as 7 histórias do E-00 implementadas (US-001 a US-007) — mas **`Git/.git` ainda não tem nenhum commit**: todo esse código está no working tree, não versionado. Não assuma que o histórico de commits reflete o estado do código; confira o disco.

> **Mudança de stack (01/08/2026):** o backend passou a seguir a arquitetura de referência do projeto irmão `seminarioteologico` (ASP.NET Core + Clean Architecture + CQRS/MediatR + EF Core), mantendo as decisões de negócio do Nexora inalteradas — local-first, event sourcing seletivo, multi-tenant via PostgreSQL RLS. Ver [ADR-036](Docs/ADRs/ADR-036-dotnet-solution-clean-architecture.md), [037](Docs/ADRs/ADR-037-aspnet-core-backend.md), [038](Docs/ADRs/ADR-038-ef-core-orm.md) e [039](Docs/ADRs/ADR-039-fronteiras-por-project-reference.md), que substituem ADR-002, 003, 005 e 015. O frontend (React/Vite/PWA) **não muda**.

Toda a documentação é em **português do Brasil**, e o vocabulário de domínio é português (mesa, comanda, praça, sangria, meio a meio, ficha técnica). Mantenha esse idioma em documentos novos. No código, [ADR-021](Docs/ADRs/ADR-021-tratamento-de-erros.md) determina que mensagens de erro voltadas ao usuário final também sejam em português.

## Hierarquia de autoridade da documentação

Dois documentos foram superados por pastas mais detalhadas. Em caso de divergência, **prevalece a pasta**:

| Fonte normativa | Supera | Motivo |
|---|---|---|
| `Docs/ADRs/` (39 ADRs individuais — 35 originais + 4 de migração para .NET) | `Docs/06-ADRs-Decisoes-Arquiteturais.md` | O 06 é sumário histórico das 14 primeiras decisões |
| `Docs/Domain/` (DDL executável, 00→13) | `Docs/03-Modelo-de-Dados.md` | O 03 é a visão conceitual; `Domain/` é o contrato de implementação |

Comece sempre por `Docs/00-INDICE-DA-DOCUMENTACAO.md`, `Docs/ADRs/README.md` e `Docs/Domain/README.md`. Para entender a arquitetura rápido, a ordem de ADRs recomendada é `001 → 036 → 037 → 039 → 004 → 038 → 006 → 007 → 018 → 017 → 020`.

**ADR aceito é normativo — código que o viola não passa em revisão.** ADR não se edita para mudar a decisão: cria-se um novo que o substitui, e o antigo vira `Substituído por ADR-XXX`. Numeração nunca é reaproveitada.

## Convenções de identificação usadas em todo o pacote

`RF-xxx` requisito funcional · `RN-xxx` regra de negócio · `RNF-xxx` requisito não funcional · `ADR-xxx` decisão arquitetural · `E-xx` épico · `US-xxx` user story · `EVT-xxx` evento de domínio · `MET-xxx` métrica.

Marcações de confiança: `[FATO]` confirmado na descoberta · `[HIPÓTESE]` interpretação da Replay, exige validação · `[PENDÊNCIA]` bloqueia definição · `[FASE n]` alocação no roadmap. Ao redigir, não promova uma hipótese a fato sem confirmação do cliente.

## Arquitetura em uma página

Cinco forças determinam quase todas as decisões técnicas (doc. 02, §1): a operação **não pode parar sem internet** (local-first), **tudo precisa ser medido** (event sourcing seletivo), o produto é **replicável em N estabelecimentos** (multi-tenant desde o dia 1), **toda camada web é personalizável** (theming em runtime, nunca build por cliente) e o pedido precisa chegar ao KDS **em menos de 2 s** (WebSocket local, sem round-trip à nuvem).

Topologia: um **edge server** por loja (mini-PC com Docker: ASP.NET Core + PostgreSQL + Redis + worker de sync) é a autoridade operacional de pedido, mesa, comanda, KDS e caixa. A **nuvem** é autoridade de cardápio, configuração, estoque consolidado, financeiro, BI, delivery e plataforma.

**Regra de ouro da autoridade:** um dado tem um único dono. Cardápio é editado na nuvem e apenas lido no local; pedido é criado no local e apenas lido na nuvem. A única exceção — saldo de estoque — é resolvida não sincronizando saldo, e sim **movimentos** ([ADR-008](Docs/ADRs/ADR-008-saldo-derivado-de-movimentos.md)).

Stack decidida (backend): C#/.NET 10 de ponta a ponta no servidor, solution única (`.slnx`) com `Directory.Build.props`/`Directory.Packages.props`, ASP.NET Core Web API com Clean Architecture + CQRS/MediatR, EF Core (provider Npgsql) sobre PostgreSQL 16 com RLS, FluentValidation, Redis + `BackgroundService` para sync/agendamentos, SignalR para realtime local. Frontend inalterado: React 18 + Vite (PWA), TanStack Query, Zustand, Dexie + Workbox, Tailwind com design tokens por tenant. Testes: xUnit (unit/API via `WebApplicationFactory`), Testcontainers.PostgreSql (integração com RLS real), Playwright (E2E), k6 (carga). CI/CD: GitHub Actions.

## Fronteiras da solution — verificadas pelo compilador, não por disciplina

```
backend/src/{Nexora.Api.Edge, Nexora.Api.Cloud, Nexora.Application,
             Nexora.Domain, Nexora.Infrastructure, Nexora.Contracts, Nexora.Shared}
backend/tests/{Nexora.UnitTests, Nexora.IntegrationTests, Nexora.ApiTests, Nexora.ArchitectureTests}
frontend/apps/{web-admin, web-pos, web-kds, web-menu, web-platform}   ← inalterado, fora desta mudança
frontend/packages/{ui, contracts, config}                            ← inalterado
infra/{edge, cloud, scripts}
```

| Projeto | Pode referenciar | Nunca referencia |
|---|---|---|
| `Domain` | nada além da BCL (`decimal`, `Guid.CreateVersion7()`, `DateTime`) | MediatR, EF Core, ASP.NET Core, `Infrastructure`, `Api.*` |
| `Application` | `Domain`, MediatR, FluentValidation, `Microsoft.EntityFrameworkCore`¹ | Npgsql/Design/Relational, ASP.NET Core, `Infrastructure` |
| `Contracts` | `Domain` | `Application`, `Infrastructure`, ASP.NET Core |
| `Infrastructure` | `Domain`, `Application`, `Contracts`, EF Core, Npgsql, StackExchange.Redis | ASP.NET Core (exceto abstrações de `Options`) |
| `Api.Edge` / `Api.Cloud` | tudo | — |
| `frontend/*` (React/TS) | `ui`, `contracts` (tipos gerados do OpenAPI) | qualquer projeto C# diretamente |

A linha que sustenta tudo: **`Nexora.Domain` não tem nenhuma `ProjectReference` nem `PackageReference` além da BCL**. É ela que garante que a regra de negócio é idêntica no edge e na nuvem — e aqui a garantia é mais forte que no stack anterior: é erro de **compilação**, não de lint. As fronteiras entre as demais camadas são reforçadas por `Nexora.ArchitectureTests` (NetArchTest), que falha o build se uma camada referenciar o que não deveria.

¹ `Application` referencia só o pacote `Microsoft.EntityFrameworkCore` (sem provider) — expõe `DbSet<T>`/`DbContext`/`IQueryable`, usado exclusivamente na porta `IApplicationDbContext`. `Microsoft.EntityFrameworkCore.Abstractions`, apesar do nome, não contém `DbSet<T>` (só metadados de baixo nível como `IProperty`) e por isso não serve para esse propósito. O pacote `Microsoft.EntityFrameworkCore` em si não depende de nenhum provider — Npgsql, `Microsoft.EntityFrameworkCore.Design`/`.Relational`, migrations e `SaveChangesInterceptor` continuam exclusivos de `Infrastructure`. `Nexora.ArchitectureTests` verifica que `Application` não referencia esses pacotes/namespaces específicos.

## Regras normativas que constrangem qualquer código escrito aqui

Estas não são preferências de estilo — são decisões aceitas com verificação automatizada prevista no CI.

| Regra | ADR |
|---|---|
| **Proibido condicional por tenant** (`if (tenant.Slug == "x")`, mapas por tenant) em qualquer camada. Toda diferença vira configuração do produto. Grep bloqueante no CI; exceções só no módulo de plataforma e em testes | [013](Docs/ADRs/ADR-013-proibicao-de-codigo-por-cliente.md) |
| **`double`/`float` são proibidos para dinheiro.** `NUMERIC(12,2)` no banco, `decimal` nativo do C# na aplicação, **string** no JSON (via `JsonConverter<decimal>` dedicado — o frontend em TS ainda precisa de string para não perder precisão). Arredondamento half-up; toda divisão concilia (a sobra vai para a primeira parcela). Insumo usa 4 casas | [017](Docs/ADRs/ADR-017-representacao-monetaria.md) |
| **Todo timestamp em UTC** (`TIMESTAMPTZ` / `DateTime` com `Kind=Utc`). Agregação de negócio usa `business_day` materializado, calculado pela virada configurável por tenant (padrão 5h). `DateTime.Now`/`DateTime.Today` bloqueados por analyzer | [018](Docs/ADRs/ADR-018-fuso-horario-e-dia-operacional.md) |
| **Nenhuma transição de estado sem emitir seu evento**, gravado na **mesma transação** do estado (`SaveChangesAsync` único, via `TransactionBehavior` do MediatR). Estado sem evento é métrica perdida e dado que não sincroniza | [006](Docs/ADRs/ADR-006-event-sourcing-seletivo.md) |
| **`Idempotency-Key` obrigatório em POST/PUT/PATCH/DELETE**, via middleware ASP.NET Core. Chave gerada pelo cliente quando a intenção nasce, não a cada tentativa. Resposta guardada 24 h | [020](Docs/ADRs/ADR-020-idempotencia-de-escrita.md) |
| **UUIDv7 gerado na origem** (`Guid.CreateVersion7()`, nativo do .NET 9+) como PK de toda entidade. `short_code` (`A47`) é apresentação — nunca chave estrangeira | [016](Docs/ADRs/ADR-016-identificadores-e-codigos.md) |
| **Isolamento por RLS, não por `WHERE` na aplicação.** `tenant_id` em toda tabela de negócio; contexto via `SET LOCAL app.tenant_id`, aplicado por um interceptor de conexão do EF Core; sem contexto o banco não retorna nada (falha fechada) | [004](Docs/ADRs/ADR-004-postgresql-rls-multitenancy.md) |
| **Erros em RFC 7807** via `ProblemDetails` nativo do ASP.NET Core, com extensões `code` estável, `recoverable`, `requiresAuthorization` e `traceId`. Recurso de outro tenant retorna **404, nunca 403**. Catálogo centralizado em `Nexora.Shared/Errors/ApiErrorCodes.cs` | [021](Docs/ADRs/ADR-021-tratamento-de-erros.md) |
| **Tenant nunca vem do cliente** em rota autenticada — sempre do token, resolvido por `ICurrentTenantContext`. Versionamento em path (`/v1`). Paginação por cursor, nunca offset | doc. 05 |
| **Soft delete sempre** (`deleted_at`); `DELETE` físico não existe (filtro global do EF Core por padrão). Auditoria e `domain_event` são append-only, com trigger que bloqueia mutação | `Domain/00` |
| **Feature flag tem dono e expira em 90 dias.** Flag que virou permanente deveria ter sido configuração | [032](Docs/ADRs/ADR-032-configuracao-e-feature-flags.md) |
| **Trunk-based**: branch de feature vive no máximo 2 dias, merge por squash, uma versão semântica única para toda a solution, release por tag. Feature grande entra atrás de flag, não em branch longa | [029](Docs/ADRs/ADR-029-branching-versionamento-release.md) |
| **`Domain` sem dependência externa**, imposto por ausência de `ProjectReference`/`PackageReference` e verificado por `Nexora.ArchitectureTests` | [039](Docs/ADRs/ADR-039-fronteiras-por-project-reference.md) |

Ao decidir onde uma demanda de cliente se encaixa: diferença de negócio que o cliente decide → **configuração**; funcionalidade contratada por plano → **módulo**; controle temporário de lançamento → **feature flag**; "só para esse cliente" → **não existe** (ADR-013). Recusas ficam registradas em `Docs/decisoes-de-produto.md`.

## Convenções de banco

`snake_case` singular; `idx_/uq_/ck_/fk_/trg_` como prefixos; índice multi-tenant sempre começa por `tenant_id`; domínios de tipo (`money_amount`, `qty_amount`, `percent_amount`, `fraction_weight`) em vez de tipos crus; enums nativos do PostgreSQL; JSONB validado por FluentValidation/`System.Text.Json` na aplicação. Duas armadilhas já resolvidas: `user` virou **`app_user`** (palavra reservada) e `order` exige aspas.

O DDL em `Docs/Domain/` está na ordem exata de execução (00 extensões → 01 plataforma → … → 12 seeds) e vira **migrations do EF Core** (`dotnet ef migrations add`) na mesma sequência — ver [13-Mapeamento-EFCore.md](Docs/Domain/13-Mapeamento-EFCore.md).

## Motion e microinterações no frontend

Nenhuma tela nasce "seca". Toda página, card, lista, diálogo, toggle, badge ou notificação nova nos 5 apps (`web-admin`, `web-kds`, `web-menu`, `web-pos`, `web-platform`) entra com animação de montagem, responde a hover/press e transiciona qualquer mudança de estado (inclusive as que chegam por WebSocket/SignalR) — nunca "pula" de um valor para outro. Ver [RNF-USA-13](Docs/08-Requisitos-Nao-Funcionais.md#6-usabilidade-e-acessibilidade--rnf-usa).

- **Fonte única dos tokens**: `packages/ui/src/tokens/motion.css` (`--dur-instant/fast/base/slow/slower`, `--ease-standard/out/in-out`, `--transition-control`) e os utilitários já prontos em `packages/ui/src/components/motion.css` (`.nx-anim-in` entrada padrão, `.nx-anim-scale-in` diálogos, `.nx-anim-toast-in` toasts/alertas, `.nx-anim-flash` destaque de atualização realtime, `.nx-stagger` entrada em cascata de listas, `.nx-skeleton`/`.nx-spinner` carregamento). Os componentes base de `packages/ui/src/components/*.css` (`db-button`, `db-card`, `db-table-card`, `db-order-ticket`, `db-menu-item-card` etc.) já os usam — component novo herda de graça; component customizado por app reusa os mesmos tokens, nunca inventa duração/easing "solto".
- **Proibido** `transition: all 0.3s` ou qualquer `ms`/`cubic-bezier` fora dos tokens acima, e proibida biblioteca externa de animação (framer-motion, GSAP): é tudo CSS nativo — motion pesado em JS custaria CPU/memória no mini-PC do edge server e concorreria com o orçamento de latência pedido→KDS (< 2 s).
- `prefers-reduced-motion: reduce` já é tratado nos tokens (zera os `--dur-*`); um `@keyframes` novo com duração fixa (não derivada de `--dur-*`, como `nx-shimmer`/`nx-spin`) precisa de override explícito nesse media query, como já feito em `components/feedback.css` e `components/motion.css`.
- Antes de criar uma animação nova, verifique se `motion.css` já cobre o caso — consistência entre os 5 apps importa mais que criatividade pontual por tela.

## Build, lint e testes

A solution existe em `Git/backend/Nexora.slnx` (SDK .NET 10, `global.json` pino `10.0.100`/`rollForward: latestFeature`) e o monorepo frontend em `Git/` (pnpm + turbo). Comandos reais, verificados:

| Comando (a partir de `Git/`) | O que faz |
|---|---|
| `dotnet build backend/Nexora.slnx` | Build de toda a solution — deve dar 0 erros |
| `dotnet test backend/Nexora.slnx` | 4 projetos de teste (`Nexora.UnitTests`, `Nexora.ApiTests`, `Nexora.ArchitectureTests`, `Nexora.IntegrationTests`) — os de integração exigem Docker (Testcontainers.PostgreSql) |
| `dotnet ef migrations add <Nome> --project backend/src/Nexora.Infrastructure --startup-project backend/src/Nexora.Infrastructure` | Nova migration (há um `DesignTimeDbContextFactory`, não precisa de projeto Api como startup) |
| `pnpm install`, `pnpm typecheck`, `pnpm vitest run` (ou `pnpm test`) | Frontend — 7 packages/apps via turbo |
| `npx tsx infra/scripts/governance.ts --root .` | Trava ADR-013/ADR-010/RLS — deve reportar zero violações |
| `node --test infra/scripts/*.test.mjs` | Meta-testes do próprio pipeline |
| `npx playwright test` | E2E (`tests/e2e/*.spec.ts`) |

O pipeline completo especificado no doc. 10 §12 (abaixo) está parcialmente refletido em `.github/workflows/`; antes de assumir que um job existe, confira o YAML — nem tudo do doc 10 foi portado 1:1.

| Gatilho | Verificações bloqueantes |
|---|---|
| Commit | `dotnet format` + analyzers (10 s), `dotnet build` — o type check do TS vira erro de compilação (30 s), unitários `dotnet test` (60 s) |
| Pull request | integração com Postgres real via Testcontainers (5 min), **isolamento multi-tenant** (30 s), **verificação ADR-013** (5 s), **`Nexora.ArchitectureTests`** (fronteiras de camada), SCA + SAST, snapshot do contrato OpenAPI, cobertura |
| Merge em `main` | E2E Playwright (15 min), build de imagens Docker, deploy em staging |
| Noturno | caos offline (20 min), integridade de dado, recálculo comparativo |
| Semanal | carga k6 (30 min) |

Alvos: cobertura global ≥ 70%, `Nexora.Domain` ≥ 90% **sem nenhum mock de infraestrutura**, pipeline de PR abaixo de 10 min. Teste de integração roda contra **PostgreSQL real em container** (Testcontainers.PostgreSql) — RLS não pode ser simulado com mock nem com o provider InMemory do EF Core.

A regra que orienta a estratégia de teste: *um bug que faz o sistema parar é ruim; um bug que apresenta um número errado com aparência de certo é pior, porque ninguém percebe.* Defeito que produz número errado é sempre severidade S1.

## Pendências abertas que bloqueiam definições

Sete questões atravessam o pacote inteiro e continuam sem resposta do cliente: emissão fiscal (NFC-e/SAT), propriedade do produto e modelo comercial, prazo/orçamento/priorização, modalidade de integração de pagamento (TEF × gateway), plano de contingência para falha do servidor local, integração com iFood, e se o app de frios é produto separado ou módulo. Prazos e datas do `09-Roadmap` são **proposta da Replay, não compromisso contratual** — o bloco de prazo do briefing veio em branco.
