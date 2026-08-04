# US-138 · Agrupamento de entregas proximas

|  |  |
|---|---|
| **Épico** | [E-13 · Delivery Proprio](./README.md) |
| **Fase** | 4 — Delivery próprio |
| **Prioridade** | C — Could have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 4 (se houver folga) |
| **Requisitos funcionais** | RF-DEL-09 |
| **Regras de negócio** | — |
| **ADRs** | — |
| **Eventos** | — |
| **Aplicações** | web-pos, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** que o sistema sugira agrupar entregas próximas,
> **para** que eu economize deslocamento sem atrasar ninguém.

## 2. Contexto e motivação

Duas entregas no mesmo bairro em uma viagem economizam tempo e combustível. O risco é atrasar a primeira em nome da segunda — e o cliente não sabe que dividiu a viagem.

Por isso a sugestão respeita o prazo prometido de cada pedido e é sempre uma **sugestão**, aprovada por alguém. Prioridade C.

## 3. Escopo

### 3.1 Dentro desta história

- Sugestão de agrupamento por proximidade de zona
- Verificação de que o prazo de cada pedido é respeitado
- Ordem sugerida das paradas
- Aprovação manual do agrupamento
- Limite configurável de paradas por rota

### 3.2 Fora desta história

- Roteirização otimizada com mapa
- Agrupamento automático sem aprovação

## 4. Critérios de aceite

```gherkin
Funcionalidade: Agrupamento de entregas

  Cenário: Sugestão por proximidade
    Dado dois pedidos prontos para a mesma zona
    Quando a sugestão for calculada
    Então deve propor agrupá-los na mesma rota
    E deve indicar a ordem sugerida das paradas

  Cenário: Prazo preservado
    Dado dois pedidos cujo agrupamento faria o primeiro estourar o prazo
    Quando a sugestão for avaliada
    Então o agrupamento não deve ser sugerido

  Cenário: Aprovação manual
    Dado um agrupamento sugerido
    Quando o operador aprovar
    Então a rota deve ser criada com as duas paradas
    E o entregador deve ver as duas na ordem definida

  Cenário: Limite de paradas
    Dado o limite de 3 paradas por rota
    Quando houver 5 pedidos na mesma zona
    Então a sugestão deve respeitar o limite

  Cenário: Recusa da sugestão
    Dado um agrupamento sugerido
    Quando o operador recusar
    Então as entregas devem seguir separadas
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-061 | `delivery.run.started` | Rota com múltiplas paradas | courierId, stops[] | ↑ |

## 7. Contrato de API

```http
GET /v1/delivery/grouping-suggestions
→ { "suggestions": [ { "orders": ["...","..."],
                       "zone": "Centro",
                       "estimatedSavingMinutes": 8,
                       "promisesRespected": true,
                       "suggestedOrder": ["...","..."] } ] }

POST /v1/delivery/runs
{ "courierId": "...", "stops": ["...","..."] }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `delivery_run` | Rota com múltiplas paradas | `courier_id`, `stops_count` |
| `delivery_stop` | Ordem da parada | `sequence`, `run_id` |
| `delivery_zone` | Proximidade | `name`, `additional_minutes` |

## 9. Comportamento offline

Sugestão calculada na nuvem; a rota resultante é operada pelo app do entregador.

## 10. Interface e experiência

- Sugestão exibida com a economia estimada, para justificar a decisão
- Confirmação explícita de que os prazos são respeitados
- Aprovação sempre manual — agrupar sem supervisão gera atraso invisível
- Ordem das paradas ajustável antes de confirmar

## 11. Métricas, alertas e observabilidade

- Entregas agrupadas contra individuais
- Economia de tempo realizada contra estimada
- OTD de pedidos agrupados contra individuais — a validação de que não prejudica

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Sugestão respeita prazo e limite de paradas |
| Integração | Rota criada com a ordem aprovada |
| Validação | OTD de agrupados comparado ao de individuais |

## 13. Dependências

**Depende de:** US-136  
**Habilita:** —

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

- Prioridade C — só entra com folga na fase. Agrupamento mal feito atrasa o primeiro cliente sem que ninguém perceba, e é o tipo de otimização que custa mais do que economiza se feita sem cuidado.

---

*US-138 · Épico E-13 · Pacote 004_DonaBetinha · Replay Studio.*