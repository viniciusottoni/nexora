---
title: US-153 — Aplicar prescrição inicial por perfil e objetivo
sidebar_position: 153
---

# US-153 — Aplicar prescrição inicial por perfil e objetivo

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-153 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (parâmetros internos) |
| Dependência principal | UserProfile, effectiveExperienceLevel |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **aplicar séries, repetições, descanso e RPE conforme perfil e objetivo**,

para **entregar um treino com volume e intensidade adequados e seguros**.

---

## 3. Contexto

Cada exercício selecionado precisa de parâmetros de execução. Eles dependem do nível efetivo (sedentário → avançado) e do objetivo (ganhar massa, perder peso, condicionamento, mais força, manter a forma), seguindo as faixas de prescrição do documento de instruções.

---

## 4. Objetivo

Definir `sets`, `reps`/tempo, `restSeconds`, faixa de RPE e frequência sugerida por exercício/quest, conforme perfil e objetivo.

---

## 5. Escopo

### Entra nesta US

- Faixas por nível efetivo (RPE, séries, reps, descanso, frequência).
- Ajustes por objetivo (reps, descanso, cardio, progressão, atributos-alvo).
- Geração dos parâmetros de cada `QuestExercise`.

### Fora desta US

- Cálculo do nível efetivo (US-150).
- Seleção de exercícios (US-151).
- Conclusão e XP (EPIC-008/009).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Sedentário: RPE 3–5, 1–2 séries, reps fixas dentro de 6–12, descanso 45–90s, treino/quests todos os dias; evitar falha e alto impacto. |
| RN-002 | Iniciante: RPE 5–6, 2–3 séries, reps fixas dentro de 8–15, descanso 45–90s, treino/quests todos os dias; full body. |
| RN-003 | Intermediário: RPE 6–8, 3–4 séries, reps em intervalo dentro de 10–20, descanso 60–180s, treino/quests todos os dias. |
| RN-004 | Avançado: RPE 6–9, 3–5 séries, reps em intervalo dentro de 4–30, descanso conforme objetivo, treino/quests todos os dias. |
| RN-005 | O objetivo ajusta reps, descanso e ênfase (massa, perda de peso, condicionamento, força, manutenção). |
| RN-006 | A prescrição nunca pode contrariar segurança, limitações ou dores. |
| RN-007 | Para sedentário e iniciante, reps são prescritas como valor fixo (`plannedRepsMin`; `plannedRepsMax` permanece nulo). Para intermediário e avançado, reps são prescritas como intervalo [`plannedRepsMin`, `plannedRepsMax`]: o usuário deve atingir o mínimo e pode ir até o máximo se estiver em condições — o sistema não trunca na meta mínima. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Define a prescrição. |
| Usuário final | Recebe os parâmetros na quest. |

---

## 8. Fluxo principal

1. Sistema lê nível efetivo e objetivo.
2. Seleciona as faixas de prescrição correspondentes.
3. Define séries, reps/tempo, descanso e RPE por exercício.
4. Preenche os `QuestExercise`.

---

## 9. Fluxos alternativos

### 9.1. Tempo curto

Em micro quests, reduzir séries/descanso para caber no tempo.

### 9.2. Conflito de objetivo

Ex.: ganhar massa + 10 min → micro quest com aviso de limitação prática.

---

## 10. Estados esperados

- prescrição aplicada;
- ajustada por tempo;
- ajustada por conflito de objetivo.

---

## 11. Impacto no Frontend Flutter

- Exibição de séries, reps, descanso e RPE por exercício.
- Para sedentário/iniciante: exibir reps como valor único (ex.: "12 reps").
- Para intermediário/avançado: exibir reps como intervalo (ex.: "10–15 reps"), deixando claro que 10 é o mínimo exigido.

---

## 12. Impacto no Backend

- Serviço de prescrição por perfil/objetivo.
- Preenchimento dos parâmetros do `QuestExercise`.
- Para intermediário/avançado: calcular e preencher `plannedRepsMin` e `plannedRepsMax` dentro das faixas do nível+objetivo.
- Para sedentário/iniciante: preencher apenas `plannedRepsMin` com valor fixo; `plannedRepsMax` = null.

---

## 13. Impacto no Banco de Dados

Entidade: `QuestExercise`.

Campos: `plannedSets`, `plannedRepsMin`, `plannedRepsMax` (nullable — null para sedentário e iniciante), `plannedDurationSeconds`, `restSeconds`, `targetRpe`.

---

## 14. Impacto em Gamificação

- Volume/intensidade adequados melhoram aderência e progresso.

---

## 15. Impacto em Monetização

- Treino bem prescrito aumenta valor percebido no trial.

---

## 16. Impacto em Internacionalização

- Parâmetros internos; rótulos traduzidos na exibição.

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/prescribe
```

Response conceitual:

```json
{
  "exercises": [
    { "exerciseId": "exr_001", "sets": 2, "repsMin": 12, "repsMax": null, "restSeconds": 60, "targetRpe": "5-6" },
    { "exerciseId": "exr_002", "sets": 3, "repsMin": 10, "repsMax": 15, "restSeconds": 90, "targetRpe": "6-8" }
  ]
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| daily_quest_generated | Geração inclui a prescrição. |

---

## 19. Critérios de aceite

### CA-001 — Prescrição por nível

Dado um usuário iniciante,

Quando a quest for gerada,

Então as séries/reps/descanso/RPE devem cair nas faixas de iniciante.

### CA-002 — Conflito objetivo × tempo

Dado ganhar massa com 10 minutos,

Quando a quest for gerada,

Então deve montar micro quest e informar a limitação prática.

### CA-003 — Intervalo de reps para intermediário/avançado

Dado um usuário intermediário ou avançado,

Quando a quest for gerada,

Então `repsMin` e `repsMax` devem estar preenchidos com valores distintos dentro das faixas do nível, e o frontend deve exibir o formato "X–Y reps".

### CA-004 — Reps fixas para sedentário/iniciante

Dado um usuário sedentário ou iniciante,

Quando a quest for gerada,

Então `repsMin` deve estar preenchido com um valor dentro da faixa do nível e `repsMax` deve ser nulo, e o frontend deve exibir o formato "X reps".

---

## 20. Critérios de teste para QA

### Backend

- cada nível produz faixas corretas de prescrição;
- objetivo ajusta reps/descanso/ênfase;
- micro quest reduz séries/descanso;
- intermediário/avançado retornam `repsMin` e `repsMax` ambos preenchidos;
- sedentário/iniciante retornam apenas `repsMin` preenchido (`repsMax` = null);
- prescrição nunca contraria segurança/limitações/dores.

---

## ✅ Decisão registrada

> A prescrição inicial deriva do nível efetivo e do objetivo, dentro das faixas do documento de instruções, sempre subordinada à segurança.
