---
title: US-093 — Receber alerta de streak em risco
sidebar_position: 93
---

# US-093 — Receber alerta de streak em risco

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-093 |
| Épico | EPIC-013 — Notificações e Retenção |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Integração principal | Firebase Cloud Messaging |
| Status | Planejada |

## 2. História do usuário

Como **usuário com streak ativo**, quero **receber alerta quando meu streak estiver em risco**, para **ter chance de concluir a quest antes de perder a sequência**.

## 3. Contexto

O streak é um dos principais mecanismos de retenção. O alerta deve proteger o hábito, mas sem gerar ansiedade ou pressão excessiva.

## 4. Objetivo

Enviar alerta quando o usuário ainda não completou a quest diária e está próximo de perder o streak.

## 5. Escopo

### Entra nesta US

- Detectar streak ativo em risco.
- Verificar quest diária não concluída.
- Enviar push com tom encorajador.
- Respeitar consentimento e limite diário.
- Deep link para a quest diária.

### Fora desta US

- Recuperação automática de streak.
- Compra de item para preservar streak.
- Campanhas agressivas.
- Notificações sociais.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Só enviar se o usuário consentiu notificações. |
| RN-002 | Só enviar se o usuário possui acesso ativo. |
| RN-003 | Só enviar se houver streak ativo em risco. |
| RN-004 | Não enviar se a quest diária já foi concluída. |
| RN-005 | Respeitar limite de notificações do dia. |
| RN-006 | O texto deve ser encorajador, não punitivo. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode receber com trial ativo e consentimento. |
| Premium Mensal | Pode receber com consentimento. |
| Premium Anual | Pode receber com consentimento. |
| Trial expirado | Não recebe alerta de streak em risco. |
| Assinatura expirada | Não recebe alerta de streak em risco. |
| Visitante | Não recebe. |

## 8. Fluxo principal

1. Job de retenção roda próximo ao fim do dia.
2. Sistema identifica usuários com streak ativo.
3. Sistema verifica se a quest diária ainda não foi concluída.
4. Sistema valida consentimento, acesso e limite diário.
5. Push é enviado com deep link para a quest.

## 9. Fluxos alternativos

### 9.1. Quest concluída

Não enviar alerta.

### 9.2. Sem streak ativo

Não enviar alerta de risco.

### 9.3. Limite diário atingido

Não enviar nova notificação.

## 10. Estados esperados

- streak seguro;
- streak em risco;
- notificação enviada;
- notificação ignorada;
- limite atingido;
- erro de envio.

## 11. Impacto Flutter

- Deep link para quest.
- Tratamento de abertura da notificação.
- Textos localizados.

## 12. Impacto Backend

- Job de avaliação de streak.
- Integração com EPIC-009.
- Regras de elegibilidade e limite diário.
- Envio via Firebase Admin SDK.

## 13. Impacto DB

Entidades/campos:

- HunterProgress;
- Quest;
- NotificationPreference;
- lastNotificationSentAt.

## 14. Impacto Gamificação

- Protege streak do usuário.
- Não altera streak por si só.
- Não concede XP por abrir push.

## 15. Impacto Monetização

- Aumenta retenção por hábito, sem pressão abusiva.

## 16. Contrato interno sugerido

```txt
POST /internal/notifications/streak-risk/run
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| streak_risk_notification_sent | Quando alerta é enviado. |
| streak_risk_notification_opened | Quando usuário abre o alerta. |

## 18. Critérios de aceite

### CA-001 — Streak em risco

Dado que o usuário tem streak ativo e não concluiu a quest,
Quando o horário de risco chegar,
Então deve receber alerta se consentiu notificações.

### CA-002 — Sem alerta indevido

Dado que o usuário já concluiu a quest,
Quando o job rodar,
Então não deve receber alerta.

## 19. Critérios de teste QA

- streak ativo em risco;
- quest concluída;
- sem streak ativo;
- acesso expirado;
- permissão negada;
- limite diário atingido;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

O alerta de streak deve proteger o hábito com tom motivador, sem transformar a gamificação em pressão negativa.
