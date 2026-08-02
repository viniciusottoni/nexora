---
title: EPIC-013 — Notificações e Retenção
sidebar_position: 13
---

# EPIC-013 — Notificações e Retenção

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-013 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P1 |
| Perfil principal | Usuário em Trial ou assinante |
| Planos impactados | Trial, Mensal e Anual |
| Integração principal | Firebase Cloud Messaging |
| Status | Planejado |

## 2. Objetivo

Enviar lembretes simples e úteis para aumentar retorno diário, proteger streak e comunicar momentos importantes do trial, sempre respeitando consentimento e limite de frequência.

## 3. Escopo

### Entra neste épico

- Permissão de notificações.
- Lembrete de quest diária.
- Alerta de streak em risco.
- Horário preferido.
- Limite de envio por usuário.
- Aviso de fim do trial.
- Comunicação de reativação.
- Aviso de quest diária perdida com impacto de XP aplicado.

### Fora deste épico

- CRM avançado.
- Segmentação complexa.
- Notificações sociais.
- Campanhas promocionais pesadas.

## 4. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-091 | Permitir notificações | P1 | [Abrir](./US-091-permitir-notificacoes.md) |
| US-092 | Receber lembrete da quest diária | P1 | [Abrir](./US-092-lembrete-quest-diaria.md) |
| US-093 | Receber alerta de streak em risco | P1 | [Abrir](./US-093-alerta-streak-risco.md) |
| US-094 | Configurar horário preferido | P1 | [Abrir](./US-094-configurar-horario-preferido.md) |
| US-095 | Evitar notificações excessivas | P1 | [Abrir](./US-095-evitar-notificacoes-excessivas.md) |
| US-123 | Receber aviso de proximidade do fim do trial | P1 | [Abrir](./US-123-aviso-fim-trial.md) |
| US-124 | Receber comunicação de reativação após trial expirado | P1 | [Abrir](./US-124-comunicacao-reativacao-trial-expirado.md) |
| US-135 | Receber aviso quando quest diária não foi completada e penalidade de XP foi aplicada | P1 | [Abrir](./US-135-aviso-quest-perdida-penalidade-xp.md) |

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-013-001 | Notificações dependem de consentimento do usuário. |
| RN-EPIC-013-002 | O usuário pode definir horário preferido quando a funcionalidade estiver ativa. |
| RN-EPIC-013-003 | O sistema deve evitar múltiplas notificações desnecessárias no mesmo dia. |
| RN-EPIC-013-004 | Notificações de trial devem ser claras e não enganosas. |
| RN-EPIC-013-005 | Usuário com assinatura ativa não deve receber alerta de fim de trial. |
| RN-EPIC-013-006 | Usuário bloqueado pode receber comunicação de reativação com baixa frequência. |
| RN-EPIC-013-007 | Aviso de quest perdida deve ocorrer após a virada do dia, apenas quando a quest não foi completada e o usuário tem acesso ativo. |

## 6. Impactos técnicos

### Flutter

- Solicitação de permissão.
- Tela de horário preferido, se implementada.
- Deep link para quest, Home ou paywall.
- Tratamento de abertura por notificação.

### Backend

- Registro de preferência de notificação.
- Registro de push token.
- Jobs de envio.
- Regras de elegibilidade, prioridade e limite.
- Integração com Firebase Admin SDK.

### Banco de dados

- NotificationPreference.
- NotificationLog.
- preferredReminderTime.
- pushToken.
- lastNotificationSentAt.

## 7. Dependências

- EPIC-003 para status de trial e assinatura.
- EPIC-009 para streak e penalidade de XP.
- Firebase configurado.

## 8. Critérios de aceite do épico

- Usuário consegue permitir ou negar notificações.
- Lembrete de quest é disparado de forma controlada.
- Alerta de streak respeita regra de retenção.
- Aviso de fim de trial aparece apenas para usuário em trial.
- Comunicação de reativação respeita baixa frequência.
- Sistema evita excesso de notificações.

## 9. Decisão registrada

Notificações são P1 no MVP. Elas devem reforçar hábito e transparência comercial, sempre com consentimento, baixa frequência e tom respeitoso.
