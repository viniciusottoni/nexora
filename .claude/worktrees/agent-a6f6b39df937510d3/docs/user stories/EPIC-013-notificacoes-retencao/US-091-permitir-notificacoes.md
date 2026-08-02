---
title: US-091 — Permitir notificações
sidebar_position: 91
---

# US-091 — Permitir notificações

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-091 |
| Épico | EPIC-013 — Notificações e Retenção |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Integração principal | Firebase Cloud Messaging |
| Status | Planejada |

## 2. História do usuário

Como **usuário com acesso ativo**, quero **permitir ou negar notificações**, para **controlar se o AWAKEN pode me lembrar de quests, streak e informações importantes**.

## 3. Contexto

Notificações só podem existir com consentimento claro. O AWAKEN deve pedir permissão de forma contextual, explicando valor sem pressionar o usuário.

## 4. Objetivo

Solicitar permissão de notificações e registrar a preferência do usuário de forma transparente.

## 5. Escopo

### Entra nesta US

- Solicitar permissão de push notification.
- Explicar por que o app quer enviar notificações.
- Registrar consentimento ou negação.
- Salvar push token quando permitido.
- Permitir funcionamento sem notificações.

### Fora desta US

- Campanhas avançadas.
- Segmentação complexa.
- Push marketing pesado.
- Notificações sociais.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Notificações dependem de consentimento do usuário. |
| RN-002 | Usuário pode negar permissão sem bloquear uso do app. |
| RN-003 | Push token só deve ser salvo quando disponível e permitido. |
| RN-004 | O app deve explicar o benefício da permissão antes ou durante a solicitação. |
| RN-005 | Se permissão for negada, o app não deve insistir repetidamente. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não recebe notificações. |
| Usuário em Trial | Pode permitir notificações. |
| Premium Mensal | Pode permitir notificações. |
| Premium Anual | Pode permitir notificações. |
| Trial expirado | Pode receber comunicação de reativação apenas se consentiu. |
| Assinatura expirada | Pode receber comunicação de reativação apenas se consentiu. |

## 8. Fluxo principal

1. Usuário acessa momento contextual para ativar notificações.
2. App explica o benefício.
3. Usuário aceita.
4. Sistema solicita permissão nativa.
5. App registra o status e o push token.
6. Backend salva preferência.

## 9. Fluxos alternativos

### 9.1. Usuário nega permissão

Registrar preferência negada e não enviar push.

### 9.2. Token indisponível

Registrar permissão, mas tentar sincronizar token futuramente.

## 10. Estados esperados

- permissão não solicitada;
- solicitando;
- permitida;
- negada;
- token registrado;
- erro de sincronização.

## 11. Impacto Flutter

- Fluxo de permissão.
- Integração com FCM.
- Armazenamento local do status.
- Tratamento de permissão negada.

## 12. Impacto Backend

- Endpoint para salvar preferência.
- Endpoint para salvar/atualizar push token.
- Validação por usuário.

## 13. Impacto DB

Entidade sugerida: NotificationPreference.

Campos:

- userId;
- pushEnabled;
- pushToken;
- permissionStatus;
- updatedAt.

## 14. Impacto Gamificação

- Permite lembretes futuros de quest e streak.
- Não concede XP por permitir notificações.

## 15. Impacto Monetização

- Suporta aviso de fim de trial e reativação, sempre com consentimento.

## 16. Contrato API sugerido

```txt
PUT /api/notifications/preferences
```

Request conceitual:

```json
{
  "pushEnabled": true,
  "pushToken": "fcm-token"
}
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| notification_permission_requested | Quando a permissão é solicitada. |
| notification_permission_granted | Quando usuário permite. |
| notification_permission_denied | Quando usuário nega. |

## 18. Critérios de aceite

### CA-001 — Permissão concedida

Dado que o usuário aceita notificações,
Quando o token FCM estiver disponível,
Então a preferência e o token devem ser salvos.

### CA-002 — Permissão negada

Dado que o usuário nega notificações,
Quando o fluxo terminar,
Então o app deve continuar funcionando sem enviar push.

## 19. Critérios de teste QA

- permissão concedida;
- permissão negada;
- token indisponível;
- atualização de token;
- reinstalação do app;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

Notificações no AWAKEN devem ser consentidas, úteis e opcionais; negar push não bloqueia a experiência principal.
