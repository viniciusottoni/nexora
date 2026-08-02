---
title: US-135 — Receber aviso quando quest diária não foi completada e penalidade de XP foi aplicada
sidebar_position: 135
---

# US-135 — Receber aviso quando quest diária não foi completada e penalidade de XP foi aplicada

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-135 |
| Épico | EPIC-013 — Notificações e Retenção |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Integração principal | Firebase Cloud Messaging |
| Status | Planejada |

## 2. História do usuário

Como **usuário com acesso ativo**, quero **receber aviso quando perdi a quest diária e a penalidade de XP foi aplicada**, para **entender o impacto e voltar no próximo dia sem desanimar**.

## 3. Contexto

Se o usuário não completa a quest diária, o sistema pode aplicar penalidade de XP conforme regras de gamificação. A notificação deve ser enviada após a virada de dia e com tom encorajador, não punitivo.

## 4. Objetivo

Enviar notificação após a virada de dia quando a quest diária não foi concluída e a penalidade foi aplicada, respeitando acesso ativo, consentimento e limite de frequência.

## 5. Escopo

### Entra nesta US

- Detectar quest diária não concluída.
- Validar que penalidade de XP foi aplicada.
- Enviar aviso após a virada de dia.
- Usar tom encorajador.
- Deep link para Home ou próxima quest.
- Respeitar consentimento e limite de notificações.

### Fora desta US

- Mensagens punitivas.
- Exposição detalhada de fórmula de penalidade.
- Recuperação automática da penalidade.
- Compra de item para anular penalidade.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A notificação deve ser enviada após a virada de dia. |
| RN-002 | Enviar apenas se o usuário tiver acesso ativo. |
| RN-003 | Enviar apenas se a quest diária não foi completada. |
| RN-004 | Enviar apenas se a penalidade de XP foi aplicada. |
| RN-005 | O tom deve ser encorajador, não punitivo. |
| RN-006 | Respeitar consentimento e limite de notificações. |
| RN-007 | Não enviar se o usuário concluiu a quest diária. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode receber com trial ativo e consentimento. |
| Premium Mensal | Pode receber com consentimento. |
| Premium Anual | Pode receber com consentimento. |
| Trial expirado | Não recebe este aviso. |
| Assinatura expirada | Não recebe este aviso. |
| Visitante | Não recebe. |

## 8. Fluxo principal

1. Após a virada de dia, job avalia quests diárias do dia anterior.
2. Sistema identifica usuários com acesso ativo.
3. Sistema verifica quest diária não concluída.
4. Sistema confirma penalidade de XP aplicada.
5. Sistema valida consentimento e limite de notificações.
6. Push é enviado com mensagem encorajadora.
7. Clique leva para Home ou próxima quest.

## 9. Fluxos alternativos

### 9.1. Quest foi concluída

Não enviar notificação.

### 9.2. Penalidade não aplicada

Não enviar este aviso.

### 9.3. Acesso expirado

Não enviar este aviso; comunicação de reativação segue US-124.

## 10. Estados esperados

- quest perdida;
- penalidade aplicada;
- aviso enviado;
- ignorado por quest concluída;
- ignorado por acesso expirado;
- ignorado por limite;
- erro de envio.

## 11. Impacto Flutter

- Deep link para Home/próxima quest.
- Tratamento de clique em notificação.
- Textos localizados.

## 12. Impacto Backend

- Job após virada de dia.
- Consulta de quest diária e penalidade.
- Integração com EPIC-009.
- Envio via Firebase Admin SDK.
- Registro da decisão de envio.

## 13. Impacto DB

Entidades/campos:

- Quest;
- QuestLog;
- HunterProgress;
- NotificationPreference;
- NotificationLog;
- xpPenaltyApplied.

## 14. Impacto Gamificação

- Comunica consequência já aplicada sem reforço negativo.
- Ajuda usuário a retomar no próximo ciclo.
- Não concede XP por abrir a notificação.

## 15. Impacto Monetização

- Reforça retorno diário em usuários ativos.
- Não deve ser enviado para usuários bloqueados.

## 16. Contrato interno sugerido

```txt
POST /internal/notifications/missed-daily-quest/run
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| missed_daily_quest_notification_sent | Quando aviso é enviado. |
| missed_daily_quest_notification_opened | Quando usuário abre aviso. |

## 18. Critérios de aceite

### CA-001 — Quest perdida com penalidade

Dado que o usuário não concluiu a quest diária e a penalidade foi aplicada,
Quando o job rodar após a virada de dia,
Então deve receber aviso se tiver acesso ativo e consentimento.

### CA-002 — Quest concluída sem aviso

Dado que o usuário concluiu a quest diária,
Quando o job rodar,
Então não deve receber aviso de quest perdida.

## 19. Critérios de teste QA

- quest diária perdida;
- penalidade aplicada;
- quest concluída;
- acesso expirado;
- permissão negada;
- limite diário;
- deep link para Home/próxima quest;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

O aviso de quest perdida deve informar a consequência de forma leve e motivar retorno, nunca punir emocionalmente o usuário.
