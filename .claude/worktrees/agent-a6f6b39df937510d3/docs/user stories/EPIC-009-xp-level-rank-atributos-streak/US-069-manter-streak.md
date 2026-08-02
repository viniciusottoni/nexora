---
title: US-069 — Manter streak (com bônus controlado de RankScore)
sidebar_position: 69
---

# US-069 — Manter streak (com bônus controlado de RankScore)

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-069 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterProgress |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **manter um streak de dias treinando**,

para **ser recompensado pela consistência sem distorcer a economia de progressão**.

---

## 3. Contexto

O streak premia consistência. Ele concede um bônus controlado de RankScore em marcos (7, 30, 90, 180, 365 dias), mas nunca pode ser a principal fonte de RankScore.

---

## 4. Objetivo

Manter o streak de dias consecutivos com quest concluída e conceder bônus controlado de RankScore nos marcos.

---

## 5. Escopo

### Entra nesta US

- Incremento de streak por dias consecutivos com quest concluída.
- Bônus de RankScore por marco de streak.
- Limitação do streak como fonte secundária de RankScore.

### Fora desta US

- Regra de virada de dia (US-070).
- Penalidade por quest perdida (US-132).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O streak aumenta quando há quest concluída em dias consecutivos. |
| RN-002 | Bônus de RankScore por marco: 7d +1, 30d +3, 90d +8, 180d +15, 365d +35. |
| RN-003 | Streak não pode ser a principal fonte de RankScore. |
| RN-004 | O bônus de streak está sujeito ao limite mensal e ao diminishing returns (US-154). |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Mantém streak. |
| Premium Mensal/Anual | Mantém streak. |
| Acesso expirado | Streak não avança. |

---

## 8. Fluxo principal

1. Usuário conclui a quest do dia.
2. Sistema incrementa o streak.
3. Ao atingir um marco, concede o bônus de RankScore.

---

## 9. Fluxos alternativos

### 9.1. Marco atingido

Aplica o bônus correspondente, respeitando limite mensal/diminishing returns.

### 9.2. Dia sem conclusão

O streak é tratado conforme a regra de virada de dia (US-070).

---

## 10. Estados esperados

- streak incrementado;
- marco com bônus;
- streak mantido.

---

## 11. Impacto no Frontend Flutter

- Indicador de streak e mensagem de marco.

---

## 12. Impacto no Backend

- Serviço de streak e aplicação de bônus de RankScore.

---

## 13. Impacto no Banco de Dados

Entidade: `HunterProgress`.

Campos: `streakDays`, `rankScore`.

---

## 14. Impacto em Gamificação

- Reforça consistência sem quebrar a economia de Rank.

---

## 15. Impacto em Monetização

- Consistência aumenta retenção e valor do trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de streak e marcos. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/hunter/progress
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| streak_updated | Quando o streak muda. |
| rank_streak_bonus_applied | Quando um bônus de marco é aplicado. |

---

## 19. Critérios de aceite

### CA-001 — Streak incrementa

Dado dias consecutivos com quest concluída,

Quando o dia virar,

Então o streak deve aumentar.

### CA-002 — Bônus controlado

Dado que o usuário atingiu 30 dias de streak,

Quando o marco for processado,

Então deve receber +3 de RankScore, sujeito ao limite mensal.

---

## 20. Critérios de teste para QA

### Backend

- streak incrementa em dias consecutivos;
- bônus por marco aplicado corretamente;
- streak não é a fonte principal de RankScore;
- bônus respeita limite mensal/diminishing returns.

---

## ✅ Decisão registrada

> O streak premia consistência com bônus controlado de RankScore em marcos, nunca como fonte principal.
