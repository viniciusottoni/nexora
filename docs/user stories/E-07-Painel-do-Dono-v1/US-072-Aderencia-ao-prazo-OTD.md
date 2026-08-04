# US-072 · Aderencia ao prazo OTD

|  |  |
|---|---|
| **Épico** | [E-07 · Painel do Dono v1](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-BI-04 |
| **Regras de negócio** | RN-013 |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** saber que percentual dos pedidos saiu dentro do prazo prometido,
> **para** que eu meça a promessa que fiz ao cliente, não só o tempo médio.

## 2. Contexto e motivação

Tempo médio e aderência ao prazo medem coisas diferentes. Uma operação com média de 9 minutos e OTD de 70% está entregando rápido para a maioria e falhando feio com uma minoria — o que gera reclamação.

A meta declarada no PRD é **OTD ≥ 85%** na Fase 2. A v1 estabelece a medição.

## 3. Escopo

### 3.1 Dentro desta história

- Cálculo de OTD: pedidos entregues dentro do `promised_at` sobre o total
- Agrupamento por hora, dia, canal e produto
- Comparativo com a meta configurada
- Distribuição do atraso dos pedidos que estouraram o prazo
- Identificação dos produtos e faixas horárias com pior aderência

### 3.2 Fora desta história

- Prazo dinâmico calculado por fila (US-118, Fase 2)
- Recalculo de promessa em tempo real (EVT-017, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Aderência ao prazo

  Cenário: Cálculo do OTD
    Dado 100 pedidos no período, sendo 83 entregues dentro do prazo
    Quando o OTD for calculado
    Então deve ser 83%

  Cenário: Comparativo com a meta
    Dado a meta de OTD em 85%
    E o realizado em 83%
    Quando a visão for exibida
    Então o desvio deve estar sinalizado

  Cenário: Distribuição do atraso
    Dado 17 pedidos que estouraram o prazo
    Quando a distribuição for exibida
    Então deve mostrar quanto cada um atrasou
    E deve ficar claro se são atrasos pequenos ou grandes

  Cenário: Pior aderência por produto
    Dado produtos com aderências distintas
    Quando o agrupamento por produto for aplicado
    Então os de pior OTD devem aparecer no topo

  Cenário: Pedido sem prazo prometido
    Dado um pedido criado antes da existência de promessa
    Quando o OTD for calculado
    Então esse pedido deve ser excluído do cálculo
    E a contagem de excluídos deve estar visível
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-013 | O prazo informado ao cliente é calculado pela fila atual, nunca fixo | **[HIPÓTESE]** — na v1 o prazo é estimado pelo tempo de preparo cadastrado |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/metrics/otd?from=...&to=...&groupBy=hour
→ { "otd": 0.83, "target": 0.85,
    "onTime": 83, "late": 17, "excluded": 2,
    "lateDistribution": [ { "bucketMinutes": "0-5",  "count": 11 },
                          { "bucketMinutes": "5-10", "count": 4 },
                          { "bucketMinutes": "10+",  "count": 2 } ],
    "series": [...] }

GET /v1/metrics/otd?groupBy=product
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order` | Prazo e realizado | `promised_at`, `served_at`, `placed_at` |
| `metric_daily` | OTD agregado | `otd`, `orders_on_time`, `orders_late` |
| `tenant_config` | Meta de OTD | `goals.otdTarget` |

> Consulta de referência (MET-020, doc. 04): `count(*) FILTER (WHERE served_at <= promised_at) / count(*)`.

## 9. Comportamento offline

Consulta de nuvem, sobre agregados. Horários de ocorrência preservados.

## 10. Interface e experiência

- OTD como número grande, com a meta ao lado
- Distribuição do atraso em faixas, não em lista de pedidos — o gestor quer o padrão, não o caso
- Produtos com pior aderência destacados, com ação sugerida (revisar tempo cadastrado)

## 11. Métricas, alertas e observabilidade

- MET-020 — aderência ao prazo
- Evolução do OTD ao longo das semanas
- Correlação entre OTD e volume da hora, revelando o efeito do pico

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo do OTD com exclusão de pedidos sem promessa |
| Integração | Agrupamentos por hora, canal e produto |
| Validação | Conferência manual contra amostra real |

## 13. Dependências

**Depende de:** US-071  
**Habilita:** US-118

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

- OTD depende da qualidade da promessa. Se o tempo cadastrado for otimista, o OTD será artificialmente ruim — a US-016 entrega o comparativo estimado versus real justamente para calibrar isso.

---

*US-072 · Épico E-07 · Pacote 004_DonaBetinha · Replay Studio.*