---
title: EPIC-019 — Fundação de Economia, Loja e Inventário
sidebar_position: 19
---

# EPIC-019 — Fundação de Economia, Loja e Inventário

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-019 |
| Fase | Fundação de economia (pós-MVP) |
| Prioridade | P1 |
| Perfil principal | Engenharia, Produto, Suporte/Finanças e QA |
| Planos impactados | Trial, Mensal e Anual |
| Plataforma | Flutter Android + Backend .NET 10 |
| Status | Planejado |

## 2. Objetivo

Construir toda a estrutura de sistema que dá suporte à economia da loja — carteira de Gold, framework de inventário/itens, catálogo orientado a dados, orquestração e **rastreamento de compras**, e **auditoria** de toda mutação de economia — além de transformar as telas de loja, inventário, carteira e extrato em UI real ligada ao backend.

A entrega **não inclui os itens nem as regras dos itens**. Ao final, a plataforma sustenta adicionar itens reais, cosméticos e regras de efeito/emissão apenas com dados e handlers, sem mudança de schema.

## 3. Contexto

A ADR-022 entregou o menor recorte de inventário/loja para o "Pergaminho da Reforja": `InventoryItem` genérico, `ShopCatalog` estático em código com preço em Gold, e compra **mock** que apenas incrementa o inventário (sem saldo, sem pagamento). A própria ADR-022 deferiu a "US de economia futura": saldo de Gold real e sua emissão, catálogo completo, e inventário com dados reais na UI.

O EPIC-018 antecipou pedaços como hardening (US-178 catálogo, US-179 IAP real, US-180 RBAC), mas sem fundação coesa: não há carteira/saldo de Gold, não há ledger de moeda, as compras não passam por auditoria (`IAuditLogService` cobre trial, entitlement, legal e exclusão de conta — não economia) e não há trilha de pedidos para suporte/finanças.

O EPIC-019 é essa fundação, formalizada na **ADR-023**. Itens concretos e suas regras ficam para um épico posterior de catálogo/itens.

## 4. Escopo

### Entra neste épico

- Carteira de Gold (`GoldWallet`) e ledger de movimentação (`GoldLedgerEntry`) — contêiner de saldo reconciliável.
- Framework genérico extensível de inventário, chaves de item e slots — sem itens concretos.
- Catálogo de loja orientado a dados (migração do `ShopCatalog` estático), nascendo vazio/legado.
- Orquestração de compra unificada (canal Gold e canal IAP) com trilha de pedido (`ShopOrder`) e status.
- **Rastreamento de compras** ponta a ponta, idempotente (ADR-010), referenciando o `iap_transaction_ledger` existente.
- **Auditoria** de toda mutação de economia: compra, concessão, débito/crédito de Gold e consumo de item.
- UI real de loja, inventário e carteira com loading/erro/empty state (sem mocks enganosos).
- UI de extrato de transações/compras no app.
- Visibilidade administrativa (read-only) de compras para suporte/finanças, sob RBAC.

### Fora deste épico

- **Itens concretos e cosméticos** (nomes, arte, raridades com efeito de jogo).
- **Regras de efeito/consumo de cada item** e **regras de emissão de Gold** (quanto se ganha por quest/streak).
- Balanceamento de economia, temporadas/passe, promoções e descontos.
- Marketplace entre usuários.
- Refund/estorno automatizado (a estrutura prevê o status `refunded`, mas o fluxo fica deferido).

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-186 | Carteira de Gold e ledger de movimentação | P1 | [Abrir](./US-186-carteira-gold-ledger.md) |
| US-187 | Framework genérico de inventário, itens e slots | P1 | [Abrir](./US-187-framework-inventario-itens.md) |
| US-188 | Catálogo de loja orientado a dados | P1 | [Abrir](./US-188-catalogo-data-driven.md) |
| US-189 | Orquestração de compra e trilha de pedido | P1 | [Abrir](./US-189-orquestracao-compra-pedido.md) |
| US-190 | Auditoria de eventos de economia | P0 | [Abrir](./US-190-auditoria-economia.md) |
| US-191 | UI de loja, inventário e carteira | P1 | [Abrir](./US-191-ui-loja-inventario-carteira.md) |
| US-192 | UI de extrato de transações e compras | P1 | [Abrir](./US-192-ui-extrato-transacoes.md) |
| US-193 | Visibilidade administrativa de compras | P1 | [Abrir](./US-193-visibilidade-admin-compras.md) |

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-019-001 | Saldo de Gold é autoridade do backend; o app nunca calcula nem credita saldo (ADR-009). |
| RN-EPIC-019-002 | Toda movimentação de Gold gera um lançamento imutável no ledger com `BalanceAfter`. |
| RN-EPIC-019-003 | Débito de Gold nunca pode resultar em saldo negativo. |
| RN-EPIC-019-004 | Toda compra gera um pedido (`ShopOrder`) com canal, produto, status e correlação. |
| RN-EPIC-019-005 | Compra é idempotente: a mesma transação/pedido não concede benefício duas vezes (ADR-010). |
| RN-EPIC-019-006 | Compra, concessão, movimentação de saldo e consumo de item geram `AuditLog` com `MetadataSafe` sanitizado (ADR-015). |
| RN-EPIC-019-007 | Catálogo é carregado de dados; sem itens fictícios nem preço Gold hardcoded no domínio. |
| RN-EPIC-019-008 | Item inativo/indisponível não aparece nem pode ser comprado. |
| RN-EPIC-019-009 | Telas de economia exibem empty state honesto quando não há itens. |
| RN-EPIC-019-010 | Visão administrativa de compras exige perfil/claim de admin (RBAC). |

## 7. Impactos técnicos

### Flutter

- Telas de loja, inventário e carteira lendo catálogo/saldo/itens reais do backend.
- Widget de saldo de Gold reutilizável no design system.
- Tela de extrato de transações/compras.
- Remoção de itens mock e preços Gold falsos; empty states reais.
- Estados de loading, erro, cancelamento e vazio em todo o fluxo de compra.

### Backend

- `GoldWallet` + `GoldLedgerEntry` com concorrência otimista.
- Framework de inventário/itens/slots extensível por chave (`ItemKeys`).
- Catálogo orientado a dados sobre `shop_products` (já existente).
- `ShopOrder` unificando canais Gold e IAP; referência ao `iap_transaction_ledger`.
- Concessão e débito idempotentes (ADR-010), correlação em todas as respostas.
- Integração de economia ao `IAuditLogService`.
- Endpoints de saldo, extrato e catálogo; endpoint admin read-only de compras sob RBAC.

### Banco de dados

- `gold_wallets` (único por `UserId`).
- `gold_ledger_entries` (append-only, índices por carteira e data).
- `shop_orders` (índices por usuário, status e `ExternalTransactionId` único).
- Índices para extrato e rastreamento.

### QA

- Saldo reconcilia com o ledger.
- Débito não gera saldo negativo.
- Compra idempotente nos dois canais.
- Toda mutação de economia produz registro de auditoria.
- Catálogo vazio renderiza empty state, sem mocks.
- Não-admin é bloqueado na visão de compras.

## 8. Dependências

- **ADR-023** — Fundação de economia (carteira, rastreamento de compras, auditoria).
- ADR-022 — Inventário e loja mínimos (base reaproveitada).
- ADR-009 — Backend como autoridade.
- ADR-010 — Idempotência.
- ADR-015 — Logs/metadados sem dados sensíveis.
- EPIC-018 (US-178/179/180) — catálogo, IAP e RBAC que esta fundação consolida.
- Infra de `AuditLog`/`IAuditLogService` (já existente).

## 9. Critérios de aceite do épico

- Existe carteira de Gold com saldo reconciliável por ledger imutável.
- Débito de Gold nunca deixa saldo negativo e sempre lança no ledger.
- Toda compra é rastreada por um pedido com canal, status e correlação, de forma idempotente.
- Toda mutação de economia (compra, concessão, saldo, consumo) é auditada com metadados sanitizados.
- O catálogo é orientado a dados e nasce sem itens fictícios.
- As telas de loja, inventário, carteira e extrato leem do backend e tratam empty state.
- A visão administrativa de compras é acessível apenas a admin (RBAC).
- Nenhum item concreto nem regra de item foi introduzido (permanecem deferidos).

## 10. Decisão registrada

> O EPIC-019 entrega a fundação de economia (carteira de Gold, framework de inventário, catálogo orientado a dados, rastreamento de compras e auditoria) e a UI estrutural correspondente, conforme ADR-023. Itens concretos, cosméticos e regras de efeito/emissão ficam explicitamente fora deste épico e serão tratados em um épico posterior de catálogo/itens.
