---
title: US-193 — Visibilidade administrativa de compras
sidebar_position: 193
---

# US-193 — Visibilidade administrativa de compras

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-193 |
| Épico | EPIC-019 — Fundação de Economia, Loja e Inventário |
| Prioridade | P1 |
| Perfil principal | Suporte/Finanças e Engenharia |
| Dependência | ADR-023, EPIC-018 US-180 (RBAC), US-189, US-190 |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **suporte/finanças**, quero **consultar compras e seu rastro de forma somente leitura**, para **investigar contestações, divergências e fraudes sem alterar dados**.

## 3. Objetivo

Expor uma visão administrativa **read-only** do rastreamento de compras (pedidos, status, canal, correlação) e do rastro de auditoria associado, protegida por RBAC. Não há ação de mutação (refund/ajuste ficam fora).

## 4. Escopo

### Entra nesta US

- Endpoint admin read-only para listar/consultar `ShopOrder` por usuário, status, canal e período.
- Vínculo do pedido ao seu rastro de auditoria (US-190) por `CorrelationId`.
- Proteção por perfil/claim de admin (RBAC — US-180).
- Projeção segura: sem token nem dado de pagamento.

### Fora desta US

- Refund/estorno e ajuste de saldo administrativos.
- Telas do site admin (consumo fica para o EPIC-017).
- Relatórios financeiros agregados.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Acesso exige perfil/claim de admin (RBAC). |
| RN-002 | A visão é somente leitura; nenhuma mutação é exposta. |
| RN-003 | A resposta não contém token nem dado de pagamento (ADR-015). |
| RN-004 | Cada consulta administrativa relevante também é auditada (`ActorType = Admin`). |
| RN-005 | Pedido e auditoria são correlacionáveis por `CorrelationId`. |

## 6. Impacto Flutter

- Nenhum (consumo pelo site admin — EPIC-017).

## 7. Impacto Backend

- `GET /api/admin/shop/orders` com filtros e paginação, sob RBAC.
- Junção pedido ↔ auditoria por correlação.
- Projeção segura.

## 8. Impacto DB

- Consultas sobre `shop_orders` e `audit_logs` (índices por status, usuário e correlação).

## 9. Critérios de aceite

### CA-001 — Acesso restrito

Dado um usuário não-admin,
quando chamar o endpoint admin de compras,
então o acesso deve ser negado.

### CA-002 — Rastreamento completo

Dado uma compra existente,
quando um admin consultá-la,
então deve ver canal, status, datas e o rastro de auditoria correlacionado, sem dado de pagamento.

### CA-003 — Auditoria do acesso admin

Dado um admin consultando compras,
quando a consulta relevante ocorrer,
então gera registro de auditoria com `ActorType = Admin`.

## 10. Decisão registrada

> Suporte/finanças têm visão read-only e auditável do rastreamento de compras sob RBAC; ações de mutação (refund/ajuste) ficam deferidas (ADR-023).
