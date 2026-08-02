---
title: US-146 — Mapear acessibilidade e adequação por nível
sidebar_position: 146
---

# US-146 — Mapear acessibilidade e adequação por nível

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-146 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (tags internas) |
| Dependência principal | ExerciseCatalog |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **mapear tags de acessibilidade e a adequação por nível de cada exercício**,

para **selecionar exercícios seguros para sedentários e iniciantes e respeitar o nível efetivo do usuário**.

---

## 3. Contexto

Além de dificuldade/impacto, a geração precisa saber rapidamente se um exercício é seguro para sedentário/iniciante e qual o nível mínimo exigido. As `accessibilityTags` e os campos `minExperienceLevel`/`suitableFor*` cumprem esse papel.

---

## 4. Objetivo

Preencher `AccessibilityTags`, `MinExperienceLevel` e os flags `SuitableForSedentary/Beginner/Intermediate/Advanced`.

---

## 5. Escopo

### Entra nesta US

- `accessibilityTags` (beginner_safe, sedentary_safe, low_impact, no_equipment, small_space, chair_supported, floor_required, wrist_neutral_possible, knee_friendly, back_friendly).
- `minExperienceLevel` (sedentario | iniciante | intermediario | avancado).
- Flags `suitableForSedentary/Beginner/Intermediate/Advanced`.

### Fora desta US

- Cálculo do nível efetivo do usuário (EPIC-006).
- Dificuldade/impacto (US-038).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Todo exercício deve ter `minExperienceLevel` definido. |
| RN-002 | Flags `suitableFor*` devem refletir dificuldade, complexidade e impacto. |
| RN-003 | Exercícios `sedentary_safe`/`beginner_safe` devem ter baixo impacto e baixa complexidade. |
| RN-004 | `minExperienceLevel` é usado no filtro eliminatório da geração. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Mapeia e revisa. |
| Usuário final | Beneficia-se na geração. |

---

## 8. Fluxo principal

1. Sistema cruza dificuldade, complexidade e impacto.
2. Define `minExperienceLevel` e flags de adequação.
3. Atribui `accessibilityTags`.
4. Salva no `ExerciseCatalog`.

---

## 9. Fluxos alternativos

### 9.1. Conflito entre métricas

Em caso de conflito, prevalece o critério mais conservador (nível mínimo mais alto).

---

## 10. Estados esperados

- mapeado;
- em revisão;
- erro de mapeamento.

---

## 11. Impacto no Frontend Flutter

- Indireto: usuário recebe exercícios adequados ao seu nível.

---

## 12. Impacto no Backend

- Serviço de adequação por nível e acessibilidade.
- Entrada para o filtro eliminatório (EPIC-006).

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseCatalog`.

Campos: `AccessibilityTags`, `MinExperienceLevel`, `SuitableForSedentary`, `SuitableForBeginner`, `SuitableForIntermediate`, `SuitableForAdvanced`.

---

## 14. Impacto em Gamificação

- Indireto; não concede XP.

---

## 15. Impacto em Monetização

- Reduz frustração inicial, melhorando conversão do trial.

---

## 16. Impacto em Internacionalização

- Tags internas; rótulos traduzidos quando exibidos.

---

## 17. Contrato de API sugerido

```txt
GET /api/exercises?minLevel=sedentario&accessibility=sedentary_safe
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_sanitized | Mapeamento compõe a sanitização. |

---

## 19. Critérios de aceite

### CA-001 — Nível mínimo presente

Dado que um exercício é mapeado,

Quando salvo,

Então deve ter `minExperienceLevel` e flags `suitableFor*` coerentes.

### CA-002 — Filtro por nível

Dado um usuário sedentário,

Quando a quest for gerada,

Então exercícios com `minExperienceLevel` superior devem ser removidos.

---

## 20. Critérios de teste para QA

### Backend

- flags refletem dificuldade/impacto/complexidade;
- `sedentary_safe` exige baixo impacto e complexidade;
- filtro por nível remove incompatíveis.

---

## ✅ Decisão registrada

> Acessibilidade e nível mínimo tornam explícita a adequação do exercício ao perfil, com critério conservador em caso de conflito.
