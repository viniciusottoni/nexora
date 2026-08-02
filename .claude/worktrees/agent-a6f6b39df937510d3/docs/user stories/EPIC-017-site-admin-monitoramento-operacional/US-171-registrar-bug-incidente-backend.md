---
title: US-171 — Registrar bug interno ou incidente de backend
sidebar_position: 171
---

# US-171 — Registrar bug interno ou incidente de backend

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-171 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Engenharia, Suporte e Produto |
| Plataforma | Web Admin (React) + Backend .NET |
| Dependência | US-164, US-166 |
| Status | Planejada |

## 2. História do usuário

Como **engenharia ou suporte**, quero **registrar bug interno ou incidente de backend no site admin**, para **acompanhar problemas operacionais que não nasceram automaticamente de logs ou tickets**.

## 3. Objetivo

Permitir registro interno de bugs/incidentes com severidade, status, componente, ambiente, origem, data de ocorrência e relação opcional com ticket, erro ou correlationId.

## 4. Escopo

### Entra nesta US

- Formulário de registro interno de bug/incidente.
- Campos obrigatórios: título, severidade, componente, ambiente, origem, status e data de ocorrência.
- Campos opcionais: correlationId, ticket relacionado, erro relacionado, descrição sanitizada e responsável.
- Histórico de status e comentários internos.
- Auditoria de criação e atualização.
- Exibição do registro na tela de bugs da US-164.

### Fora desta US

- Integração obrigatória com Jira/GitHub Issues.
- Gestão completa de incident response.
- Postmortem avançado.
- Notificação automática para usuários finais.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Bug interno deve ter severidade, status, componente, ambiente, origem e data. |
| RN-002 | Descrição e comentários não podem conter senha, token ou payload sensível. |
| RN-003 | Criação e atualização devem ser auditadas. |
| RN-004 | Registro pode se relacionar a ticket, erro ou correlationId existente. |
| RN-005 | Incidente fechado deve preservar histórico de mudanças. |

## 6. Fluxo principal

1. Admin acessa registrar bug/incidente.
2. Admin preenche campos obrigatórios e contexto seguro.
3. Sistema valida dados e sanitização básica.
4. Sistema salva o registro e cria auditoria.
5. Bug/incidente aparece na tela de monitoramento.

## 7. Impacto Frontend React

- Formulário de criação/edição de bug interno.
- Campos controlados para severidade, componente, ambiente, origem e status.
- Relação opcional com ticket ou erro existente.

## 8. Impacto Backend

- Endpoints admin para criar e atualizar bug/incidente.
- Validação de campos obrigatórios.
- Sanitização e auditoria.
- Consulta integrada com a listagem da US-164.

## 9. Impacto DB

- Entidade ou tabela de bug/incidente operacional.
- Histórico de alterações e vínculos opcionais.
- Índices por severidade, status, componente, ambiente e data.

## 10. Critérios de aceite

### CA-001 — Bug interno criado

Dado que engenharia preenche os campos obrigatórios,
quando salvar o registro,
então o bug/incidente deve aparecer na listagem de bugs.

### CA-002 — Campos obrigatórios validados

Dado que severidade ou ambiente não foi informado,
quando tentar salvar,
então o sistema deve impedir a criação e indicar o campo pendente.

### CA-003 — Criação auditada

Dado que um bug interno é criado,
quando o registro for salvo,
então deve existir AuditLog com ator admin, recurso e correlationId quando disponível.

## 11. Decisão registrada

> Bugs e incidentes podem ser registrados manualmente no admin para complementar logs automáticos, sempre com campos mínimos, sanitização e auditoria.
