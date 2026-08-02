---
title: US-125 — Rastrear início, contagem e expiração do trial
sidebar_position: 125
---

# US-125 — Rastrear início, contagem e expiração do trial

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-125 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto e Engenharia |
| Integrações | Firebase Analytics e logs de backend |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **rastrear início, contagem e expiração do trial**, para **medir ativação, urgência real e bloqueio pós-teste**.

## 3. Contexto

O trial de 7 dias é o primeiro contato completo do usuário com o AWAKEN. Medir início, dias restantes e expiração é essencial para conversão.

## 4. Objetivo

Registrar eventos comerciais do ciclo de trial sem enganar o usuário e sem expor dados de pagamento.

## 5. Escopo

### Entra nesta US

- Evento de trial iniciado.
- Evento de visualização de dias restantes.
- Evento de trial próximo do fim.
- Evento de trial expirado.
- Evento de paywall pós-trial.

### Fora desta US

- Promoções personalizadas.
- A/B test de preço.
- Dados sensíveis de pagamento.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Trial iniciado deve ser rastreado uma única vez por usuário. |
| RN-002 | Expiração do trial deve ser rastreada quando acesso for bloqueado. |
| RN-003 | Usuário que assinou não deve gerar evento de trial expirado indevido. |
| RN-004 | Eventos não devem conter dados de cartão ou pagamento. |

## 7. Eventos sugeridos

| Evento | Quando dispara |
|---|---|
| trial_started | Quando trial começa. |
| trial_days_remaining_viewed | Quando usuário vê dias restantes. |
| trial_ending_soon | Quando trial está próximo do fim. |
| trial_expired | Quando trial expira. |
| paywall_after_trial_viewed | Quando paywall pós-trial aparece. |

## 8. Payload mínimo

```json
{
  "days_remaining": 2,
  "source": "home",
  "access_status": "trial_active"
}
```

## 9. Impacto Flutter

- Instrumentar telas de trial, Home e paywall.
- Evitar duplicidade em reconstrução de tela.
- Não enviar dados sensíveis.

## 10. Impacto Backend

- Logar início e expiração do trial.
- Garantir evento idempotente de expiração.
- CorrelationId em falhas de status de acesso.

## 11. Critérios de aceite

### CA-001 — Trial iniciado

Dado que o usuário inicia o trial,
Quando o status for salvo,
Então `trial_started` deve ser registrado.

### CA-002 — Trial expirado

Dado que o trial terminou sem assinatura,
Quando o acesso for bloqueado,
Então `trial_expired` deve ser registrado.

## 12. Decisão registrada

O ciclo do trial deve ser mensurado com precisão porque é central para ativação e conversão do AWAKEN.
