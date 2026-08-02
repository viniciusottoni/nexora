---
title: US-058 — Marcar exercício como concluído
sidebar_position: 58
---

# US-058 — Marcar exercício como concluído

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-058 |
| Épico | EPIC-008 — Execução da Quest e Registro do Treino |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | QuestExercise e HunterProgress |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário executando uma quest**,

quero **marcar cada exercício como concluído**,

para **registrar meu progresso real e receber recompensas proporcionais ao que executei**.

---

## 3. Contexto

Cada exercício é uma unidade de progresso. Ao concluir um exercício, o sistema deve registrar o avanço, calcular XP geral, conceder XP interno para 1 ou 2 atributos visíveis do jogador conforme o catálogo, e conceder +1 XP interno de Sabedoria por padrão.

---

## 4. Objetivo

Permitir marcar exercício como concluído de forma idempotente, atualizando progresso parcial, XP geral e XP interno de atributos conforme regras do produto.

---

## 5. Escopo

### Entra nesta US

- Marcar exercício como concluído.
- Registrar `completedAt`.
- Conceder XP geral do exercício.
- Conceder XP interno em 1 ou 2 atributos visíveis do exercício, sem contar Sabedoria.
- Conceder de 1 a 4 XP internos por atributo impactado, conforme a dificuldade efetiva montada para o exercício.
- Conceder +1 XP interno de Sabedoria por exercício concluído, sem exigir exibição no card/lista do exercício.
- Converter 10 XP internos acumulados em 1 ponto visível no atributo, conforme EPIC-009.
- Evitar duplicidade de recompensa.

### Fora desta US

- Conclusão final da quest.
- Recompensa final de dungeon.
- Sensor automático.
- Validação biomecânica.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Só é possível marcar exercício de quest em andamento. |
| RN-002 | Exercício já concluído não deve conceder XP novamente. |
| RN-003 | Cada exercício concede XP geral (`xpReward`). |
| RN-004 | Cada exercício concede XP interno nos 1 ou 2 atributos visíveis listados em `attributeImpacts`; Sabedoria não conta nesse limite visual. |
| RN-005 | Cada atributo visível impactado por um exercício deve receber de 1 a 4 XP internos de atributo conforme a dificuldade efetiva montada: 1 para a montagem mais simples e 4 para a mais difícil dentro da margem permitida. |
| RN-006 | A cada 10 XP internos acumulados em um atributo, o jogador ganha 1 ponto visível naquele atributo, conforme EPIC-009. |
| RN-007 | A dificuldade efetiva deve considerar a margem configurável do exercício na quest, como volume, intensidade, variação, carga, tempo, descanso e complexidade quando aplicável. |
| RN-008 | Todo exercício concluído concede +1 XP interno de Sabedoria por padrão, como aprendizado inato da execução. |
| RN-009 | O evento deve ser idempotente para evitar recompensa duplicada. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode marcar se quest estiver em andamento. |
| Premium Mensal | Pode marcar se quest estiver em andamento. |
| Premium Anual | Pode marcar se quest estiver em andamento. |
| Trial expirado | Não inicia novas quests; progressos em andamento seguem regra vigente. |
| Assinatura expirada | Não inicia novas quests; progressos em andamento seguem regra vigente. |
| Visitante | Não pode marcar. |

---

## 8. Fluxo principal

1. Usuário executa exercício.
2. Usuário toca em “Concluir exercício”.
3. App envia conclusão para backend.
4. Backend valida estado da quest e do exercício.
5. Backend registra conclusão e recompensa parcial.
6. Backend aplica XP interno nos atributos visíveis impactados conforme a dificuldade efetiva, aplica +1 XP interno de Sabedoria, e converte pontos quando atingir múltiplos de 10.
7. App atualiza progresso visual e feedback de ganhos.

---

## 9. Fluxos alternativos

### 9.1. Exercício já concluído

Backend retorna sucesso idempotente sem conceder XP novamente.

### 9.2. Quest cancelada ou concluída

Backend rejeita a marcação.

---

## 10. Estados esperados

- exercício pendente;
- concluindo;
- concluído;
- já concluído;
- erro de conclusão;
- quest encerrada.

---

## 11. Impacto no Frontend Flutter

- Botão “Concluir exercício”.
- Estado de loading por exercício.
- Feedback visual de conclusão.
- Feedback dos atributos visíveis impactados e XP interno ganho, sem exibir Sabedoria padrão no exercício.
- Atualização de progresso.
- Evitar múltiplos toques gerando chamadas repetidas.

---

## 12. Impacto no Backend

- Endpoint para concluir exercício.
- Idempotência por QuestExercise.
- Atualização de XP geral, XP interno de atributos conforme dificuldade efetiva, +1 XP interno de Sabedoria e conversão a cada 10 XP internos.
- Retorno dos ganhos do exercício.

---

## 13. Impacto no Banco de Dados

Entidades:

- QuestExercise;
- Exercise;
- HunterProgress;
- HunterAttributes.

Campos relevantes:

- QuestExercise.status;
- QuestExercise.completedAt;
- QuestExercise.xpEarned;
- QuestExercise.attributeXpEarned.

---

## 14. Impacto em Gamificação

- Concede XP por exercício.
- Concede XP interno em 1 ou 2 atributos visíveis conforme `attributeImpacts`.
- Cada atributo visível impactado recebe de 1 a 4 XP internos por exercício conforme a dificuldade efetiva.
- Concede +1 XP interno de Sabedoria por exercício concluído, por baixo dos panos.
- A cada 10 XP internos acumulados, forma 1 ponto visível no atributo.
- Não pode duplicar recompensa.

---

## 15. Impacto em Monetização

- Recurso de execução para usuários com acesso ativo.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Botões e mensagens de conclusão. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/{questId}/exercises/{questExerciseId}/complete
```

Response conceitual:

```json
{
  "questExerciseId": "uuid",
  "status": "completed",
  "xpEarned": 20,
  "effectiveDifficulty": 3,
  "attributeXpEarned": {
    "strength": 3,
    "vitality": 1,
    "wisdom": 1
  },
  "attributePointsGranted": {
    "strength": 0,
    "vitality": 0
  }
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_completed | Quando exercício é concluído com sucesso. |

Propriedades:

- `exercise_id`;
- `xp_earned`;
- `attribute_xp_earned`;
- `attribute_points_granted`;
- `quest_type`.

---

## 19. Critérios de aceite

### CA-001 — Conclusão válida

Dado que a quest está em andamento,

Quando usuário concluir exercício,

Então o exercício deve ser marcado como concluído e recompensado.

### CA-002 — Sem duplicidade

Dado que o exercício já foi concluído,

Quando a mesma chamada ocorrer novamente,

Então XP e atributos não devem duplicar.

### CA-003 — XP interno de atributo

Dado que o exercício possui `attributeImpacts`,

Quando usuário concluir exercício,

Então o backend deve conceder XP interno para 1 ou 2 atributos visíveis, com 1 a 4 XP internos por atributo impactado conforme a dificuldade efetiva, além de +1 XP interno de Sabedoria por padrão.

### CA-004 — Conversão para ponto visível

Dado que um atributo acumula 10 XP internos,

Quando a conclusão do exercício for processada,

Então o sistema deve converter esses 10 XP internos em 1 ponto visível naquele atributo.

---

## 20. Critérios de teste para QA

- concluir exercício em daily;
- concluir exercício em dungeon;
- concluir exercício em raid;
- validar XP do exercício;
- validar 1 ou 2 atributos visíveis impactados, sem contar Sabedoria;
- validar XP interno de atributo entre 1 e 4 por atributo conforme dificuldade efetiva;
- validar +1 XP interno de Sabedoria por exercício concluído;
- validar conversão de 10 XP internos em 1 ponto visível;
- tentar duplicar conclusão;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Cada exercício concluído é uma unidade real de progresso e deve recompensar XP geral, XP interno em 1 ou 2 atributos específicos visíveis e +1 XP interno de Sabedoria por baixo dos panos, sem permitir duplicidade.
