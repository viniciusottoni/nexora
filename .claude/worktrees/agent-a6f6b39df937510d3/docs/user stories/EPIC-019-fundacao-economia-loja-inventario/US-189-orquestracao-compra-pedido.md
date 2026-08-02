---
title: US-189 — Orquestração de compra e trilha de pedido
sidebar_position: 189
---

# US-189 — Orquestração de compra e trilha de pedido (rastreamento)

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-189 |
| Épico | EPIC-019 — Fundação de Economia, Loja e Inventário |
| Prioridade | P1 |
| Fase | Fundação de economia (pós-MVP) |
| Perfil principal | Engenharia |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência | ADR-023, ADR-010, US-186, US-187, US-188 |
| Status | Planejada |

## 2. História do usuário

Como **engenharia e suporte**, quero **toda compra registrada numa trilha de pedido rastreável e idempotente**, para **conceder benefícios com segurança e investigar qualquer compra depois**.

## 3. Objetivo

Unificar a compra dos dois canais — Gold (interno) e IAP/RevenueCat (dinheiro real) — atrás de um pedido (`ShopOrder`) com status e correlação, substituindo a compra mock da ADR-022. Esta US trata a **orquestração e o rastreamento**; o efeito de cada item permanece fora.

## 4. Escopo

### Entra nesta US

- `ShopOrder` (`UserId`, `Channel` gold/iap, `ProductKey`, `Status` pending/granted/failed/refunded, `ExternalTransactionId?`, `CorrelationId`, timestamps).
- Compra em Gold: debita carteira (US-186) e concede via inventário (US-187).
- Compra IAP: referencia o `iap_transaction_ledger` existente e concede após validação.
- Idempotência por `ExternalTransactionId` (IAP) e por chave de pedido (Gold) — ADR-010.
- Transição de status com timestamps de cada etapa.
- Substituição do `POST /shop/items/{itemKey}/purchase` mock.

### Fora desta US

- Efeito/consumo de cada item.
- Regras de emissão de Gold.
- Refund/estorno automatizado (status previsto, fluxo deferido).

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Cliente nunca credita inventário nem saldo sozinho. |
| RN-002 | Toda compra gera um `ShopOrder` antes da concessão. |
| RN-003 | A mesma transação/pedido não concede benefício duas vezes (ADR-010). |
| RN-004 | Produto inativo/indisponível não pode ser comprado. |
| RN-005 | Falha de compra deixa o pedido em `failed` e não concede nada. |
| RN-006 | Compra em Gold respeita saldo (sem negativo) e gera lançamento no ledger. |
| RN-007 | Todo pedido carrega `CorrelationId` em resposta e logs (ADR-019). |

## 6. Fluxo principal

1. Usuário seleciona produto na loja.
2. Backend cria `ShopOrder` em `pending`.
3. Canal Gold: valida saldo → debita (ledger) → concede item. Canal IAP: app inicia compra → RevenueCat valida → webhook processado.
4. Backend concede o benefício de forma idempotente.
5. Pedido passa a `granted`; em erro, `failed`.
6. App sincroniza inventário/saldo após confirmação do servidor.

## 7. Impacto Flutter

- Substituir `ShopController.purchase` mock.
- Tratar loading, sucesso, cancelamento e erro.
- Atualizar saldo e inventário após confirmação.

## 8. Impacto Backend

- Entidade `ShopOrder` e máquina de status.
- Orquestrador de compra por canal.
- Reuso da carteira (US-186) e do `iap_transaction_ledger`.
- Concessão idempotente; correlação ponta a ponta.

## 9. Impacto DB

Entidades sugeridas:

- ShopOrder (`shop_orders`, índices por usuário, status e `ExternalTransactionId` único);
- reuso de GoldLedgerEntry, InventoryItem e IapTransactionLedger.

## 10. Critérios de aceite

### CA-001 — Pedido rastreável

Dado uma compra,
quando ela é iniciada,
então existe um `ShopOrder` com canal, produto, status e correlação.

### CA-002 — Idempotência

Dado que a mesma transação chega duas vezes,
quando o backend processar,
então o benefício é concedido uma única vez e o pedido não duplica.

### CA-003 — Gold sem saldo

Dado saldo insuficiente,
quando comprar em Gold,
então o pedido fica `failed`, sem débito nem concessão.

## 11. Decisão registrada

> Toda compra passa por um pedido rastreável e idempotente nos dois canais; o efeito de cada item fica fora desta US (ADR-023).
