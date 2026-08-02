---
title: US-043 — Bloquear geração para acesso expirado
sidebar_position: 43
---

# US-043 — Bloquear geração para acesso expirado

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-043 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Subscription (EPIC-003) |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **bloquear a geração de quest para trial ou assinatura expirada**,

para **cumprir o modelo comercial do AWAKEN**.

---

## 3. Contexto

Não há plano gratuito permanente. Usuários com trial ou assinatura expirada não podem gerar quest, mas mantêm o progresso e veem o paywall (EPIC-003).

---

## 4. Objetivo

Impedir a geração de quest quando o acesso não estiver ativo, retornando estado de bloqueio adequado.

---

## 5. Escopo

### Entra nesta US

- Verificação de acesso ativo antes da geração.
- Estado bloqueado com encaminhamento ao paywall.
- Preservação do progresso.

### Fora desta US

- Tela e regras de paywall (EPIC-003).
- Penalidade de XP (US-129).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Apenas trial ativo ou assinatura ativa geram quest. |
| RN-002 | Acesso expirado retorna bloqueio (403) e direciona ao paywall. |
| RN-003 | O bloqueio não apaga progresso. |
| RN-004 | A penalidade de XP só se aplica a usuários com acesso ativo. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Gera quest. |
| Premium Mensal/Anual | Gera quest. |
| Trial expirado | Bloqueado. |
| Assinatura expirada | Bloqueado. |

---

## 8. Fluxo principal

1. Usuário solicita a quest.
2. Sistema verifica o status de acesso.
3. Se inativo, retorna bloqueio e direciona ao paywall.

---

## 9. Fluxos alternativos

### 9.1. Acesso reativado

Após assinar, a geração volta a funcionar (EPIC-003).

---

## 10. Estados esperados

- acesso ativo (gera);
- acesso expirado (bloqueado);
- erro de verificação.

---

## 11. Impacto no Frontend Flutter

- Estado bloqueado com CTA para o paywall.

---

## 12. Impacto no Backend

- Verificação de entitlement antes da geração.
- Resposta 403 em acesso inativo.

---

## 13. Impacto no Banco de Dados

Entidade: `Subscription`.

---

## 14. Impacto em Gamificação

- Sem geração de XP no estado bloqueado.

---

## 15. Impacto em Monetização

- Garante o modelo de assinatura obrigatória após o trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagem de bloqueio. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/daily/generate
```

Response (bloqueado):

```json
{ "error": "access_expired", "redirect": "paywall" }
```

> **Nota de implementação:** o bloqueio de `/api/quests/daily/generate` reaproveita o
> `ActiveAccessMiddleware` (US-020/US-121), que já intercepta qualquer rota autenticada
> fora da allowlist (`/api/auth`, `/api/subscriptions`, `/api/app-config`, `/swagger`,
> `/health`) quando o acesso está expirado. A resposta real é
> `{ "code": "ACCESS_BLOCKED", "accessStatus": "trial_expired" | "subscription_expired", "correlationId" }`,
> mantendo um único contrato de bloqueio em todo o backend em vez de um formato
> paralelo só para quests. No app, qualquer 403 já é mapeado para `AccessBlockedError`
> e direciona ao paywall (`AppRoutes.subscription`).

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| quest_generation_blocked | Quando a geração é bloqueada por acesso expirado. |

---

## 19. Critérios de aceite

### CA-001 — Bloqueio por expiração

Dado um usuário com trial expirado,

Quando tentar gerar quest,

Então deve receber bloqueio e ser direcionado ao paywall.

### CA-002 — Progresso preservado

Dado um usuário bloqueado,

Quando o acesso expira,

Então o progresso deve permanecer salvo.

---

## 20. Critérios de teste para QA

### Backend

- acesso ativo gera; expirado retorna 403;
- progresso não é apagado;
- evento `quest_generation_blocked` é emitido.

### E2E

- trial expirado vê paywall ao tentar treinar;
- após assinar, geração volta a funcionar.

---

## ✅ Decisão registrada

> Geração de quest exige acesso ativo; expirado é bloqueado e direcionado ao paywall, sem perder progresso.
