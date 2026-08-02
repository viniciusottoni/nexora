---
title: US-239 — Aplicar regras científicas de recuperação muscular e anti-sobrecarga
sidebar_position: 239
---

# US-239 — Aplicar regras científicas de recuperação muscular e anti-sobrecarga

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-239 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (cálculo interno) |
| Dependência principal | US-237 (split map), US-238 (dia resolvido), US-036 (grupo muscular), US-236 (movementFamily/relações), US-150 (nível efetivo), US-062 (treino concluído) |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **aplicar regras baseadas na ciência do treinamento (janela de recuperação por grupo muscular, frequência semanal, volume recuperável e variação de estímulo) sobre o alvo do dia**,

para **evitar sobrecarga muscular e overtraining, garantindo que cada grupo tenha recuperado antes de ser exigido de novo — inclusive no Full Body, que treina o corpo inteiro todos os dias**.

---

## 3. Contexto

O AWAKEN gera uma quest **todos os dias** (US-153). Treinar diariamente exige uma camada explícita de recuperação para não sobrecarregar os mesmos grupos musculares. O split (US-237) e a rotação (US-238) já distribuem o estímulo entre os dias; esta US adiciona a **inteligência fisiológica** que:

1. rastreia o **estado de recuperação de cada grupo muscular** do usuário a partir dos treinos concluídos;
2. impede/limita treinar de novo um grupo que ainda não recuperou;
3. mira a **frequência e o volume semanais** cientificamente adequados por nível;
4. para o **Full Body** (corpo inteiro diário), força variação de padrão/exercício, alternância de ênfase entre sessões e redução de volume/intensidade por grupo, para manter cada músculo dentro da sua janela de recuperação.

Base científica adotada (princípios estáveis e amplamente aceitos, não dependentes de data):

- Síntese proteica muscular fica elevada por ~24–48h após o treino de um grupo; grupos grandes ou sessões de alto volume/ênfase excêntrica pedem até ~72h.
- Treinar cada músculo ~2x/semana tende a gerar mais hipertrofia do que 1x/semana com volume equalizado.
- Existe uma faixa de volume semanal recuperável por músculo que cresce com o nível (menos para sedentário/iniciante, mais para avançado); ultrapassá-la de forma recorrente aproxima do overtraining.

Tudo é **determinístico** (tabelas + histórico), sem IA. Esta US **não** escolhe exercícios (US-240/US-151) nem prescreve séries/reps (US-153); ela produz **restrições e moduladores** (cap de volume, ajuste de RPE, exclusão de repetição de estímulo, ênfase do dia) que essas USes consomem.

---

## 4. Objetivo

Manter `MuscleRecoveryState` por usuário e grupo muscular e produzir, para o dia resolvido (US-238), um `RecoveryPlan` com: status de recuperação por grupo, fator de cap de volume por grupo, ajuste de RPE, famílias de movimento a evitar (anti-repetição) e — para `full_body` — a ênfase da sessão do dia.

---

## 5. Escopo

### Entra nesta US

- Entidade `MuscleRecoveryState` (por usuário × grupo muscular) atualizada a cada treino concluído (US-062).
- Janela de recuperação por grupo e por intensidade da última sessão (48h/72h).
- Alvos de frequência semanal (~2x) e faixas de volume semanal recuperável por nível efetivo (US-150).
- Cálculo do status de recuperação: `recuperado`, `em_recuperacao`, `fadigado`.
- Modulação anti-sobrecarga: cap de volume, redução de RPE, exclusão de `movementFamily` recém-usada (via US-236).
- Regras específicas de `full_body`: alternância de ênfase em microciclo, variação de padrão/exercício, redução de volume por grupo por sessão.
- `RecoveryPlan` como saída consumível pela US-240.

### Fora desta US

- Definição do alvo muscular do dia (US-237) e resolução do dia (US-238).
- Seleção final e pontuação de exercícios (US-240/US-151).
- Prescrição numérica de séries/reps/descanso/RPE (US-153) — aqui só entram os **limites**.
- Filtro eliminatório de segurança por dor/limitação (US-045) — recuperação é independente de contraindicação.

---

## 6. Parâmetros científicos (tabelas determinísticas)

### 6.1. Janela de recuperação por grupo muscular

| Situação da última sessão do grupo | Janela mínima até novo estímulo alto |
|---|---|
| Estímulo leve (volume baixo, sem falha) | ~24h |
| Estímulo moderado (padrão) | ~48h |
| Estímulo alto (alto volume / grupo grande: pernas, costas, peito / muita ênfase excêntrica) | ~72h |

`horasDesdeUltimo(grupo) < janela(grupo)` ⇒ grupo `em_recuperacao` (ou `fadigado` se muito abaixo da janela e com volume semanal já alto).

### 6.2. Frequência e volume semanal recuperável por nível efetivo

| Nível efetivo (US-150) | Frequência-alvo por músculo | Séries semanais recuperáveis por músculo (faixa) | Séries por sessão por músculo (teto) |
|---|---|---|---|
| Sedentário | 2x (corpo inteiro) | 6–10 | 1–2 |
| Iniciante | 2x | 8–12 | 2–3 |
| Intermediário | 2x | 12–16 | 3–4 |
| Avançado | 1–2x | 14–20 | 3–5 |

Ao atingir o teto semanal do músculo, novos estímulos altos naquele músculo na mesma semana são reduzidos (cap) — não bloqueados quando o dia é o primário do split, mas rebaixados.

### 6.3. Fatores de modulação (saída)

- `volumeCapFactor(grupo) ∈ [0.25, 1.0]`: 1.0 se `recuperado`; ~0.5 se `em_recuperacao`; ~0.25 se `fadigado`.
- `rpeCapDelta(grupo)`: 0 se `recuperado`; −1 a −2 pontos de RPE se `em_recuperacao`/`fadigado`.
- `avoidMovementFamilies(grupo)`: `movementFamily` (US-236) usadas na última sessão do grupo, para forçar variação.

---

## 7. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Cada grupo muscular tem uma janela mínima de recuperação (24/48/72h) conforme a intensidade da última sessão; treinar de novo antes disso aciona modulação anti-sobrecarga. |
| RN-002 | O sistema mira ~2x/semana de frequência por músculo (1–2x no avançado), sem exceder a faixa de volume semanal recuperável do nível. |
| RN-003 | Grupo `em_recuperacao`/`fadigado` recebe `volumeCapFactor` e `rpeCapDelta` reduzidos; nunca é exigido em estímulo alto. |
| RN-004 | Quando o dia (US-237/US-238) é o **primário** de um grupo ainda em recuperação (ex.: ciclo curto), o grupo não é bloqueado, mas tem volume/intensidade rebaixados e priorização de variação. |
| RN-005 | Grupos **secundários** que se sobrepõem a sessões recentes têm o volume rebaixado antes dos primários. |
| RN-006 | Para `full_body`, cada sessão toca o corpo inteiro com **volume por grupo reduzido** e **ênfase alternada** em microciclo (ex.: empurrar → puxar → pernas), mantendo cada grupo dentro da janela de recuperação. |
| RN-007 | Para `full_body`, a mesma `movementFamily` (US-236) de um grupo não deve se repetir em sessões consecutivas quando houver variante equivalente aprovada disponível. |
| RN-008 | O `MuscleRecoveryState` é atualizado apenas por treinos concluídos (US-062); quests não concluídas não geram fadiga. |
| RN-009 | Todo o cálculo é determinístico a partir de tabelas + histórico; sem IA e sem aleatoriedade não-semeada. |
| RN-010 | Recuperação é independente de segurança: as regras desta US nunca liberam um exercício bloqueado por dor/limitação (US-045). |

---

## 8. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Mantém o estado de recuperação e gera o `RecoveryPlan`. |
| Usuário final | Recebe treino já modulado; pode ver aviso de "grupo em recuperação" quando aplicável. |

---

## 9. Fluxo principal

1. Ao concluir um treino (US-062), o sistema atualiza `MuscleRecoveryState` dos grupos trabalhados (data, intensidade, séries, `movementFamily`).
2. Na geração do dia, recebe o dia resolvido (US-238) e o alvo do dia (US-237).
3. Calcula o status de recuperação de cada grupo-alvo (seção 6).
4. Deriva `volumeCapFactor`, `rpeCapDelta` e `avoidMovementFamilies` por grupo.
5. Se `full_body`, determina a ênfase da sessão (microciclo) e reforça a variação de padrão/exercício.
6. Entrega o `RecoveryPlan` à US-240.

---

## 10. Fluxos alternativos

### 10.1. Grupo primário do dia ainda em recuperação

Ciclo curto (ex.: ABC feito diariamente com pernas exigidas cedo): o grupo não é bloqueado (é o primário do split), mas recebe cap de volume e −RPE, priorizando variação (RN-004).

### 10.2. Volume semanal no teto

Se o músculo já atingiu o teto semanal do nível, novos estímulos altos são rebaixados até a virada de semana.

### 10.3. Full body em dias consecutivos

Ênfase alterna no microciclo e a `movementFamily` não repete, distribuindo o estresse (RN-006, RN-007).

### 10.4. Retorno após folga longa

Grupos totalmente recuperados voltam a `recuperado` (sem penalidade); o volume semanal reinicia na virada de semana.

---

## 11. Estados esperados

- por grupo: `recuperado`, `em_recuperacao`, `fadigado`;
- `RecoveryPlan` calculado;
- ênfase de full body definida;
- volume semanal no teto (rebaixamento ativo).

---

## 12. Impacto no Frontend Flutter

- Aviso opcional "grupo em recuperação — volume reduzido hoje" na revisão do treino (US-050).
- Indireto: séries/RPE exibidos já refletem os limites (via US-153).

---

## 13. Impacto no Backend

- Atualização de `MuscleRecoveryState` no fechamento do treino (US-062).
- Serviço `RecoveryPlanner.plan(userId, resolvedDay)` → `RecoveryPlan`.
- Cálculo de status por grupo, cap de volume, RPE e anti-repetição (usando relações/`movementFamily` de US-236).
- Seletor de ênfase de full body por microciclo.

---

## 14. Impacto no Banco de Dados

### `MuscleRecoveryState` (novo, por usuário × grupo)

`Id`, `UserId`, `MuscleGroup` (enum US-036), `LastTrainedAt`, `LastIntensity` (`light`|`moderate`|`heavy`), `LastMovementFamilies` (lista), `WeeklySetsAccumulated`, `WeekAnchorDate`, `UpdatedAt`.

### Artefato de geração (`Quest`/`WorkoutSession`)

`RecoveryPlanJson` (status por grupo, `volumeCapFactor`, `rpeCapDelta`, `avoidMovementFamilies`, `fullBodyEmphasis`).

---

## 15. Impacto em Gamificação

- Evitar overtraining preserva a `streak` (US-069) e a consistência; o aviso de recuperação reforça a narrativa de "Hunter que treina de forma inteligente".

---

## 16. Impacto em Monetização

- Indireto: treino que respeita a recuperação diferencia o AWAKEN dos concorrentes que sobrecarregam, aumentando retenção e conversão pós-trial.

---

## 17. Impacto em Internacionalização

- Cálculo interno; apenas o aviso de "grupo em recuperação" usa chave i18n.

---

## 18. Contrato de API sugerido

```txt
GET /api/quests/recovery-plan
```

Response conceitual:

```json
{
  "programKey": "abc",
  "resolvedDayKey": "C",
  "fullBodyEmphasis": null,
  "groups": [
    { "muscleGroup": "quadriceps", "status": "recovering", "volumeCapFactor": 0.5, "rpeCapDelta": -1, "avoidMovementFamilies": ["back-squat"] },
    { "muscleGroup": "hamstrings", "status": "recovered", "volumeCapFactor": 1.0, "rpeCapDelta": 0, "avoidMovementFamilies": [] }
  ]
}
```

Para `full_body`, `fullBodyEmphasis` assume `push` | `pull` | `legs` conforme o microciclo.

---

## 19. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| muscle_recovery_state_updated | Ao fechar um treino concluído. |
| recovery_plan_generated | Ao gerar o plano de recuperação do dia. |
| overload_guard_applied | Quando um grupo recebe cap de volume/RPE por recuperação incompleta. |

---

## 20. Critérios de aceite

### CA-001 — Janela de recuperação respeitada

Dado que o usuário treinou pernas pesado ontem,

Quando a quest de hoje incluir pernas como secundário,

Então o volume de pernas deve ser rebaixado (`volumeCapFactor` reduzido) por estar dentro da janela de ~72h.

### CA-002 — Full body alterna ênfase e varia estímulo

Dado um usuário em `full_body` em dois dias consecutivos,

Quando as quests forem geradas,

Então a ênfase deve alternar (ex.: empurrar → puxar) e a mesma `movementFamily` de cada grupo não deve se repetir havendo variante equivalente.

### CA-003 — Grupo primário em recuperação não é bloqueado, mas rebaixado

Dado um ciclo curto em que o grupo primário do dia ainda está em recuperação,

Quando a quest for gerada,

Então o grupo é mantido (é o primário do split) com volume/RPE reduzidos e variação priorizada.

### CA-004 — Teto de volume semanal

Dado que um músculo atingiu o teto semanal do nível,

Quando surgir novo estímulo alto na mesma semana,

Então o estímulo deve ser rebaixado até a virada de semana.

### CA-005 — Quest não concluída não gera fadiga

Dado que a quest de ontem não foi concluída,

Quando o estado de recuperação for avaliado,

Então nenhum grupo deve constar como treinado por causa dela.

---

## 21. Critérios de teste para QA

### Backend

- janelas 24/48/72h aplicadas conforme intensidade da última sessão;
- frequência-alvo ~2x e faixas de volume por nível respeitadas;
- `volumeCapFactor`/`rpeCapDelta` corretos por status;
- full body alterna ênfase e evita repetir `movementFamily`;
- estado atualizado só por treino concluído;
- recuperação nunca libera exercício bloqueado por segurança.

---

## ✅ Decisão registrada

> A recuperação é uma camada científica determinística sobre o split (US-237) e a rotação (US-238): rastreia o estado por grupo muscular a partir dos treinos concluídos e emite um `RecoveryPlan` (cap de volume, ajuste de RPE, anti-repetição de estímulo e — no Full Body — ênfase alternada em microciclo). O objetivo é evitar sobrecarga e overtraining sem bloquear o primário do dia, mantendo a segurança (US-045) sempre soberana.
