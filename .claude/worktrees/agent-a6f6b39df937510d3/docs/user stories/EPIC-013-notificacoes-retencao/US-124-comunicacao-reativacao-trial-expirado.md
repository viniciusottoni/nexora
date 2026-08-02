---
title: US-124 — Receber comunicação de reativação após trial expirado
sidebar_position: 124
---

# US-124 — Receber comunicação de reativação após trial expirado

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-124 |
| Épico | EPIC-013 — Notificações e Retenção |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Trial expirado ou assinatura expirada |
| Plano | Trial expirado, Assinatura expirada |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Integração principal | Firebase Cloud Messaging |
| Status | Planejada |

## 2. História do usuário

Como **usuário com acesso expirado**, quero **receber comunicação ocasional de reativação**, para **saber que posso voltar ao AWAKEN sem perder minha jornada**.

## 3. Contexto

Após expiração, o app pode comunicar reativação com baixa frequência. A mensagem deve lembrar que o progresso foi preservado e levar o usuário à assinatura, sem insistência agressiva.

## 4. Objetivo

Enviar comunicação de reativação para usuários bloqueados que consentiram notificações, respeitando baixa frequência e transparência.

## 5. Escopo

### Entra nesta US

- Identificar usuários com trial expirado ou assinatura expirada.
- Enviar comunicação de reativação com baixa frequência.
- Reforçar que progresso foi preservado.
- Deep link para paywall/planos.
- Respeitar consentimento e limite de envio.

### Fora desta US

- Campanhas complexas de winback.
- Descontos personalizados.
- Sequências agressivas.
- Retargeting externo.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Usuário bloqueado pode receber comunicação de reativação com baixa frequência. |
| RN-002 | Usuário deve ter consentido notificações. |
| RN-003 | Usuário com assinatura ativa não deve receber reativação. |
| RN-004 | Mensagem deve ser clara e não enganosa. |
| RN-005 | Frequência deve ser menor que lembretes de usuário ativo. |
| RN-006 | Deep link deve levar para paywall/planos. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Trial expirado | Pode receber reativação com baixa frequência se consentiu. |
| Assinatura expirada | Pode receber reativação com baixa frequência se consentiu. |
| Usuário em Trial | Não recebe reativação. |
| Premium Mensal | Não recebe reativação. |
| Premium Anual | Não recebe reativação. |
| Visitante | Não recebe. |

## 8. Fluxo principal

1. Job identifica usuários com acesso expirado.
2. Sistema verifica consentimento e frequência.
3. Sistema confirma que o usuário não possui assinatura ativa.
4. Push é enviado com mensagem de retorno.
5. Clique abre paywall/planos.

## 9. Fluxos alternativos

### 9.1. Usuário reativou assinatura

Não enviar comunicação de reativação.

### 9.2. Frequência mínima não atingida

Não enviar e registrar decisão.

### 9.3. Permissão negada

Não enviar push.

## 10. Estados esperados

- elegível para reativação;
- enviado;
- ignorado por frequência;
- ignorado por assinatura ativa;
- permissão negada;
- erro de envio.

## 11. Impacto Flutter

- Deep link para paywall/planos.
- Tratamento de abertura por push.
- Mensagem coerente com progresso preservado.

## 12. Impacto Backend

- Job de reativação.
- Validação de status de acesso.
- Controle de frequência baixa.
- Envio via Firebase Admin SDK.

## 13. Impacto DB

Entidades/campos:

- Subscription;
- NotificationPreference;
- NotificationLog;
- lastReactivationNotificationSentAt.

## 14. Impacto Gamificação

- Reforça que progresso e jornada foram preservados.
- Não concede XP.

## 15. Impacto Monetização

- Suporta reativação de usuários expirados sem dark pattern.

## 16. Contrato interno sugerido

```txt
POST /internal/notifications/reactivation/run
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| reactivation_notification_sent | Quando comunicação é enviada. |
| reactivation_notification_opened | Quando usuário abre a comunicação. |

## 18. Critérios de aceite

### CA-001 — Expirado elegível

Dado que o usuário está com acesso expirado e consentiu notificações,
Quando a frequência permitir,
Então pode receber comunicação de reativação.

### CA-002 — Assinante não recebe

Dado que o usuário reativou a assinatura,
Quando o job rodar,
Então não deve receber comunicação de reativação.

## 19. Critérios de teste QA

- trial expirado;
- assinatura expirada;
- assinatura reativada;
- frequência baixa respeitada;
- permissão negada;
- deep link para paywall;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

Reativação deve lembrar o usuário de que sua jornada está preservada, mas com baixa frequência e sem pressão abusiva.
