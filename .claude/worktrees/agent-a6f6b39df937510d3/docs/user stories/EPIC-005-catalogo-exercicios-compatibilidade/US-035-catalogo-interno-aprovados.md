---
title: US-035 — Manter catálogo interno de exercícios aprovados
sidebar_position: 35
---

# US-035 — Manter catálogo interno de exercícios aprovados

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-035 |
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

quero **manter um catálogo interno de exercícios aprovados**,

para **montar treinos reais sem depender 100% de IA nem da API externa em tempo real**.

---

## 3. Contexto

A geração de quests precisa de uma fonte estável e confiável de exercícios. O catálogo interno (`ExerciseCatalog`) consolida exercícios já importados, sanitizados e aprovados, e serve tanto à geração principal quanto ao fallback por templates do EPIC-006.

---

## 4. Objetivo

Garantir que exista um conjunto mínimo de exercícios aprovados (`isApprovedForWorkoutGeneration = true`), com metadados suficientes, disponível para a geração de quests e para o fallback.

---

## 5. Escopo

### Entra nesta US

- Consulta ao catálogo de exercícios aprovados.
- Garantia de cobertura mínima por tipo, padrão de movimento e nível.
- Disponibilização do catálogo como fonte da geração e do fallback.
- Marcação de exercícios `deprecated` sem quebrar quests já geradas.

### Fora desta US

- Importação (US-143), normalização (US-144), sanitização (US-148) e aprovação (US-149).
- Geração da quest (EPIC-006).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Apenas exercícios com `isApprovedForWorkoutGeneration = true` entram na geração. |
| RN-002 | O catálogo deve ter cobertura mínima para iniciantes e treino sem equipamento. |
| RN-003 | O catálogo deve permitir fallback caso a IA não gere treino. |
| RN-004 | A geração não deve depender da API externa em tempo real. |
| RN-005 | Exercícios `deprecated` não entram em novas quests, mas não corrompem quests passadas. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Sem acesso. |
| Usuário em Trial | Consome catálogo via geração. |
| Premium Mensal | Consome catálogo via geração. |
| Premium Anual | Consome catálogo via geração. |
| Trial/Assinatura expirados | Não geram quest (EPIC-003/006). |

---

## 8. Fluxo principal

1. A geração solicita exercícios compatíveis ao catálogo.
2. O catálogo retorna apenas exercícios aprovados.
3. Se a IA falhar, o fallback usa o mesmo catálogo aprovado por templates.

---

## 9. Fluxos alternativos

### 9.1. Catálogo insuficiente para o perfil

O sistema amplia critérios com segurança (ex.: variantes de regressão) e registra o evento para curadoria.

### 9.2. Exercício depreciado

Exercícios `deprecated` são excluídos de novas seleções sem afetar histórico.

---

## 10. Estados esperados

- catálogo disponível;
- catálogo com cobertura mínima;
- catálogo insuficiente (alerta de curadoria);
- erro de consulta.

---

## 11. Impacto no Frontend Flutter

- Sem tela própria; consome o catálogo via quest gerada.

---

## 12. Impacto no Backend

- Serviço de consulta/filtragem de exercícios aprovados.
- Indicadores de cobertura mínima do catálogo.
- Suporte a status `deprecated`.

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseCatalog`.

Campos relevantes: `IsApprovedForWorkoutGeneration`, `ExerciseType`, `MovementPattern`, `MinExperienceLevel`, `RequiredEquipment`.

---

## 14. Impacto em Gamificação

- Base para XP de atributo via `ExerciseAttributeContribution`.
- Não concede XP por si só.

---

## 15. Impacto em Monetização

- Garante valor real durante o trial (treino sempre disponível).

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Nomes e instruções exibidos. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/exercises?approved=true&level=iniciante&equipment=none
```

Response conceitual:

```json
{
  "items": [
    { "id": "exr_001", "namePtBr": "Agachamento livre", "exerciseType": "strength" }
  ],
  "total": 1
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| daily_quest_generated | Geração consome o catálogo. |
| dungeon_generated | Geração de dungeon consome o catálogo. |

---

## 19. Critérios de aceite

### CA-001 — Apenas aprovados

Dado que existem exercícios não aprovados,

Quando a geração consultar o catálogo,

Então apenas exercícios aprovados devem ser retornados.

### CA-002 — Fallback disponível

Dado que a IA falhou,

Quando o fallback for acionado,

Então deve montar treino a partir do catálogo aprovado.

---

## 20. Critérios de teste para QA

### Backend

- consulta retorna somente `isApprovedForWorkoutGeneration = true`;
- exercícios `deprecated` não aparecem em novas seleções;
- cobertura mínima para iniciante sem equipamento é atendida.

### E2E

- geração principal usa o catálogo;
- fallback por templates usa o mesmo catálogo aprovado.

---

## ✅ Decisão registrada

> O catálogo interno aprovado é a fonte única de exercícios para geração e fallback. O AWAKEN não depende de IA nem da API externa em tempo real para montar treino.
