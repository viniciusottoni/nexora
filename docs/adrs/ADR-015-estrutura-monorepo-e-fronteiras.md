# ADR-015 · Estrutura do monorepo e fronteiras de dependência

| | |
|---|---|
| **Status** | Substituído por ADR-039 |
| **Data** | 31/07/2026 (substituído em 01/08/2026) |
| **Decisores** | Tech Lead |
| **Substituído por** | [ADR-039](ADR-039-fronteiras-por-project-reference.md) |
| **Relacionados** | ADR-002, ADR-003, ADR-013 |
| **Requisitos afetados** | RNF-MAN-02, RNF-MAN-03 |

> ⚠ **Substituído em 01/08/2026.** As fronteiras entre camadas passaram a ser impostas por `ProjectReference` do .NET (checado em tempo de compilação) e por `Nexora.ArchitectureTests`, em vez de zonas do ESLint. Ver [ADR-039](ADR-039-fronteiras-por-project-reference.md). Conteúdo abaixo mantido como registro histórico — o princípio ("domínio não importa nada") permanece válido, só a mecânica de imposição mudou.

---

## Contexto

Monorepo sem fronteiras explícitas degrada rapidamente: tudo importa tudo, o domínio passa a depender do banco, o frontend importa código de servidor e a promessa do ADR-002 (regra escrita uma vez, válida nos dois lados) se perde silenciosamente.

Fronteira que depende de disciplina humana não sobrevive a prazo apertado. Precisa ser verificada por máquina.

## Decisão

**Estrutura de pacotes com camadas e regras de importação verificadas automaticamente no CI.**

## Detalhamento

### Estrutura

```
dona-betinha/
├── apps/
│   ├── api-edge/          NestJS · servidor da loja
│   ├── api-cloud/         NestJS · nuvem
│   ├── web-admin/         React · gestão, financeiro, painel do dono
│   ├── web-pos/           React · garçom e caixa (PWA)
│   ├── web-kds/           React · cozinha (quiosque)
│   ├── web-menu/          React · cardápio e delivery (público)
│   └── web-platform/      React · painel da Replay
├── packages/
│   ├── domain/            regras puras — SEM framework, SEM I/O
│   ├── events/            schemas de evento (Zod) + catálogo tipado
│   ├── contracts/         DTOs de API compartilhados
│   ├── db/                Prisma schema, migrations, seeds, withTenant
│   ├── sync/              motor de sincronização (outbox/inbox)
│   ├── metrics/           derivação de indicadores
│   ├── ui/                design system e theming
│   └── config/            presets de ESLint, TS, Tailwind, Vitest
├── infra/
│   ├── edge/              docker-compose, install.sh, runbooks
│   ├── cloud/             IaC, manifests
│   └── scripts/           provisionamento, carga de cardápio
└── Docs/
```

### Camadas e regra de dependência

```
    apps/*            pode importar tudo abaixo
       │
   packages/db, sync, metrics, ui       infraestrutura e apresentação
       │
   packages/contracts, events           contratos
       │
   packages/domain                      NÃO importa nada acima
```

| Pacote | Pode importar | Nunca importa |
|---|---|---|
| `domain` | apenas bibliotecas puras (zod, decimal.js, date-fns) | Nest, Prisma, React, `db`, `sync`, `apps/*` |
| `events` | `domain`, zod | Nest, Prisma, React |
| `contracts` | `domain`, `events`, zod | Nest, Prisma, React |
| `db` | `domain`, `events`, Prisma | React, `apps/*` |
| `metrics` | `domain`, `events`, `db` | React, `apps/*` |
| `ui` | React, `contracts` | Nest, Prisma, `db` |
| `apps/api-*` | tudo, exceto `ui` | — |
| `apps/web-*` | `ui`, `contracts`, `domain` | `db`, `sync`, `metrics`, Nest |

> A linha mais importante: **`packages/domain` não importa nada.** É ela que garante que a regra de negócio é a mesma no edge, na nuvem e no navegador.

### Verificação automática

```js
// eslint.config.js — presets em packages/config
'import/no-restricted-paths': ['error', {
  zones: [
    { target: './packages/domain', from: './packages/db' },
    { target: './packages/domain', from: './apps' },
    { target: './apps/web-*',      from: './packages/db' },
    { target: './apps/web-*',      from: './packages/sync' },
  ],
}],
```

Complemento no CI: verificação de que `packages/domain/package.json` não lista nenhuma dependência de framework.

### Convenção de importação

```ts
import { calculateHalfAndHalfPrice } from '@db/domain';
import { orderPlacedSchema }         from '@db/events';
import { withTenant }                from '@db/db';
```

Caminhos relativos entre pacotes são proibidos.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Sem fronteiras formais | Sem atrito inicial | Degradação garantida em poucos meses | O custo aparece justamente quando não há tempo para pagar |
| Nx com boundaries por tags | Ferramental rico | Complexidade e opinião maiores | ESLint com zonas resolve com menos peso |
| Repositórios separados | Fronteira física | Versionamento cruzado permanente | Descartado no ADR-002 |
| Arquitetura hexagonal completa | Purismo | Cerimônia desproporcional ao tamanho do time | O essencial (domínio puro) já é obtido com a regra de zonas |

## Consequências

**Positivas**

- A promessa do ADR-002 é verificada por máquina, não por disciplina
- Domínio testável sem banco e sem framework — testes rápidos, cobertura alta viável
- Frontend não consegue acidentalmente puxar código de servidor
- Estrutura serve como mapa mental compartilhado do sistema

**Negativas**

- Regra pode parecer burocrática no começo
- Às vezes exige mover código para o lugar certo antes de usá-lo
- Configuração de lint e de build inicial mais trabalhosa

**Mitigações**

- Presets centralizados em `packages/config` — configurar uma vez
- Mensagem de erro do lint aponta este ADR e explica o motivo
- Scaffolding de novo pacote via script, já com as regras aplicadas

## Como validar

- Lint com zonas restritas roda em todo PR (bloqueante)
- `packages/domain/package.json` sem dependência de framework (verificação no CI)
- Cobertura de `packages/domain` ≥ 90% sem nenhum mock de infraestrutura

## Revisitar quando

- Um novo aplicativo exigir uma camada que não se encaixa na estrutura
- O time crescer a ponto de justificar divisão por times donos de pacotes
