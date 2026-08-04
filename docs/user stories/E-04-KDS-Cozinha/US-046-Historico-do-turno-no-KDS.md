# US-046 · Historico do turno no KDS

|  |  |
|---|---|
| **Épico** | [E-04 · KDS Cozinha](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | S — Should have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 4 |
| **Requisitos funcionais** | RF-KDS-14 |
| **Regras de negócio** | — |
| **ADRs** | ADR-018 |
| **Eventos** | — |
| **Aplicações** | web-kds, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** pizzaiolo (P3),
> **quero** consultar os pedidos já concluídos no turno,
> **para** que eu consiga verificar o que foi feito quando alguém questiona.

## 2. Contexto e motivação

Situação frequente: o garçom diz que a pizza da mesa 12 não saiu, e a cozinha tem certeza de que saiu. Sem histórico, a discussão não tem resolução.

O histórico também serve à própria cozinha como referência de ritmo: quantos pedidos já saíram e em que tempo médio.

## 3. Escopo

### 3.1 Dentro desta história

- Lista de itens concluídos no turno, ordenada do mais recente
- Busca por código curto e por mesa
- Exibição dos carimbos de tempo de cada item
- Contagem e tempo médio do turno
- Delimitação pelo dia operacional (ADR-018)

### 3.2 Fora desta história

- Relatórios gerenciais (E-07)
- Reimpressão de comanda
- Refazimento de item (Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Histórico do turno

  Cenário: Consulta por código
    Dado um item concluído com código curto 47
    Quando o operador buscar por 47 no histórico
    Então deve ver o item com todos os seus carimbos de tempo

  Cenário: Delimitação pelo dia operacional
    Dado o dia operacional virando às 5h
    E um item concluído às 00h40
    Quando o histórico for consultado às 01h00
    Então o item deve aparecer no turno corrente

  Cenário: Resumo do turno
    Dado 84 itens concluídos no turno
    Quando o histórico for aberto
    Então deve exibir a contagem e o tempo médio de produção do turno

  Cenário: Busca por mesa
    Quando o operador buscar pela mesa 12
    Então deve ver todos os itens daquela mesa no turno
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/kds/history?shift=current&stationId=...
→ { "items": [ { "shortCode": "47", "productName": "...", "table": "12",
                 "firedAt": "...", "readyAt": "...", "prepSeconds": 546,
                 "operator": {...} } ],
    "summary": { "count": 84, "avgPrepSeconds": 612 } }

GET /v1/kds/history?shift=current&search=47
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Itens concluídos | `status` em READY/SERVED, `business_day`, carimbos |
| `tenant_config` | Virada do dia operacional | `operation.businessDayStartHour` |

## 9. Comportamento offline

Integralmente local, consultando o PostgreSQL do edge.

## 10. Interface e experiência

- Busca por código curto como caminho principal — é o número que todo mundo usa para falar do pedido
- Navegação por teclado, sem exigir mouse
- Saída do histórico de volta à fila em uma tecla — o operador não pode ficar preso na consulta

## 11. Métricas, alertas e observabilidade

- Contagem de consultas ao histórico — alta indica atrito de comunicação entre salão e cozinha
- Produção por operador e por turno

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Delimitação correta pelo dia operacional configurável |
| Integração | Busca por código e por mesa |
| Integração | Resumo do turno bate com a contagem real |

## 13. Dependências

**Depende de:** US-041  
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

—

---

*US-046 · Épico E-04 · Pacote 004_DonaBetinha · Replay Studio.*