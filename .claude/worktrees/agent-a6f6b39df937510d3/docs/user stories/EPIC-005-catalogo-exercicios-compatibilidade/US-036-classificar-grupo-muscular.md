---
title: US-036 — Classificar exercícios por grupo muscular e partes do corpo
sidebar_position: 36
---

# US-036 — Classificar exercícios por grupo muscular e partes do corpo

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-036 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | ExerciseCatalog |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **classificar exercícios por grupo muscular primário/secundário e partes do corpo**,

para **equilibrar treinos e permitir filtros por músculo trabalhado**.

---

## 3. Contexto

Para montar treinos equilibrados e variados, cada exercício precisa indicar os grupos musculares principais e secundários e as partes do corpo envolvidas, usando enums internos consistentes.

---

## 4. Objetivo

Preencher `PrimaryMuscleGroups`, `SecondaryMuscleGroups` e `BodyParts` de cada exercício do catálogo.

---

## 5. Escopo

### Entra nesta US

- Classificação de grupo muscular principal (obrigatório, ao menos 1).
- Classificação de grupos secundários.
- Mapeamento de partes do corpo.
- Uso na seleção/equilíbrio de treino.

### Fora desta US

- Equipamento (US-037), dificuldade/impacto (US-038), tags (US-040/145/146).
- Atributos (US-147).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Todo exercício deve ter pelo menos um grupo muscular principal. |
| RN-002 | Grupos secundários são opcionais, mas recomendados. |
| RN-003 | Grupos e partes do corpo devem usar enums internos. |
| RN-004 | A classificação deve permitir equilíbrio entre grupos na geração. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Classifica e revisa. |
| Usuário final | Visualiza músculo trabalhado no exercício. |

---

## 8. Fluxo principal

1. Sistema lê os músculos normalizados do exercício.
2. Define grupo(s) principal(is) e secundário(s).
3. Mapeia partes do corpo.
4. Salva no `ExerciseCatalog`.

---

## 9. Fluxos alternativos

### 9.1. Sem músculo principal

Exercício sem grupo muscular principal não pode ser aprovado (ver US-149).

---

## 10. Estados esperados

- classificado;
- sem músculo principal (bloqueado para aprovação);
- em revisão.

---

## 11. Impacto no Frontend Flutter

- Exibição do músculo trabalhado na tela de instrução do exercício.

---

## 12. Impacto no Backend

- Serviço de classificação muscular.
- Filtro por grupo muscular na geração.

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseCatalog`.

Campos: `PrimaryMuscleGroups`, `SecondaryMuscleGroups`, `BodyParts`.

---

## 14. Impacto em Gamificação

- Indireto: grupos musculares ajudam a definir atributos (US-147).

---

## 15. Impacto em Monetização

- Treino equilibrado aumenta valor percebido.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Nomes de músculos exibidos. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/exercises?muscleGroup=peitoral
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_sanitized | Classificação compõe a sanitização. |

---

## 19. Critérios de aceite

### CA-001 — Grupo principal presente

Dado que um exercício é classificado,

Quando salvo,

Então deve ter pelo menos um grupo muscular principal.

### CA-002 — Filtro por músculo

Dado um grupo muscular,

Quando consultado,

Então o catálogo deve retornar exercícios daquele grupo.

---

## 20. Critérios de teste para QA

### Backend

- exercício sem grupo principal é rejeitado na aprovação;
- filtro por grupo muscular retorna corretamente;
- enums internos são respeitados.

---

## ✅ Decisão registrada

> Todo exercício deve declarar pelo menos um grupo muscular principal, base para equilíbrio de treino e mapeamento de atributos.
