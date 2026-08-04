# US-071 · Tempos por etapa com media e p90

|  |  |
|---|---|
| **Épico** | [E-07 · Painel do Dono v1](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-BI-02, RF-BI-03 |
| **Regras de negócio** | RN-020 |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud, packages/metrics |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** ver quanto tempo leva cada etapa do pedido, com média e p90,
> **para** que eu descubra onde está o gargalo em vez de dar tiro no escuro.

## 2. Contexto e motivação

Responde às duas declarações mais diretas da descoberta: *"eu dou um tiro no escuro pois não sei quais etapas hoje são mais rápidas e mais lentas"* e *"eu vou saber quantos minutos a minha pizza tá sendo feita"*.

A escolha por **média e p90** é deliberada. A média esconde o cliente que esperou 40 minutos; o p90 mostra. Um p90 muito acima da média significa que a operação é inconsistente — diagnóstico completamente diferente de uma operação uniformemente lenta.

As sete métricas de tempo derivam diretamente dos seis carimbos da US-032.

## 3. Escopo

### 3.1 Dentro desta história

- MET-001 a MET-007: fila, montagem, cocção, finalização, expedição, produção e total
- Média e percentil 90 de cada etapa
- Agrupamento por hora, dia, canal e produto
- Comparativo com o período anterior
- Comparativo com a meta configurada
- Visualização que evidencia qual etapa domina o tempo total

### 3.2 Fora desta história

- Aderência ao prazo (US-072)
- Drill-down até o pedido (US-076)
- Mapa de calor de demanda (US-119, Fase 2)
- Indicador de ocupação do gargalo (US-117, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Tempos por etapa

  Cenário: Decomposição do tempo total
    Dado pedidos concluídos no período
    Quando o gestor abrir a visão de tempos
    Então deve ver fila, montagem, cocção, finalização e expedição separadamente
    E a soma das etapas deve corresponder ao tempo total

  Cenário: Média e p90
    Dado 200 pedidos no período
    Quando os tempos forem calculados
    Então deve ser exibida a média e o percentil 90 de cada etapa
    E a diferença entre os dois deve ser visualmente perceptível

  Cenário: Identificação do gargalo
    Dado tempo de fila muito superior às demais etapas
    Quando a visão for exibida
    Então a etapa dominante deve estar destacada
    E deve ficar claro que ela responde pela maior parte do tempo total

  Cenário: Agrupamento por hora
    Dado o filtro de agrupamento por hora
    Quando a série for gerada
    Então cada faixa horária deve trazer contagem, média e p90

  Cenário: Comparativo com a meta
    Dado a meta de 10 minutos para o tempo total do salão
    Quando a visão for exibida
    Então o realizado deve ser comparado à meta
    E o desvio deve estar sinalizado

  Cenário: Item sem passagem pelo gargalo
    Dado itens que não passam pelo forno
    Quando os tempos forem calculados
    Então as etapas de cocção devem ser omitidas para esses itens
    E o tempo total deve permanecer correto

  Cenário: Horário correto após operação offline
    Dado pedidos feitos offline entre 20h e 21h
    Quando a série por hora for gerada após a sincronização
    Então eles devem aparecer na faixa das 20h
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-020 | Métrica de horário usa sempre `ocorrido_em` | Toda a série temporal é construída sobre `occurred_at` |
| RN-002 | A cozinha registra obrigatoriamente início e conclusão | Sem T1 e T4 não há tempo de produção |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> Derivado dos carimbos gravados pelos eventos EVT-002, EVT-005 a EVT-009.

## 7. Contrato de API

```http
GET /v1/metrics/times?from=...&to=...&groupBy=hour&channel=DINE_IN
→ { "series": [ { "hour": "2026-07-31T20:00:00Z",
                  "orders": 24,
                  "avgSeconds": 640, "p90Seconds": 980,
                  "stages": { "queue":    { "avg": 214, "p90": 420 },
                              "assembly": { "avg": 96,  "p90": 140 },
                              "cook":     { "avg": 240, "p90": 320 },
                              "finish":   { "avg": 30,  "p90": 45 },
                              "serve":    { "avg": 60,  "p90": 180 } },
                  "otd": 0.83 } ],
    "targetSeconds": 600,
    "asOf": "...", "syncDelaySeconds": 4 }

GET /v1/metrics/times?groupBy=product
GET /v1/metrics/times?groupBy=station
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `metric_hourly` | Agregados por hora | `bucket_hour`, `avg_*_seconds`, `p90_*_seconds`, `orders` |
| `order_item` | Fonte dos carimbos, para recálculo | Os seis carimbos e `business_day` |
| `tenant_config` | Metas configuradas | `thresholds.targetTotalMinutes` |

> A consulta de referência está no documento 04, seção 6.2 (MET-006): `percentile_cont(0.9)` sobre a diferença entre `served_at` e `placed_at`, agrupado por hora e canal.

## 9. Comportamento offline

Consulta de nuvem. Os dados dependem da sincronização, mas os **horários** são os de ocorrência — é o que torna a série confiável mesmo após operação offline (US-064).

O recálculo noturno corrige agregados afetados por eventos que chegaram atrasados.

## 10. Interface e experiência

- Visualização que mostra a composição do tempo total por etapa, não cinco gráficos separados
- Média e p90 no mesmo gráfico — a distância entre os dois é a informação mais útil
- Meta como linha de referência, sempre visível
- Etapa dominante destacada automaticamente, sem o gestor precisar comparar números
- Toque em qualquer ponto leva ao drill-down (US-076)

## 11. Métricas, alertas e observabilidade

- MET-001 tempo de fila, MET-002 montagem, MET-003 cocção, MET-004 finalização, MET-005 expedição
- MET-006 tempo total, MET-007 tempo de produção
- Evolução do p90 ao longo das semanas — mede se a operação está ficando mais consistente

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo de média e percentil 90 com amostras pequenas e grandes |
| Unitário | Decomposição correta para itens com e sem passagem pelo gargalo |
| Integração | Soma das etapas corresponde ao tempo total |
| Integração | Série por hora usa `occurred_at`, não `recorded_at` |
| Desempenho | Consulta de 30 dias em menos de 3 s |
| Validação | Números conferidos manualmente contra uma amostra de pedidos reais |

## 13. Dependências

**Depende de:** US-032, US-064  
**Habilita:** US-070, US-072, US-076, US-117

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

- Amostra pequena torna o p90 instável. Exibir a contagem junto com o percentil e sinalizar quando a amostra for insuficiente.
- Se algum carimbo não estiver sendo gravado, a etapa correspondente fica invisível — o teste de cobertura de eventos (US-060) é a proteção.

---

*US-071 · Épico E-07 · Pacote 004_DonaBetinha · Replay Studio.*