---
title: US-119 — Assinar plano anual
sidebar_position: 119
---

# US-119 — Assinar plano anual

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-119 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em trial, trial expirado ou assinatura expirada |
| Plano | Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | RevenueCat e Google Play Billing |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com trial ativo ou expirado, após ter escolhido o plano na pricing e criado a conta**,

quero **assinar o plano anual**,

para **continuar usando o AWAKEN com melhor custo-benefício**.

---

## 3. Contexto

O plano anual é a opção estratégica para maior compromisso e melhor receita antecipada. Deve ser apresentado com clareza, sem pressão enganosa, e a compra só deve acontecer depois do cadastro e da vinculação da escolha salva na pricing.

---

## 4. Objetivo

Permitir que o usuário assine o plano anual já escolhido na pricing, liberando acesso após confirmação da assinatura.

---

## 5. Escopo

### Entra nesta US

- Aplicação da escolha anual salva na pricing.
- Abertura do fluxo de compra via RevenueCat.
- Confirmação de assinatura anual.
- Atualização do status para assinatura ativa.
- Indicação de melhor custo-benefício, se configurada.
- Tratamento de cancelamento ou falha.

### Fora desta US

- Plano mensal.
- Promoções temporárias.
- Upgrade/downgrade.
- Reembolso.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Plano anual deve liberar acesso enquanto estiver ativo. |
| RN-002 | Compra só deve ser considerada concluída após confirmação da loja/RevenueCat e após existir uma conta vinculada à escolha salva na pricing. |
| RN-003 | Cancelamento do fluxo não deve liberar acesso. |
| RN-004 | O benefício financeiro do anual deve ser verdadeiro e baseado no preço configurado. |
| RN-005 | Usuário já assinante anual não deve comprar o mesmo plano indevidamente. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode assinar anual depois de ter criado a conta e salvo a escolha na pricing. |
| Trial expirado | Pode assinar anual para reativar. |
| Assinatura expirada | Pode assinar anual para reativar. |
| Premium Mensal | Mudança de mensal para anual é fora desta US. |
| Premium Anual | Já possui plano anual ativo. |

---

## 8. Fluxo principal

1. Usuário confirma o plano anual salvo na pricing.
2. App inicia compra via RevenueCat.
3. Loja confirma assinatura.
4. App/backend sincronizam status.
5. Acesso é liberado.
6. Usuário volta para onboarding, Home ou tela anterior.

---

## 9. Fluxos alternativos

### 9.1. Compra cancelada

Usuário cancela o fluxo e continua com o status anterior.

### 9.2. Falha de sincronização

App deve permitir restaurar/sincronizar assinatura novamente.

---

## 10. Estados esperados

- plano anual selecionado;
- compra em andamento;
- compra concluída;
- compra cancelada;
- falha de compra;
- acesso restaurado.

---

## 11. Impacto no Frontend Flutter

- CTA do plano anual.
- Destaque visual do melhor custo-benefício.
- Integração RevenueCat.
- Loading de compra.
- Mensagens de sucesso, cancelamento e erro.

---

## 12. Impacto no Backend

- Sincronizar status da assinatura.
- Persistir plano anual.
- Liberar acesso.

---

## 13. Impacto no Banco de Dados

Entidade: Subscription.

Campos:

- plan = annual;
- status;
- expiresAt;
- revenueCatCustomerId.

---

## 14. Impacto em Gamificação

- Libera continuidade de quests, XP, rank e streak.
- Não concede XP apenas por assinar.

---

## 15. Impacto em Monetização

- Gera receita anual.
- Deve ser apresentado como melhor custo-benefício somente se isso for verdadeiro.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos de compra anual. |
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
| annual_plan_selected | Quando anual é selecionado. |
| subscription_started | Quando assinatura é reconhecida. |
| subscription_abandoned | Quando compra é cancelada. |

---

## 19. Critérios de aceite

### CA-001 — Compra anual concluída

Dado que o usuário seleciona anual,

Quando a loja confirma a assinatura,

Então o acesso deve ser liberado.

### CA-002 — Compra cancelada

Dado que o usuário cancela a compra,

Quando retornar ao app,

Então o acesso não deve ser liberado indevidamente.

---

## 20. Critérios de teste para QA

- compra anual concluída;
- compra cancelada;
- falha de compra;
- trial ativo comprando anual;
- trial expirado comprando anual;
- reabertura do app após compra.

---

## ✅ Decisão registrada

> Plano anual deve ser a opção de melhor custo-benefício para usuários que desejam continuidade maior no AWAKEN.
