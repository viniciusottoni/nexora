---
title: US-166 — Registrar auditoria das ações administrativas
sidebar_position: 166
---

# US-166 — Registrar auditoria das ações administrativas

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-166 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Segurança, Engenharia, Suporte e Produto |
| Plataforma | Web Admin (React) + Backend .NET |
| Dependência | EPIC-015 US-108, US-159, US-160 |
| Status | Planejada |

## 2. História do usuário

Como **time de segurança e operação**, quero **auditar ações administrativas relevantes**, para **saber quem fez o quê, quando, em qual recurso e com qual correlação**.

## 3. Objetivo

Expandir a auditoria para o contexto admin, registrando ações sensíveis do painel sem expor payloads sensíveis.

## 4. Escopo

### Entra nesta US

- Auditoria de login, falha de login, setup/validação de MFA e bloqueios.
- Auditoria de leitura sensível quando aplicável.
- Auditoria de atualização de tickets, bugs, alertas e exportações.
- Tela de audit log com busca e filtros.
- Metadados sanitizados com actor, ação, recurso, data, origem e correlationId.
- Proteção de leitura do audit log por perfil administrativo.

### Fora desta US

- Auditoria imutável em storage externo WORM.
- SIEM avançado.
- Exportação irrestrita de logs.
- Payload completo de requisições.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Toda ação administrativa relevante deve gerar AuditLog. |
| RN-002 | AuditLog não pode conter senha, token, segredo MFA ou payload sensível. |
| RN-003 | Eventos admin devem diferenciar ator admin, sistema e usuário comum. |
| RN-004 | Exportações administrativas devem ser auditadas. |
| RN-005 | Usuário comum não pode consultar AuditLog. |

## 6. Fluxo principal

1. Admin executa ação relevante no painel.
2. Backend processa a ação.
3. Backend cria AuditLog com metadados seguros.
4. Admin autorizado acessa a tela de audit log.
5. Sistema permite buscar e filtrar registros.

## 7. Impacto Frontend React

- Página de audit log.
- Filtros por ator, ação, recurso, período e origem.
- Detalhe de registro com metadados seguros.

## 8. Impacto Backend

- Integração dos módulos admin ao AuditLog.
- Sanitização de metadados.
- Endpoint protegido para consulta de auditoria.

## 9. Impacto DB

- Índices por actorType, actorId, action, resourceType, resourceId, createdAt e correlationId.

## 10. Critérios de aceite

### CA-001 — Ação administrativa auditada

Dado que suporte altera um ticket,
quando a alteração for salva,
então deve existir AuditLog com ator admin, ação, recurso e data.

### CA-002 — Segredo não auditado

Dado que admin configura MFA,
quando o evento for auditado,
então o segredo TOTP não deve aparecer no log.

### CA-003 — Consulta protegida

Dado que um usuário não-admin tenta consultar audit log,
quando chamar o endpoint,
então o acesso deve ser negado.

## 11. Decisão registrada

> Toda operação administrativa relevante deve ser auditável com metadados seguros, preservando rastreabilidade sem transformar log em vazamento de dados.
