---
title: US-038 — Classificar dificuldade, complexidade técnica e impacto
sidebar_position: 38
---

# US-038 — Classificar dificuldade, complexidade técnica e impacto

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-038 |
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

quero **classificar dificuldade, complexidade técnica e impacto de cada exercício**,

para **adaptar o treino ao nível do usuário e proteger contra alto impacto/complexidade indevidos**.

---

## 3. Contexto

Adaptar treino ao nível exige três medidas distintas: dificuldade geral (1–5), complexidade técnica (1–5) e impacto articular (0–5). Elas alimentam o filtro eliminatório e a pontuação da geração (EPIC-006), evitando exercícios perigosos para sedentários/iniciantes.

---

## 4. Objetivo

Preencher `DifficultyLevel` (1–5), `TechnicalComplexity` (1–5) e `ImpactLevel` (0–5) de cada exercício.

---

## 5. Escopo

### Entra nesta US

- Definição de dificuldade geral (1–5).
- Definição de complexidade técnica (1–5).
- Definição de impacto articular (0–5).
- Uso desses valores no filtro e pontuação da geração.

### Fora desta US

- `minExperienceLevel` e adequação por nível (US-146).
- Tags de risco/articulação (US-040).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Todo exercício deve ter `DifficultyLevel` entre 1 e 5. |
| RN-002 | Todo exercício deve ter `TechnicalComplexity` entre 1 e 5. |
| RN-003 | Todo exercício deve ter `ImpactLevel` entre 0 e 5. |
| RN-004 | Alta complexidade técnica deve bloquear o exercício para sedentário/iniciante na geração. |
| RN-005 | Alto impacto deve ser evitado para sedentário com IMC alto na geração. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Classifica e revisa. |
| Usuário final | Vê dificuldade do exercício (quando exibida). |

---

## 8. Fluxo principal

1. Sistema avalia metadados do provider e heurísticas internas.
2. Define dificuldade, complexidade e impacto.
3. Salva no `ExerciseCatalog`.

---

## 9. Fluxos alternativos

### 9.1. Metadados insuficientes

Sem dados suficientes, o exercício recebe valores conservadores e vai para revisão.

---

## 10. Estados esperados

- classificado;
- valores conservadores aplicados;
- em revisão.

---

## 11. Impacto no Frontend Flutter

- Exibição opcional do nível de dificuldade no exercício.

---

## 12. Impacto no Backend

- Serviço de classificação de dificuldade/complexidade/impacto.
- Entrada para filtro eliminatório e pontuação (EPIC-006).

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseCatalog`.

Campos: `DifficultyLevel`, `TechnicalComplexity`, `ImpactLevel`.

---

## 14. Impacto em Gamificação

- Indireto: dificuldade/intensidade podem ampliar o XP do atributo principal (US-147).

---

## 15. Impacto em Monetização

- Treino seguro e adequado ao nível aumenta retenção no trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Rótulos de dificuldade. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/exercises?maxDifficulty=2&maxImpact=1
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_sanitized | Classificação compõe a sanitização. |

---

## 19. Critérios de aceite

### CA-001 — Métricas presentes

Dado que um exercício é classificado,

Quando salvo,

Então deve ter dificuldade (1–5), complexidade (1–5) e impacto (0–5).

### CA-002 — Bloqueio por complexidade

Dado um usuário iniciante,

Quando a quest for gerada,

Então exercícios de alta complexidade técnica devem ser removidos.

---

## 20. Critérios de teste para QA

### Backend

- valores fora de faixa são rejeitados;
- sem metadados aplica valores conservadores;
- filtro por dificuldade/impacto funciona.

---

## ✅ Decisão registrada

> Dificuldade, complexidade técnica e impacto são métricas obrigatórias que alimentam o filtro de segurança e a pontuação da geração.
