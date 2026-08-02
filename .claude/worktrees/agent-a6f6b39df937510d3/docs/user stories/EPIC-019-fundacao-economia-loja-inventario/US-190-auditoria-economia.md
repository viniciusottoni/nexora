---
title: US-190 — Auditoria de eventos de economia
sidebar_position: 190
---

# US-190 — Auditoria de eventos de economia

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-190 |
| Épico | EPIC-019 — Fundação de Economia, Loja e Inventário |
| Prioridade | P0 |
| Fase | Fundação de economia (pós-MVP) |
| Perfil principal | Engenharia, Segurança e Suporte/Finanças |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência | ADR-023, ADR-015, US-186, US-189 |
| Status | Planejada |

## 2. História do usuário

Como **time de segurança e suporte**, quero **toda mutação de economia registrada em auditoria**, para **investigar fraudes, divergências de saldo e contestações de compra com rastro confiável**.

## 3. Objetivo

Integrar a economia ao `IAuditLogService` existente (hoje cobrindo trial, entitlement, legal e exclusão de conta) para que compra, concessão, movimentação de Gold e consumo de item sejam auditados. Auditoria é **critério de aceite, não follow-up**.

## 4. Escopo

### Entra nesta US

- Registro de auditoria em: compra iniciada, concessão de benefício, débito/crédito de Gold, consumo de item.
- `ResourceType` dedicados (`ShopOrder`, `GoldWallet`, `InventoryItem`) e `Action` estáveis.
- `ActorType` correto (User/System/Admin), incluindo concessões automáticas por webhook como `System`.
- `MetadataSafe` sem dados sensíveis (valores, chaves de item, status — nunca token/pagamento) — ADR-015.
- `CorrelationId` ligando auditoria, pedido e logs.

### Fora desta US

- Painel/relatório administrativo (US-193 trata a visão; aqui é só o registro).
- Retenção/expurgo de auditoria (política geral, fora do épico).

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Toda mutação de economia gera um `AuditLog`. |
| RN-002 | `MetadataSafe` nunca contém token, dado de pagamento ou payload completo (ADR-015). |
| RN-003 | Concessões automáticas (webhook/sistema) usam `ActorType = System`. |
| RN-004 | O registro de auditoria compartilha `CorrelationId` com o pedido e os logs. |
| RN-005 | Falha ao auditar não pode passar silenciosamente (deve ser observável). |

## 6. Impacto Flutter

- Nenhum direto (auditoria é server-side).

## 7. Impacto Backend

- Chamadas a `IAuditLogService.RecordAsync` nos handlers de compra, concessão, carteira e consumo.
- `Action`/`ResourceType` padronizados para economia.
- Garantia de `MetadataSafe` sanitizado.

## 8. Impacto DB

- Uso de `audit_logs` (existente); índices por `ResourceType`/`ResourceId` se necessário para consulta.

## 9. Critérios de aceite

### CA-001 — Compra auditada

Dado uma compra concedida,
quando o fluxo concluir,
então existem registros de auditoria de compra e concessão com o mesmo `CorrelationId`.

### CA-002 — Metadados sanitizados

Dado um registro de auditoria de compra IAP,
quando inspecioná-lo,
então não deve conter token nem dado de pagamento, apenas metadados seguros.

### CA-003 — Movimento de saldo auditado

Dado um débito/crédito de Gold,
quando ocorrer,
então gera um `AuditLog` referenciando a carteira e o motivo.

## 10. Decisão registrada

> Toda mutação de economia é auditada via `IAuditLogService` com metadados sanitizados; a visualização administrativa fica na US-193 (ADR-023).
