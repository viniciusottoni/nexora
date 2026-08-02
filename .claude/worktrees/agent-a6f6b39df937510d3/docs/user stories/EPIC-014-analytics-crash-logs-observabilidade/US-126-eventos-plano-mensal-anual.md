---
title: US-126 — Rastrear escolha de plano mensal ou anual
sidebar_position: 126
---

# US-126 — Rastrear escolha de plano mensal ou anual

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-126 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto e Engenharia |
| Integrações | Firebase Analytics e RevenueCat |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **rastrear escolha de plano mensal ou anual**, para **entender preferência comercial e conversão por plano**.

## 3. Contexto

O AWAKEN possui plano mensal e anual. A escolha do plano precisa ser rastreada antes da compra e conciliada com o status de assinatura depois.

## 4. Objetivo

Diferenciar cliques, tentativas e sucesso de assinatura mensal ou anual.

## 5. Escopo

### Entra nesta US

- Clique no plano mensal.
- Clique no plano anual.
- Início do fluxo de compra.
- Compra concluída.
- Falha ou cancelamento do fluxo.
- Restore purchase quando aplicável.

### Fora desta US

- Dados de cartão.
- Dados fiscais.
- A/B test de preço.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Plano mensal e anual devem ter eventos separados. |
| RN-002 | Eventos não devem conter dados sensíveis de pagamento. |
| RN-003 | Compra concluída deve ser conciliada com entitlement ativo. |
| RN-004 | Falhas devem ter código funcional genérico. |

## 7. Eventos sugeridos

| Evento | Quando dispara |
|---|---|
| monthly_plan_selected | Quando plano mensal é escolhido. |
| annual_plan_selected | Quando plano anual é escolhido. |
| purchase_started | Quando fluxo de compra inicia. |
| subscription_started | Quando assinatura fica ativa. |
| purchase_failed | Quando compra falha. |
| purchase_cancelled | Quando usuário cancela fluxo. |

## 8. Payload mínimo

```json
{
  "plan": "annual",
  "source": "paywall_after_trial",
  "result": "started"
}
```

## 9. Impacto Flutter

- Instrumentar CTAs de plano.
- Registrar início, falha e cancelamento de compra.
- Não enviar dados sensíveis.

## 10. Impacto Backend

- Registrar status de entitlement.
- Conciliar eventos com RevenueCat quando aplicável.

## 11. Critérios de aceite

### CA-001 — Plano mensal

Dado que o usuário escolhe plano mensal,
Quando toca no CTA,
Então deve disparar `monthly_plan_selected`.

### CA-002 — Plano anual

Dado que o usuário escolhe plano anual,
Quando toca no CTA,
Então deve disparar `annual_plan_selected`.

## 12. Decisão registrada

A escolha de plano deve ser rastreada sem expor dados de pagamento, permitindo medir preferência por mensal ou anual.
