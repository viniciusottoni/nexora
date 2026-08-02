---
title: US-145 — Mapear padrão de movimento e tags de objetivo
sidebar_position: 145
---

# US-145 — Mapear padrão de movimento e tags de objetivo

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-145 |
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

quero **mapear o padrão de movimento e as tags de objetivo de cada exercício**,

para **selecionar exercícios coerentes com o objetivo do usuário e equilibrar padrões de movimento**.

---

## 3. Contexto

O objetivo do usuário (ganhar massa, perder peso, condicionamento, mais força, manter a forma) define prioridades fisiológicas. Para casá-las com exercícios, cada exercício precisa de `movementPattern` e `goalTags`.

---

## 4. Objetivo

Preencher `MovementPattern` e `GoalTags` de cada exercício, conforme as taxonomias definidas no épico.

---

## 5. Escopo

### Entra nesta US

- Atribuição de `movementPattern` (squat, hinge, horizontal_push, vertical_push, horizontal_pull, vertical_pull, lunge, carry, core_flexion, core_anti_extension, core_anti_rotation, locomotion, jump, balance, mobility).
- Atribuição de `goalTags` (hypertrophy, fat_loss, conditioning, strength, maintenance).
- Uso na afinidade por objetivo e no mapeamento de atributos (US-147).

### Fora desta US

- Tags de risco/acessibilidade (US-040/146).
- Pontuação por objetivo na geração (EPIC-006).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Todo exercício deve ter um `movementPattern` da taxonomia. |
| RN-002 | Todo exercício deve ter afinidade com pelo menos 1 `goalTag`. |
| RN-003 | `movementPattern` e `goalTags` devem usar valores das taxonomias oficiais. |
| RN-004 | O mapeamento alimenta a contribuição automática de atributos (US-147). |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Mapeia e revisa. |
| Usuário final | Sem acesso direto. |

---

## 8. Fluxo principal

1. Sistema analisa tipo, músculos e descrição do exercício.
2. Atribui `movementPattern` e `goalTags`.
3. Salva no `ExerciseCatalog`.

---

## 9. Fluxos alternativos

### 9.1. Padrão ambíguo

Quando o padrão é ambíguo, o exercício vai para revisão antes de aprovar.

---

## 10. Estados esperados

- mapeado;
- ambíguo (em revisão);
- erro de mapeamento.

---

## 11. Impacto no Frontend Flutter

- Sem impacto direto.

---

## 12. Impacto no Backend

- Serviço de mapeamento de padrão/objetivo.
- Entrada para afinidade por objetivo e atributos.

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseCatalog`.

Campos: `MovementPattern`, `GoalTags`.

---

## 14. Impacto em Gamificação

- Base para mapeamento automático de atributos por padrão de movimento.

---

## 15. Impacto em Monetização

- Treino coerente com o objetivo aumenta valor percebido.

---

## 16. Impacto em Internacionalização

- Tags internas; rótulos exibíveis traduzidos quando necessário.

---

## 17. Contrato de API sugerido

```txt
GET /api/exercises?goalTag=strength&movementPattern=squat
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_sanitized | Mapeamento compõe a sanitização. |

---

## 19. Critérios de aceite

### CA-001 — Padrão e objetivo presentes

Dado que um exercício é mapeado,

Quando salvo,

Então deve ter `movementPattern` válido e ao menos 1 `goalTag`.

### CA-002 — Afinidade por objetivo

Dado um objetivo do usuário,

Quando a quest for gerada,

Então exercícios com `goalTags` afins devem ter prioridade.

---

## 20. Critérios de teste para QA

### Backend

- valores fora da taxonomia são rejeitados;
- exercício sem `goalTag` não é aprovado;
- filtro por padrão/objetivo funciona.

---

## ✅ Decisão registrada

> Padrão de movimento e tags de objetivo são obrigatórios e conectam o exercício ao objetivo do usuário e ao mapeamento de atributos.
