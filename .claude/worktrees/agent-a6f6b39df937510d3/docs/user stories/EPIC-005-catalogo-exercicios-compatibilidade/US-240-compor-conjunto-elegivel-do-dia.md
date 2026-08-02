---
title: US-240 — Compor o conjunto elegível do dia (split + rotação + recuperação) para a geração da quest
sidebar_position: 240
---

# US-240 — Compor o conjunto elegível do dia (split + rotação + recuperação) para a geração da quest

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-240 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (perfil interno) |
| Dependência principal | US-237 (split), US-238 (dia resolvido), US-239 (recuperação), US-035 (catálogo aprovado), US-045 (filtro), US-151 (pontuação) |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **combinar, de forma determinística, o programa configurado, o dia resolvido pela rotação e o plano de recuperação em um "perfil-alvo do dia"**,

para **entregar à geração de quest (EPIC-006) exatamente qual é o alvo muscular, o orçamento de volume por grupo e as restrições do dia — a regra final que personaliza a escolha de exercícios de cada dia**.

---

## 3. Contexto

As três USes anteriores respondem partes do problema: **o que cada dia treina** (US-237), **qual dia é hoje** (US-238) e **como respeitar a recuperação** (US-239). Esta US é a **orquestração**: junta tudo em um único artefato — o `DailyWorkoutBlueprint` (perfil-alvo do dia) — que a geração consome.

O blueprint não escolhe os exercícios finais; ele **define o alvo e as restrições** e delega:

- o **filtro eliminatório de segurança** (dor/limitação/equipamento) à US-045;
- a **pontuação e seleção** dentro do orçamento de tempo à US-151/US-152;
- a **prescrição** de séries/reps/descanso/RPE à US-153.

O que esta US garante é que a seleção só considere exercícios **coerentes com o dia** (grupos e padrões do split), **respeitando a recuperação** (caps/RPE/anti-repetição) e **sem depender de IA**. Também define o comportamento quando não há elegíveis suficientes (fallback, US-046) e o dia de descanso.

Entradas de perfil do usuário (US-034/perfil): objetivo, nível efetivo (US-150), tempo por treino, equipamento, limitações e dores — usadas pelas USes delegadas, mas referenciadas aqui para compor o alvo (ex.: equipamento restringe padrões viáveis).

---

## 4. Objetivo

Produzir o `DailyWorkoutBlueprint`: alvo muscular ponderado do dia, padrões de movimento-alvo, orçamento de volume por grupo (aplicando `volumeCapFactor` da US-239), restrições (famílias a evitar, RPE máximo por grupo), ênfase (para full body) e política de fallback — pronto para o pipeline de geração.

---

## 5. Escopo

### Entra nesta US

- Resolução do programa ativo e do dia (consumindo US-238) e do alvo do dia (US-237).
- Aplicação do `RecoveryPlan` (US-239) sobre o alvo: cap de volume por grupo, RPE máximo, `avoidMovementFamilies`, ênfase de full body.
- Cálculo do **orçamento de volume por grupo** do dia (nº-alvo de exercícios/séries por grupo) a partir do nível efetivo e do tempo disponível.
- Montagem do conjunto elegível de candidatos do catálogo aprovado (US-035) coerentes com grupos/padrões do dia — **antes** do filtro de segurança (US-045).
- Definição de dia de descanso / fallback (US-046) quando não houver elegíveis suficientes.
- Registro do blueprint para auditoria (US-049).

### Fora desta US

- Definição do split (US-237), rotação (US-238) e cálculo de recuperação (US-239).
- Filtro eliminatório de segurança (US-045) — o blueprint entra como insumo dele.
- Pontuação/seleção final e orçamento de tempo (US-151/US-152).
- Prescrição numérica (US-153).
- Geração da dungeon/side quest (US-127/US-128).

---

## 6. Pipeline determinístico do dia

```txt
1. programKey ← programa ativo do usuário (US-231/232); se ausente, default por nível/rank (sedentário/iniciante → full_body).
2. resolvedDay ← US-238.resolve(programKey, histórico)         // letra do dia
3. dayTarget ← US-237.getDayTarget(programKey, resolvedDay)     // grupos + padrões
4. recoveryPlan ← US-239.plan(userId, resolvedDay)             // caps, RPE, anti-repetição, ênfase FB
5. Para full_body: aplicar ênfase (recoveryPlan.fullBodyEmphasis) ponderando os grupos do dia.
6. volumeBudget ← orçamentoPorGrupo(nívelEfetivo, tempoDisponível) × volumeCapFactor(grupo)
7. candidatos ← catálogoAprovado(US-035) filtrado por:
     grupo ∈ dayTarget.groups  E  padrão ∈ dayTarget.patterns
     E  movementFamily ∉ recoveryPlan.avoidMovementFamilies(grupo)
8. Se |candidatos| < mínimo por grupo → aplicar variação/relaxar anti-repetição; se ainda insuficiente → fallback (US-046).
9. Emitir DailyWorkoutBlueprint (alvo ponderado, volumeBudget, restrições, rpeMax por grupo, ênfase).
10. Entregar ao pipeline de geração (US-045 → US-151 → US-152 → US-153).
```

O blueprint é **coerência de alvo + restrições**, não a lista final: a segurança (US-045) ainda pode remover candidatos, e o tempo (US-151) ainda limita a quantidade.

---

## 7. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A composição do dia deve derivar exclusivamente de programa + dia resolvido + plano de recuperação, de forma determinística e sem IA. |
| RN-002 | O conjunto elegível só inclui exercícios **aprovados** (US-035) cujos grupo e padrão pertencem ao alvo do dia (US-237). |
| RN-003 | O orçamento de volume por grupo aplica o `volumeCapFactor` da US-239; nenhum grupo excede seu teto por sessão/semana. |
| RN-004 | Exercícios cuja `movementFamily` está em `avoidMovementFamilies` (US-239) são preteridos; só entram se não houver variante equivalente para cumprir o volume mínimo. |
| RN-005 | O blueprint não substitui o filtro de segurança (US-045); ele é insumo, e a segurança permanece soberana. |
| RN-006 | Sem programa selecionado, aplica-se o default por nível/rank (sedentário/iniciante → `full_body`), respeitando restrições de rank (US-231). |
| RN-007 | Se não houver elegíveis suficientes para o alvo do dia, aciona-se o fallback (US-046) em vez de gerar um treino incoerente. |
| RN-008 | O blueprint é versionado com `splitMapVersion` (US-237) e registra as decisões para auditoria (US-049). |
| RN-009 | Para `full_body`, a ênfase do dia (US-239) apenas pondera os grupos; todos os padrões principais continuam representados na sessão. |
| RN-010 | O blueprint respeita equipamento/ambiente do perfil ao restringir padrões viáveis (ex.: sem barra → preterir padrões que exijam barra). |

---

## 8. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Compõe e registra o blueprint do dia. |
| Usuário final | Recebe a quest resultante e vê o alvo/rótulo do dia. |

---

## 9. Fluxo principal

1. Sistema resolve programa e dia (US-238) e obtém o alvo (US-237).
2. Aplica o plano de recuperação (US-239).
3. Calcula o orçamento de volume por grupo (nível + tempo).
4. Monta o conjunto elegível coerente com o alvo, a partir do catálogo aprovado.
5. Verifica suficiência; se faltar, relaxa anti-repetição ou aciona fallback.
6. Emite o `DailyWorkoutBlueprint` e o entrega à geração.

---

## 10. Fluxos alternativos

### 10.1. Sem programa selecionado

Default por nível/rank (RN-006), tipicamente `full_body` para sedentário/iniciante.

### 10.2. Elegíveis insuficientes

Relaxa anti-repetição; persistindo a falta, aciona fallback (US-046).

### 10.3. Equipamento limitado

Padrões inviáveis por falta de equipamento são preteridos; o alvo do grupo é mantido com padrões viáveis.

### 10.4. Dia de descanso previsto

Se a política do programa/perfil indicar descanso, o blueprint marca `restDay = true` e a geração oferece uma sessão leve/mobilidade em vez de estímulo alto.

---

## 11. Estados esperados

- blueprint composto;
- blueprint com fallback acionado;
- blueprint em modo descanso;
- default de programa aplicado.

---

## 12. Impacto no Frontend Flutter

- A tela de revisão do treino (US-050) exibe o dia/alvo (ex.: "Dia C — Pernas") e, quando aplicável, o aviso de recuperação/descanso.

---

## 13. Impacto no Backend

- Orquestrador `DailyWorkoutBlueprintBuilder.build(userId)` compondo US-237/238/239 + catálogo (US-035).
- Cálculo do orçamento de volume por grupo (nível + tempo).
- Montagem do conjunto elegível e verificação de suficiência.
- Emissão do blueprint para o pipeline (US-045 → US-151 → US-153) e registro de auditoria (US-049).

---

## 14. Impacto no Banco de Dados

### Artefato de geração (`Quest`/`WorkoutSession`)

`DailyWorkoutBlueprintJson` (programKey, resolvedDayKey, grupos-alvo ponderados, padrões-alvo, `volumeBudgetPorGrupo`, `rpeMaxPorGrupo`, `avoidMovementFamilies`, `fullBodyEmphasis`, `restDay`, `fallbackUsed`, `splitMapVersion`).

Consome `ExerciseCatalog` (US-035) e `ExerciseRelationship`/taxonomia (US-236) para variação.

---

## 15. Impacto em Gamificação

- Alvo coerente por dia garante distribuição de XP de atributos (US-147) alinhada ao foco do dia e evita "treinos aleatórios".

---

## 16. Impacto em Monetização

- Personalização real e explicável por dia é o diferencial central do produto frente aos concorrentes, sustentando a conversão pós-trial.

---

## 17. Impacto em Internacionalização

- Perfil interno; rótulos do dia/avisos usam chaves i18n (US-237/US-239).

---

## 18. Contrato de API sugerido

```txt
POST /api/quests/compose-day
```

Response conceitual:

```json
{
  "programKey": "abc",
  "resolvedDayKey": "C",
  "restDay": false,
  "fallbackUsed": false,
  "splitMapVersion": "v1",
  "targetGroups": [
    { "muscleGroup": "quadriceps", "weight": 1.0, "volumeBudgetSets": 3, "rpeMax": 8, "avoidMovementFamilies": ["back-squat"] },
    { "muscleGroup": "hamstrings", "weight": 0.8, "volumeBudgetSets": 3, "rpeMax": 9 },
    { "muscleGroup": "glutes", "weight": 0.8, "volumeBudgetSets": 2, "rpeMax": 9 },
    { "muscleGroup": "calves", "weight": 0.5, "volumeBudgetSets": 2, "rpeMax": 9 }
  ],
  "targetPatterns": ["squat", "hinge", "lunge", "core_flexion"],
  "eligibleExerciseCount": 42
}
```

---

## 19. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| daily_blueprint_composed | Quando o blueprint do dia é emitido. |
| daily_blueprint_fallback | Quando o fallback é acionado por falta de elegíveis. |
| daily_blueprint_rest_day | Quando o dia é marcado como descanso. |

---

## 20. Critérios de aceite

### CA-001 — Coerência de alvo

Dado um usuário no dia C (Pernas) do ABC,

Quando o blueprint for composto,

Então o conjunto elegível deve conter apenas exercícios aprovados de pernas/core com padrões `squat`/`hinge`/`lunge`/`core_*`.

### CA-002 — Recuperação aplicada

Dado que pernas está em recuperação,

Quando o blueprint for composto,

Então o `volumeBudgetSets` de pernas deve refletir o `volumeCapFactor` da US-239 e `rpeMax` deve estar reduzido.

### CA-003 — Fallback por insuficiência

Dado que não há elegíveis suficientes para o alvo do dia,

Quando o blueprint for composto,

Então o fallback (US-046) deve ser acionado em vez de gerar treino incoerente.

### CA-004 — Default sem programa

Dado um usuário sem programa selecionado,

Quando o blueprint for composto,

Então deve aplicar `full_body` como default coerente com o nível/rank.

### CA-005 — Segurança soberana

Dado um exercício coerente com o dia mas bloqueado por dor/limitação,

Quando o pipeline rodar,

Então o filtro de segurança (US-045) deve removê-lo mesmo constando no elegível do blueprint.

---

## 21. Critérios de teste para QA

### Backend

- blueprint só inclui aprovados coerentes com grupo/padrão do dia;
- caps de volume/RPE da US-239 aplicados por grupo;
- anti-repetição respeitada, relaxada só quando necessário;
- fallback acionado por insuficiência;
- default de programa aplicado sem seleção;
- equipamento limita padrões viáveis;
- determinismo: mesma entrada → mesmo blueprint.

### E2E

- a quest gerada reflete o dia/alvo correto e respeita segurança e recuperação de ponta a ponta.

---

## ✅ Decisão registrada

> O `DailyWorkoutBlueprint` é a regra final e determinística que personaliza a escolha de exercícios de cada dia: combina o programa configurado, o dia resolvido pela rotação (US-238), o alvo do split (US-237) e o plano de recuperação (US-239) em um alvo ponderado com orçamento de volume e restrições. Ele alimenta o pipeline da EPIC-006 (US-045 → US-151 → US-153), sem IA, mantendo a segurança soberana e acionando fallback (US-046) quando não há elegíveis coerentes suficientes.
