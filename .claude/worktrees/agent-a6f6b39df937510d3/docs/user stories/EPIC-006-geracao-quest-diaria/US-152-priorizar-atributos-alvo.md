---
title: US-152 — Priorizar atributos-alvo e atributos baixos do usuário
sidebar_position: 152
---

# US-152 — Priorizar atributos-alvo e atributos baixos do usuário

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-152 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (cálculo interno) |
| Dependência principal | ExerciseAttributeContribution, UserAttributes |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **priorizar exercícios que evoluem atributos baixos e ligados ao objetivo**,

para **equilibrar o desenvolvimento do Hunter e reforçar a meta do usuário**.

---

## 3. Contexto

O `targetAttributeScore` aumenta a prioridade de exercícios que contribuem para um atributo baixo do usuário e/ou para o atributo mais ligado ao objetivo, sempre sem conflitar com dores e limitações. É um ajuste sobre a pontuação (US-151).

---

## 4. Objetivo

Calcular o `targetAttributeScore` e incorporá-lo à pontuação, considerando atributos baixos e atributos-alvo por objetivo.

---

## 5. Escopo

### Entra nesta US

- Identificação dos atributos baixos do usuário.
- Mapeamento dos atributos-alvo por objetivo (ganhar massa → Força/Vitalidade/Foco; perder peso → Resistência/Vitalidade/Força; condicionamento → Resistência/Vitalidade/Agilidade; mais força → Força/Foco/Vitalidade; manter a forma → equilíbrio).
- Cálculo do `targetAttributeScore` e integração à fórmula.

### Fora desta US

- Concessão de XP (EPIC-008/009).
- Filtro eliminatório (US-045).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | `targetAttributeScore` aumenta quando o exercício contribui para um atributo baixo do usuário. |
| RN-002 | `targetAttributeScore` aumenta quando o exercício contribui para o atributo mais ligado ao objetivo. |
| RN-003 | O ajuste nunca pode contrariar dores/limitações nem superar a segurança. |
| RN-004 | O ajuste é secundário ao filtro eliminatório e ao peso de segurança. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Calcula o ajuste. |
| Usuário final | Recebe treino mais equilibrado. |

---

## 8. Fluxo principal

1. Sistema lê os atributos atuais do usuário.
2. Identifica atributos baixos e atributos-alvo por objetivo.
3. Calcula `targetAttributeScore` por exercício.
4. Integra à pontuação (US-151).

---

## 9. Fluxos alternativos

### 9.1. Conflito com segurança

Se o exercício de maior `targetAttributeScore` for inseguro, ele é descartado pelo filtro/segurança.

---

## 10. Estados esperados

- calculado;
- integrado à pontuação;
- descartado por segurança.

---

## 11. Impacto no Frontend Flutter

- Sem impacto direto.

---

## 12. Impacto no Backend

- Serviço de cálculo do atributo-alvo.
- Integração ao motor de pontuação.

---

## 13. Impacto no Banco de Dados

Entidades: `UserAttributes`, `ExerciseAttributeContribution`.

Campos: pontos/XP por atributo do usuário; vetor de XP do exercício.

---

## 14. Impacto em Gamificação

- Direciona a evolução para atributos baixos e ligados ao objetivo, sem comprometer segurança.

---

## 15. Impacto em Monetização

- Sensação de progresso direcionado aumenta retenção.

---

## 16. Impacto em Internacionalização

- Cálculo interno; sem textos.

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/target-attribute-score
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| daily_quest_generated | Geração usa o ajuste de atributo-alvo. |

---

## 19. Critérios de aceite

### CA-001 — Atributo baixo priorizado

Dado um usuário com Força baixa e objetivo ganhar massa,

Quando a quest for gerada,

Então exercícios com `strengthXp` alto devem ganhar prioridade (respeitada a segurança).

### CA-002 — Segurança acima do atributo

Dado um exercício de alto atributo-alvo, porém inseguro,

Quando a geração rodar,

Então ele não deve ser selecionado.

---

## 20. Critérios de teste para QA

### Backend

- atributos baixos elevam o score;
- atributos-alvo por objetivo são mapeados corretamente;
- segurança/limitações prevalecem sobre o ajuste.

---

## ✅ Decisão registrada

> O ajuste por atributo-alvo direciona a evolução para atributos baixos e ligados ao objetivo, sempre subordinado ao filtro de segurança.
