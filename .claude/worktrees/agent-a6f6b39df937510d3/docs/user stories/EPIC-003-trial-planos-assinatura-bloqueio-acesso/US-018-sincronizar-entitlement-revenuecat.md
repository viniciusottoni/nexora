---
title: US-018 — Sincronizar entitlement com RevenueCat
sidebar_position: 18
---

# US-018 — Sincronizar entitlement com RevenueCat

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-018 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | RevenueCat e Subscription |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **sincronizar o entitlement com RevenueCat**,

para **liberar, bloquear ou reativar o acesso corretamente conforme assinatura do usuário**.

---

## 3. Contexto

O AWAKEN usará RevenueCat para assinaturas. O app e o backend precisam reconhecer se o usuário possui acesso ativo, expirado ou em trial para evitar bloqueios indevidos ou liberação sem assinatura.

---

## 4. Objetivo

Manter o status comercial do usuário consistente entre app, backend e RevenueCat.

---

## 5. Escopo

### Entra nesta US

- Consultar status de entitlement.
- Sincronizar plano mensal e anual.
- Atualizar status de assinatura.
- Tratar expiração e renovação.
- Tratar falha temporária de sincronização.
- Atualizar acesso do usuário.

### Fora desta US

- Tela visual de paywall.
- Checkout completo.
- Cupons e promoções.
- Painel financeiro interno.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | RevenueCat deve ser usado como fonte de status de assinatura paga. |
| RN-002 | Backend deve manter cópia do status para decisão de acesso. |
| RN-003 | Assinatura ativa deve liberar recursos protegidos. |
| RN-004 | Assinatura expirada deve bloquear recursos protegidos. |
| RN-005 | Falha temporária de sincronização não deve apagar progresso. |
| RN-006 | O sistema deve evitar estados contraditórios entre app e backend. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não possui entitlement. |
| Usuário em Trial | Acesso pelo status de trial. |
| Premium Mensal | Acesso liberado se assinatura mensal ativa. |
| Premium Anual | Acesso liberado se assinatura anual ativa. |
| Trial expirado | Bloqueado até assinatura ativa. |
| Assinatura expirada | Bloqueado até nova assinatura ativa. |
| Sistema | Pode sincronizar status. |

---

## 8. Fluxo principal

1. Usuário abre app ou conclui compra.
2. App consulta RevenueCat.
3. Backend recebe ou consulta status comercial.
4. Subscription é atualizada.
5. App recebe status final de acesso.
6. Usuário é liberado ou bloqueado conforme status.

---

## 9. Fluxos alternativos

### 9.1. Falha de sincronização

O app deve exibir estado controlado e permitir nova tentativa.

### 9.2. Assinatura expirada

Backend atualiza status para expirado e app direciona para paywall.

---

## 10. Estados de tela ou estados esperados

- sincronizando;
- assinatura ativa;
- assinatura expirada;
- trial ativo;
- erro de sincronização;
- acesso bloqueado.

---

## 11. Impacto no Frontend Flutter

- Integrar SDK RevenueCat.
- Consultar status comercial.
- Atualizar estado global de acesso.
- Exibir loading/erro quando sincronizar.

---

## 12. Impacto no Backend

- Receber status de assinatura.
- Persistir plano e expiração.
- Expor status consolidado para o app.
- Tratar webhooks, quando configurado.

---

## 13. Impacto no Banco de Dados

Entidade principal: Subscription.

Campos relevantes:

- plan;
- status;
- entitlement;
- revenueCatCustomerId;
- expiresAt;
- lastRevenueCatSyncAt.

---

## 14. Impacto em Gamificação

- Acesso ativo libera novas quests e evolução.
- Acesso expirado bloqueia novas ações, mas preserva progresso.

---

## 15. Impacto em Monetização

- Essencial para receita e controle de acesso.
- Evita liberação indevida de recursos pagos.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de erro de sincronização. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/subscriptions/sync
```

Response conceitual:

```json
{
  "accessStatus": "subscription_active",
  "plan": "monthly",
  "expiresAt": "2026-07-18T10:00:00Z"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| subscription_started | Quando assinatura ativa é reconhecida. |
| subscription_expired | Quando assinatura expira. |
| access_restored | Quando acesso é restaurado. |

---

## 19. Critérios de aceite

### CA-001 — Assinatura ativa

Dado que RevenueCat informa assinatura ativa,

Quando o sistema sincronizar,

Então o usuário deve ter acesso liberado.

### CA-002 — Assinatura expirada

Dado que RevenueCat informa expiração,

Quando o sistema sincronizar,

Então recursos protegidos devem ser bloqueados.

---

## 20. Critérios de teste para QA

- assinatura mensal ativa;
- assinatura anual ativa;
- assinatura expirada;
- falha de sincronização;
- restauração de acesso;
- status divergente entre app e backend.

---

## ✅ Decisão registrada

> RevenueCat controla a assinatura paga, mas o backend deve consolidar o status final usado para liberar ou bloquear recursos do AWAKEN.
