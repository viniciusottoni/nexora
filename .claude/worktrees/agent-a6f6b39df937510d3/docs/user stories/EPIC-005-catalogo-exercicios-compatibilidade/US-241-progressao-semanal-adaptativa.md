---
title: US-241 — Progressão semanal adaptativa (sobrecarga progressiva por estado, rank e atributos)
sidebar_position: 241
---

# US-241 — Progressão semanal adaptativa (sobrecarga progressiva por estado, rank e atributos)

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-241 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR/EN/ES/FR apenas em avisos ("treino mais desafiador esta semana", "semana de deload") |
| Dependência principal | US-034 (perfil editável), US-150 (nível efetivo), US-153 (prescrição), US-239 (recuperação/volume), US-240 (blueprint do dia), US-039/US-236 (variantes), EPIC-009 (rank e atributos) |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **reavaliar semanalmente o estado atual do jogador — configurações de perfil (que ele pode ter mudado), rank e atributos — e ajustar exercícios, séries, repetições e descanso por sobrecarga progressiva e autorregulação**,

para **que o treino acompanhe a evolução do jogador e ele se sinta sempre minimamente desafiado, progredindo de forma segura e cientificamente embasada**.

---

## 3. Contexto

A prescrição inicial (US-153) define o ponto de partida por nível e objetivo, e a US-240 monta o alvo do dia. Falta a camada que faz o treino **evoluir com o jogador ao longo do tempo**. Sem ela, o usuário estagna: o mesmo estímulo que desafiava na semana 1 fica fácil na semana 4, e mudanças de perfil (peso, tempo por treino, equipamento, objetivo) não se refletem no treino.

Esta US adiciona a **progressão semanal adaptativa**, apoiada em princípios consolidados do treinamento (não dependentes de data):

- **Sobrecarga progressiva**: para continuar adaptando, a demanda precisa aumentar gradualmente ao longo do tempo (mais repetições, mais séries, menos descanso, variação mais difícil, maior RPE-alvo).
- **Dupla progressão**: progredir repetições dentro da faixa; ao atingir o topo da faixa em todas as séries, avançar o estímulo (nova série, variante mais difícil, menos descanso).
- **Autorregulação por RPE/desempenho**: decidir a cada semana se **progride, mantém ou regride** com base no que o usuário conseguiu executar (reps atingidas e esforço percebido), mantendo as sessões dentro de uma **faixa-alvo de desafio**.
- **Progressão de volume com teto e deload**: adicionar ~1 série por músculo por semana até o teto recuperável do nível (US-239) e então **descarregar (deload)** para dissipar fadiga; deload também é acionado por semanas seguidas difíceis/falhas.
- **Ritmo por nível**: iniciantes progridem mais rápido (progressão quase linear); avançados progridem mais devagar e dependem mais de variação/periodização.

Além do desempenho, a **evolução do personagem** entra na conta: o **rank** (E→S, EPIC-009) eleva o teto de complexidade/RPE e habilita programas mais difíceis (US-231), e os **6 atributos** (Força, Agilidade, Resistência, Vitalidade, Foco, Sabedoria) enviesam o **vetor de progressão** (ex.: Força alta → viés para variantes mais pesadas/menos reps; Resistência alta → viés para mais reps/menos descanso), fazendo o treino refletir o "build" do Hunter.

Tudo é **determinístico** (tabelas + histórico + estado), sem IA. Esta US produz um `WeeklyProgressionPlan` que **modula** a seleção (US-240/US-151) e a prescrição (US-153); ela **não** substitui a segurança (US-045) nem os tetos de recuperação (US-239), que permanecem soberanos.

---

## 4. Objetivo

Manter um `WeeklyProgressionState` por usuário e, na virada de cada semana (e a cada mudança relevante de perfil), calcular um `WeeklyProgressionPlan` que ajuste, de forma segura e científica: nível de dificuldade dos exercícios (variantes), séries (volume), repetições, descanso e RPE-alvo — considerando desempenho recente, mudanças de configuração, rank e atributos.

---

## 5. Escopo

### Entra nesta US

- Reavaliação semanal do estado do jogador (snapshot de perfil + rank + atributos + desempenho da semana).
- Detecção de mudança de configuração desde a última semana (peso, tempo por treino, equipamento, objetivo, nível) e recalibração.
- Decisão de progressão por movimento/grupo: **progredir / manter / regredir** (autorregulação por reps atingidas e RPE).
- Progressão de volume (+séries) com teto recuperável (US-239) e **deload** periódico/por fadiga.
- Ajuste de repetições (dupla progressão), descanso e RPE-alvo.
- Troca para variante mais difícil/fácil via grafo de progressão/regressão (US-039/US-236).
- Viés do vetor de progressão por rank e atributos.
- Faixa-alvo de desafio ("sempre minimamente desafiado").
- `WeeklyProgressionPlan` consumível por US-240 (seleção) e US-153 (prescrição).

### Fora desta US

- Prescrição inicial base por nível/objetivo (US-153) — aqui é apenas a evolução sobre ela.
- Cálculo de nível efetivo inicial (US-150) — pode ser elevado por esta US ao longo do tempo.
- Tetos de recuperação e anti-sobrecarga (US-239) — aqui são respeitados como limite, não redefinidos.
- Cálculo de rank/atributos/rankScore (EPIC-009) — insumos, não recalculados aqui.
- Filtro eliminatório de segurança (US-045) — soberano.
- Edição manual do treino pelo usuário (EPIC-007).

---

## 6. Modelo de progressão (determinístico)

### 6.1. Gatilhos de reavaliação

- **Semanal**: na virada da semana (ancorada em `WeekAnchorDate`, alinhada à US-239 e ao dia de streak US-070).
- **Por mudança de perfil**: ao salvar mudança relevante (US-034) — peso, tempo por treino, equipamento, objetivo, nível — recalibra imediatamente para a próxima geração.

### 6.2. Decisão de progressão por movimento (autorregulação)

Com base no desempenho registrado da semana (reps atingidas × meta e RPE médio × RPE-alvo):

| Sinal da semana | Decisão | Ação no estímulo |
|---|---|---|
| Metas cumpridas com folga (RPE ≤ alvo − 1; reps no topo da faixa em todas as séries) | **Progredir** | Avançar: +reps até o topo → +1 série (até teto US-239) → variante mais difícil (US-039/236) → −descanso (~10–15s) → +1 RPE-alvo |
| Metas cumpridas dentro da faixa (RPE ≈ alvo) | **Manter/incrementar leve** | +reps dentro da faixa; sem novo eixo de sobrecarga |
| Metas não cumpridas / RPE ≥ alvo + 1 / sessões perdidas por fadiga | **Regredir/segurar** | Manter ou reduzir reps/série, ou variante mais fácil; não adicionar carga |

Só **um eixo** de sobrecarga avança por vez (reps → séries → variante → descanso → RPE), para o aumento ser gradual.

### 6.3. Volume, teto e deload

- Progressão de volume: até ~+1 série por músculo por semana, respeitando o teto semanal recuperável do nível (US-239).
- **Deload** (semana leve: volume ~−40–50% e RPE reduzido) quando:
  - atingido ~4–6 semanas de progressão contínua no mesociclo; ou
  - ~2 semanas seguidas de metas não cumpridas / RPE acima do alvo (fadiga acumulada).
- Após o deload, retoma a progressão a partir de um patamar levemente acima do início do ciclo anterior.

### 6.4. Faixa-alvo de desafio ("minimamente desafiado")

- Cada nível/objetivo define uma faixa de RPE-alvo (US-153). O sistema mantém as sessões dentro dela: consistentemente abaixo → progride; consistentemente acima → segura/regride.

### 6.5. Recalibração por mudança de perfil

- **Peso**: reancorar baseline relativo ao peso corporal (exercícios com peso do corpo) e ênfase de objetivo; mudança grande é sinalizada.
- **Tempo por treino**: recomputar o orçamento de volume por sessão (entrada da US-240).
- **Equipamento**: repovoar as variantes de progressão disponíveis (US-039/236).
- **Objetivo/Nível**: reancorar faixas de reps/descanso/RPE (US-153) e o ponto de progressão.

### 6.6. Viés por rank e atributos (evolução do personagem)

- **Rank** (EPIC-009): eleva o teto de complexidade técnica e o teto de RPE-alvo conforme sobe (E→S), coerente com o desbloqueio de programas por rank (US-231).
- **Atributos** (vetor de viés, dentro dos limites de segurança/recuperação):
  - Força alta → viés a variantes mais desafiadoras e faixas de reps mais baixas;
  - Resistência alta → viés a mais reps e menor descanso;
  - Agilidade alta → viés a variantes mais complexas/dinâmicas (respeitando impacto e limitações);
  - Vitalidade alta → maior tolerância de volume dentro do teto recuperável;
  - Foco alto → maior tolerância a complexidade técnica;
  - Sabedoria → não enviesa carga (é o atributo de consistência, US-131).

---

## 7. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A cada semana (e a cada mudança relevante de perfil), a geração deve usar o estado **atual** do jogador (perfil + rank + atributos + desempenho), nunca um snapshot desatualizado. |
| RN-002 | A progressão é autorregulada: progride, mantém ou regride conforme reps atingidas e RPE em relação às metas, mantendo as sessões na faixa-alvo de desafio. |
| RN-003 | Apenas um eixo de sobrecarga (reps → séries → variante → descanso → RPE) avança por vez, para aumento gradual. |
| RN-004 | O aumento de volume respeita o teto semanal recuperável do nível (US-239); nunca ultrapassa o limite recuperável. |
| RN-005 | Deload é obrigatório após ~4–6 semanas de progressão contínua ou após ~2 semanas seguidas de fadiga/metas não cumpridas. |
| RN-006 | Mudança de configuração (peso, tempo, equipamento, objetivo, nível) recalibra a progressão para a próxima geração. |
| RN-007 | Rank e atributos enviesam o vetor de progressão (complexidade/RPE/reps/descanso/volume) dentro dos limites de segurança e recuperação. |
| RN-008 | A troca para variante mais difícil/fácil usa o grafo de progressão/regressão (US-039/US-236) e respeita equipamento e limitações. |
| RN-009 | A progressão nunca contraria segurança (US-045) nem recuperação (US-239); ambos são soberanos sobre o desafio. |
| RN-010 | Todo o cálculo é determinístico e reproduzível a partir do estado + histórico; sem IA e sem aleatoriedade não-semeada. |
| RN-011 | A regressão por fadiga/queda de desempenho não penaliza XP/rank; é ajuste de treino, não punição (a penalidade de XP é da US-129/US-132). |

---

## 8. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Mantém o estado de progressão e gera o plano semanal. |
| Usuário final | Recebe o treino evoluído; vê avisos de "treino mais desafiador" ou "semana de deload". |

---

## 9. Fluxo principal

1. Na virada da semana (ou ao salvar mudança de perfil), o sistema monta o snapshot atual: perfil (US-034), nível efetivo (US-150), rank e atributos (EPIC-009) e desempenho da semana (US-062).
2. Detecta mudanças de configuração e recalibra baselines (seção 6.5).
3. Para cada movimento/grupo, aplica a decisão de progressão autorregulada (seção 6.2).
4. Aplica progressão de volume com teto e verifica gatilho de deload (seção 6.3).
5. Aplica viés por rank/atributos (seção 6.6), dentro dos limites de segurança/recuperação.
6. Persiste o `WeeklyProgressionState` e emite o `WeeklyProgressionPlan`.
7. US-240 usa o plano para a seleção do dia; US-153 usa o plano para os parâmetros de execução.

---

## 10. Fluxos alternativos

### 10.1. Primeira semana

Sem histórico de progressão → usa a prescrição inicial (US-153) como patamar base, sem avançar eixo.

### 10.2. Semana de deload

Volume/RPE reduzidos por uma semana; ao fim, retoma a progressão acima do início do ciclo anterior.

### 10.3. Mudança grande de peso/objetivo

Recalibra e sinaliza; pode reancorar o nível efetivo (US-150) e as faixas (US-153).

### 10.4. Baixa aderência / semana incompleta

Se faltam dados suficientes (poucas sessões concluídas), mantém o patamar (não progride às cegas).

### 10.5. Teto recuperável atingido

Não adiciona volume; progride por outro eixo (variante/densidade) ou aciona deload.

---

## 11. Estados esperados

- progressão aplicada (progrediu / manteve / regrediu);
- recalibração por mudança de perfil;
- semana de deload;
- dados insuficientes (patamar mantido);
- teto recuperável atingido.

---

## 12. Impacto no Frontend Flutter

- Aviso na revisão do treino (US-050): "treino mais desafiador esta semana" ou "semana de deload — recuperação".
- Indireto: séries/reps/descanso/RPE exibidos (US-153) já refletem o plano semanal.

---

## 13. Impacto no Backend

- Job semanal `WeeklyProgressionReviewer.review(userId)` na virada da semana + hook em mudança de perfil (US-034).
- Cálculo autorregulado por movimento, volume/deload e viés por rank/atributos.
- Emissão do `WeeklyProgressionPlan` para US-240 e US-153; leitura de rank/atributos (EPIC-009) e desempenho (US-062).

---

## 14. Impacto no Banco de Dados

### `WeeklyProgressionState` (novo, por usuário)

`Id`, `UserId`, `WeekAnchorDate`, `MesocycleWeekIndex`, `ProfileSnapshotHash`, `RankSnapshot`, `AttributesSnapshot`, `PerMovementPointersJson` (por movimento/grupo: `setTarget`, `repTarget`, `restSeconds`, `rpeTarget`, `difficultyTier`, `currentExerciseId`), `ConsecutiveEasyWeeks`, `ConsecutiveHardWeeks`, `DeloadDue`, `UpdatedAt`.

### Artefato de geração (`Quest`/`WorkoutSession`)

`WeeklyProgressionPlanJson` (decisões por movimento, `volumeDelta`, `restDelta`, `rpeTarget`, `difficultyTier`, `deloadWeek`, `recalibratedFromProfileChange`).

---

## 15. Impacto em Gamificação

- Desafio calibrado ao rank/atributos reforça a fantasia de evolução do Hunter: subir de rank e de atributo torna o treino visivelmente mais desafiador, fechando o laço treino → XP → evolução → treino mais forte.

---

## 16. Impacto em Monetização

- Progressão contínua e perceptível é o principal motor de retenção de longo prazo e de conversão do trial para assinatura: o usuário vê que está evoluindo.

---

## 17. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR/EN/ES/FR | Apenas os avisos ("mais desafiador", "deload") usam chaves i18n; o cálculo é interno. |

---

## 18. Contrato de API sugerido

```txt
GET /api/quests/weekly-progression
```

Response conceitual:

```json
{
  "weekAnchorDate": "2026-07-06",
  "mesocycleWeekIndex": 3,
  "deloadWeek": false,
  "recalibratedFromProfileChange": ["training_time"],
  "rank": "D",
  "movements": [
    {
      "muscleGroup": "chest",
      "decision": "progress",
      "axis": "add_set",
      "setTarget": 4,
      "repTarget": [8, 12],
      "restSeconds": 75,
      "rpeTarget": 8,
      "difficultyTier": 3,
      "suggestedExerciseId": "0031"
    },
    {
      "muscleGroup": "quadriceps",
      "decision": "hold",
      "axis": "reps",
      "setTarget": 3,
      "repTarget": [10, 15],
      "restSeconds": 90,
      "rpeTarget": 8,
      "difficultyTier": 2
    }
  ]
}
```

Valores de `decision`: `progress`, `hold`, `regress`, `deload`. Valores de `axis`: `reps`, `add_set`, `harder_variant`, `less_rest`, `higher_rpe`.

---

## 19. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| weekly_progression_reviewed | Quando a reavaliação semanal roda. |
| progression_applied | Quando um movimento progride/mantém/regride. |
| progression_deload_triggered | Quando o deload é acionado. |
| progression_recalibrated_profile_change | Quando uma mudança de perfil recalibra a progressão. |

---

## 20. Critérios de aceite

### CA-001 — Sobrecarga progressiva quando fácil

Dado que o usuário cumpriu as metas com RPE abaixo do alvo por uma semana,

Quando a progressão semanal rodar,

Então o estímulo deve avançar em exatamente um eixo (reps, séries, variante, descanso ou RPE), respeitando o teto de recuperação.

### CA-002 — Segurar/regredir quando difícil

Dado que o usuário não cumpriu as metas ou treinou acima do RPE-alvo,

Quando a progressão rodar,

Então o estímulo deve manter ou reduzir, sem adicionar sobrecarga, e sem penalizar XP/rank.

### CA-003 — Deload obrigatório

Dado ~4–6 semanas de progressão contínua ou ~2 semanas seguidas de fadiga,

Quando a progressão rodar,

Então a semana deve ser marcada como deload (volume/RPE reduzidos).

### CA-004 — Recalibração por mudança de perfil

Dado que o usuário alterou o tempo por treino ou o peso,

Quando a próxima geração ocorrer,

Então o orçamento de volume/baselines devem ser recalibrados a partir do valor atual.

### CA-005 — Viés por rank/atributos

Dado dois usuários iguais exceto pelo atributo Força (um alto, um baixo),

Quando a progressão rodar,

Então o de Força alta deve receber viés para variantes mais desafiadoras/faixas de reps mais baixas, dentro dos limites de segurança.

### CA-006 — Recuperação e segurança soberanas

Dado que a progressão sugeriria mais volume,

Quando o teto recuperável (US-239) ou um bloqueio de segurança (US-045) se aplicar,

Então o limite prevalece sobre o desafio.

---

## 21. Critérios de teste para QA

### Backend

- reavaliação dispara na virada de semana e em mudança de perfil;
- decisão progride/mantém/regride conforme reps × meta e RPE × alvo;
- apenas um eixo de sobrecarga avança por vez;
- volume respeita teto recuperável; deload dispara nos gatilhos corretos;
- mudança de peso/tempo/equipamento/objetivo recalibra baselines;
- rank/atributos enviesam dentro dos limites;
- determinismo: mesmo estado/histórico → mesmo plano;
- segurança e recuperação prevalecem sobre o desafio;
- regressão não penaliza XP/rank.

### E2E

- ao longo de semanas, o treino fica progressivamente mais desafiador e reflete mudanças de perfil e evolução de rank/atributos, com deload periódico.

---

## ✅ Decisão registrada

> A progressão semanal é uma camada determinística e cientificamente embasada (sobrecarga progressiva + dupla progressão + autorregulação por RPE + deload) que relê o **estado atual** do jogador — perfil (que pode ter mudado), rank e atributos — e ajusta exercícios, séries, repetições e descanso para manter o usuário sempre minimamente desafiado e evoluindo. Ela modula a seleção (US-240) e a prescrição (US-153), enviesada pela evolução do personagem (rank/atributos), mas sempre subordinada à segurança (US-045) e aos tetos de recuperação (US-239), sem IA.
