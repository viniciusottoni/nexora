---
title: US-045 — Filtrar exercícios incompatíveis por segurança (filtro eliminatório)
sidebar_position: 45
---

# US-045 — Filtrar exercícios incompatíveis por segurança (filtro eliminatório)

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-045 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (regra interna) |
| Dependência principal | ExerciseCatalog, UserProfile |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **remover exercícios incompatíveis antes de pontuar**,

para **proteger a confiança e a segurança do usuário**.

---

## 3. Contexto

Antes de qualquer pontuação, a geração deve eliminar exercícios incompatíveis com nível, equipamento, tempo, limitações, dores, impacto, complexidade e status de aprovação. Esta US expande a antiga "impedir exercícios incompatíveis" para o filtro eliminatório completo.

---

## 4. Objetivo

Aplicar o filtro eliminatório de segurança sobre o catálogo aprovado, retornando apenas exercícios elegíveis.

---

## 5. Escopo

### Entra nesta US

- Remoção por `minExperienceLevel > effectiveExperienceLevel`.
- Remoção por equipamento indisponível.
- Remoção por `timeCost` que estoura o tempo do treino.
- Remoção por conflito com `physicalLimitations` (limitationBlock/contraindication).
- Remoção por conflito com `physicalPains` (painBlock).
- Remoção por alto impacto para sedentário com IMC alto.
- Remoção por alta complexidade técnica para sedentário/iniciante.
- Remoção de exercícios não aprovados.
- Substituição por regressão quando disponível, antes de remover.

### Fora desta US

- Pontuação dos elegíveis (US-151).
- Cálculo do nível efetivo (US-150).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O filtro roda antes da pontuação. |
| RN-002 | Limitações e dores têm prioridade sobre objetivo e recompensa. |
| RN-003 | Apenas exercícios com `isApprovedForWorkoutGeneration = true` entram. |
| RN-004 | Quando houver regressão segura, preferir substituição à remoção. |
| RN-005 | Dor relatada em execução prevalece sobre o onboarding na próxima geração. |
| RN-006 | Gamificação nunca supera segurança. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Aplica o filtro. |
| Usuário final | Recebe treino seguro. |

---

## 8. Fluxo principal

1. Sistema recebe o catálogo aprovado e o perfil.
2. Remove exercícios incompatíveis pelos critérios do filtro.
3. Substitui por regressão quando possível.
4. Retorna a lista de elegíveis para a pontuação.

---

## 9. Fluxos alternativos

### 9.1. Lista vazia após filtro

O sistema relaxa critérios não relacionados a segurança (ex.: variedade) e, se necessário, aciona fallback (US-046).

### 9.2. Sem regressão

Exercício incompatível é removido sem substituição.

---

## 10. Estados esperados

- filtrando;
- elegíveis disponíveis;
- lista insuficiente (relaxar/fallback).

---

## 11. Impacto no Frontend Flutter

- Indireto: usuário não vê exercícios contraindicados.

---

## 12. Impacto no Backend

- Motor de filtro eliminatório.
- Cruzamento com tags de limitação/dor e métricas de impacto/complexidade.

---

## 13. Impacto no Banco de Dados

Entidades: `ExerciseCatalog`, `UserProfile`.

Campos: `MinExperienceLevel`, `RequiredEquipment`, `ImpactLevel`, `TechnicalComplexity`, `LimitationBlockTags`, `PainBlockTags`, `ContraindicationTags`, `IsApprovedForWorkoutGeneration`; `physicalLimitations`, `physicalPains`, `effectiveExperienceLevel`, `bmi`.

---

## 14. Impacto em Gamificação

- Garante que recompensas não venham de exercícios inseguros.

---

## 15. Impacto em Monetização

- Segurança real protege a confiança e a retenção.

---

## 16. Impacto em Internacionalização

- Regra interna; mensagens de substituição traduzidas quando exibidas.

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/eligible-exercises
```

Request:

```json
{
  "effectiveExperienceLevel": "iniciante",
  "physicalLimitations": ["knee_problem"],
  "physicalPains": ["lombar"],
  "availableMinutes": 30
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| quest_generation_failed | Quando o filtro deixa a lista inviável e a geração falha. |

---

## 19. Critérios de aceite

### CA-001 — Limitação física

Dado que o usuário marcou limitação no joelho,

Quando o sistema gerar treino,

Então exercícios com `knee_high_stress` e alto impacto devem ser removidos ou substituídos.

### CA-002 — Dor física

Dado que o usuário marcou dor lombar,

Quando o sistema gerar treino,

Então exercícios com `lumbar_high_stress` devem ser removidos ou substituídos.

---

## 20. Critérios de teste para QA

### Backend

- cada critério do filtro remove os exercícios corretos;
- regressão é preferida quando disponível;
- não aprovados nunca entram;
- lista vazia relaxa critérios não-segurança e/ou aciona fallback.

---

## ✅ Decisão registrada

> O filtro eliminatório roda antes da pontuação e remove tudo que for incompatível com nível, equipamento, tempo, limitações, dores, impacto, complexidade e aprovação. Segurança vem primeiro.
