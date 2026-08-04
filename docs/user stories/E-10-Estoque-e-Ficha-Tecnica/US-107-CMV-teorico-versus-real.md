# US-107 · CMV teorico versus real

|  |  |
|---|---|
| **Épico** | [E-10 · Estoque e Ficha Tecnica](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-EST-08 |
| **Regras de negócio** | — |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud, packages/metrics |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** comparar o que eu deveria ter consumido com o que realmente consumi,
> **para** que eu descubra perda e desvio que hoje são completamente invisíveis.

## 2. Contexto e motivação

É o indicador de maior retorno financeiro do produto. O **CMV teórico** é a soma dos custos calculados pelas fichas técnicas dos itens vendidos. O **CMV real** vem da equação de estoque: inicial + compras − final.

A diferença entre os dois é perda não registrada, desvio ou ficha técnica errada. É a resposta concreta a *"não se sabe quanto sobrou"*.

A meta declarada no PRD é divergência **≤ 5%** em 90 dias após a Fase 2.

## 3. Escopo

### 3.1 Dentro desta história

- Cálculo do CMV teórico a partir dos custos congelados nos itens
- Cálculo do CMV real pela equação de estoque
- Divergência em valor e percentual
- Composição da divergência por insumo
- Alerta ao gestor acima do limiar
- Série histórica da divergência
- Separação da perda registrada, para isolar a divergência não explicada

### 3.2 Fora desta história

- Margem por produto (US-109)
- Prime cost (US-124, Fase 3)

## 4. Critérios de aceite

```gherkin
Funcionalidade: CMV teórico versus real

  Cenário: Divergência acima do limiar
    Dado CMV teórico de R$ 18.420 e real de R$ 19.870
    Quando o período for apurado
    Então a divergência de 7,9% deve ser exibida
    E, acima do limiar de 5%, o gestor deve ser alertado
    E a composição por insumo deve estar disponível

  Cenário: Composição da divergência
    Dado uma divergência total apurada
    Quando a composição for exibida
    Então cada insumo deve mostrar sua contribuição
    E os de maior impacto devem aparecer primeiro

  Cenário: Perda registrada isolada
    Dado R$ 800 de perda registrada no período
    Quando a divergência for decomposta
    Então a perda registrada deve ser separada da divergência não explicada

  Cenário: Período sem contagem
    Dado um período sem contagem de estoque final
    Quando o CMV real for solicitado
    Então deve ser indicado que não é possível calcular
    E deve ser sugerida a realização de contagem

  Cenário: Série histórica
    Dado seis meses de apuração
    Quando a série for exibida
    Então deve mostrar a evolução da divergência
    E a tendência deve estar visível

  Cenário: Divergência negativa
    Dado CMV real inferior ao teórico
    Quando a divergência for apurada
    Então deve ser sinalizada como possível erro de ficha técnica
    E não como economia
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> Derivado de `stock.deducted`, `stock.received`, `stock.wasted` e `stock.counted`.

## 7. Contrato de API

```http
GET /v1/metrics/cmv?period=2026-07
→ { "theoretical": 1842000, "actual": 1987000,
    "divergence": 145000, "divergencePercent": 7.9,
    "registeredWaste": 80000,
    "unexplainedDivergence": 65000,
    "byIngredient": [ { "ingredientId": "...", "name": "Mussarela",
                        "theoretical": 420000, "actual": 478000,
                        "divergence": 58000, "sharePercent": 40.0 } ],
    "revenue": 4820000, "cmvPercent": 41.2 }

GET /v1/metrics/cmv/series?months=6
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | CMV teórico, custos congelados | `unit_cost`, `quantity` |
| `stock_movement` | Base do CMV real | `type`, `quantity`, `unit_cost` |
| `inventory_count` | Estoque inicial e final | `counted_at`, itens contados |
| `purchase` | Compras do período | `total_cost` |

> CMV teórico = `Σ order_item.unit_cost × quantity`. CMV real = estoque inicial + compras − estoque final, via `inventory_count` (ERD, seção 4).

## 9. Comportamento offline

Apuração de nuvem, sobre dados consolidados. Depende de sincronização completa do período.

## 10. Interface e experiência

- Dois números lado a lado, com a divergência em destaque
- Composição por insumo ordenada por impacto — o gestor precisa saber onde agir primeiro
- Perda registrada separada da não explicada: a primeira é conhecida, a segunda é o problema
- Divergência negativa sinalizada como suspeita de ficha errada, nunca como ganho
- Série histórica com a meta de 5% como linha de referência

## 11. Métricas, alertas e observabilidade

- CMV teórico, real e divergência por período
- CMV como percentual do faturamento
- Divergência não explicada — o número que justifica o épico
- Alerta ao gestor acima do limiar configurado

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo do CMV teórico a partir dos custos congelados |
| Unitário | Cálculo do CMV real pela equação de estoque |
| Unitário | Decomposição da divergência, isolando a perda registrada |
| Integração | Alerta disparado acima do limiar |
| Integração | Período sem contagem indica impossibilidade, sem inventar número |
| Validação | Conferência manual em um período fechado, com dados reais |

## 13. Dependências

**Depende de:** US-103, US-104, US-105, US-106  
**Habilita:** US-124

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Persona, ação e resultado estão claros
- [ ] Critérios de aceite escritos em Gherkin
- [ ] Requisito funcional (RF) e evento (EVT) referenciados
- [ ] Dependências identificadas e resolvidas
- [ ] Desenho de tela existe (quando há interface)
- [ ] Estimada pelo time
- [ ] Comportamento offline definido
- [ ] Impacto em métrica e alerta identificado

**DoD — a história só é concluída quando:**

- [ ] Código revisado e aprovado por outro desenvolvedor
- [ ] Testes unitários dos casos de negócio passando
- [ ] Teste de integração do fluxo principal passando
- [ ] Teste de isolamento multi-tenant (quando a história toca tabela com `tenant_id`)
- [ ] Eventos emitidos conforme o catálogo do documento 04
- [ ] Comportamento offline verificado (quando aplicável)
- [ ] Critérios de aceite validados em ambiente de teste pelo PO
- [ ] Sem violação do ADR-013 (proibição de código por cliente)
- [ ] Documentação atualizada (OpenAPI, catálogo de eventos, modelo de dados)
- [ ] Observabilidade instrumentada (log estruturado + traço OpenTelemetry)
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- CMV real exige contagem de estoque disciplinada. Sem contagem, o indicador não existe — é dependência de processo, não de sistema.
- Divergência alta nos primeiros meses é esperada e reflete fichas ainda imprecisas. A meta de 5% é para 90 dias após a Fase 2, não para o primeiro mês.

---

*US-107 · Épico E-10 · Pacote 004_DonaBetinha · Replay Studio.*