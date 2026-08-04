# US-073 · Faturamento com comparativo

|  |  |
|---|---|
| **Épico** | [E-07 · Painel do Dono v1](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-BI-05 |
| **Regras de negócio** | RN-020 |
| **ADRs** | ADR-012, ADR-018 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** ver o faturamento do dia, da semana e do mês, sempre comparado,
> **para** que eu saiba se está bom ou ruim, não apenas quanto foi.

## 2. Contexto e motivação

Diretriz de desenho do painel (Visão Geral, 7.8): *comparativo sempre presente — número solto não gera decisão; número contra período anterior ou meta, sim*.

O comparativo relevante para restaurante não é o dia anterior, e sim o **mesmo dia da semana**: comparar sábado com sexta não diz nada; comparar sábado com a média dos últimos quatro sábados, sim.

O dia operacional (ADR-018) importa aqui: faturamento de sábado inclui o que foi vendido às 00h40 de domingo.

## 3. Escopo

### 3.1 Dentro desta história

- Faturamento por dia, semana e mês
- Comparativo com o mesmo período anterior e com a média do mesmo dia da semana
- Variação absoluta e percentual
- Série histórica com tendência
- Delimitação pelo dia operacional configurado
- Faturamento bruto e líquido (descontando taxa de cartão)

### 3.2 Fora desta história

- Venda por canal, produto e categoria (US-074)
- Resultado e margem (Fase 2 e 3)
- Metas configuráveis (RF-BI-10, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Faturamento com comparativo

  Cenário: Faturamento do dia comparado
    Dado o faturamento de hoje em R$ 4.820,00
    E a média das últimas quatro sextas em R$ 4.290,00
    Quando o painel for exibido
    Então deve mostrar variação de +12,4%

  Cenário: Delimitação pelo dia operacional
    Dado o dia operacional virando às 5h
    E R$ 380,00 vendidos entre 00h e 00h40 de domingo
    Quando o faturamento de sábado for apurado
    Então os R$ 380,00 devem estar incluídos em sábado

  Cenário: Faturamento líquido
    Dado R$ 4.820,00 brutos com R$ 96,00 de taxa de cartão
    Quando o líquido for exibido
    Então deve ser R$ 4.724,00
    E a diferença deve estar identificada como custo de taxa

  Cenário: Série histórica
    Dado 90 dias de operação
    Quando a série for exibida
    Então deve mostrar a tendência
    E os dias da semana devem ser distinguíveis

  Cenário: Período sem operação
    Dado um dia em que a loja não abriu
    Quando entrar na série
    Então deve aparecer como fechado, não como faturamento zero
    E não deve entrar no cálculo da média
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-020 | Métrica usa `ocorrido_em` | Faturamento atribuído ao dia operacional de ocorrência |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> Derivado de `payment.registered` (EVT-032). Reação normativa: pagamento gera receita e ticket na métrica.

## 7. Contrato de API

```http
GET /v1/metrics/revenue?period=day&date=2026-07-31
→ { "gross": 482000, "net": 472400, "cardFees": 9600,
    "comparison": { "sameWeekdayAvg": 429000, "variancePercent": 12.4,
                    "previousPeriod": 451000 },
    "orders": 184, "asOf": "..." }

GET /v1/metrics/revenue?period=month&date=2026-07
GET /v1/metrics/revenue/series?from=...&to=...&granularity=day
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `metric_daily` | Faturamento agregado | `business_day`, `revenue`, `net_revenue`, `orders`, `card_fees` |
| `payment` | Fonte, com valor líquido gerado | `amount`, `net_amount`, `fee_amount` |
| `tenant_config` | Virada do dia operacional | `operation.businessDayStartHour` |

## 9. Comportamento offline

Consulta de nuvem. Dias operados offline aparecem corretamente após a sincronização, pelo `business_day` materializado.

## 10. Interface e experiência

- Número principal grande; comparativo logo abaixo, com cor e seta
- Bruto e líquido lado a lado — a taxa de cartão costuma ser invisível ao dono
- Série histórica com os dias da semana distinguíveis, para que o padrão semanal apareça
- Dia sem operação claramente marcado, nunca como zero

## 11. Métricas, alertas e observabilidade

- Faturamento bruto e líquido por dia, semana e mês
- Variação contra o mesmo dia da semana
- Custo total de taxa de cartão — insumo direto do financeiro (RF-FIN-10)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo do comparativo com o mesmo dia da semana, excluindo dias fechados |
| Unitário | Delimitação pelo dia operacional, incluindo virada após a meia-noite |
| Integração | Faturamento bate com a soma dos pagamentos |
| Integração | Líquido descontando corretamente as taxas por provedor |

## 13. Dependências

**Depende de:** US-052, US-064  
**Habilita:** US-070, US-074, US-127

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

- Média de comparação com poucos dias de histórico é instável. Nos primeiros 30 dias, sinalizar que a base de comparação é curta.

---

*US-073 · Épico E-07 · Pacote 004_DonaBetinha · Replay Studio.*