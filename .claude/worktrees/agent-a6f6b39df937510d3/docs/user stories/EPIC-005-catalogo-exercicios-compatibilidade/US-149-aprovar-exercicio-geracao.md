---
title: US-149 — Aprovar exercício para geração de quests
sidebar_position: 149
---

# US-149 — Aprovar exercício para geração de quests

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-149 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema / Curador |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (processo interno) |
| Dependência principal | ExerciseCatalog |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema/curador**,

quero **aprovar exercícios que atendem todos os critérios**,

para **liberá-los para a geração de quests com segurança**.

---

## 3. Contexto

A aprovação é o portão final do pipeline. Só exercícios que passam por todos os critérios obrigatórios recebem `isApprovedForWorkoutGeneration = true` e entram na geração.

---

## 4. Objetivo

Validar os critérios de aprovação, alternar status para `approved` e habilitar `isApprovedForWorkoutGeneration`.

---

## 5. Escopo

### Entra nesta US

- Verificação dos critérios de aprovação.
- Transição de status `pending_review` → `approved` / `rejected`.
- Marcação de `isApprovedForWorkoutGeneration`.
- Suporte a exceção manual auditável (ex.: mídia).
- Suporte a `deprecated` para retirar exercícios sem afetar histórico.

### Fora desta US

- Sanitização (US-148).
- Geração/uso na quest (EPIC-006).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Só pode ser `approved` o exercício com nome PT-BR, grupo muscular principal, tipo, equipamento mapeado, mídia válida e instrução. |
| RN-002 | Deve ter `minExperienceLevel`, impacto definido e tags de articulação. |
| RN-003 | Deve ter `goalTags` e tags de limitação/dor quando necessário. |
| RN-004 | Deve ter contribuição de atributos com `wisdomXp >= 1` e 1 atributo extra > 0. |
| RN-005 | Exercício reprovado recebe `rejected` com motivo. |
| RN-006 | Exceção manual (ex.: mídia) deve ser registrada e auditável. |
| RN-007 | Apenas exercícios `approved` entram na geração. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Curador | Aprova/rejeita. |
| Usuário final | Consome exercícios aprovados via quest. |

---

## 8. Fluxo principal

1. Sistema lê exercícios `pending_review`.
2. Verifica todos os critérios de aprovação.
3. Marca `approved` + `isApprovedForWorkoutGeneration = true` ou `rejected` com motivo.

---

## 9. Fluxos alternativos

### 9.1. Exceção manual

Curador aprova exercício sem mídia padrão, registrando justificativa auditável.

### 9.2. Depreciação

Exercício aprovado pode ser marcado `deprecated`, saindo de novas seleções sem afetar quests passadas.

---

## 10. Estados esperados

- pending_review;
- approved;
- rejected (com motivo);
- deprecated.

---

## 11. Impacto no Frontend Flutter

- Indireto: somente aprovados aparecem em quests.

---

## 12. Impacto no Backend

- Motor de validação de aprovação.
- Transição de status e flag de geração.
- Trilha de auditoria de exceções.

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseCatalog`.

Campos: `SanitizationStatus`, `IsApprovedForWorkoutGeneration`, `UpdatedAt`.

---

## 14. Impacto em Gamificação

- Garante que XP de atributo só venha de exercícios aprovados.

---

## 15. Impacto em Monetização

- Qualidade e segurança sustentam a conversão do trial.

---

## 16. Impacto em Internacionalização

- Processo interno; exige nome PT-BR presente.

---

## 17. Contrato de API sugerido

```txt
POST /api/admin/exercises/{id}/approve
POST /api/admin/exercises/{id}/reject
```

Response conceitual:

```json
{
  "id": "exr_001",
  "status": "approved",
  "isApprovedForWorkoutGeneration": true
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_approved | Quando o exercício é aprovado. |
| exercise_rejected | Quando o exercício é rejeitado. |

---

## 19. Critérios de aceite

### CA-001 — Aprovação completa

Dado que um exercício atende todos os critérios,

Quando aprovado,

Então deve ficar `approved` com `isApprovedForWorkoutGeneration = true`.

### CA-002 — Bloqueio sem atributo

Dado que um exercício não possui contribuição de atributo válida,

Quando o sistema tentar aprová-lo,

Então a aprovação deve ser bloqueada.

---

## 20. Critérios de teste para QA

### Backend

- aprovação exige todos os critérios obrigatórios;
- exercício sem atributo/mídia/instrução é rejeitado;
- exceção manual é registrada;
- `deprecated` sai de novas seleções sem afetar histórico.

### E2E

- somente aprovados entram na geração de quest.

---

## ✅ Decisão registrada

> A aprovação é o portão final: nenhum exercício é usado em quest sem atender todos os critérios obrigatórios; exceções são manuais e auditáveis.
