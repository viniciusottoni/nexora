---
title: US-163 — Triar e atualizar tickets de suporte no site admin
sidebar_position: 163
---

# US-163 — Triar e atualizar tickets de suporte no site admin

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-163 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Suporte, Engenharia e Produto |
| Plataforma | Web Admin (React) + Backend .NET |
| Dependência | US-162, US-166, EPIC-018 US-176 |
| Status | Planejada |

## 2. História do usuário

Como **suporte do AWAKEN**, quero **triar e atualizar tickets no site admin**, para **organizar atendimento, priorizar problemas críticos e manter histórico confiável**.

## 3. Objetivo

Permitir que administradores autorizados alterem status, prioridade, categoria, responsável e notas internas dos tickets abertos pelo app.

## 4. Escopo

### Entra nesta US

- Atualização de status do ticket.
- Alteração de prioridade e categoria.
- Atribuição de responsável interno.
- Registro de notas internas.
- Histórico de alterações no detalhe do ticket.
- Auditoria de toda ação relevante.

### Fora desta US

- Resposta direta ao usuário por chat.
- Criação inicial de ticket pelo admin.
- Automação de SLA.
- Fechamento automático por inatividade.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Apenas admin autorizado pode alterar ticket. |
| RN-002 | Toda alteração de ticket deve gerar auditoria. |
| RN-003 | Notas internas não são visíveis ao usuário final. |
| RN-004 | Ticket fechado deve manter histórico completo. |
| RN-005 | Status deve seguir fluxo controlado: aberto, em triagem, em andamento, resolvido, fechado. |

## 6. Fluxo principal

1. Suporte abre o detalhe de um ticket.
2. Suporte altera status, prioridade, categoria ou responsável.
3. Sistema valida permissão e transição.
4. Sistema salva alteração e registra auditoria.
5. Histórico do ticket passa a exibir a ação realizada.

## 7. Impacto Frontend React

- Formulários de triagem no detalhe do ticket.
- Componentes de histórico e notas internas.
- Confirmação para ações críticas, como fechamento.

## 8. Impacto Backend

- Endpoints admin para atualização de ticket.
- Validação de transição de status.
- AuditLog com ator admin e metadados sanitizados.

## 9. Impacto DB

- Histórico de eventos do ticket.
- Campos de status, prioridade, categoria, responsável e timestamps.

## 10. Critérios de aceite

### CA-001 — Status atualizado

Dado que suporte autorizado altera o status de um ticket,
quando salvar,
então o novo status deve aparecer na lista e no detalhe.

### CA-002 — Histórico preservado

Dado que um ticket foi alterado,
quando consultar seu detalhe,
então o histórico deve mostrar quem alterou, quando e o que mudou.

### CA-003 — Ação auditada

Dado que uma alteração relevante ocorre,
quando a ação for concluída,
então deve existir registro de auditoria administrativa.

## 11. Decisão registrada

> O site admin pode triar e atualizar tickets do app, sempre com histórico e auditoria, mas não cria tickets para usuário final no MVP.
