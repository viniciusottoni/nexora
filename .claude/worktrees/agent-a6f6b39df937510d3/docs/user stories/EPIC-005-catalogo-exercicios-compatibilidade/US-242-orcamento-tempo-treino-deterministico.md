---
title: US-242 — Orçamento de tempo do treino determinístico (duração estimada × tempo disponível)
sidebar_position: 242
---

# US-242 — Orçamento de tempo do treino determinístico (duração estimada × tempo disponível)

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-242 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR/EN/ES/FR apenas em avisos ("treino ajustado ao seu tempo", "micro quest") |
| Dependência principal | US-028 (tempo disponível), US-153 (prescrição), US-151 (seleção), US-045 (filtro), US-240 (blueprint), US-241 (progressão) |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **estimar a duração do treino de forma precisa e determinística — somando execução, descanso, aquecimento, transições e finalização — e ajustar a quantidade de exercícios, séries, repetições e descanso para caber no tempo configurado sem trair a intensidade do objetivo**,

para **entregar uma quest que respeita o tempo real do usuário e continua cientificamente coerente com o objetivo dele**.

---

## 3. Contexto

Hoje o cuidado com o tempo existe, mas apenas em nível de princípio: a US-028 captura `availableMinutesPerWorkout`, o filtro (US-045) remove exercícios cujo `timeCost` estoura o orçamento, a seleção (US-151) diz "respeitar o orçamento de tempo" e retorna `estimatedDurationMinutes`, e a prescrição (US-153) reduz séries/descanso em "micro quest". Falta, porém, **o modelo numérico que amarra tudo**: como exatamente a duração é estimada e como o conflito entre o descanso exigido pelo objetivo/intensidade e o tempo curto é resolvido.

Esta US define o **orçamento de tempo determinístico**: uma fórmula de duração estimada por exercício e por quest, um conjunto de constantes configuráveis (`WorkoutTimeModel`) e uma ordem de resolução de conflito **objetivo/intensidade × tempo** baseada na ciência — preservar o descanso mínimo que o objetivo exige e ajustar o volume (exercícios/séries), em vez de espremer o descanso a ponto de descaracterizar o estímulo. Tudo determinístico, sem IA.

O orçamento é consumido pelo filtro (US-045, `timeCost` por exercício), pela seleção (US-151, empacotamento no tempo), pela prescrição (US-153, séries/reps/descanso), pelo blueprint do dia (US-240, orçamento de volume) e pela progressão (US-241, recalibração quando o tempo muda). Ele **não** redefine as faixas de prescrição (US-153) nem os tetos de recuperação (US-239); ele os respeita como limites.

---

## 4. Objetivo

Definir `WorkoutTimeModel` (constantes) e o cálculo de `estimatedDurationSeconds` por exercício (`timeCost`) e por quest, com uma faixa-alvo de utilização do tempo disponível e uma ordem determinística de ajuste (reduzir/aumentar) que respeita a intensidade do objetivo e nunca ultrapassa o tempo configurado.

---

## 5. Escopo

### Entra nesta US

- Fórmula de duração estimada por exercício (`timeCost`) e por quest (execução + descanso + aquecimento + transições + finalização).
- Constantes configuráveis `WorkoutTimeModel` (tempo por repetição, transição entre exercícios, aquecimento por nível/objetivo, finalização, buffer).
- Faixa-alvo de utilização do tempo disponível (limite rígido superior + utilização mínima).
- Ordem determinística de resolução do conflito objetivo/intensidade × tempo (piso de descanso por objetivo → reduzir exercícios → reduzir séries → densidade para condicionamento → micro quest).
- Formato micro quest e formato densidade (superset/circuito) para objetivos de condicionamento/perda de peso.
- Recalibração quando o tempo disponível muda (US-034/US-241).

### Fora desta US

- Captura do tempo disponível (US-028).
- Faixas de séries/reps/descanso/RPE por nível/objetivo (US-153) — aqui são insumo/limite.
- Seleção e pontuação de exercícios (US-151) — consome o orçamento.
- Filtro eliminatório (US-045) — consome o `timeCost`.
- Tetos de recuperação/volume (US-239) e blueprint (US-240) — respeitados como limite.
- Cronômetro de execução em tempo real (EPIC-008).

---

## 6. Modelo de tempo determinístico

### 6.1. Constantes (`WorkoutTimeModel`, configuráveis)

| Constante | Valor base | Observação |
|---|---|---|
| `secondsPerRep` | ~3s | Tempo médio por repetição controlada; pode subir (~4–5s) para tempo/ênfase excêntrica. |
| `transitionSeconds` | ~45s | Preparo/troca entre exercícios (equipamento, posição). |
| `warmupSeconds` | Sedentário/Iniciante ~300s; Intermediário ~360–480s; Avançado ~480–600s | Reduzido em micro quest. |
| `cooldownSeconds` | ~120s | Finalização/mobilidade; pode ser omitido em micro quest. |
| `restSeconds` | de US-153 | Por nível e objetivo. |
| `microQuestThresholdMinutes` | ~15 | Abaixo disso, formato micro quest. |
| `minUtilization` | ~0.85 | Utilização mínima do tempo disponível (evita treino curto demais). |

### 6.2. Duração por exercício (`timeCost`)

```txt
execPorSerie(exercício) =
    reps × secondsPerRep            // exercício por repetição
    | plannedDurationSeconds        // exercício por tempo (isometria/cardio)

timeCost(exercício) =
    transitionSeconds
  + sets × execPorSerie
  + (sets − 1) × restSeconds        // descanso apenas ENTRE séries, não após a última
```

### 6.3. Duração da quest

```txt
estimatedDurationSeconds =
    warmupSeconds
  + Σ timeCost(exercício)           // para todos os exercícios selecionados
  + cooldownSeconds
```

### 6.4. Orçamento e faixa-alvo

```txt
availableSeconds = availableMinutesPerWorkout × 60
limiteRígido     = availableSeconds                      // nunca ultrapassar
alvoMínimo       = minUtilization × availableSeconds      // não ficar curto demais
```

A quest é válida quando `alvoMínimo ≤ estimatedDurationSeconds ≤ limiteRígido`.

### 6.5. Resolução do conflito objetivo/intensidade × tempo

Quando `estimatedDurationSeconds > limiteRígido`, ajustar nesta ordem (determinística), sem violar segurança (US-045) nem recuperação (US-239):

1. **Preservar o piso de descanso do objetivo** — nunca cortar o descanso abaixo do mínimo fisiológico do objetivo/intensidade (ex.: força/massa pesada mantêm descanso longo; o descanso não é a variável de corte primária).
2. **Reduzir a quantidade de exercícios** — remover primeiro os de menor prioridade (score US-151), até o mínimo coerente com o alvo do dia (US-240).
3. **Reduzir séries** — em direção ao mínimo do nível (US-153).
4. **Densidade (só condicionamento/perda de peso)** — converter para superset/circuito com pares de músculos não concorrentes, reduzindo o descanso efetivo (a densidade é o próprio estímulo nesses objetivos).
5. **Micro quest** — se ainda não couber no mínimo, gerar micro quest com aviso de limitação (US-153), reduzindo aquecimento/finalização, sempre sem ultrapassar o tempo.

Quando `estimatedDurationSeconds < alvoMínimo`, aumentar nesta ordem: **+séries** (até o teto recuperável US-239/progressão US-241) → **+1 exercício** coerente com o alvo do dia — nunca ultrapassando `limiteRígido`.

---

## 7. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A duração estimada deve somar execução, descanso entre séries, aquecimento, transições e finalização (RN-EPIC-006-004). |
| RN-002 | A quest nunca pode exceder o tempo disponível configurado (limite rígido). |
| RN-003 | A quest deve utilizar pelo menos a fração mínima do tempo disponível (`minUtilization`), salvo micro quest ou falta de elegíveis. |
| RN-004 | O descanso não é a variável de corte primária; preserva-se o piso de descanso do objetivo/intensidade e ajusta-se antes a quantidade de exercícios e séries. |
| RN-005 | Densidade (superset/circuito) só é aplicada para objetivos de condicionamento/perda de peso, respeitando compatibilidade e segurança. |
| RN-006 | Exercícios por tempo (isometria/cardio) usam `plannedDurationSeconds`; por repetição usam `reps × secondsPerRep`. |
| RN-007 | `secondsPerRep`, `transitionSeconds`, `warmupSeconds`, `cooldownSeconds` e `minUtilization` são configuráveis e versionados (`WorkoutTimeModel`). |
| RN-008 | Mudança do tempo disponível (US-034) recalibra o orçamento na próxima geração (via US-241). |
| RN-009 | O orçamento respeita, como limites, as faixas de prescrição (US-153), os tetos de recuperação/volume (US-239) e o alvo do dia (US-240); não os redefine. |
| RN-010 | O cálculo é 100% determinístico e reproduzível; sem IA e sem aleatoriedade não-semeada. |
| RN-011 | O ajuste por tempo nunca contraria segurança (US-045); um exercício bloqueado não entra para "preencher tempo". |

---

## 8. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Calcula o orçamento e ajusta a quest ao tempo. |
| Usuário final | Recebe a quest dentro do seu tempo; vê aviso de micro quest quando aplicável. |

---

## 9. Fluxo principal

1. Sistema lê `availableMinutesPerWorkout` (US-028) e o `WorkoutTimeModel`.
2. Calcula `timeCost` de cada candidato (US-045 usa para eliminar quem estoura sozinho).
3. Durante a seleção (US-151), soma `estimatedDurationSeconds` e mantém dentro da faixa-alvo.
4. Se estourar, aplica a ordem de resolução (seção 6.5); se sobrar tempo, aumenta volume dentro dos tetos.
5. Consolida a prescrição final (US-153) coerente com o tempo.
6. Registra `estimatedDurationSeconds` e os ajustes para auditoria (US-049).

---

## 10. Fluxos alternativos

### 10.1. Objetivo de força/massa + tempo curto

Preserva o descanso longo do objetivo, reduz exercícios/séries; se não couber, micro quest com aviso (não espreme o descanso).

### 10.2. Objetivo de condicionamento + tempo curto

Aplica densidade (superset/circuito) para caber mais trabalho no tempo, reduzindo descanso efetivo de forma intencional.

### 10.3. Tempo sobrando

Aumenta séries (até teto US-239/US-241) e, se necessário, adiciona um exercício coerente com o alvo do dia.

### 10.4. Tempo muito curto (≤ microQuestThreshold)

Formato micro quest: aquecimento reduzido, poucos exercícios, foco no essencial.

### 10.5. Mudança de tempo pelo usuário

Recalibra o orçamento na próxima geração (US-241).

---

## 11. Estados esperados

- dentro da faixa-alvo;
- ajustado por corte (excesso);
- ajustado por acréscimo (sobra);
- micro quest;
- formato densidade (condicionamento).

---

## 12. Impacto no Frontend Flutter

- A revisão do treino (US-050) exibe a duração estimada e, quando aplicável, "micro quest — ajustada ao seu tempo".
- Indireto: séries/reps/descanso já refletem o orçamento.

---

## 13. Impacto no Backend

- `WorkoutTimeModel` (config versionada).
- `TimeBudgetCalculator.estimate(exercise|quest)` → segundos.
- Integração com US-045 (`timeCost`), US-151 (empacotamento) e US-153 (prescrição final).
- Rotina de resolução de conflito (seção 6.5).

---

## 14. Impacto no Banco de Dados

### `WorkoutTimeModel` (novo, config)

`Id`, `Version`, `SecondsPerRep`, `TransitionSeconds`, `WarmupSecondsByLevel`, `CooldownSeconds`, `MicroQuestThresholdMinutes`, `MinUtilization`, `IsActive`, `CreatedAt`.

### `ExerciseCatalog` / `QuestExercise`

`EstimatedTimeCostSeconds` por exercício da quest (derivado).

### Artefato de geração (`Quest`/`WorkoutSession`)

`EstimatedDurationSeconds`, `TimeBudgetSeconds`, `TimeAdjustmentApplied` (`none`|`reduced_exercises`|`reduced_sets`|`density`|`micro_quest`|`added_volume`), `WorkoutTimeModelVersion`.

---

## 15. Impacto em Gamificação

- Duração coerente com o tempo real reduz abandono e sustenta a streak (US-069); micro quest ainda conta como quest concluída.

---

## 16. Impacto em Monetização

- Entregar treino que "cabe na rotina" é argumento direto de retenção e de conversão do trial.

---

## 17. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR/EN/ES/FR | Apenas avisos ("micro quest", "ajustado ao seu tempo") usam chaves i18n; o cálculo é interno. |

---

## 18. Contrato de API sugerido

```txt
POST /api/quests/time-budget
```

Response conceitual:

```json
{
  "availableMinutes": 30,
  "estimatedDurationSeconds": 1710,
  "timeBudgetSeconds": 1800,
  "utilization": 0.95,
  "timeAdjustmentApplied": "reduced_sets",
  "workoutTimeModelVersion": "v1",
  "exercises": [
    { "exerciseId": "0031", "sets": 3, "reps": 12, "restSeconds": 75, "estimatedTimeCostSeconds": 420 }
  ]
}
```

Valores de `timeAdjustmentApplied`: `none`, `reduced_exercises`, `reduced_sets`, `density`, `micro_quest`, `added_volume`.

---

## 19. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| time_budget_computed | Quando a duração estimada é calculada para a quest. |
| time_budget_adjusted | Quando um ajuste (corte/acréscimo/densidade) é aplicado. |
| micro_quest_generated | Quando o formato micro quest é acionado por tempo curto. |

---

## 20. Critérios de aceite

### CA-001 — Duração estimada completa

Dado uma quest selecionada,

Quando a duração for estimada,

Então deve somar aquecimento, execução, descanso entre séries, transições e finalização, e não exceder o tempo disponível.

### CA-002 — Objetivo de força + tempo curto preserva descanso

Dado ganhar força com tempo curto,

Quando o orçamento ajustar,

Então o descanso longo do objetivo é preservado e o corte recai sobre quantidade de exercícios/séries, chegando a micro quest se necessário.

### CA-003 — Condicionamento usa densidade

Dado objetivo de condicionamento com tempo curto,

Quando o orçamento ajustar,

Então pode aplicar superset/circuito reduzindo descanso efetivo, respeitando segurança.

### CA-004 — Tempo sobrando aumenta volume dentro dos tetos

Dado que a duração ficou abaixo da utilização mínima,

Quando o orçamento ajustar,

Então adiciona séries/exercícios até a faixa-alvo, sem ultrapassar o teto recuperável (US-239) nem o tempo disponível.

### CA-005 — Determinismo

Dado o mesmo perfil, alvo do dia e catálogo,

Quando o orçamento rodar duas vezes,

Então a duração estimada e os ajustes devem ser idênticos.

---

## 21. Critérios de teste para QA

### Backend

- `timeCost` por exercício soma transição + séries×execução + (séries−1)×descanso;
- duração da quest inclui aquecimento e finalização;
- nunca ultrapassa o tempo disponível; respeita utilização mínima;
- conflito de objetivo preserva piso de descanso e corta volume primeiro;
- densidade só para condicionamento/perda de peso;
- exercícios por tempo usam `plannedDurationSeconds`;
- mudança de tempo recalibra;
- determinismo garantido; segurança e recuperação soberanas.

### E2E

- quests de 10, 30 e 50 min ficam dentro do tempo, coerentes com o objetivo, com micro quest quando aplicável.

---

## ✅ Decisão registrada

> O orçamento de tempo é um modelo determinístico e preciso: estima a duração da quest somando execução, descanso entre séries, aquecimento, transições e finalização (`WorkoutTimeModel`), mantém a quest dentro de uma faixa-alvo do tempo disponível (nunca excedendo o limite) e resolve o conflito objetivo/intensidade × tempo preservando o piso de descanso do objetivo e ajustando primeiro a quantidade de exercícios e séries — usando densidade só para condicionamento e micro quest como último recurso. Ele alimenta o filtro (US-045), a seleção (US-151), a prescrição (US-153), o blueprint (US-240) e a progressão (US-241), sempre subordinado à segurança e à recuperação.
