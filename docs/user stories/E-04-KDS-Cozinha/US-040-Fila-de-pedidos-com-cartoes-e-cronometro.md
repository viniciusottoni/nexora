# US-040 · Fila de pedidos com cartoes e cronometro

|  |  |
|---|---|
| **Épico** | [E-04 · KDS Cozinha](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 13 pontos |
| **Sprint sugerida** | Sprint 4 |
| **Requisitos funcionais** | RF-KDS-02, RF-KDS-03, RF-KDS-05 |
| **Regras de negócio** | RN-002 |
| **ADRs** | ADR-011, ADR-012 |
| **Eventos** | EVT-004 |
| **Aplicações** | web-kds, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** pizzaiolo (P3),
> **quero** ver a fila de pedidos com o tempo de cada um correndo na tela,
> **para** que eu saiba o que fazer primeiro e quanto tempo já se passou.

## 2. Contexto e motivação

É o coração do épico e a tela mais exigente do produto em termos de restrição física. O operador está a 1,5 metro do monitor, com farinha nas mãos, sob calor e ruído, com fila de pedidos crescendo.

O cronômetro atende diretamente à declaração do cliente: *"eu vou saber quantos minutos a minha pizza tá sendo feita"*. O escalonamento de cor transforma o número em ação: verde é normal, amarelo é atenção, vermelho é atraso.

Os limiares vêm do produto (US-016), com herança do padrão do tenant. Calibrá-los mal é o erro mais comum: se tudo fica vermelho, a cozinha aprende a ignorar a cor e o indicador morre.

## 3. Escopo

### 3.1 Dentro desta história

- Grade de cartões, um por pedido, ordenada por chegada
- Cronômetro por pedido, correndo desde T0
- Escalonamento de cor por limiares configuráveis (normal, atenção, crítico)
- Exibição de código curto, mesa, canal, itens, modificadores e observações
- Estados do item: `QUEUED`, `FIRED`, `IN_OVEN`, `OUT_OF_OVEN`, `READY`
- Atualização em tempo real por WebSocket
- Layout responsivo por quantidade de pedidos na fila
- Modo quiosque em tela cheia, sem barra de navegador

### 3.2 Fora desta história

- Avanço de estado (US-041)
- Filtro por praça (US-042)
- Contagem all-day (US-043)
- Prioridade dinâmica e fire time (E-11, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Fila do KDS

  Cenário: Escalonamento de cor
    Dado limiares de 12 min (atenção) e 18 min (crítico)
    Quando um pedido ultrapassar 12 minutos
    Então o cartão deve ficar amarelo
    E ao ultrapassar 18 minutos deve ficar vermelho com alerta sonoro

  Cenário: Legibilidade
    Dado o KDS em monitor de 21 polegadas
    Quando houver 12 pedidos na fila
    Então o texto do produto deve ser legível a 1,5 metro de distância

  Cenário: Chegada de pedido novo
    Dado a fila com 5 pedidos
    Quando um novo pedido for confirmado no salão
    Então o cartão deve aparecer em até 2 segundos
    E deve entrar na posição correta pela ordem de chegada

  Cenário: Informação completa no cartão
    Dado um item com modificadores e observação
    Quando o cartão for exibido
    Então deve mostrar código curto, mesa, produto, quantidade,
         modificadores e observação
    E remoções devem aparecer em destaque

  Cenário: Meio a meio no cartão
    Dado um item com duas frações
    Quando o cartão for exibido
    Então deve mostrar "Pizza G · Mussarela / Calabresa"
    E os dois sabores devem ser legíveis sem abrir detalhe

  Cenário: Fila crescente
    Dado a fila passando de 12 para 20 pedidos
    Quando a tela reorganizar
    Então os cartões devem reduzir de tamanho mantendo a legibilidade mínima
    E o pedido mais antigo deve continuar visível

  Cenário: Cronômetro correto após operação offline
    Dado um pedido criado às 20h03 com a loja offline
    Quando o cartão for exibido às 20h10
    Então o cronômetro deve marcar 7 minutos, não zero

  Cenário: Item cancelado
    Dado um item na fila que foi cancelado no salão
    Quando o cancelamento chegar
    Então o cartão deve sumir da fila em até 2 segundos
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-002 | A cozinha registra obrigatoriamente início e conclusão de cada item | A fila é onde essa marcação acontece |
| RN-003 | Cada transição gera alerta aos perfis envolvidos | Atraso crítico dispara alerta sonoro e visual |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-004 | `order.item.queued` | Item entra na fila | stationId, position | ↑ |

> Esta história consome os eventos do ciclo; a emissão de T1 a T4 acontece na US-041.

## 7. Contrato de API

```http
GET /v1/kds/queue?stationId=...
→ {
    "items": [
      { "orderItemId": "...", "orderCode": "A47", "shortCode": "47",
        "productName": "Pizza G Mussarela / Calabresa",
        "quantity": 1, "modifiers": ["sem cebola"], "notes": "bem assada",
        "status": "QUEUED",
        "placedAt": "...", "fireAt": null, "elapsedSeconds": 214,
        "thresholdState": "WARNING",
        "table": "12", "channel": "DINE_IN" }
    ],
    "asOf": "..."
  }

# WebSocket:
{ "type": "order.placed",       "data": { ... } }
{ "type": "order.item.cancelled", "data": { "orderItemId": "..." } }
```

> `elapsedSeconds` vem do servidor, e o cliente apenas incrementa localmente — assim o cronômetro fica correto mesmo se a aba ficar suspensa.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Item da fila com carimbos | `status`, `placed_at`, `station_id`, `notes` |
| `order` | Contexto do pedido | `short_code`, `channel`, `session_id` |
| `product_variant` | Limiares de tempo | `prep_minutes`, `warn_minutes`, `critical_minutes` |
| `tenant_config` | Limiares padrão | `thresholds.orderWarnMinutes`, `thresholds.orderCriticalMinutes` |

## 9. Comportamento offline

Integralmente local. O KDS é a aplicação que **mais** depende de funcionar offline: se a cozinha parar, a loja para.

O cronômetro usa `placed_at` (que é `occurredAt`), não o horário de chegada da mensagem — um pedido criado durante uma queda de LAN aparece com o tempo já decorrido, não zerado.

Recarregar a página não perde nada: o estado vem do servidor local a cada carga.

## 10. Interface e experiência

- **Legibilidade a 1,5 m é requisito medido, não estimado** — validar em monitor real com a equipe
- Alto contraste; cor nunca é a única portadora de informação (o tempo em números acompanha a cor)
- Modo quiosque em tela cheia, sem barra de navegador nem elemento clicável acidental
- Nenhuma animação que distraia; transição de cor suave, sem piscar
- Cartão mais antigo sempre visível, mesmo com fila longa
- Zero digitação livre em qualquer ponto da tela

## 11. Métricas, alertas e observabilidade

- Tempo de fila (MET-001) e tempo de produção (MET-007) por item e por praça
- Distribuição de itens por estado de limiar (verde, amarelo, vermelho) por faixa horária
- Tamanho médio e máximo da fila por hora — insumo de dimensionamento de cozinha
- Alerta ao gestor quando o tempo médio da última hora ultrapassar a meta (RF-ALT-01)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo de estado de limiar com herança do padrão do tenant |
| Integração | Cartão aparece em menos de 2 s após a confirmação do pedido |
| Integração | Cronômetro correto para pedido criado offline |
| Integração | Item cancelado some da fila |
| Desempenho | Fila com 40 itens mantém a interface fluida no hardware de referência |
| Usabilidade | Teste presencial de legibilidade a 1,5 m com 12 pedidos |
| Caos offline | Fila correta e reativa com a internet da loja derrubada |

## 13. Dependências

**Depende de:** US-031, US-016  
**Habilita:** US-041, US-042, US-043, US-045

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
- [ ] Validação presencial com a equipe de cozinha antes de considerar concluída

## 15. Riscos, premissas e pendências

- **Risco de hardware** — o monitor e o teclado numérico do KDS ainda não foram definidos. Validar na Sprint 0, porque o desenho da tela depende do tamanho real.
- Fila muito longa (acima de 30 itens) exige decisão de produto sobre paginação versus redução de cartão. O modo pico (US-047) endereça isso.

---

*US-040 · Épico E-04 · Pacote 004_DonaBetinha · Replay Studio.*