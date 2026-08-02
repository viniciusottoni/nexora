---
title: US-123 — Receber aviso de proximidade do fim do trial
sidebar_position: 123
---

# US-123 — Receber aviso de proximidade do fim do trial

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-123 |
| Épico | EPIC-013 — Notificações e Retenção |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial |
| Plano | Trial |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Integração principal | Firebase Cloud Messaging |
| Status | Planejada |

## 2. História do usuário

Como **usuário em trial**, quero **receber aviso quando meu teste estiver perto de acabar**, para **decidir assinar sem ser pego de surpresa**.

## 3. Contexto

O modelo comercial do AWAKEN exige assinatura após o trial. O aviso de fim de trial deve ser transparente, útil e não enganoso, evitando pressão excessiva.

## 4. Objetivo

Enviar notificação informando proximidade do fim do trial para usuário elegível, com deep link para planos/paywall.

## 5. Escopo

### Entra nesta US

- Identificar trial próximo do fim.
- Enviar aviso apenas para usuário em trial ativo.
- Não enviar para assinantes.
- Respeitar consentimento e limite de notificações.
- Deep link para tela de planos/paywall.

### Fora desta US

- Descontos personalizados.
- Campanhas avançadas.
- Mensagens enganosas de urgência.
- Retentativas agressivas.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Notificações de trial devem ser claras e não enganosas. |
| RN-002 | Usuário com assinatura ativa não deve receber alerta de fim de trial. |
| RN-003 | Usuário deve ter consentido notificações. |
| RN-004 | Aviso deve respeitar limite diário de notificações. |
| RN-005 | Conteúdo deve informar que o acesso será bloqueado após o fim do trial se não houver assinatura. |
| RN-006 | Deep link deve levar para planos/paywall. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode receber aviso se consentiu. |
| Premium Mensal | Não recebe aviso de fim de trial. |
| Premium Anual | Não recebe aviso de fim de trial. |
| Trial expirado | Usa regra de reativação. |
| Assinatura expirada | Usa regra de reativação. |
| Visitante | Não recebe. |

## 8. Fluxo principal

1. Job verifica usuários em trial ativo.
2. Sistema calcula proximidade do fim do trial.
3. Sistema remove usuários já assinantes.
4. Sistema valida consentimento e limite diário.
5. Push é enviado com mensagem clara.
6. Clique abre tela de planos/paywall.

## 9. Fluxos alternativos

### 9.1. Usuário já assinou

Não enviar aviso.

### 9.2. Limite diário atingido

Não enviar ou reagendar conforme regra de prioridade.

### 9.3. Permissão negada

Não enviar push.

## 10. Estados esperados

- trial próximo do fim;
- aviso enviado;
- usuário assinante ignorado;
- limite atingido;
- permissão negada;
- erro de envio.

## 11. Impacto Flutter

- Deep link para planos/paywall.
- Tratamento de abertura por push.
- Textos localizados.

## 12. Impacto Backend

- Job de trial ending.
- Consulta de status de trial/assinatura.
- Envio via Firebase Admin SDK.
- Registro de envio e limite.

## 13. Impacto DB

Entidades/campos:

- Subscription;
- Trial;
- NotificationPreference;
- NotificationLog.

## 14. Impacto Gamificação

- Preserva expectativa do usuário sobre continuidade de progresso.
- Não concede XP.

## 15. Impacto Monetização

- Ajuda conversão com transparência.
- Evita dark pattern e surpresa no bloqueio.

## 16. Contrato interno sugerido

```txt
POST /internal/notifications/trial-ending/run
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| trial_ending_notification_sent | Quando aviso é enviado. |
| trial_ending_notification_opened | Quando usuário abre o aviso. |

## 18. Critérios de aceite

### CA-001 — Trial próximo do fim

Dado que o usuário está em trial ativo próximo do fim,
Quando o job rodar,
Então deve receber aviso se consentiu notificações.

### CA-002 — Assinante não recebe

Dado que o usuário já assinou,
Quando o job rodar,
Então ele não deve receber aviso de fim de trial.

## 19. Critérios de teste QA

- trial próximo do fim;
- trial não próximo do fim;
- usuário já assinante;
- permissão negada;
- limite diário;
- deep link para paywall;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

Aviso de fim de trial deve ser transparente, objetivo e respeitoso, ajudando o usuário a decidir sem pressão enganosa.
