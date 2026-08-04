# ADR-002 · TypeScript de ponta a ponta em monorepo

| | |
|---|---|
| **Status** | Substituído por ADR-036 |
| **Data** | 31/07/2026 (substituído em 01/08/2026) |
| **Decisores** | Tech Lead |
| **Substituído por** | [ADR-036](ADR-036-dotnet-solution-clean-architecture.md) |
| **Relacionados** | ADR-003, ADR-015, ADR-029 |
| **Requisitos afetados** | RNF-MAN-01 a 03 |

> ⚠ **Substituído em 01/08/2026.** O backend passou a seguir a arquitetura de referência do projeto `seminarioteologico` (C#/.NET, Clean Architecture). A decisão de linguagem/estrutura de repositório para o backend agora vive em [ADR-036](ADR-036-dotnet-solution-clean-architecture.md). O conteúdo abaixo é mantido como registro histórico do raciocínio original — não é mais normativo para o backend. O frontend (React/TS) não foi afetado por essa mudança e continua regido pelo espírito deste ADR quanto a tipos e contratos compartilhados dentro de `frontend/`.

---

## Contexto

O produto tem sete aplicações (edge, cloud, admin, POS, KDS, cardápio, plataforma) que compartilham as mesmas entidades, as mesmas regras de negócio e os mesmos contratos.

O risco central não é de produtividade — é de **divergência**. Se a regra que calcula o preço de uma pizza meio a meio for implementada duas vezes (uma no backend, outra no frontend; ou uma no edge, outra na nuvem), elas vão divergir. E divergência de regra de negócio neste sistema produz **número errado com aparência de certo** — o pior defeito possível, porque o dono decide com base nele.

## Forças em jogo

| Força | Descrição |
|---|---|
| Consistência de regra | A mesma regra precisa valer no edge e na nuvem, sem duplicação |
| Contrato entre camadas | Front e back não podem divergir em tipos |
| Tamanho do time | Equipe pequena; não comporta especialização por linguagem |
| Refatoração | Mudança de contrato precisa ser atômica |

## Decisão

**TypeScript em todas as camadas, em monorepo gerenciado por pnpm workspaces + Turborepo.**

As regras de negócio puras vivem em `packages/domain` e são consumidas **igualmente** pela API do edge e pela API da nuvem. Os contratos de API vivem em `packages/contracts` e são consumidos pelo backend e pelo frontend.

## Detalhamento

```
dona-betinha/
├── apps/          aplicações executáveis
├── packages/      código compartilhado
│   ├── domain/    regras puras — sem framework, sem I/O
│   ├── contracts/ DTOs e tipos de API
│   ├── events/    schemas de evento (Zod)
│   └── ...
└── infra/
```

Regra fundamental: `packages/domain` **não importa nada** de NestJS, Prisma, React ou banco de dados. É TypeScript puro, testável sem infraestrutura, com cobertura mínima de 90% (RNF-MAN-01).

```ts
// packages/domain/pricing/half-and-half.ts — puro, sem dependência
export function calculateHalfAndHalfPrice(
  fractions: Fraction[],
  rule: HalfPricingRule,
): Money { /* ... */ }
```

Consumido identicamente:

```ts
// apps/api-edge  e  apps/api-cloud
import { calculateHalfAndHalfPrice } from '@db/domain';
```

### Ferramentas

| Item | Escolha |
|---|---|
| Gerenciador de pacotes | pnpm (workspaces, disco eficiente, resolução estrita) |
| Orquestrador de build | Turborepo (cache local e remoto, grafo de tarefas) |
| Node | 22 LTS |
| TypeScript | strict mode obrigatório, `noUncheckedIndexedAccess` ligado |

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Backend em Go ou Java, front em TS | Melhor desempenho de CPU; tipagem forte | Regras de domínio duplicadas entre backend e frontend | Divergência garantida; equipe pequena não sustenta duas stacks |
| Repositórios separados por aplicação | Independência de deploy | Versionamento cruzado de contratos vira trabalho permanente | Sete repositórios com contratos acoplados é atrito diário |
| Monorepo com Nx | Ferramental mais rico | Curva e opinião maiores | Turborepo atende com menos complexidade |
| JavaScript sem tipos | Menos cerimônia | Sem garantia de contrato | Inaceitável em sistema que calcula dinheiro e estoque |

## Consequências

**Positivas**

- Regra de negócio escrita **uma única vez**, usada por edge e nuvem
- Tipos compartilhados: quebra de contrato falha no build, não em produção
- Refatoração atômica em uma única PR
- Um só time, uma só stack, uma só curva de aprendizado

**Negativas**

- Node é menos eficiente que Go em uso de CPU — irrelevante nesta volumetria (doc. 03, §14)
- Monorepo exige disciplina de fronteiras: sem regra, tudo importa tudo (endereçado pelo ADR-015)
- Build cache mal configurado torna o CI lento

**Mitigações**

- ADR-015 define fronteiras de dependência verificadas automaticamente
- Turborepo com cache remoto no CI
- Limite de tempo de pipeline de PR: 10 minutos (RNF-MAN)

## Como validar

- `packages/domain` não possui nenhuma dependência de framework no `package.json`
- Teste de fronteira no CI: importação proibida quebra o build
- Cobertura de `packages/domain` ≥ 90%

## Revisitar quando

- Um módulo específico apresentar gargalo de CPU comprovado que justifique outra linguagem (candidato natural: worker de agregação de métricas)
- O time crescer a ponto de comportar especialização
