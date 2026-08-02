---
title: US-180 — RBAC e autorização por perfil nos endpoints admin
sidebar_position: 180
---

# US-180 — RBAC e autorização por perfil nos endpoints admin

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-180 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P0 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Admin e Engenharia |
| Dependência | ADR de RBAC/autorização administrativa |
| Status | Planejada |

## 2. História do usuário

Como **admin do AWAKEN**,
quero **que endpoints administrativos exijam perfil administrativo real**,
para **evitar que qualquer usuário autenticado acesse funções internas**.

## 3. Objetivo

Implementar RBAC com role/claim de admin e aplicar política de autorização nos controllers administrativos.

## 4. Escopo

### Entra nesta US

- Definir role/claim de admin.
- Criar policy `Admin` no backend.
- Aplicar policy em endpoints administrativos.
- Garantir negação para usuário autenticado não-admin.
- Preparar base para EPIC-017.

### Fora desta US

- Painel admin completo.
- Matriz complexa de permissões por módulo.
- Delegação de permissões no app mobile.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Endpoint administrativo exige perfil/claim de admin. |
| RN-002 | `[Authorize]` simples é insuficiente para endpoint admin. |
| RN-003 | Usuário comum autenticado deve receber acesso negado. |
| RN-004 | Acesso negado deve ser registrado em log seguro. |
| RN-005 | EPIC-017 depende desta base de autorização. |

## 6. Fluxo principal

1. Usuário faz requisição a endpoint admin.
2. Backend valida autenticação.
3. Backend valida policy `Admin`.
4. Admin autorizado prossegue.
5. Não-admin recebe resposta de acesso negado.

## 7. Impacto Backend

- Configurar roles/claims.
- Criar policy `Admin`.
- Aplicar `[Authorize(Policy = "Admin")]` nos controllers admin.
- Testes de autorização.

## 8. Impacto DB

- Campo/tabela de roles ou claims.
- Seed seguro de admin inicial, se necessário.

## 9. Critérios de aceite

### CA-001 — Admin acessa

Dado que o usuário possui role admin,
quando chamar endpoint administrativo,
então a requisição deve ser autorizada.

### CA-002 — Usuário comum bloqueado

Dado que o usuário é autenticado mas não admin,
quando chamar endpoint administrativo,
então deve receber acesso negado.

## 10. Decisão registrada

> RBAC é pré-requisito do EPIC-017; endpoint administrativo não pode depender apenas de autenticação genérica.
