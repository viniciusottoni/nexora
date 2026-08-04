# US-030 · Criar pedido com itens modificadores e fracoes

|  |  |
|---|---|
| **Épico** | [E-03 · Pedido e Roteamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 13 pontos |
| **Sprint sugerida** | Sprint 2 |
| **Requisitos funcionais** | RF-PED-01, RF-PED-08, RF-SAL-03, RF-SAL-04 |
| **Regras de negócio** | RN-001, RN-004 |
| **ADRs** | ADR-020, ADR-017, ADR-034 |
| **Eventos** | EVT-001, EVT-002, EVT-003 |
| **Aplicações** | web-menu, web-pos, api-edge, packages/domain |
| **Autoridade do dado** | Local — o pedido nasce na loja e a nuvem apenas o lê |

---

## 1. História

> **Como** cliente do salão (P1) e garçom (P2),
> **quero** montar e enviar um pedido com itens, adicionais, frações e observações,
> **para** que o que eu pedi chegue exatamente como pedi, sem papel no meio do caminho.

## 2. Contexto e motivação

É a história mais importante do MVP. Tudo o que vem depois — KDS, caixa, métrica, estoque, financeiro — consome o que é criado aqui.

A regra de autoridade é clara (doc. 02, 2.1): **pedido é criado no local e apenas lido na nuvem**. Isso elimina qualquer possibilidade de conflito de sincronização neste domínio.

O ponto mais sensível é a idempotência. Em rede instável, o garçom toca "enviar", perde o sinal e toca de novo. Sem idempotência, a cozinha recebe duas pizzas — e o cliente recebe uma conta errada. O documento 05 chama isso de inegociável, e é.

## 3. Escopo

### 3.1 Dentro desta história

- Rascunho (`DRAFT`) e confirmação (`PLACED`) do pedido
- Itens com quantidade, variação, modificadores, frações e observação livre
- Cálculo de preço por canal, com adicionais e regra de fração
- Validação de regras de modificador (mínimo, máximo, obrigatório)
- Idempotência por `Idempotency-Key`, com resposta guardada por 24 h
- Preservação de `X-Occurred-At` enviado pelo dispositivo
- Cálculo do prazo estimado inicial (`promisedAt`)
- Acréscimo de itens a pedido já confirmado
- Código curto do pedido para uso no KDS

### 3.2 Fora desta história

- Roteamento às praças e notificação (US-031)
- Cancelamento (US-033)
- Prazo dinâmico por fila (US-118, Fase 2)
- Baixa de estoque (US-103, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Criação de pedido

  Cenário: Pedido do cliente na mesa
    Dado um cliente com itens no carrinho
    Quando confirmar o pedido
    Então o pedido deve ser criado com status PLACED
    E o evento order.placed deve ser emitido com occurredAt
    E o prazo estimado deve ser retornado
    E cada item deve entrar em QUEUED na praça correta

  Cenário: Reenvio por instabilidade de rede
    Dado que o cliente tocou "enviar" duas vezes por falha de sinal
    Quando a segunda requisição chegar com a mesma Idempotency-Key
    Então deve retornar o mesmo pedido, sem duplicar
    E a resposta deve trazer o header Idempotent-Replay: true

  Cenário: Pedido pelo celular do garçom
    Dado o garçom autenticado por PIN em dispositivo registrado
    Quando lançar o pedido pela mesa 12
    Então o pedido deve registrar o garçom como autor
    E o comportamento deve ser idêntico ao do pedido feito pelo cliente

  Cenário: Grupo de modificadores obrigatório pendente
    Dado um item cujo grupo "Tamanho" é obrigatório
    Quando o pedido for enviado sem a escolha
    Então deve receber 422 com o grupo pendente identificado
    E nenhum pedido deve ser criado

  Cenário: Preço aplicado por canal
    Dado uma pizza com R$ 45,00 no salão e R$ 52,00 no delivery
    Quando o pedido for criado no canal DINE_IN
    Então o preço registrado deve ser R$ 45,00
    E deve ficar gravado no item, não apenas referenciado

  Cenário: Observação livre por item
    Dado um item com a observação "bem assada, sem cebola"
    Quando o pedido for confirmado
    Então a observação deve aparecer no cartão do KDS
    E deve constar no comprovante

  Cenário: Acréscimo a pedido já confirmado
    Dado um pedido em IN_PRODUCTION
    Quando o cliente adicionar mais um item
    Então o item deve ser acrescentado ao mesmo pedido
    E o evento order.item.added deve ser emitido
    E o novo item deve entrar em QUEUED

  Cenário: Produto indisponível no momento do envio
    Dado um item que ficou indisponível enquanto estava no carrinho
    Quando o pedido for enviado
    Então deve receber 422 identificando o item
    E os demais itens não devem ser criados parcialmente

  Cenário: Horário de ocorrência preservado
    Dado um pedido criado às 20h03 com a loja offline
    Quando sincronizar às 21h15
    Então occurredAt deve permanecer 20h03
    E recordedAt deve ser 21h15
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-001 | Todo pedido confirmado é roteado simultaneamente para cozinha e caixa | Esta história cria e emite; a US-031 executa o roteamento |
| RN-004 | Toda ação registra autor, horário e dispositivo | `actor_id`, `device_id` e `occurred_at` gravados no pedido e em cada evento |
| RN-009 | Regra de precificação de meio a meio configurável | Aplicada no cálculo do item |
| RN-020 | Métrica de horário usa sempre `ocorrido_em` | `X-Occurred-At` é a fonte, não o relógio do servidor |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-001 | `order.created` | Rascunho aberto | channel, sessionId, tableId | ↑ |
| EVT-002 | `order.placed` | **T0** — pedido confirmado | items[], total, promisedAt | ↑ |
| EVT-003 | `order.item.added` | Item acrescentado a pedido aberto | variantId, qty, modifiers, fractions | ↑ |
| EVT-004 | `order.item.queued` | Item entra na fila da praça | stationId, position | ↑ |

> Os eventos são gravados na mesma transação do estado, via outbox (ADR-007, regra R6 do doc. 04).

## 7. Contrato de API

```http
POST /v1/orders
Authorization: Bearer <token>
Idempotency-Key: <uuid>
X-Device-Id: <uuid>
X-Occurred-At: 2026-07-31T20:47:12.334Z
{
  "channel": "DINE_IN",
  "sessionId": "...",
  "items": [
    { "variantId": "<pizza-grande>", "quantity": 1, "notes": "bem assada",
      "fractions": [ { "variantId": "<mussarela-g>", "weight": 0.5 },
                     { "variantId": "<calabresa-g>", "weight": 0.5 } ],
      "modifiers": [ { "modifierId": "<borda-catupiry>" } ] }
  ]
}
→ 201 {
    "order": { "id": "...", "code": "A47", "shortCode": "47",
               "status": "PLACED", "total": 6000,
               "items": [ { "id": "...", "status": "QUEUED",
                            "stationId": "<forno>", "unitPrice": 5200 } ] },
    "promisedAt": "2026-07-31T20:59:00Z",
    "estimatedMinutes": 12
  }

→ 422 { "code": "MODIFIER_GROUP_REQUIRED",
        "meta": { "itemIndex": 0, "groupId": "...", "groupName": "Tamanho" } }
→ 422 { "code": "PRODUCT_UNAVAILABLE", "meta": { "variantId": "..." } }

POST /v1/orders/{id}/items          # acrescentar item a pedido aberto
GET  /v1/orders/{id}

# Caminho público (cliente na mesa):
POST /v1/public/orders
```

> Toda escrita retorna o estado resultante, evitando re-fetch (princípio 7 do doc. 05).

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order` | Pedido | `code`, `short_code`, `channel`, `session_id`, `status`, `placed_at`, `promised_at`, `total`, `actor_id`, `device_id` |
| `order_item` | Item com os seis carimbos | `variant_id`, `station_id`, `quantity`, `unit_price`, `notes`, `status`, `placed_at` |
| `order_item_fraction` | Frações do meio a meio | `variant_id`, `weight`, `name_snapshot` |
| `order_item_modifier` | Adicionais escolhidos | `modifier_id`, `name_snapshot`, `price_delta_snapshot` |
| `outbox` | Evento gravado na mesma transação | `event_id`, `type`, `payload`, `device_seq` |
| `idempotency_key` | Resposta guardada por 24 h | `key`, `response`, `expires_at` |

> `unit_price` é gravado no item, não referenciado — o comprovante precisa continuar correto após reajuste de preço.

## 9. Comportamento offline

**História crítica para RF-OFF-01.** A criação de pedido é 100% local: validação, cálculo de preço, atribuição de praça e emissão de evento acontecem no edge, sem qualquer chamada à nuvem.

Três mecanismos sustentam isso:

1. **Outbox transacional** — o evento é gravado junto com o estado; se o processo cair, o evento continua pendente.
2. **`X-Occurred-At`** — o horário do fato vem do dispositivo, não do servidor da nuvem. Um pedido feito às 20h03 e sincronizado às 21h15 é contabilizado às 20h (RN-020).
3. **Fila no cliente** — o PWA guarda a ação em IndexedDB (Dexie) se o edge estiver momentaneamente inacessível, e reenvia com a mesma `Idempotency-Key`.

O dispositivo indica o estado offline de forma discreta, sem alarmar o cliente do salão.

## 10. Interface e experiência

- Envio otimista: o item aparece na comanda imediatamente, com confirmação silenciosa depois
- Erro de validação apontando o item e o campo exatos, nunca mensagem genérica
- Preço total sempre visível durante a montagem, atualizado a cada escolha
- No celular do garçom, o fluxo inteiro cabe com uma das mãos ocupada
- Código curto do pedido exibido após a confirmação — é o que a cozinha chama em voz alta

## 11. Métricas, alertas e observabilidade

- **T0** (`placed_at`) — marco zero de toda métrica de tempo do produto
- Contagem de pedidos por canal, hora e operador
- Ticket médio por pedido e por sessão
- Taxa de reenvio idempotente — indicador direto de qualidade da rede da loja
- Erros de validação por tipo — alto em modificador obrigatório indica cardápio mal configurado

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo de preço com modificadores, frações e canal; as três regras de fração |
| Unitário | Validação de todos os casos de grupo de modificadores |
| Unitário | Máquina de estados: transições válidas e proibidas do documento 04 |
| Integração | Idempotência com reenvio da mesma chave retorna a mesma resposta |
| Integração | Evento gravado no outbox na mesma transação do estado |
| Integração | `occurredAt` preservado após sincronização atrasada |
| Integração | Falha parcial não cria pedido incompleto |
| Caos offline | Criação de pedido com a nuvem inacessível |
| E2E | Cliente monta e envia pelo QR; garçom lança pelo celular; ambos chegam iguais |
| Carga | 500 pedidos em 30 minutos sem degradação (cenário de pico do doc. 10) |

## 13. Dependências

**Depende de:** US-012, US-013, US-014, US-022, US-032  
**Habilita:** US-031, US-033, US-040, US-051, US-103

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
- [ ] Teste de pico executado com o volume declarado no documento 10

## 15. Riscos, premissas e pendências

- É a história de maior estimativa do MVP (13 pontos). Se estourar, fatiar por canal: primeiro o pedido do garçom, depois o do cliente na mesa.
- Observação livre em excesso indica que faltam modificadores estruturados — monitorar no piloto e converter em modificador.

---

*US-030 · Épico E-03 · Pacote 004_DonaBetinha · Replay Studio.*