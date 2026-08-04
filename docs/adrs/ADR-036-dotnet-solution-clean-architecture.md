# ADR-036 · C#/.NET em solution única com Clean Architecture

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 01/08/2026 |
| **Decisores** | Tech Lead |
| **Substitui** | [ADR-002](ADR-002-typescript-monorepo.md) |
| **Relacionados** | ADR-001, ADR-003 (substituído), ADR-015 (substituído), ADR-037, ADR-038, ADR-039 |
| **Requisitos afetados** | RNF-MAN-01 a 03 |

---

## Contexto

O ADR-002 fixou TypeScript de ponta a ponta (backend + frontend) em monorepo pnpm/Turborepo, com o argumento central de que regra de negócio duplicada entre camadas produz divergência — e divergência neste sistema significa número errado com aparência de certo, o pior defeito possível.

Esse argumento continua válido. O que muda é a decisão de qual linguagem/ferramental sustenta o **backend**: a Replay decidiu seguir, para novos backends, a arquitetura de referência já validada no projeto irmão `seminarioteologico` (`D:\OneDrive\Workspace\seminarioteologico\backend`) — C#/.NET com Clean Architecture, CQRS via MediatR e Entity Framework Core. O objetivo é reutilizar um padrão de arquitetura já testado em produção, com convenções de camadas, testes e CI já maduras, em vez de manter dois padrões de backend distintos entre projetos da Replay.

O frontend (React 18 + Vite + TypeScript) **não muda** — a exigência de "um só time, um só idioma" do ADR-002 permanece válida dentro do frontend e na fronteira de contratos (OpenAPI gerando tipos TS), só deixa de se estender ao processo do servidor.

## Forças em jogo

| Força | Descrição |
|---|---|
| Consistência de regra | A mesma regra de domínio precisa valer no edge e na nuvem, sem duplicação — motivo original do ADR-002 |
| Padronização entre projetos Replay | Duas stacks de backend diferentes entre projetos da Replay custam manutenção de conhecimento e onboarding |
| Maturidade do padrão de referência | `seminarioteologico` já resolveu, em produção, a separação Domain/Application/Infrastructure/Contracts/Api e o pipeline MediatR (Validation → Logging → Transaction) |
| Fronteira mais forte que lint | C# com `ProjectReference` transforma "domínio não importa nada" em erro de **compilação**, não de lint (ver ADR-039) |
| Compatibilidade com decisões já aceitas | PostgreSQL + RLS (ADR-004), local-first (ADR-001) e event sourcing seletivo (ADR-006) precisam continuar válidos com o novo stack |

## Decisão

**O backend do ecossistema Nexora passa a ser C#/.NET 10, organizado como uma solution única (`.slnx`) com Clean Architecture**, seguindo o mesmo particionamento de projetos do `seminarioteologico`: `Domain`, `Application`, `Infrastructure`, `Contracts`, `Shared`, mais os pontos de entrada `Api.Edge` e `Api.Cloud` (em vez de um único `Api`, porque o Nexora — diferente do seminário — tem topologia local-first com dois processos ASP.NET Core distintos; ver ADR-001).

O frontend continua TypeScript/React, fora do escopo desta troca.

## Detalhamento

### Estrutura

```
nexora/
├── backend/
│   ├── src/
│   │   ├── Nexora.Domain/            entidades, máquinas de estado, regras puras — zero dependências
│   │   ├── Nexora.Application/        commands, queries, handlers, validators, behaviors (MediatR)
│   │   ├── Nexora.Infrastructure/    EF Core, RLS, Redis, SignalR, storage, integrações
│   │   ├── Nexora.Contracts/         DTOs de request/response da API
│   │   ├── Nexora.Shared/            constantes de erro, helpers sem regra de negócio
│   │   ├── Nexora.Api.Edge/          ASP.NET Core — servidor da loja
│   │   └── Nexora.Api.Cloud/         ASP.NET Core — nuvem
│   ├── tests/
│   │   ├── Nexora.UnitTests/
│   │   ├── Nexora.IntegrationTests/
│   │   ├── Nexora.ApiTests/
│   │   └── Nexora.ArchitectureTests/
│   ├── Directory.Build.props        configuração MSBuild comum (nullable, LangVersion, analyzers)
│   ├── Directory.Packages.props     versionamento central de pacotes NuGet
│   ├── NuGet.config
│   ├── global.json                  fixa o SDK do .NET
│   └── Nexora.slnx
└── frontend/                        React/TS — inalterado, fora deste ADR
```

`Directory.Build.props` e `Directory.Packages.props` cumprem, no ecossistema .NET, o papel que `packages/config` (presets de ESLint/TS/Tailwind) cumpria no ADR-002: configuração compartilhada aplicada uma vez, para toda a solution.

### Divisão de responsabilidade por camada

| Camada | Responsabilidade |
|---|---|
| `Domain` | Entidades, regras de domínio, value objects, exceções de domínio. Sem dependência de nenhum pacote além da BCL |
| `Application` | Commands, queries, handlers, `IPipelineBehavior` (validação, logging, transação), abstrações (`ICurrentTenantContext`, interfaces de repositório) |
| `Infrastructure` | EF Core (`AppDbContext`), interceptor de RLS, repositórios, autenticação/JWT, Redis, SignalR hubs, storage, adaptadores de pagamento/fiscal |
| `Contracts` | DTOs públicos de entrada e saída da API |
| `Shared` | Catálogo de códigos de erro, utilitários sem regra de negócio |
| `Api.Edge` / `Api.Cloud` | Controllers, middlewares, `Program.cs`, composição de DI específica de cada topologia |

### Por que dois pontos de entrada (`Api.Edge` e `Api.Cloud`) em vez de um `Api` único

O `seminarioteologico` é SaaS puro na nuvem — um único `Api`. O Nexora tem uma exigência que o seminário não tem (ADR-001): a loja precisa continuar operando sem internet, com um processo ASP.NET Core próprio rodando no mini-PC da loja. `Api.Edge` e `Api.Cloud` compartilham `Domain`, `Application`, `Contracts` e a maior parte de `Infrastructure` — cada um registra, no seu `Program.cs`, apenas os `MediatR.RegisterServicesFromAssembly` e os controllers relevantes ao seu contexto (equivalente aos módulos NestJS diferentes por app do ADR-003 substituído).

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Manter TypeScript/NestJS (ADR-002/003 originais) | Já documentado, equipe já avaliou | Duas stacks de backend diferentes entre projetos Replay; perde o ganho de reuso de padrão validado | A decisão de padronizar em .NET veio de fora deste projeto — diretriz da Replay |
| Backend em .NET, mas em repositório separado do frontend | Isolamento total | Recria o problema original do ADR-002: versionamento cruzado de contratos vira trabalho manual | Contratos entre `Contracts` (C#) e `frontend/packages/contracts` (TS) precisam nascer do mesmo OpenAPI; monorepo com duas raízes de build resolve isso melhor |
| Um único projeto `Api` (como o `seminarioteologico`), sem separar Edge/Cloud | Mais simples, menos projetos | Não expressa a autoridade operacional distinta de ADR-001; dificulta compor cada processo só com o que precisa | A exigência de local-first é estrutural, não pode ser escondida atrás de feature flags dentro de um único `Api` |

## Consequências

**Positivas**

- Reaproveita um padrão de Clean Architecture já validado em produção (`seminarioteologico`), reduzindo risco de arquitetura nova
- `Domain` sem dependências é garantido pelo compilador (ausência de `ProjectReference`), não por regra de lint — mais forte que o ADR-002 original
- Onboarding de quem já conhece o `seminarioteologico` é imediato
- Tipagem estática forte, com `nullable` obrigatório e analisadores Roslyn no build

**Negativas**

- Perde-se o compartilhamento literal de código entre backend e frontend (TS↔TS) que o ADR-002 original oferecia; a fronteira agora é um contrato HTTP/OpenAPI, não mais um import direto de `packages/domain`
- Duas linguagens no repositório (C# no backend, TS no frontend) exigem dois pipelines de build/lint/test no CI
- Equipe precisa de proficiência em C#/.NET além de TypeScript

**Mitigações**

- Tipos TS do frontend são **gerados automaticamente** a partir do OpenAPI publicado pelo backend (mesmo princípio de "fonte única de verdade" do ADR-002, aplicado à fronteira HTTP em vez de a um pacote compartilhado)
- CI roda os dois pipelines em paralelo (dotnet e node), sem serializar
- `ARCHITECTURE.md` do `seminarioteologico` serve como referência viva de convenções para reduzir a curva de aprendizado

## Como validar

- `Nexora.Domain.csproj` não tem nenhuma `ProjectReference` nem `PackageReference` além da BCL
- `dotnet build` falha se uma camada referenciar o que não deveria (erro de compilação, não de lint)
- `Nexora.ArchitectureTests` roda em todo PR e falha se as regras de camada (ADR-039) forem violadas
- Cobertura de `Nexora.Domain` ≥ 90%, sem mock de infraestrutura

## Revisitar quando

- A Replay decidir por outro padrão de referência entre projetos
- Um módulo específico apresentar gargalo que justifique isolá-lo em outro runtime (candidato: worker de agregação de métricas, se volumetria crescer muito além do previsto)
