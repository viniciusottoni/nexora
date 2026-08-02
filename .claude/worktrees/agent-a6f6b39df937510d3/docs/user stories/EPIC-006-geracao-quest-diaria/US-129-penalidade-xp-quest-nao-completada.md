---
title: US-129 — Aplicar penalidade de XP por quest diária não completada
sidebar_position: 129
---

# US-129 — Aplicar penalidade de XP por quest diária não completada

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-129 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Quest, HunterProgress (EPIC-009) |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **aplicar uma penalidade de XP quando a quest diária não for completada**,

para **incentivar consistência sem punir excessivamente**.

---

## 3. Contexto

Não completar a quest diária gera uma penalidade de XP aplicada na virada de dia. A penalidade é progressiva: 1 dia sem fazer gera -10 XP, 2 dias geram -20 XP, 3 dias geram -30 XP, e assim por diante, sempre com piso de 0 XP. A regra de XP em si pertence ao EPIC-009; aqui se define o gatilho ligado à daily.

---

## 4. Objetivo

Detectar quests diárias não completadas e acionar a penalidade de XP na virada de dia, apenas para usuários com acesso ativo.

---

## 5. Escopo

### Entra nesta US

- Detecção de daily não completada na virada de dia.
- Acionamento da penalidade (cálculo em EPIC-009).
- Aplicação apenas com acesso ativo.
- Não aplicar penalidade por dungeon.

### Fora desta US

- Cálculo do valor exato e atualização de progresso (EPIC-009).
- Streak (EPIC-009).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A penalidade é aplicada na virada de dia para a daily não completada. |
| RN-002 | A penalidade é progressiva por dias consecutivos sem completar a daily: 1 dia = -10 XP, 2 dias = -20 XP, 3 dias = -30 XP, e assim por diante. |
| RN-003 | A penalidade não leva o XP abaixo de 0. |
| RN-004 | A penalidade só se aplica a usuários com acesso ativo. |
| RN-005 | Dungeons não geram penalidade. |
| RN-006 | A penalidade não deve gerar punição visual agressiva. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Sujeito à penalidade. |
| Premium Mensal/Anual | Sujeito à penalidade. |
| Acesso expirado | Sem penalidade. |

---

## 8. Fluxo principal

1. Job de virada de dia verifica a daily do dia anterior.
2. Se não completada e o acesso está ativo, aciona a penalidade (EPIC-009).
3. Registra o evento de penalidade.

---

## 9. Fluxos alternativos

### 9.1. Acesso expirado

Sem penalidade enquanto o acesso estiver inativo.

### 9.2. XP no piso

Se o XP já está em 0, a penalidade não o torna negativo.

---

## 10. Estados esperados

- daily completada (sem penalidade);
- daily não completada (penalidade acionada);
- acesso expirado (sem penalidade).

---

## 11. Impacto no Frontend Flutter

- Mensagem leve, não agressiva, sobre a penalidade.

---

## 12. Impacto no Backend

- Job de virada de dia.
- Acionamento da penalidade no serviço de XP (EPIC-009).

---

## 13. Impacto no Banco de Dados

Entidades: `Quest`, `HunterProgress` (EPIC-009).

Campos: status de conclusão da daily; XP/penalidade no progresso.

---

## 14. Impacto em Gamificação

- Incentiva consistência; penalidade calibrada para não desmotivar.

---

## 15. Impacto em Monetização

- Equilíbrio entre incentivo e frustração protege a retenção.

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
(interno) job: daily_rollover_penalty
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| daily_quest_missed | Quando a daily não foi completada. |
| xp_penalty_applied | Quando a penalidade é aplicada. |

---

## 19. Critérios de aceite

### CA-001 — Penalidade no dia seguinte

Dado que o usuário não completou a daily e tem acesso ativo,

Quando virar o dia,

Então deve receber a penalidade progressiva definida em EPIC-009.

### CA-002 — Sem acesso, sem penalidade

Dado que o acesso está expirado,

Quando virar o dia,

Então nenhuma penalidade deve ser aplicada.

---

## 20. Critérios de teste para QA

### Backend

- daily não completada com acesso ativo aciona penalidade progressiva;
- penalidade não deixa XP negativo;
- acesso expirado não gera penalidade;
- dungeon não gera penalidade.

### E2E

- não completar a daily reflete penalidade no dia seguinte, de forma não agressiva.

---

## ✅ Decisão registrada

> Não completar a quest diária gera penalidade de XP na virada de dia, apenas com acesso ativo, em escala progressiva de -10 XP por dia consecutivo sem completar a daily, com piso de 0.
