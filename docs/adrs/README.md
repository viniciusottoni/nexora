# ADRs — Architecture Decision Records
## Ecossistema Nexora · Projeto 004_DonaBetinha

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Produto** | Ecossistema de controle total para estabelecimentos de alimentação |
| **Versão do conjunto** | 1.0 |
| **Data** | 31/07/2026 |
| **Mantenedor** | Tech Lead — Replay Studio |

---

## O que é um ADR

Um Architecture Decision Record registra **uma decisão arquiteturalmente significativa**, com o contexto que a produziu, as alternativas descartadas e as consequências aceitas — inclusive as ruins.

Um ADR não documenta como o sistema funciona (isso é a documentação técnica). Ele documenta **por que** o sistema é assim, para que daqui a dois anos ninguém desfaça uma decisão sem entender o que ela estava resolvendo.

### Regras deste repositório de decisões

| # | Regra |
|---|---|
| 1 | **ADR não se edita para mudar a decisão.** Cria-se um novo que o substitui, e o antigo passa a `Substituído por ADR-XXX` |
| 2 | ADR aceito é **normativo** — código que o viola não passa em revisão |
| 3 | Toda decisão que afeta mais de um módulo, ou que é cara de reverter, exige ADR |
| 4 | ADR curto é melhor que ADR longo, mas **as alternativas descartadas nunca podem faltar** |
| 5 | Toda ADR referencia os requisitos (RF/RNF) que a motivaram |

### Status possíveis

| Status | Significado |
|---|---|
| `Proposto` | Em discussão, ainda não vale |
| `Aceito` | Normativo — vale para todo o código |
| `Substituído por ADR-XXX` | Superado por decisão posterior |
| `Descontinuado` | Deixou de fazer sentido, sem substituto |
| `Adiado` | Decisão consciente de não decidir agora, com gatilho definido |

---

## Índice por categoria

### Fundação arquitetural

| ADR | Título | Status |
|---|---|---|
| [001](ADR-001-arquitetura-local-first.md) | Arquitetura local-first com servidor na loja | Aceito |
| ~~[002](ADR-002-typescript-monorepo.md)~~ | ~~TypeScript de ponta a ponta em monorepo~~ | Substituído por [036](ADR-036-dotnet-solution-clean-architecture.md) |
| ~~[003](ADR-003-nestjs-backend.md)~~ | ~~NestJS como framework de backend~~ | Substituído por [037](ADR-037-aspnet-core-backend.md) |
| ~~[015](ADR-015-estrutura-monorepo-e-fronteiras.md)~~ | ~~Estrutura do monorepo e fronteiras de dependência~~ | Substituído por [039](ADR-039-fronteiras-por-project-reference.md) |
| [036](ADR-036-dotnet-solution-clean-architecture.md) | C#/.NET em solution única com Clean Architecture | Aceito |
| [037](ADR-037-aspnet-core-backend.md) | ASP.NET Core com CQRS/MediatR como padrão de aplicação | Aceito |
| [039](ADR-039-fronteiras-por-project-reference.md) | Fronteiras de camada impostas por ProjectReference e testes de arquitetura | Aceito |

### Dados e persistência

| ADR | Título | Status |
|---|---|---|
| [004](ADR-004-postgresql-rls-multitenancy.md) | PostgreSQL com RLS para multi-tenancy | Aceito |
| ~~[005](ADR-005-prisma-orm.md)~~ | ~~Prisma como ORM~~ | Substituído por [038](ADR-038-ef-core-orm.md) |
| [038](ADR-038-ef-core-orm.md) | Entity Framework Core como ORM, mantendo PostgreSQL e RLS | Aceito |
| [016](ADR-016-identificadores-e-codigos.md) | UUIDv7 como identificador e código curto de pedido | Aceito |
| [017](ADR-017-representacao-monetaria.md) | Representação monetária e regra de arredondamento | Aceito |
| [018](ADR-018-fuso-horario-e-dia-operacional.md) | Fuso horário e conceito de dia operacional | Aceito |
| [019](ADR-019-migrations-e-compatibilidade.md) | Migrations e compatibilidade do parque instalado | Aceito |
| [035](ADR-035-particionamento-e-retencao.md) | Particionamento e retenção do event store | Aceito |

### Eventos e sincronização

| ADR | Título | Status |
|---|---|---|
| [006](ADR-006-event-sourcing-seletivo.md) | Event sourcing seletivo, não completo | Aceito |
| [007](ADR-007-transactional-outbox.md) | Sincronização por transactional outbox | Aceito |
| [008](ADR-008-saldo-derivado-de-movimentos.md) | Saldo de estoque derivado de movimentos | Aceito |
| [020](ADR-020-idempotencia-de-escrita.md) | Idempotência obrigatória em toda escrita | Aceito |
| [034](ADR-034-relogio-e-sequencia.md) | Relógio, sequência e tolerância a desvio | Aceito |

### Aplicação e interface

| ADR | Título | Status |
|---|---|---|
| [009](ADR-009-pwa-vs-nativo.md) | PWA em vez de aplicativo nativo | Aceito |
| [010](ADR-010-theming-em-runtime.md) | Theming em runtime, build único | Aceito |
| [011](ADR-011-websocket-com-fallback.md) | WebSocket local com fallback de polling | Aceito |
| [027](ADR-027-offline-no-cliente.md) | Estratégia de offline no dispositivo | Aceito |
| [028](ADR-028-cache-e-invalidacao-catalogo.md) | Cache e invalidação do catálogo | Aceito |
| [030](ADR-030-armazenamento-de-midia.md) | Armazenamento e entrega de mídia | Aceito |

### Segurança e acesso

| ADR | Título | Status |
|---|---|---|
| [014](ADR-014-autenticacao-por-pin.md) | Autenticação por PIN para perfis operacionais | Aceito |
| [023](ADR-023-modelo-de-autorizacao.md) | Modelo de autorização RBAC com elevação pontual | Aceito |
| [031](ADR-031-gestao-de-segredos.md) | Gestão de segredos e credenciais | Aceito |

### Integrações

| ADR | Título | Status |
|---|---|---|
| [024](ADR-024-abstracao-de-pagamento.md) | Abstração de provedor de pagamento | Aceito |
| [025](ADR-025-emissao-fiscal.md) | Emissão fiscal por adaptador — decisão parcialmente adiada | Adiado |
| [026](ADR-026-impressao-termica.md) | Impressão térmica por serviço no edge | Aceito |

### Produto e governança

| ADR | Título | Status |
|---|---|---|
| [013](ADR-013-proibicao-de-codigo-por-cliente.md) | Proibição de código específico por cliente | Aceito |
| [032](ADR-032-configuracao-e-feature-flags.md) | Configuração por tenant e feature flags | Aceito |
| [029](ADR-029-branching-versionamento-release.md) | Branching, versionamento e release do parque | Aceito |

### Operação e qualidade

| ADR | Título | Status |
|---|---|---|
| [012](ADR-012-agregados-precalculados.md) | Agregados pré-calculados para o painel | Aceito |
| [021](ADR-021-tratamento-de-erros.md) | Tratamento e taxonomia de erros | Aceito |
| [022](ADR-022-observabilidade-e-correlacao.md) | Observabilidade e correlação de requisições | Aceito |
| [033](ADR-033-backup-e-recuperacao-do-edge.md) | Backup e recuperação do servidor local | Aceito |

---

## Decisões adiadas conscientemente

| Tema | Gatilho para decidir | ADR |
|---|---|---|
| Emissão fiscal — qual provedor | Definição do cliente + contador | [025](ADR-025-emissao-fiscal.md) |
| Integração TEF de maquininha | Definição comercial do cliente | [024](ADR-024-abstracao-de-pagamento.md) |
| Banco por tenant | Cliente exigir isolamento físico contratual | [004](ADR-004-postgresql-rls-multitenancy.md) |
| Data warehouse dedicado | Acima de ~100 lojas | [012](ADR-012-agregados-precalculados.md) |
| App nativo do entregador | Fase 4, se PWA não atender GPS em segundo plano | [009](ADR-009-pwa-vs-nativo.md) |

---

## Como propor um novo ADR

1. Copie `ADR-template.md`
2. Numere sequencialmente (não reaproveite número, mesmo de ADR descontinuado)
3. Abra como PR com status `Proposto`
4. Discuta na revisão de arquitetura mensal (ou antes, se bloqueante)
5. Ao aprovar, mude o status para `Aceito` e atualize este índice

---

*Replay Studio — Projeto 004_DonaBetinha.*
