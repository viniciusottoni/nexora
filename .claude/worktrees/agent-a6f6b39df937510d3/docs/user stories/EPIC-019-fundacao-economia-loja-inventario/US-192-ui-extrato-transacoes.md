---
title: US-192 — UI de extrato de transações e compras
sidebar_position: 192
---

# US-192 — UI de extrato de transações e compras

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-192 |
| Épico | EPIC-019 — Fundação de Economia, Loja e Inventário |
| Prioridade | P1 |
| Fase | Fundação de economia (pós-MVP) |
| Perfil principal | Engenharia Flutter e Suporte |
| Dependência | ADR-023, US-186, US-189 |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **usuário**, quero **ver meu extrato de movimentações de Gold e minhas compras**, para **acompanhar o que gastei, ganhei e comprei com transparência**.

## 3. Objetivo

Expor ao usuário, no app, o histórico de movimentações de Gold (US-186) e de pedidos de compra (US-189), com status e datas. Reforça a transparência do freemium honesto e reduz contestações.

## 4. Escopo

### Entra nesta US

- Endpoint de extrato paginado (movimentações de Gold + pedidos).
- Tela de extrato com lançamento, motivo, valor, canal e status.
- Datas em UTC persistidas, exibidas no fuso local (ADR-022/US-172).
- Empty state quando não há histórico.

### Fora desta US

- Exportação/relatório administrativo (US-193).
- Filtros avançados e busca.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O extrato mostra apenas dados do usuário autenticado. |
| RN-002 | Cada item exibe motivo, valor, canal e status legíveis e localizados. |
| RN-003 | Datas exibidas no fuso local; persistência em UTC. |
| RN-004 | Extrato é paginado e ordenado por data decrescente. |
| RN-005 | Sem histórico, exibe empty state honesto. |

## 6. Impacto Flutter

- Tela de extrato com paginação e estados de UI.
- Formatação localizada de valor, data e status.

## 7. Impacto Backend

- `GET /api/economy/transactions` paginado, combinando ledger de Gold e pedidos.
- Projeção segura (sem dados de pagamento).

## 8. Impacto DB

- Consultas sobre `gold_ledger_entries` e `shop_orders` (índices por usuário e data).

## 9. Critérios de aceite

### CA-001 — Extrato do usuário

Dado um histórico de movimentações e compras,
quando abrir o extrato,
então deve listar os lançamentos do usuário com motivo, valor, canal, status e data local.

### CA-002 — Paginação

Dado um histórico longo,
quando rolar a lista,
então as páginas carregam sem duplicar nem travar.

### CA-003 — Sem histórico

Dado nenhum lançamento,
quando abrir o extrato,
então exibe empty state localizado.

## 10. Decisão registrada

> O usuário tem extrato transparente de Gold e compras; relatórios administrativos ficam na US-193 (ADR-023).
