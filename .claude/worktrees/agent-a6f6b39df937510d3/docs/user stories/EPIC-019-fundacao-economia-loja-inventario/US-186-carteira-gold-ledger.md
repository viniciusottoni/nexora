---
title: US-186 — Carteira de Gold e ledger de movimentação
sidebar_position: 186
---

# US-186 — Carteira de Gold e ledger de movimentação

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-186 |
| Épico | EPIC-019 — Fundação de Economia, Loja e Inventário |
| Prioridade | P1 |
| Fase | Fundação de economia (pós-MVP) |
| Perfil principal | Engenharia e Produto |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência | ADR-023, ADR-009 |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **uma carteira de Gold com saldo reconciliável**, para **sustentar compras internas sem depender de saldo calculado no app**.

## 3. Objetivo

Criar o contêiner de saldo de Gold e o ledger imutável de movimentação, com o backend como única autoridade. Esta US **não define como o Gold é emitido nem o que ele compra** — apenas a estrutura que guarda e movimenta saldo.

## 4. Escopo

### Entra nesta US

- `GoldWallet` (`UserId`, `Balance`, concorrência otimista).
- `GoldLedgerEntry` imutável (`Direction`, `Amount`, `Reason`, `ReferenceType`, `ReferenceId`, `BalanceAfter`, `CorrelationId`).
- Operações de domínio de crédito e débito.
- Endpoint de consulta de saldo do usuário autenticado.
- Criação preguiçosa da carteira no primeiro acesso (saldo 0).

### Fora desta US

- Regras de emissão de Gold (ganho por quest/streak).
- O que cada item custa ou faz.
- Top-up de Gold por dinheiro real.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O backend é a única autoridade do saldo (ADR-009). |
| RN-002 | Toda movimentação gera um lançamento imutável com `BalanceAfter`. |
| RN-003 | Débito não pode resultar em saldo negativo. |
| RN-004 | Saldo deve ser reconciliável pela soma do ledger. |
| RN-005 | Movimentações concorrentes não podem corromper o saldo (concorrência otimista). |

## 6. Impacto Flutter

- Consumir endpoint de saldo.
- Expor saldo via provider para o widget de carteira (US-191).

## 7. Impacto Backend

- Entidades `GoldWallet` e `GoldLedgerEntry`.
- Serviço de carteira com `Credit`/`Debit` transacionais.
- `GET /api/economy/wallet` com saldo e correlação.
- Reutilização por compras (US-189) e auditoria (US-190).

## 8. Impacto DB

Entidades sugeridas:

- GoldWallet (`gold_wallets`, único por `UserId`);
- GoldLedgerEntry (`gold_ledger_entries`, append-only, índice por carteira e data).

## 9. Critérios de aceite

### CA-001 — Saldo inicial

Dado que o usuário nunca teve carteira,
quando consultar o saldo,
então deve retornar 0 sem erro e criar a carteira.

### CA-002 — Débito não fica negativo

Dado um saldo insuficiente,
quando ocorrer um débito,
então a operação deve falhar e nenhum lançamento de débito é gravado.

### CA-003 — Reconciliação

Dado um histórico de lançamentos,
quando somar o ledger,
então o resultado deve igualar o saldo atual.

## 10. Decisão registrada

> A carteira guarda e movimenta saldo de forma reconciliável; emissão e custo de itens ficam fora desta US (ADR-023).
