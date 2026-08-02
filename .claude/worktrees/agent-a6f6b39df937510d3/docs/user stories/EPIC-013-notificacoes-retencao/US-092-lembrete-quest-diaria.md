---
title: US-092 — Receber lembrete da quest diária
sidebar_position: 92
---

# US-092 — Receber lembrete da quest diária

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-092 |
| Épico | EPIC-013 — Notificações e Retenção |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Integração principal | Firebase Cloud Messaging |
| Status | Planejada |

## 2. História do usuário

Como **usuário com acesso ativo**, quero **receber um lembrete da quest diária**, para **não esquecer meu treino do dia**.

## 3. Contexto

O AWAKEN depende de hábito diário. O lembrete deve ser útil, curto e enviado de forma controlada, respeitando consentimento, horário preferido e limite de notificações.

## 4. Objetivo

Enviar lembrete de quest diária para usuários com permissão ativa e quest ainda não concluída.

## 5. Escopo

### Entra nesta US

- Enviar lembrete da quest diária.
- Respeitar permissão de notificações.
- Respeitar horário preferido quando configurado.
- Evitar envio se a quest já foi concluída.
- Deep link para a quest diária.

### Fora desta US

- Campanhas comerciais avançadas.
- Notificações sociais.
- CRM completo.
- Lembretes múltiplos agressivos.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Só enviar para usuário que consentiu notificações. |
| RN-002 | Só enviar para usuário com acesso ativo. |
| RN-003 | Não enviar se a quest diária já foi concluída. |
| RN-004 | Respeitar limite diário de notificações. |
| RN-005 | Se houver horário preferido, usar esse horário. |
| RN-006 | O tom deve ser motivador, não punitivo. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode receber com trial ativo e consentimento. |
| Premium Mensal | Pode receber com consentimento. |
| Premium Anual | Pode receber com consentimento. |
| Trial expirado | Não recebe lembrete de quest diária. |
| Assinatura expirada | Não recebe lembrete de quest diária. |
| Visitante | Não recebe. |

## 8. Fluxo principal

1. Scheduler identifica usuários elegíveis.
2. Sistema verifica consentimento e acesso ativo.
3. Sistema verifica se a quest diária foi concluída.
4. Sistema dispara push no horário adequado.
5. Usuário toca na notificação.
6. App abre a quest diária.

## 9. Fluxos alternativos

### 9.1. Quest já concluída

Não enviar notificação.

### 9.2. Permissão negada

Não enviar notificação.

### 9.3. Token inválido

Registrar falha e não bloquear o app.

## 10. Estados esperados

- elegível;
- não elegível;
- enviado;
- ignorado por quest concluída;
- token inválido;
- erro de envio.

## 11. Impacto Flutter

- Deep link para quest.
- Tratamento de clique em push.
- Textos localizados.

## 12. Impacto Backend

- Job/scheduler de lembretes.
- Consulta de elegibilidade.
- Envio via Firebase Admin SDK.
- Registro de envio para limite diário.

## 13. Impacto DB

Entidades/campos:

- NotificationPreference;
- Quest;
- lastNotificationSentAt;
- preferredReminderTime;
- pushToken.

## 14. Impacto Gamificação

- Ajuda a proteger consistência diária.
- Não concede XP por abrir notificação.

## 15. Impacto Monetização

- Aumenta retenção de usuários ativos.
- Não deve ser usado como pressão comercial.

## 16. Contrato interno sugerido

```txt
POST /internal/notifications/daily-quest-reminders/run
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| daily_quest_reminder_sent | Quando lembrete é enviado. |
| daily_quest_reminder_opened | Quando usuário abre a notificação. |

## 18. Critérios de aceite

### CA-001 — Envio válido

Dado que o usuário consentiu e não concluiu a quest,
Quando chegar o horário de lembrete,
Então deve receber uma notificação.

### CA-002 — Sem envio indevido

Dado que a quest já foi concluída,
Quando o job rodar,
Então nenhuma notificação deve ser enviada.

## 19. Critérios de teste QA

- lembrete com permissão ativa;
- sem permissão;
- quest concluída;
- acesso expirado;
- deep link para quest;
- limite diário respeitado;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

O lembrete de quest diária deve reforçar hábito com baixo ruído, respeitando consentimento, acesso ativo e estado real da quest.
