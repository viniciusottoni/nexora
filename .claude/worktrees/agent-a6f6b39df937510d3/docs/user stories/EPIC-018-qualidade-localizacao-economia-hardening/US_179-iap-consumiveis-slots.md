---
title: US-179 — Comprar consumíveis e slots via RevenueCat IAP
sidebar_position: 179
---

# US-179 — Comprar consumíveis e slots de inventário via RevenueCat IAP

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-179 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P1 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Assinante ou usuário elegível |
| Dependência | ADR de IAP de consumíveis/slots |
| Status | Planejada |

## 2. História do usuário

Como **usuário elegível**,
quero **comprar itens consumíveis ou slots de inventário de forma real pela loja**,
para **receber o benefício corretamente no AWAKEN**.

## 3. Objetivo

Substituir compra mock por fluxo real de IAP via RevenueCat, com concessão validada no servidor e idempotente.

## 4. Escopo

### Entra nesta US

- Produto consumível via loja.
- Produto de slot de inventário via loja.
- Compra iniciada no app.
- Webhook RevenueCat validando compra.
- Concessão pelo backend.
- Idempotência por transação da loja.

### Fora desta US

- Moeda virtual emitida por jogo.
- Marketplace.
- Compra fora da loja.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Cliente nunca credita inventário sozinho. |
| RN-002 | Concessão deve ocorrer no backend após validação da loja/RevenueCat. |
| RN-003 | A mesma transação não pode conceder benefício duas vezes. |
| RN-004 | Produto indisponível ou inativo não pode ser comprado. |
| RN-005 | Falha de compra deve exibir mensagem clara e não conceder item. |

## 6. Fluxo principal

1. Usuário seleciona produto.
2. App inicia compra via RevenueCat.
3. Loja processa pagamento.
4. RevenueCat envia evento/webhook.
5. Backend valida produto e transação.
6. Backend concede item ou slot de forma idempotente.
7. App atualiza inventário após sincronização.

## 7. Impacto Flutter

- Substituir `ShopController.purchase` mock.
- Tratar loading, sucesso, cancelamento e erro.
- Atualizar inventário após confirmação do servidor.

## 8. Impacto Backend

- Endpoint/webhook RevenueCat.
- Ledger de transações.
- Validação de produto.
- Concessão idempotente.

## 9. Impacto DB

Entidades sugeridas:

- IapTransactionLedger;
- InventoryItem;
- InventorySlot;
- ShopProduct.

## 10. Critérios de aceite

### CA-001 — Compra concede benefício

Dado que a loja confirmou compra válida,
quando o webhook for processado,
então o backend deve conceder o item ou slot.

### CA-002 — Idempotência

Dado que o mesmo evento da loja chega duas vezes,
quando o backend processar,
então o benefício deve ser concedido apenas uma vez.

## 11. Decisão registrada

> Compra real de itens e slots deve ser validada no servidor; o app apenas inicia compra e consulta resultado.
