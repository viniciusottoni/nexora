---
title: US-195 — Validar IAP consumível e slot server-side antes de conceder item
sidebar_position: 195
---

# US-195 — Validar IAP consumível e slot server-side antes de conceder item

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-195 |
| Épico | EPIC-020 — Hardening de Segurança e Fechamento de Vulnerabilidades |
| Prioridade | P0 |
| Fase | Bloqueador pré-produção / pré-teste aberto |
| Perfil principal | Usuário autenticado, RevenueCat, Backend, Loja |
| Plano | Trial, Mensal, Anual |
| Idiomas impactados | PT-BR / EN / ES / FR |
| Dependência principal | RevenueCat, ShopProduct, ShopOrder, Inventory, IapTransactionLedger |
| Status | Planejada |

## 2. História do usuário

Como **usuário que comprou um item legítimo na loja**,

quero **receber o item apenas depois que a transação for validada pelo servidor**,

para **garantir uma loja justa, sem fraude e sem duplicidade de concessão**.

## 3. Contexto

O app hoje envia `transactionId`, `productKey` e `store` para o backend após o RevenueCat retornar sucesso local. O backend concede o item com base nesses dados. Isso permite tentativa de fraude por request manual com transaction id inventado ou product key alterado.

## 4. Objetivo

Garantir que consumíveis e slots comprados por IAP só sejam concedidos após validação server-side da transação e idempotência por transação externa.

## 5. Escopo

### Entra nesta US

- Validar transação IAP no servidor via RevenueCat API/webhook antes da concessão.
- Confirmar que o produto comprado corresponde ao `ShopProduct` esperado.
- Confirmar que o comprador corresponde ao usuário autenticado/RevenueCat App User ID.
- Garantir idempotência por transaction id/original transaction id/store.
- Retornar estado claro: concedido, pendente, inválido, duplicado.
- Criar testes negativos de transaction id inventado e product key trocado.

### Fora desta US

- Design visual da loja.
- Criação de novos produtos.
- Reembolso automático.
- Antifraude comportamental avançado.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O cliente não pode determinar sozinho que uma compra é válida. |
| RN-002 | O backend deve validar a transação no RevenueCat ou por webhook assinado. |
| RN-003 | O produto validado deve corresponder ao produto configurado no catálogo. |
| RN-004 | O usuário validado deve corresponder ao usuário autenticado. |
| RN-005 | Transação duplicada não pode conceder item duas vezes. |
| RN-006 | Transação inválida deve gerar `ShopOrder` failed ou rejected rastreável, sem concessão. |
| RN-007 | Recibos/tokens brutos não devem ser persistidos em logs nem auditoria. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode processar IAP. |
| Trial | Pode comprar item permitido. |
| Premium Mensal/Anual | Pode comprar item permitido. |
| Assinatura expirada | Pode comprar apenas se regra comercial permitir. |
| Admin interno | Pode consultar pedidos, não conceder manualmente por esse endpoint. |
| Sistema/RevenueCat | Pode confirmar transação via webhook. |

## 8. Fluxo principal

1. Usuário inicia compra no app.
2. RevenueCat/loja conclui a compra localmente.
3. App solicita validação/concessão ao backend com referência da transação.
4. Backend consulta RevenueCat ou aguarda webhook confiável.
5. Backend valida produto, usuário, loja, status e idempotência.
6. Backend cria/atualiza `ShopOrder`.
7. Backend concede item/slot.
8. Backend registra ledger/auditoria.
9. App atualiza inventário.

## 9. Fluxos alternativos

### Transação pendente

Backend retorna status `pending_validation`; app informa que a compra está sendo confirmada.

### Transação inválida

Backend marca pedido como `rejected`/`failed`, não concede item e retorna erro localizado.

### Produto divergente

Backend rejeita se a transação validada corresponde a outro produto.

### Transação duplicada

Backend retorna sucesso idempotente com o pedido original, sem incrementar inventário novamente.

## 10. Estados esperados

- carregando compra;
- compra validando;
- compra concedida;
- compra pendente;
- compra inválida;
- compra duplicada;
- erro de rede;
- erro inesperado com `correlationId`.

## 11. Impacto no Frontend Flutter

- Tratar `pending_validation` sem mostrar item como disponível antes da concessão.
- Atualizar inventário após resposta do backend.
- Exibir mensagem de compra pendente/falha.
- Evitar expor detalhes técnicos da transação em erro ao usuário.

## 12. Impacto no Backend

- Refatorar `ProcessIapPurchaseCommandHandler`.
- Criar serviço de validação RevenueCat.
- Ajustar `ShopOrder` para estados `pending_validation`, `granted`, `failed`, `rejected` se necessário.
- Garantir transação atômica para débito/concessão/ledger.
- Criar testes de concorrência/idempotência.

## 13. Impacto no Banco de Dados

Entidades impactadas:

```txt
ShopOrder
IapTransactionLedger
InventoryItem
InventorySlot
RevenueCatEvent
```

Restrições:

- índice único por `Store + ExternalTransactionId`;
- índice por `OriginalTransactionId`, se disponível;
- status rastreável;
- auditoria sem recibo bruto.

## 14. Impacto em Gamificação

- Slots e consumíveis com efeito real só aparecem após concessão confirmada.
- Evita vantagem indevida por compra forjada.

## 15. Impacto em Monetização

- Protege receita da loja.
- Reduz suporte por compra duplicada.
- Mantém rastreabilidade para conciliação financeira.

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de compra pendente, inválida e restaurada. |
| EN | Mesmas mensagens localizadas. |
| ES | Mesmas mensagens localizadas. |
| FR | Mesmas mensagens localizadas. |

## 17. Contrato de API sugerido

### Endpoint

```txt
POST /api/v1/shop/iap/process
```

### Request sugerido

```json
{
  "transactionReference": "string"
}
```

O backend deve resolver produto, loja, status e usuário pela validação server-side.

### Response conceitual

```json
{
  "orderId": "uuid",
  "status": "granted|pending_validation|rejected|failed",
  "inventoryUpdated": true,
  "correlationId": "uuid"
}
```

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| iap_validation_started | Backend inicia validação. |
| iap_validation_pending | Compra ainda não confirmada. |
| iap_granted | Item concedido. |
| iap_rejected | Transação inválida. |
| iap_duplicate_ignored | Transação duplicada ignorada. |

## 19. Critérios de aceite

### CA-001 — Compra válida

Dado que a transação é válida no RevenueCat,
Quando o backend processa,
Então o item é concedido uma única vez.

### CA-002 — Transaction id inventado

Dado que o usuário envia uma transação inexistente,
Quando o backend valida,
Então a compra é rejeitada e nenhum item é concedido.

### CA-003 — Product key divergente

Dado que a transação pertence a outro produto,
Quando processada,
Então o backend rejeita a concessão solicitada.

### CA-004 — Duplicidade

Dado que a mesma transação é enviada duas vezes,
Quando processada,
Então o inventário permanece com apenas uma concessão.

## 20. Critérios de teste para QA

- compra válida consumível;
- compra válida slot;
- compra duplicada;
- transaction id inexistente;
- product id divergente;
- usuário divergente;
- RevenueCat indisponível;
- concorrência com duas chamadas simultâneas;
- mensagens localizadas.

## ✅ Decisão registrada

Nenhum item, slot ou benefício de IAP será concedido apenas porque o app informou que a compra ocorreu; a validação server-side passa a ser obrigatória.