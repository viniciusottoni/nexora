---
title: US-150 — Calcular nível efetivo (effectiveExperienceLevel) para a geração
sidebar_position: 150
---

# US-150 — Calcular nível efetivo (effectiveExperienceLevel) para a geração

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-150 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (cálculo interno) |
| Dependência principal | UserProfile (experienceLevel, trainingDuration) |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **calcular o nível efetivo do usuário**,

para **gerar treinos no nível certo, de forma conservadora quando houver conflito**.

---

## 3. Contexto

O nível informado (`experienceLevel`) pode conflitar com o tempo de treino (`trainingDuration`). O `effectiveExperienceLevel` reconcilia esses dados e é a referência usada no filtro e na prescrição, sempre escolhendo o nível mais seguro em caso de conflito.

---

## 4. Objetivo

Derivar `effectiveExperienceLevel` a partir do perfil e disponibilizá-lo para a geração.

---

## 5. Escopo

### Entra nesta US

- Regras de validação entre `experienceLevel` e `trainingDuration`.
- Aplicação do nível mais conservador em conflito.
- Disponibilização do nível efetivo para filtro/prescrição.
- Recalibração futura com base no desempenho real (regressão/progressão recorrente).

### Fora desta US

- Coleta de nível e tempo de treino (EPIC-004).
- Filtro eliminatório (US-045) e prescrição (US-153).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | "não treino" trata como sedentário; "menos de 1 mês" como iniciante absoluto. |
| RN-002 | "1 a 6 meses" iniciante em consolidação; "6 a 12 meses" iniciante avançado/intermediário leve. |
| RN-003 | "mais de 1 ano" pode ser intermediário; "mais de 3 anos" pode liberar avançado, se confirmado. |
| RN-004 | Em conflito entre nível e tempo, aplicar o nível mais seguro. |
| RN-005 | Histórico de regressões recorrentes rebaixa o nível efetivo no padrão afetado; progressões recorrentes elevam. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Calcula o nível efetivo. |
| Usuário final | Beneficia-se na geração. |

---

## 8. Fluxo principal

1. Sistema lê `experienceLevel` e `trainingDuration`.
2. Aplica as regras de validação.
3. Em conflito, escolhe o nível mais conservador.
4. Disponibiliza `effectiveExperienceLevel` para a geração.

---

## 9. Fluxos alternativos

### 9.1. Conflito forte

Ex.: avançado + não treino → nível efetivo sedentário.

### 9.2. Recalibração por desempenho

Sempre escolher regressão em um padrão rebaixa o nível efetivo naquele padrão.

---

## 10. Estados esperados

- calculado;
- ajustado por conflito;
- recalibrado por desempenho.

---

## 11. Impacto no Frontend Flutter

- Sem impacto direto (uso interno na geração).

---

## 12. Impacto no Backend

- Serviço de cálculo do nível efetivo.
- Reuso por filtro e prescrição.

---

## 13. Impacto no Banco de Dados

Entidade: `UserProfile`.

Campos: `experienceLevel`, `trainingDuration`, `effectiveExperienceLevel` (derivado/persistido).

---

## 14. Impacto em Gamificação

- Indireto: nível adequado evita esforço inseguro por recompensa.

---

## 15. Impacto em Monetização

- Treino no nível certo melhora a experiência do trial.

---

## 16. Impacto em Internacionalização

- Cálculo interno; sem textos.

---

## 17. Contrato de API sugerido

```txt
GET /api/users/me/effective-level
```

Response conceitual:

```json
{
  "experienceLevel": "avancado",
  "trainingDuration": "nao_treino",
  "effectiveExperienceLevel": "sedentario"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| daily_quest_generated | Geração usa o nível efetivo. |

---

## 19. Critérios de aceite

### CA-001 — Conflito conservador

Dado experienceLevel avançado e trainingDuration "não treino",

Quando calcular o nível efetivo,

Então o resultado deve ser sedentário.

### CA-002 — Uso na geração

Dado um nível efetivo calculado,

Quando a quest for gerada,

Então o filtro e a prescrição devem usar o nível efetivo.

---

## 20. Critérios de teste para QA

### Backend

- todas as combinações de nível × tempo produzem o nível esperado;
- conflito sempre escolhe o mais seguro;
- recalibração por desempenho ajusta o nível no padrão afetado.

---

## ✅ Decisão registrada

> O nível efetivo reconcilia nível informado e tempo de treino, sempre conservador em conflito, e é a referência para filtro e prescrição.
