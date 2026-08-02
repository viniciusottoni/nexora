---
title: US-019 — Reconhecer acesso de assinante mensal ou anual
sidebar_position: 19
---

# US-019 — Reconhecer acesso de assinante mensal ou anual

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-019 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Premium Mensal e Premium Anual |
| Plano | Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Subscription status |
| Status | Planejada |

---

## 2. História do usuário

Como **assinante mensal ou anual**,

quero **ter meu acesso reconhecido no app**,

para **usar o AWAKEN sem fricção enquanto minha assinatura estiver ativa**.

---

## 3. Contexto

Após assinar, o usuário precisa ter acesso liberado imediatamente. O app deve reconhecer assinatura ativa ao abrir, ao sincronizar e ao retornar de uma compra.

---

## 4. Objetivo

Garantir que usuário com plano mensal ou anual ativo acesse onboarding, quests, histórico, perfil e recursos protegidos do MVP.

---

## 5. Escopo

### Entra nesta US

- Reconhecer assinatura mensal ativa.
- Reconhecer assinatura anual ativa.
- Liberar recursos protegidos.
- Exibir status de assinante quando necessário.
- Manter acesso após reabrir app.
- Tratar inconsistência temporária de status.

### Fora desta US

- Compra do plano.
- Cancelamento.
- Upgrade/downgrade entre planos.
- Reembolso.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Assinatura mensal ativa libera acesso. |
| RN-002 | Assinatura anual ativa libera acesso. |
| RN-003 | Assinatura expirada bloqueia acesso. |
| RN-004 | Usuário assinante não deve ver paywall obrigatório. |
| RN-005 | Se onboarding não foi concluído, assinante deve ir para onboarding. |
| RN-006 | Se onboarding foi concluído, assinante deve ir para Home/Quest. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não possui acesso de assinante. |
| Usuário em Trial | Acesso temporário, não assinante. |
| Premium Mensal | Pode acessar recursos protegidos. |
| Premium Anual | Pode acessar recursos protegidos. |
| Trial expirado | Não pode acessar sem assinar. |
| Assinatura expirada | Não pode acessar sem renovar. |

---

## 8. Fluxo principal

1. Usuário abre o app.
2. App verifica sessão.
3. App sincroniza status de assinatura.
4. Sistema identifica assinatura ativa.
5. App libera rotas protegidas.
6. Usuário segue para onboarding ou Home/Quest.

---

## 9. Fluxos alternativos

### 9.1. Assinante sem onboarding

Se o usuário assinou antes de concluir onboarding, deve continuar onboarding.

### 9.2. Status temporariamente indisponível

O app deve exibir estado de verificação, sem liberar acesso indevidamente.

---

## 10. Estados de tela ou estados esperados

- verificando assinatura;
- assinatura mensal ativa;
- assinatura anual ativa;
- assinatura expirada;
- erro de sincronização;
- acesso liberado.

---

## 11. Impacto no Frontend Flutter

- Estado global de acesso.
- Guards de rota.
- Mensagem de status ativo.
- Redirecionamento correto.

---

## 12. Impacto no Backend

- Expor status consolidado.
- Atualizar plano ativo.
- Tratar expiração.
- Registrar mudanças relevantes.

---

## 13. Impacto no Banco de Dados

Entidade principal: Subscription.

Campos relevantes:

- plan;
- status;
- accessStatus;
- expiresAt;
- lastRevenueCatSyncAt.

---

## 14. Impacto em Gamificação

- Assinante ativo pode gerar quests, concluir treinos e evoluir.
- Não concede XP apenas por reconhecer assinatura.

---

## 15. Impacto em Monetização

- Garante entrega do valor pago.
- Evita mostrar paywall indevido para assinante ativo.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Status e mensagens de assinatura. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/subscriptions/status
```

Response conceitual:

```json
{
  "accessStatus": "subscription_active",
  "plan": "annual",
  "onboardingCompleted": true
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| subscription_started | Quando assinatura ativa é reconhecida pela primeira vez. |
| access_restored | Quando acesso é liberado após assinatura. |

---

## 19. Critérios de aceite

### CA-001 — Mensal ativo

Dado que o usuário possui plano mensal ativo,

Quando abrir o app,

Então deve acessar recursos protegidos.

### CA-002 — Anual ativo

Dado que o usuário possui plano anual ativo,

Quando abrir o app,

Então deve acessar recursos protegidos.

### CA-003 — Paywall indevido

Dado que o usuário é assinante ativo,

Quando navegar no app,

Então não deve ver paywall obrigatório.

---

## 20. Critérios de teste para QA

- mensal ativo;
- anual ativo;
- assinante sem onboarding;
- assinante com onboarding concluído;
- assinatura expirada;
- erro de sincronização;
- paywall indevido.

---

## ✅ Decisão registrada

> Assinante mensal ou anual ativo deve ter acesso reconhecido de forma imediata e consistente em todos os fluxos protegidos do AWAKEN.
