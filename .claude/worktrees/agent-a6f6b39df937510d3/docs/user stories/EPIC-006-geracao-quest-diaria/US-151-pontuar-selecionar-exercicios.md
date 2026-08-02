---
title: US-151 — Pontuar e selecionar exercícios com prioridade de segurança
sidebar_position: 151
---

# US-151 — Pontuar e selecionar exercícios com prioridade de segurança

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-151 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (cálculo interno) |
| Dependência principal | ExerciseCatalog, UserProfile |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **pontuar e selecionar os exercícios elegíveis com peso alto de segurança**,

para **montar o melhor treino possível sem comprometer a segurança do usuário**.

---

## 3. Contexto

Após o filtro eliminatório, os exercícios elegíveis são pontuados por afinidade com objetivo, compatibilidade de nível, segurança, encaixe de tempo, variedade e progressão. A segurança recebe peso alto, e a recompensa de XP/atributos é o último critério.

---

## 4. Objetivo

Calcular `exerciseScore` para cada elegível e selecionar os exercícios da quest dentro do orçamento de tempo.

---

## 5. Escopo

### Entra nesta US

- Cálculo de `goalAffinityScore`, `safetyScore`, `levelMatchScore`, `timeFitScore`, `varietyScore`, `progressionFitScore`.
- Integração com `targetAttributeScore` (US-152).
- Seleção final respeitando o orçamento de tempo e o equilíbrio de padrões.

### Fora desta US

- Filtro eliminatório (US-045).
- Detalhe do atributo-alvo (US-152).
- Prescrição numérica (US-153).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | `safetyScore` deve ter peso alto na fórmula. |
| RN-002 | A seleção deve respeitar o orçamento de tempo do treino. |
| RN-003 | A recompensa de XP/atributos é o critério de menor prioridade. |
| RN-004 | A seleção deve buscar equilíbrio entre padrões de movimento/grupos. |
| RN-005 | A pontuação só se aplica a exercícios já aprovados e elegíveis. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Pontua e seleciona. |
| Usuário final | Recebe a quest resultante. |

---

## 8. Fluxo principal

1. Sistema recebe os elegíveis.
2. Calcula os componentes da pontuação.
3. Aplica o ajuste por atributo-alvo (US-152).
4. Ordena por `exerciseScore`.
5. Seleciona dentro do orçamento de tempo, equilibrando padrões.

---

## 9. Fluxos alternativos

### 9.1. Empate de pontuação

Desempate por variedade e por atributo-alvo.

### 9.2. Tempo restante

Preenche com exercícios curtos compatíveis ou encerra a seleção.

---

## 10. Estados esperados

- pontuando;
- selecionado;
- tempo esgotado (seleção encerrada).

---

## 11. Impacto no Frontend Flutter

- Indireto: recebe a lista final ordenada.

---

## 12. Impacto no Backend

- Motor de pontuação configurável por pesos.
- Seleção com orçamento de tempo.

---

## 13. Impacto no Banco de Dados

Entidades: `ExerciseCatalog`, `QuestExercise`.

Campos: métricas de objetivo, nível, segurança, tempo e variedade; ordem/seleção em `QuestExercise`.

---

## 14. Impacto em Gamificação

- A pontuação considera atributos como critério final, nunca acima da segurança.

---

## 15. Impacto em Monetização

- Treino bem montado aumenta a percepção de qualidade.

---

## 16. Impacto em Internacionalização

- Cálculo interno; sem textos.

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/score-and-select
```

Response conceitual:

```json
{
  "selected": [ { "exerciseId": "exr_001", "score": 0.82 } ],
  "estimatedDurationMinutes": 30
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| daily_quest_generated | Quando a seleção compõe a quest. |

---

## 19. Critérios de aceite

### CA-001 — Peso de segurança

Dado dois exercícios com afinidade de objetivo semelhante,

Quando pontuados,

Então o mais seguro deve ter prioridade na seleção.

### CA-002 — Orçamento de tempo

Dado um treino de 30 minutos,

Quando a seleção rodar,

Então o total estimado não deve exceder o tempo disponível.

---

## 20. Critérios de teste para QA

### Backend

- a fórmula aplica os pesos corretos com segurança alta;
- seleção respeita orçamento de tempo;
- equilíbrio de padrões é observado;
- empates usam variedade e atributo-alvo.

---

## ✅ Decisão registrada

> A pontuação prioriza segurança e encaixe no tempo; XP/atributos são o último critério, nunca acima da segurança.
