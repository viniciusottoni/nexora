# US-137 · Aviso de pedido proximo de sair

|  |  |
|---|---|
| **Épico** | [E-13 · Delivery Proprio](./README.md) |
| **Fase** | 4 — Delivery próprio |
| **Prioridade** | S — Should have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Fase 4 |
| **Requisitos funcionais** | RF-DEL-08 |
| **Regras de negócio** | RN-003 |
| **ADRs** | — |
| **Eventos** | — |
| **Aplicações** | web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** entregador (P5),
> **quero** ser avisado quando meu próximo pedido estiver quase pronto,
> **para** que eu não fique esperando parado nem chegue atrasado.

## 2. Contexto e motivação

Resolve os dois lados do desperdício de tempo no despacho: entregador parado esperando e pedido pronto esfriando à espera do entregador.

O aviso usa o tempo de preparo restante estimado para antecipar a chamada.

## 3. Escopo

### 3.1 Dentro desta história

- Aviso ao entregador quando o pedido está a X minutos de ficar pronto
- Limiar configurável
- Aviso à operação quando não há entregador disponível
- Fila de despacho ordenada por previsão de prontidão

### 3.2 Fora desta história

- Agrupamento de entregas (US-138)
- Convocação automática de entregador terceirizado

## 4. Critérios de aceite

```gherkin
Funcionalidade: Aviso de pedido próximo de sair

  Cenário: Aviso antecipado
    Dado o limiar configurado em 5 minutos
    Quando um pedido estiver a 5 minutos de ficar pronto
    Então o entregador designado deve ser avisado

  Cenário: Sem entregador disponível
    Dado um pedido próximo de ficar pronto e nenhum entregador livre
    Quando o limiar for atingido
    Então a operação deve ser alertada

  Cenário: Fila de despacho
    Dado vários pedidos em produção
    Quando a fila de despacho for exibida
    Então deve estar ordenada pela previsão de prontidão

  Cenário: Pedido antecipado
    Dado um pedido que ficou pronto antes do previsto
    Quando isso ocorrer
    Então o aviso deve ser disparado imediatamente
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-003 | Cada transição gera alerta aos perfis envolvidos | Entregador e operação alertados |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
# WebSocket, sala role:courier e user:{courierId}:
{ "type": "alert.raised",
  "data": { "alertType": "ORDER_ALMOST_READY",
            "orderId": "...", "code": "A47",
            "estimatedReadyInMinutes": 5 } }

PATCH /v1/tenant/config
{ "delivery": { "courierNotifyMinutesBefore": 5 } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Previsão de prontidão | `fire_at`, `prep_minutes`, `status` |
| `delivery_stop` | Entregador designado | `courier_id` |
| `tenant_config` | Limiar | `delivery.courierNotifyMinutesBefore` |

## 9. Comportamento offline

Alerta local, pelo WebSocket do edge, quando o entregador está na loja.

## 10. Interface e experiência

- Aviso curto e direto, com o código do pedido
- Fila de despacho visível ao operador, ordenada por previsão
- Alerta de ausência de entregador para a operação, não para o cliente

## 11. Métricas, alertas e observabilidade

- Tempo de despacho (pronto até saída) antes e depois do aviso
- Pedidos que ficaram prontos sem entregador disponível

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Aviso disparado no limiar configurado |
| Integração | Alerta à operação quando não há entregador |

## 13. Dependências

**Depende de:** US-136, US-115  
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

*US-137 · Épico E-13 · Pacote 004_DonaBetinha · Replay Studio.*