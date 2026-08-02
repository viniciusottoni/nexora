---
title: US-099 — Rastrear visualização de trial, planos e paywall
sidebar_position: 99
---

# US-099 — Rastrear visualização de trial, planos e paywall

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-099 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto, Engenharia e QA |
| Integrações | Firebase Analytics |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **rastrear visualizações de trial, planos e paywall**, para **entender conversão, bloqueios e pontos de abandono do modelo comercial**.

## 3. Contexto

O MVP não possui plano gratuito permanente. O usuário passa por trial e depois precisa assinar. É obrigatório medir exposição aos planos, paywall pós-trial e tentativas de restauração.

## 4. Objetivo

Registrar eventos comerciais básicos de visualização e interação com trial, planos e paywall.

## 5. Escopo

### Entra nesta US

- Visualização da tela de trial/planos.
- Visualização de paywall pós-trial.
- Clique em plano mensal.
- Clique em plano anual.
- Tentativa de restaurar compra.
- Bloqueio por trial ou assinatura expirada.

### Fora desta US

- BI avançado.
- A/B test de preço.
- Segmentação de campanha.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Eventos comerciais devem indicar origem da visualização. |
| RN-002 | Usuário assinante não deve gerar evento de paywall pós-trial indevido. |
| RN-003 | Eventos não devem incluir dados de pagamento sensíveis. |
| RN-004 | Cliques em plano mensal e anual devem ser diferenciados. |
| RN-005 | Bloqueios de acesso devem ser rastreados. |

## 7. Eventos sugeridos

| Evento | Quando dispara |
|---|---|
| trial_plans_viewed | Quando tela de trial/planos é exibida. |
| paywall_after_trial_viewed | Quando paywall pós-trial aparece. |
| monthly_plan_selected | Quando plano mensal é escolhido. |
| annual_plan_selected | Quando plano anual é escolhido. |
| purchase_restore_started | Quando restauração inicia. |
| access_blocked | Quando acesso é bloqueado. |

## 8. Payload mínimo

```json
{
  "source": "onboarding_gate",
  "access_status": "trial_expired",
  "plan": "annual"
}
```

## 9. Impacto Flutter

- Instrumentar telas de planos/paywall.
- Registrar cliques nos CTAs.
- Evitar dados sensíveis de pagamento.

## 10. Impacto Backend

- Logs de bloqueio e status comercial.
- CorrelationId em erros de acesso ou assinatura.

## 11. Impacto QA

- Trial ativo.
- Trial expirado.
- Plano mensal escolhido.
- Plano anual escolhido.
- Restore purchase.
- Usuário assinante sem paywall indevido.

## 12. Critérios de aceite

### CA-001 — Paywall rastreado

Dado que o trial expirou,
Quando o usuário for enviado ao paywall,
Então `paywall_after_trial_viewed` deve ser disparado.

### CA-002 — Plano diferenciado

Dado que o usuário toca no plano anual,
Quando o CTA for acionado,
Então deve ser disparado evento específico do plano anual.

## 13. Decisão registrada

O modelo comercial precisa ser mensurado sem coletar dados sensíveis de pagamento ou criar rastreamento invasivo.
