---
title: US-057 — Acompanhar exercício por exercício
sidebar_position: 57
---

# US-057 — Acompanhar exercício por exercício

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-057 |
| Épico | EPIC-008 — Execução da Quest e Registro do Treino |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | QuestExercise ordenado |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário executando uma quest**,

quero **acompanhar o treino exercício por exercício**,

para **saber exatamente o que fazer, em qual ordem e quanto falta para terminar**.

---

## 3. Contexto

Durante o treino, a experiência precisa ser simples e objetiva. O usuário não deve se perder em informações excessivas ou precisar navegar por telas complexas.

---

## 4. Objetivo

Exibir a sequência ordenada de exercícios da quest, com instruções básicas, progresso visual, atributos impactados e estado de conclusão de cada exercício.

---

## 5. Escopo

### Entra nesta US

- Lista ordenada de exercícios.
- Destaque do exercício atual.
- Exibição de séries, repetições, tempo e descanso.
- Exibição de instruções simples.
- Exibição dos atributos que o treino contribui.
- Exibição de 1 ou 2 atributos impactados por cada exercício, sem contar Sabedoria.
- Exibição do XP de atributo previsto por exercício, de 1 a 4 pontos por atributo impactado, conforme a dificuldade efetiva montada para o exercício.
- Progresso geral da quest.
- Suporte a daily, dungeon e raid.
- Cronômetro regressivo (countdown) de descanso entre séries, derivado de `restSeconds` do exercício atual.
- Cronômetro crescente do tempo total da sessão, derivado de `Quest.startedAt`.

### Fora desta US

- Cronômetro **avançado** por exercício: contagem automática de repetições, timer por repetição/tempo-sob-tensão, detecção de cadência, séries com auto-progressão por sensor.
- Vídeo-aulas próprias.
- Sensor automático de execução.
- Integração com wearables.

> Os dois cronômetros simples listados em "Entra nesta US" (descanso entre séries e tempo total da sessão) são puramente client-side; nenhum estado de cronômetro é persistido no servidor.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Apenas quest em andamento pode ser acompanhada. |
| RN-002 | Exercícios devem aparecer na ordem definida pela quest. |
| RN-003 | Exercício concluído deve aparecer visualmente marcado. |
| RN-004 | Exercício atual deve ser destacado. |
| RN-005 | Dados exibidos devem refletir a QuestExercise salva. |
| RN-006 | Quest cancelada ou concluída não deve permitir continuidade como em andamento. |
| RN-007 | A tela deve deixar visível quais atributos o treino contribui, considerando a soma dos atributos impactados pelos exercícios. |
| RN-008 | Cada exercício deve exibir 1 ou 2 atributos impactados do jogador, sem contar Sabedoria. |
| RN-009 | Cada atributo visível impactado por um exercício deve conceder de 1 a 4 XP internos de atributo conforme a dificuldade efetiva montada para o exercício: 1 para a montagem mais simples e 4 para a mais difícil dentro da margem permitida. |
| RN-010 | A cada 10 XP internos acumulados em um atributo, o jogador ganha 1 ponto visível naquele atributo, conforme EPIC-009. |
| RN-011 | A dificuldade efetiva deve considerar a margem configurável do exercício na quest, como volume, intensidade, variação, carga, tempo, descanso e complexidade quando aplicável. |
| RN-012 | Todo exercício concluído concede +1 XP interno de Sabedoria por padrão, mas Sabedoria não deve aparecer nos atributos visíveis do exercício por ser ganho padrão de aprendizado. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode acompanhar com trial ativo. |
| Premium Mensal | Pode acompanhar com assinatura ativa. |
| Premium Anual | Pode acompanhar com assinatura ativa. |
| Trial expirado | Não inicia novas execuções; execução em andamento deve seguir regra do produto. |
| Assinatura expirada | Não inicia novas execuções; execução em andamento deve seguir regra do produto. |
| Visitante | Não pode acompanhar. |

---

## 8. Fluxo principal

1. Usuário inicia a quest.
2. App exibe a tela de execução.
3. App exibe o resumo dos atributos que o treino contribui.
4. App destaca o primeiro exercício não concluído.
5. Usuário acompanha instruções, volume previsto e atributos impactados pelo exercício atual.
6. Usuário marca exercícios como concluídos até o fim.

---

## 9. Fluxos alternativos

### 9.1. Quest sem exercícios

App deve exibir erro controlado e impedir execução.

### 9.2. Falha ao carregar exercício

App deve permitir tentar novamente sem perder progresso já salvo.

---

## 10. Estados esperados

- carregando execução;
- exercício atual;
- exercício concluído;
- atributos do treino visíveis;
- atributos do exercício visíveis;
- progresso parcial;
- descanso em contagem regressiva;
- descanso pausado;
- descanso finalizado (alerta);
- erro de carregamento;
- quest cancelada;
- quest concluída.

---

## 11. Impacto no Frontend Flutter

- Tela de execução.
- Lista ou stepper de exercícios.
- Card do exercício atual.
- Barra de progresso.
- Estado visual de concluído.
- Resumo visual dos atributos impactados pelo treino.
- Chips/badges de 1 ou 2 atributos impactados por exercício, com XP de atributo previsto pela dificuldade efetiva, sem exibir Sabedoria.
- Cronômetro regressivo de descanso entre séries (pausável/retomável; alerta visual e vibração ao zerar), derivado de `restSeconds` do exercício atual.
- Cronômetro de tempo total da sessão, derivado de `startedAt`.
- Textos localizados.

---

## 12. Impacto no Backend

- Endpoint para consultar execução em andamento.
- Retornar exercícios ordenados.
- Retornar status de cada QuestExercise.
- Retornar atributos impactados do treino de forma agregada.
- Retornar 1 ou 2 atributos visíveis impactados por exercício, com 1 a 4 XP internos por atributo, calculados pela dificuldade efetiva montada.
- Retornar ou aplicar +1 XP interno de Sabedoria por exercício concluído no backend, sem exigir exibição no card/lista do exercício.

---

## 13. Impacto no Banco de Dados

Entidades:

- Quest;
- QuestExercise;
- Exercise.

Campos relevantes:

- QuestExercise.order;
- QuestExercise.status;
- QuestExercise.attributeImpacts;
- QuestExercise.completedAt.

---

## 14. Impacto em Gamificação

- Ajuda o usuário a perceber avanço durante a quest.
- Mostra antecipadamente quais atributos serão treinados.
- Cada exercício contribui com 1 ou 2 atributos visíveis do jogador, sem contar Sabedoria.
- Cada atributo visível impactado recebe de 1 a 4 XP internos conforme a dificuldade efetiva do exercício.
- Sabedoria recebe +1 XP interno por exercício concluído, por baixo dos panos.
- A cada 10 XP internos, forma 1 ponto visível no atributo.
- Não concede XP apenas por visualizar.

---

## 15. Impacto em Monetização

- Tela faz parte da execução protegida por acesso ativo.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Instruções e labels de execução. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/quests/{questId}/execution
```

Response conceitual:

```json
{
  "questId": "uuid",
  "questType": "dungeon",
  "status": "in_progress",
  "startedAt": "2026-06-23T18:00:00Z",
  "attributeXpPreview": {
    "strength": 3,
    "vitality": 1,
    "wisdom": 1
  },
  "exercises": [
    {
      "questExerciseId": "uuid",
      "order": 1,
      "name": "Agachamento livre",
      "status": "pending",
      "sets": 3,
      "repsMin": 8,
      "repsMax": 12,
      "restSeconds": 60,
      "targetRpe": "8",
      "videoUrl": null,
      "xpReward": 12,
      "effectiveDifficulty": 3,
      "attributeImpacts": {
        "strength": 3,
        "vitality": 1
      },
      "hiddenAttributeImpacts": {
        "wisdom": 1
      },
      "completedAt": null
    }
  ]
}
```

> Os campos `restSeconds` (descanso entre séries) e `startedAt` (início da quest) alimentam os dois cronômetros client-side desta US — nenhum estado de cronômetro é retornado ou persistido pelo backend.
> O campo `attributeXpPreview` resume os XP internos de atributo previstos para o treino, incluindo Sabedoria. Cada item em `attributeImpacts` representa um atributo visível do exercício, com valor entre 1 e 4 calculado a partir da dificuldade efetiva (`effectiveDifficulty`): 1 para a montagem mais simples e 4 para a mais difícil dentro da margem permitida do exercício. Sabedoria entra como `hiddenAttributeImpacts.wisdom = 1` por exercício concluído e não deve aparecer no card/lista do exercício.

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| quest_execution_viewed | Quando tela de execução é exibida. |

---

## 19. Critérios de aceite

### CA-001 — Lista ordenada

Dado que a quest possui exercícios,

Quando abrir execução,

Então os exercícios devem aparecer na ordem correta.

### CA-002 — Progresso visual

Dado que um exercício foi concluído,

Quando voltar para a execução,

Então ele deve aparecer marcado como concluído.

### CA-003 — Countdown de descanso

Dado que o usuário concluiu uma série de um exercício com `restSeconds` definido,

Quando iniciar o descanso,

Então um cronômetro regressivo deve contar de `restSeconds` até 0, ser pausável/retomável, e ao zerar emitir alerta (vibração + sinal visual; som opcional).

### CA-004 — Tempo total da sessão

Dado que a quest está em andamento,

Quando a tela de execução está aberta,

Então um cronômetro deve exibir o tempo decorrido desde `startedAt`, atualizando a cada segundo, sem chamar o backend.

### CA-005 — Atributos visíveis

Dado que a quest possui exercícios com atributos impactados,

Quando abrir execução,

Então a tela deve mostrar quais atributos visíveis o treino contribui e quais 1 ou 2 atributos cada exercício contribui, sem contar Sabedoria.

### CA-006 — XP de atributo previsto

Dado que um exercício contribui para atributos,

Quando ele aparecer na execução,

Então cada atributo visível impactado deve exibir de 1 a 4 XP internos previstos, conforme a dificuldade efetiva montada para o exercício, sem exibir Sabedoria.

---

## 20. Critérios de teste para QA

- acompanhar daily;
- acompanhar dungeon;
- acompanhar raid;
- validar ordem dos exercícios;
- validar countdown de descanso (contagem, pausa/retomada, alerta ao zerar);
- validar cronômetro de tempo total da sessão;
- validar resumo dos atributos impactados pelo treino;
- validar 1 ou 2 atributos impactados por exercício, sem contar Sabedoria;
- validar XP interno de atributo previsto entre 1 e 4 por atributo conforme dificuldade efetiva;
- validar que Sabedoria recebe +1 XP interno por exercício concluído sem aparecer no card/lista do exercício;
- validar progresso parcial;
- simular falha de carregamento;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> A tela de execução deve ser simples, ordenada e focada em guiar o usuário pelo treino real sem distrações.
