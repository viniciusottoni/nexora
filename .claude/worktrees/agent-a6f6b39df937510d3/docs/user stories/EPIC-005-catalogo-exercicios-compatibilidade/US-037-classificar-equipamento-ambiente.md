---
title: US-037 — Classificar exercícios por equipamento e ambiente
sidebar_position: 37
---

# US-037 — Classificar exercícios por equipamento e ambiente

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-037 |
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

quero **classificar exercícios por equipamento necessário e ambiente**,

para **respeitar o que o usuário possui e gerar treino sem equipamento por padrão**.

---

## 3. Contexto

Equipamentos não são coletados no onboarding; o sistema gera quests compatíveis com treino sem equipamento por padrão. Para isso, cada exercício precisa declarar o equipamento exigido (enum interno) e o ambiente (casa, academia ou ambos).

---

## 4. Objetivo

Preencher `RequiredEquipment` e `Environment` de cada exercício, permitindo priorizar exercícios `no_equipment` e filtrar por disponibilidade.

---

## 5. Escopo

### Entra nesta US

- Mapeamento de equipamento para enum interno.
- Classificação de ambiente: casa, academia ou ambos.
- Marcação de exercícios sem equipamento (`no_equipment`).
- Filtro por equipamento disponível na geração.

### Fora desta US

- Coleta de equipamentos do usuário (pós-MVP / perfil).
- Dificuldade/impacto (US-038), tags de risco (US-040).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Todo exercício deve declarar equipamento necessário, mapeado para enum interno. |
| RN-002 | Exercícios sem equipamento devem receber a tag `no_equipment` (acessibilidade). |
| RN-003 | O ambiente deve ser casa, academia ou ambos. |
| RN-004 | A geração padrão prioriza exercícios compatíveis com treino sem equipamento. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Classifica e revisa. |
| Usuário final | Vê equipamento exigido no exercício. |

---

## 8. Fluxo principal

1. Sistema lê o equipamento normalizado do exercício.
2. Mapeia para enum interno e define ambiente.
3. Marca `no_equipment` quando aplicável.
4. Salva no `ExerciseCatalog`.

---

## 9. Fluxos alternativos

### 9.1. Equipamento desconhecido

Equipamento sem enum vai para revisão e o exercício não avança até resolução.

---

## 10. Estados esperados

- classificado;
- equipamento pendente de mapeamento;
- em revisão.

---

## 11. Impacto no Frontend Flutter

- Exibição do equipamento exigido na tela de instrução.

---

## 12. Impacto no Backend

- Serviço de classificação de equipamento/ambiente.
- Filtro por equipamento na geração.

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseCatalog`.

Campos: `RequiredEquipment`, `Environment`, `AccessibilityTags` (`no_equipment`).

---

## 14. Impacto em Gamificação

- Indireto; não concede XP.

---

## 15. Impacto em Monetização

- Treino sem equipamento garante valor imediato no trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Nomes de equipamentos exibidos. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/exercises?equipment=none&environment=casa
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_sanitized | Classificação compõe a sanitização. |

---

## 19. Critérios de aceite

### CA-001 — Equipamento mapeado

Dado que um exercício é classificado,

Quando salvo,

Então deve ter equipamento mapeado para enum interno e ambiente definido.

### CA-002 — Treino sem equipamento

Dado que o usuário não tem equipamento,

Quando a quest for gerada,

Então devem ser priorizados exercícios `no_equipment`.

---

## 20. Critérios de teste para QA

### Backend

- equipamento desconhecido vai para revisão;
- filtro por equipamento retorna corretamente;
- exercícios `no_equipment` são priorizados na geração padrão.

---

## ✅ Decisão registrada

> Todo exercício declara equipamento e ambiente; a geração padrão prioriza treino sem equipamento, já que o onboarding não coleta equipamentos.
