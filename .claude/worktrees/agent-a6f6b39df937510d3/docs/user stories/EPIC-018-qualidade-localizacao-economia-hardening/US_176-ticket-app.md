---
title: US-176 — Abrir ticket pelo app
sidebar_position: 176
---

# US-176 — Abrir ticket pelo app

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-176 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P0 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Usuário em Trial ou assinante |
| Status | Planejada |

## 2. História do usuário

Como **usuário do AWAKEN**, quero **abrir ticket pelo app**, para **enviar dúvida, relato ou sugestão diretamente pelo produto**.

## 3. Objetivo

Criar fluxo de abertura de ticket com categoria, idioma, descrição, versão do app e correlationId quando existir.

## 4. Escopo

### Entra nesta US

- Abrir ticket pelo app.
- Categorias: relato, dúvida, sugestão.
- Descrição do usuário.
- Idioma atual do app.
- Versão/build do app.
- Status inicial do ticket.
- Confirmação de envio.

### Fora desta US

- Painel admin de triagem.
- Chat em tempo real.
- Anexos no MVP.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Ticket deve ter categoria obrigatória. |
| RN-002 | Ticket deve registrar idioma do usuário. |
| RN-003 | Payload não deve incluir dados sensíveis automaticamente. |
| RN-004 | Ticket deve ter status inicial. |
| RN-005 | EPIC-017 consome tickets criados pelo app. |

## 6. Fluxo principal

1. Usuário abre configurações ou ajuda.
2. Seleciona abrir ticket.
3. Escolhe categoria.
4. Descreve a solicitação.
5. App envia ticket ao backend.
6. Backend cria `SupportTicket`.
7. App exibe confirmação.

## 7. Impacto Flutter

- Tela/formulário de ticket.
- Validação de categoria e descrição.
- Feedback de envio.
- Integração com configurações.

## 8. Impacto Backend

- Endpoint de criação de ticket.
- Entidade `SupportTicket`.
- Sanitização de payload.
- Logs com correlationId.

## 9. Impacto DB

Campos sugeridos:

- id;
- userId;
- category;
- status;
- language;
- description;
- appVersion;
- correlationId;
- createdAt.

## 10. Critérios de aceite

### CA-001 — Ticket criado

Dado que o usuário preenche categoria e descrição,
quando enviar,
então o backend deve criar ticket com status inicial.

### CA-002 — Payload seguro

Dado que o app envia contexto técnico,
quando o ticket for salvo,
então não deve incluir dados físicos, dores ou limitações automaticamente.

## 11. Decisão registrada

> O app é a porta de entrada oficial de tickets; o EPIC-017 consome os tickets criados.
