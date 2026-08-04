# US-075 · Ticket medio giro de mesa e ocupacao

|  |  |
|---|---|
| **Épico** | [E-07 · Painel do Dono v1](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-BI-07 |
| **Regras de negócio** | — |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** ver ticket médio, giro de mesa e ocupação do salão,
> **para** que eu saiba se o problema é falta de cliente ou cliente gastando pouco.

## 2. Contexto e motivação

Três indicadores que, juntos, explicam o faturamento do salão. Faturamento baixo com ocupação alta e ticket baixo é um problema de cardápio ou de venda sugestiva; faturamento baixo com ocupação baixa é problema de movimento.

O giro de mesa mede quantas vezes cada mesa foi usada no período — indicador que responde diretamente à pergunta de eficiência do espaço.

## 3. Escopo

### 3.1 Dentro desta história

- Ticket médio geral, por canal, por mesa e por pessoa
- Giro de mesa por dia e por ambiente
- Taxa de ocupação por faixa horária
- Tempo médio de permanência
- Faturamento por mesa e por ambiente
- Comparativo com o período anterior

### 3.2 Fora desta história

- Faturamento por metro quadrado (exige planta, fase posterior)
- Mapa de calor de demanda (US-119, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Ticket médio, giro e ocupação

  Cenário: Ticket médio por pessoa
    Dado sessões com contagem de pessoas informada
    Quando o ticket por pessoa for calculado
    Então deve ser a receita dividida pelo total de pessoas atendidas

  Cenário: Sessão sem contagem de pessoas
    Dado uma sessão sem guestCount informado
    Quando o ticket por pessoa for calculado
    Então essa sessão deve ser excluída do cálculo
    E a quantidade de excluídas deve estar visível

  Cenário: Giro de mesa
    Dado 20 mesas e 68 sessões encerradas no dia
    Quando o giro for calculado
    Então deve ser 3,4 sessões por mesa

  Cenário: Ocupação por faixa horária
    Dado o histórico de sessões abertas por hora
    Quando a ocupação for exibida
    Então cada faixa deve mostrar o percentual de mesas ocupadas
    E o pico de ocupação deve estar destacado

  Cenário: Ambiente com desempenho distinto
    Dado dois ambientes com ocupações diferentes
    Quando a visão por ambiente for exibida
    Então a diferença deve ficar evidente
    E deve incluir faturamento e giro por ambiente

  Cenário: Tempo de permanência
    Dado sessões encerradas no período
    Quando a permanência média for calculada
    Então deve usar o intervalo entre abertura e liberação da mesa
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> Derivado de `table.session.opened` (EVT-020), `table.session.closed` (EVT-023) e `table.released` (EVT-026).

## 7. Contrato de API

```http
GET /v1/metrics/tables?from=...&to=...
→ { "avgTicket": 2564, "avgTicketPerPerson": 641,
    "sessionsWithoutGuestCount": 12,
    "tableTurnover": 3.4,
    "avgDurationMinutes": 62,
    "occupancy": [ { "hour": "20:00", "occupiedPercent": 0.85 } ],
    "byArea": [ { "area": "Salão", "revenue": 318000,
                  "turnover": 3.8, "avgTicket": 2610 } ] }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `table_session` | Sessões encerradas | `opened_at`, `closed_at`, `released_at`, `guest_count`, `total` |
| `dining_table` | Denominador do giro e da ocupação | `area_id` |
| `metric_daily` | Agregados | `sessions`, `avg_session_seconds`, `avg_ticket` |

> Giro de mesa = `metric_daily.sessions ÷ count(dining_table)` (ERD, seção 4).

## 9. Comportamento offline

Consulta de nuvem, sobre agregados.

## 10. Interface e experiência

- Três indicadores na mesma tela, porque só fazem sentido juntos
- Ocupação como gráfico por faixa horária, com o pico destacado
- Comparação entre ambientes lado a lado — costuma revelar que uma área rende muito menos
- Sinalização de quantas sessões foram excluídas por falta de `guestCount`

## 11. Métricas, alertas e observabilidade

- Ticket médio geral, por canal, por mesa e por pessoa
- Giro de mesa por dia e por ambiente
- Taxa de ocupação por faixa horária
- Tempo médio de permanência

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo de giro, ocupação e permanência |
| Unitário | Exclusão de sessões sem `guestCount` do ticket por pessoa |
| Integração | Ocupação por hora reconstruída corretamente do histórico |

## 13. Dependências

**Depende de:** US-022, US-073  
**Habilita:** US-070

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

- `guestCount` não preenchido invalida o ticket por pessoa. A obrigatoriedade na abertura pelo garçom (US-022) é a mitigação; medir o percentual de preenchimento no piloto.

---

*US-075 · Épico E-07 · Pacote 004_DonaBetinha · Replay Studio.*