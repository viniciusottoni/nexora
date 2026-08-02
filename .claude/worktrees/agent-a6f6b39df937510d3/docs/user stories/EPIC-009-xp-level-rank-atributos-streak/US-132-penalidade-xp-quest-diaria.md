---
title: US-132 — Receber penalidade de XP por quest diária não completada
sidebar_position: 132
---

# US-132 — Receber penalidade de XP por quest diária não completada

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-132 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterProgress |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **aplicar uma penalidade de XP quando a quest diária não for completada**,

para **incentivar consistência sem punir excessivamente**.

---

## 3. Contexto

Esta US é o cálculo/aplicação da penalidade acionada pela virada de dia (US-070) e pela regra de geração (EPIC-006 / US-129). A penalidade é progressiva por dias consecutivos sem completar a daily: 1 dia = -10 XP, 2 dias = -20 XP, 3 dias = -30 XP, e assim por diante. Ela nunca leva o XP abaixo de 0.

---

## 4. Objetivo

Calcular e aplicar a penalidade de XP na virada de dia para quests diárias não completadas.

---

## 5. Escopo

### Entra nesta US

- Cálculo do valor da penalidade progressiva (10 XP por dia consecutivo sem completar a daily).
- Aplicação na virada de dia, apenas com acesso ativo.
- Piso de 0 XP.
- Comunicação não agressiva.

### Fora desta US

- Gatilho de virada de dia (US-070).
- Penalidade não se aplica a dungeons (US-129).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A penalidade é progressiva por dias consecutivos sem completar a daily: 1 dia = -10 XP, 2 dias = -20 XP, 3 dias = -30 XP, e assim por diante. |
| RN-002 | A penalidade é aplicada na virada de dia. |
| RN-003 | A penalidade não leva o XP abaixo de 0. |
| RN-004 | Só se aplica a usuários com acesso ativo. |
| RN-005 | Dungeons não geram penalidade. |
| RN-006 | A comunicação não deve ser visualmente agressiva. |
| RN-007 | Ao completar a daily, o contador de dias consecutivos sem fazer zera. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Sujeito à penalidade. |
| Premium Mensal/Anual | Sujeito à penalidade. |
| Acesso expirado | Sem penalidade. |

---

## 8. Fluxo principal

1. A virada de dia detecta a daily não completada (US-070).
2. Sistema calcula a penalidade progressiva com base nos dias consecutivos sem completar a daily.
3. Aplica respeitando o piso de 0 XP.
4. Registra o evento.

---

## 9. Fluxos alternativos

### 9.1. XP no piso

Se o XP já está em 0, a penalidade não o torna negativo.

### 9.2. Acesso expirado

Sem penalidade.

---

## 10. Estados esperados

- penalidade calculada;
- penalidade aplicada;
- piso atingido;
- sem penalidade (acesso/dungeon).

---

## 11. Impacto no Frontend Flutter

- Mensagem leve sobre a penalidade.

---

## 12. Impacto no Backend

- Cálculo e aplicação da penalidade no serviço de XP.

---

## 13. Impacto no Banco de Dados

Entidade: `HunterProgress`.

Campos: `xp`, registro de penalidade.

---

## 14. Impacto em Gamificação

- Incentiva consistência sem desmotivar.

---

## 15. Impacto em Monetização

- Equilíbrio reduz abandono.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagem de penalidade. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
(interno) applyDailyPenalty(userId)
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| xp_penalty_applied | Quando a penalidade é aplicada (`amount`). |
| daily_quest_missed | Quando a daily não foi completada. |

---

## 19. Critérios de aceite

### CA-001 — Penalidade calibrada

Dado que a daily não foi completada e o acesso está ativo,

Quando virar o dia,

Então a penalidade deve seguir a progressão de -10 XP por dia consecutivo sem completar a daily.

### CA-002 — Piso de 0 XP

Dado que o XP já está em 0,

Quando a penalidade for aplicada,

Então o XP não deve ficar negativo.

---

## 20. Critérios de teste para QA

### Backend

- penalidade progressiva conforme os dias consecutivos sem completar a daily;
- não fica negativa;
- só com acesso ativo;
- dungeon não gera penalidade.

---

## ✅ Decisão registrada

> A penalidade de XP por daily não completada é progressiva (-10 XP por dia consecutivo sem completar a daily), aplicada na virada de dia com piso de 0, apenas para acesso ativo.
