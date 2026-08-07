# US-064 · Preservacao do horario de ocorrencia

|  |  |
|---|---|
| **Épico** | [E-06 · Sincronizacao Local-Nuvem](./README.md) — ❌ **CANCELADA** |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 6 |
| **Requisitos funcionais** | RF-OFF-04 |
| **Regras de negócio** | RN-020 |
| **ADRs** | ADR-018, ADR-034, ADR-035 |
| **Eventos** | — |
| **Aplicações** | api-edge, api-cloud, packages/events |
| **Autoridade do dado** | Local (origem do horário) |

---

> ❌ **Cancelada em 06/08/2026.** Mudança de foco de negócio: o produto passa a operar 100% online, sem edge nem sincronização (ver [ADR-040](../../adrs/ADR-040-arquitetura-100-online-api-unica.md) e [E-16](../E-16-iMenu-Online/README.md)). Conteúdo mantido como registro histórico.

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** que o relatório mostre o pedido no horário em que ele realmente aconteceu,
> **para** que eu confie no mapa de calor do meu pico.

## 2. Contexto e motivação

É a regra que mantém a métrica válida em uma arquitetura offline-first. Sem ela, um pedido feito às 20h03 durante uma queda de internet apareceria no relatório às 21h15 — e o gestor tomaria decisão de escala com base em um pico que não existiu.

A RN-020 é categórica: *métrica de horário usa sempre `ocorrido_em`, nunca o horário de sincronização*. E a regra R2 do catálogo de eventos reforça: *`occurredAt` é o horário do fato; toda métrica de tempo usa este campo*.

Duas complicações práticas: relógio de dispositivo desvia (ADR-034) e dia operacional não coincide com dia civil (ADR-018).

## 3. Escopo

### 3.1 Dentro desta história

- `occurred_at` obrigatório em todo evento, originado no dispositivo ou no edge
- `recorded_at` atribuído na nuvem, distinto e nunca sobrescrevendo o anterior
- Correção de desvio de relógio do dispositivo, com registro do ajuste aplicado
- `business_day` materializado conforme a regra de virada configurada
- Particionamento de `domain_event` por `occurred_at`
- Recálculo noturno do dia anterior, corrigindo agregados afetados por eventos atrasados

### 3.2 Fora desta história

- Agregação de métricas propriamente dita (E-07)
- Indicador de atraso na interface (US-065)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Preservação do horário de ocorrência

  Cenário: Métrica após sincronização atrasada
    Dado um pedido feito às 20h03 offline
    E sincronizado às 21h15
    Quando o relatório por faixa horária for gerado
    Então o pedido deve ser contabilizado às 20h

  Cenário: Distinção entre os dois horários
    Dado o evento sincronizado
    Quando for consultado na nuvem
    Então occurredAt deve ser 20h03 e recordedAt deve ser 21h15
    E nenhum processo deve sobrescrever occurredAt

  Cenário: Relógio do dispositivo adiantado
    Dado um dispositivo com relógio 4 minutos à frente do edge
    Quando enviar X-Occurred-At
    Então o edge deve corrigir pelo desvio conhecido do dispositivo
    E deve registrar o ajuste aplicado para diagnóstico

  Cenário: Desvio extremo rejeitado
    Dado um dispositivo com relógio 6 horas divergente
    Quando enviar um evento
    Então o desvio deve ser tratado como anomalia
    E o horário do edge deve ser usado, com registro explícito da substituição

  Cenário: Dia operacional após a meia-noite
    Dado o dia operacional configurado para virar às 5h
    E um pedido feito às 00h40
    Quando o fechamento do dia for apurado
    Então o pedido deve pertencer ao dia operacional anterior

  Cenário: Particionamento correto
    Dado um evento ocorrido em julho e sincronizado em agosto
    Quando for gravado na nuvem
    Então deve ficar na partição de julho

  Cenário: Recálculo noturno
    Dado agregados do dia anterior calculados antes da chegada de eventos atrasados
    Quando o job noturno executar
    Então o dia anterior deve ser recalculado por completo
    E os agregados devem refletir todos os eventos, inclusive os atrasados
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-020 | Métrica de horário usa sempre `ocorrido_em`, nunca o horário de sincronização | É o objeto desta história — regra categórica, sem exceção |
| RN-004 | Toda ação registra autor, horário e dispositivo | O horário registrado é o de ocorrência |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> Aplica-se a **todos** os eventos do catálogo. É contrato transversal, não comportamento de um evento específico.

## 7. Contrato de API

```http
# Envio pelo dispositivo:
X-Occurred-At: 2026-07-31T20:03:12.334Z

# Evento resultante:
{
  "id": "01919e2a-...",
  "type": "order.placed",
  "occurredAt": "2026-07-31T20:03:12.334Z",   // horário do FATO
  "recordedAt": "2026-07-31T21:15:03.812Z",   // chegada na nuvem
  "deviceSeq": 148223,
  "origin": "EDGE",
  "clockSkewMs": 240000                        // ajuste aplicado, se houve
}

# Consulta por período usa sempre occurred_at:
GET /v1/metrics/times?from=...&to=...&groupBy=hour
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `domain_event` | Os dois horários, particionada por ocorrência | `occurred_at`, `recorded_at`, `clock_skew_ms` |
| `order` / `order_item` | Carimbos e dia operacional | `placed_at`, `business_day` |
| `device` | Desvio de relógio conhecido | `clock_skew_ms`, `skew_measured_at` |
| `metric_hourly` / `metric_daily` | Agregados por ocorrência | `bucket_hour`, `business_day` |

> `business_day` é materializado na gravação, nunca calculado em consulta — consulta por período não pode depender de função em tempo de execução (ADR-018, decisão 4 do ERD).

## 9. Comportamento offline

Esta história **é** o que torna a métrica confiável em operação offline. Sem ela, toda a camada de inteligência de gestão seria inválida sempre que houvesse queda de internet — e a queda de internet é justamente a premissa do produto.

O princípio 6 da Visão Geral (14.2) complementa: o painel do dono reflete o atraso de sincronização de forma explícita, nunca apresentando dado defasado como se fosse tempo real (US-065).

## 10. Interface e experiência

- Sem interface própria
- Efeito visível: relatórios corretos por faixa horária mesmo após operação offline prolongada

## 11. Métricas, alertas e observabilidade

- Distribuição do intervalo entre `occurred_at` e `recorded_at` por instalação
- Desvio de relógio por dispositivo
- Contagem de correções e de substituições de horário aplicadas
- Contagem de recálculos noturnos que alteraram agregados já publicados

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo de `business_day` com virada configurável, incluindo horário de verão |
| Unitário | Correção de desvio de relógio, adiantado e atrasado |
| Integração | Pedido offline às 20h03 sincronizado às 21h15 é contabilizado às 20h |
| Integração | Particionamento por `occurred_at`, não por `recorded_at` |
| Integração | Recálculo noturno corrige agregados afetados por eventos atrasados |
| Propriedade | Nenhum processo do sistema sobrescreve `occurred_at` |

## 13. Dependências

**Depende de:** US-032, US-062  
**Habilita:** US-071, US-073, US-119

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
- [ ] Teste específico de métrica após 6 horas de operação offline

## 15. Riscos, premissas e pendências

- É a regra mais fácil de violar por engano: basta um `now()` no lugar de `occurred_at` em uma consulta. Teste de propriedade e revisão de código específica são a proteção.
- Horário de verão e mudança de fuso exigem tratamento explícito no cálculo do dia operacional.

---

*US-064 · Épico E-06 · Pacote 004_DonaBetinha · Replay Studio.*