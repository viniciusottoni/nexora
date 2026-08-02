---
title: US-118 — Assinar plano mensal
sidebar_position: 118
---

# US-118 — Assinar plano mensal

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-118 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em trial, trial expirado ou assinatura expirada |
| Plano | Mensal |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | RevenueCat e Google Play Billing |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com trial ativo ou expirado, após ter escolhido o plano na pricing e criado a conta**,

quero **assinar o plano mensal**,

para **continuar usando o AWAKEN com pagamento recorrente mensal**.

---

## 3. Contexto

O plano mensal é a opção de menor compromisso. Ele precisa estar disponível para quem está em trial, trial expirado ou assinatura expirada, mas a compra só deve acontecer depois do cadastro e da vinculação da escolha salva na pricing.

---

## 4. Objetivo

Permitir que o usuário assine o plano mensal já escolhido na pricing, liberando acesso após confirmação da assinatura.

---

## 5. Escopo

### Entra nesta US

- Aplicação da escolha mensal salva na pricing.
- Abertura do fluxo de compra da loja via RevenueCat.
- Confirmação de assinatura.
- Atualização do status para assinatura ativa.
- Tratamento de cancelamento ou falha.

### Fora desta US

- Plano anual.
- Cupons.
- Upgrade/downgrade.
- Reembolso.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Plano mensal deve liberar acesso enquanto estiver ativo. |
| RN-002 | Compra só deve ser considerada concluída após confirmação da loja/RevenueCat e após existir uma conta vinculada à escolha salva na pricing. |
| RN-003 | Cancelamento do fluxo não deve liberar acesso. |
| RN-004 | Falha de pagamento não deve apagar progresso. |
| RN-005 | Usuário já assinante mensal não deve comprar o mesmo plano indevidamente. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode assinar mensal depois de ter criado a conta e salvo a escolha na pricing. |
| Trial expirado | Pode assinar mensal para reativar. |
| Assinatura expirada | Pode assinar mensal para reativar. |
| Premium Mensal | Já possui plano mensal ativo. |
| Premium Anual | Pode visualizar status, mas mudança de plano é fora desta US. |

---

## 8. Fluxo principal

1. Usuário confirma o plano mensal salvo na pricing.
2. App inicia compra via RevenueCat.
3. Loja confirma assinatura.
4. App/backend sincronizam status.
5. Acesso é liberado.
6. Usuário volta para onboarding, Home ou tela anterior.

---

## 9. Fluxos alternativos

### 9.1. Compra cancelada

Usuário cancela o fluxo da loja e permanece no status anterior.

### 9.2. Falha de sincronização

App deve permitir restaurar/sincronizar assinatura novamente.

---

## 10. Estados esperados

- plano mensal selecionado;
- compra em andamento;
- compra concluída;
- compra cancelada;
- falha de compra;
- acesso restaurado.

---

## 11. Impacto no Frontend Flutter

- CTA do plano mensal.
- Integração RevenueCat.
- Loading de compra.
- Mensagens de sucesso, cancelamento e erro.
- Redirecionamento pós-compra.

---

## 12. Impacto no Backend

- Sincronizar status da assinatura.
- Persistir plano mensal.
- Liberar acesso.

---

## 13. Impacto no Banco de Dados

Entidade: Subscription.

Campos:

- plan = monthly;
- status;
- expiresAt;
- revenueCatCustomerId.

---

## 14. Impacto em Gamificação

- Libera continuidade de quests, XP, rank e streak.
- Não concede XP apenas por assinar.

---

## 15. Impacto em Monetização

- Gera receita recorrente mensal.
- Deve respeitar regras da loja e RevenueCat.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos de compra mensal. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

A compra é iniciada pelo SDK RevenueCat. Backend deve sincronizar status:

```txt
POST /api/subscriptions/sync
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| monthly_plan_selected | Quando mensal é selecionado. |
| subscription_started | Quando assinatura é reconhecida. |
| subscription_abandoned | Quando compra é cancelada. |

---

## 19. Critérios de aceite

### CA-001 — Compra mensal concluída

Dado que o usuário seleciona mensal,

Quando a loja confirma a assinatura,

Então o acesso deve ser liberado.

### CA-002 — Compra cancelada

Dado que o usuário cancela a compra,

Quando retornar ao app,

Então o acesso não deve ser liberado indevidamente.

---

## 20. Critérios de teste para QA

- compra mensal concluída;
- compra cancelada;
- falha de compra;
- trial ativo comprando mensal;
- trial expirado comprando mensal;
- reabertura do app após compra.

---

## ✅ Decisão registrada

> Plano mensal deve ser a opção recorrente de menor compromisso para continuar usando o AWAKEN após ou durante o trial.
