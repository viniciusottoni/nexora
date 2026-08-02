---
title: EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak
sidebar_position: 9
---

# EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-009 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Usuário em Trial ou assinante |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejado |

## 2. Objetivo

Aplicar a gamificação central do AWAKEN, recompensando treinos concluídos com XP, evolução de Level, Rank (E→SSS via RankScore), 6 atributos e streak. O épico também define a curva exponencial de Rank, o diminishing returns dos Ranks altos, o limite mensal saudável, a proteção contra abuso e a penalidade progressiva de XP por quest diária não completada.

## 3. Contexto de produto

A gamificação é o principal diferencial do AWAKEN. O usuário precisa sentir que cada treino gera evolução real dentro do sistema. Existem 6 atributos — Força, Agilidade, Resistência, Vitalidade, Foco e Sabedoria — cada um com Level (0–10) e XP interno (0–9, onde acumular 10 resulta em level up). Cada exercício mostra 1 ou 2 atributos impactados, sem contar Sabedoria. Sabedoria é especial: todo exercício concluído concede +1 XP interno a ela por baixo dos panos, como aprendizagem inata da execução.

O **Rank** representa o patamar de evolução física acumulada e é calculado a partir do **RankScore** (soma dos pontos reais de atributos). A progressão de Rank é aproximadamente exponencial: avança rápido no começo e lento nos Ranks altos, de modo que o Rank SSS represente cerca de 3 anos de treino constante. A recompensa deve ser motivadora sem criar punições agressivas que levem ao abandono.

## 4. Escopo

### Entra neste épico

- Ganho de XP ao concluir exercício dentro da quest (diária ou dungeon).
- Cálculo de XP por esforço, dificuldade e conclusão do exercício.
- Level up (progressão geral do Hunter).
- Evolução dos 6 atributos: Força, Agilidade, Resistência, Vitalidade, Foco e Sabedoria.
- Progressão de atributo: cada atributo tem Level (0–10) e XP interno (0–9); ao acumular 10 XP internos, o level sobe em 1.
- Sabedoria recebe +1 XP interno automaticamente a cada exercício concluído, sem aparecer como atributo visível do exercício.
- **RankScore**: soma dos pontos reais de atributos válidos para Rank.
- **Rank** E→SSS com curva exponencial (`calculateRank`) e recálculo sempre que o RankScore muda.
- **Diminishing returns** a partir do Rank A.
- **Limite mensal saudável** de RankScore e proteção contra abuso/ganho artificial.
- **Bônus controlado de streak** no RankScore (fonte secundária).
- Penalidade progressiva de XP por quest diária não completada (-10 XP por dia consecutivo sem completar a daily).
- Streak e regra de virada de dia.
- Feedback visual de conquista, level up e atributo evoluído.
- Preservação de progresso após trial ou assinatura expirada.

### Fora deste épico

- Cálculo do Rank/RankScore inicial no onboarding (pertence ao EPIC-004; usa o `calculateRank` e o teto deste épico).
- Master Quests e seus bônus de RankScore (Pós-MVP).
- Badges completos.
- Penalidades fortes.
- Ranking social e temporadas.
- Sistema de leitura para Sabedoria (Pós-MVP).

## 5. RankScore e curva de Rank

### 5.1 RankScore

```txt
RankScore = soma dos pontos reais de atributos válidos para Rank
(Força + Agilidade + Resistência + Vitalidade + Foco + Sabedoria)
```

Fontes de RankScore: evolução real de atributos (treino), bônus controlado de streak e (Pós-MVP) bônus de Master Quest. RankScore não pode ser comprado nem concedido por ações sem esforço real.

### 5.2 Curva exponencial (`calculateRank`)

| Rank | RankScore | Salto aproximado | Tempo médio (treino constante) |
|---|---:|---:|---:|
| E | 6–17 | — | imediato |
| D | 18–29 | +12 | 0–1 mês |
| C | 30–47 | +12/+18 | 1–2 meses |
| B | 48–83 | +18 | 3–4 meses |
| A | 84–155 | +36 | 6–8 meses |
| S | 156–299 | +72 | 10–14 meses |
| SS | 300–587 | +144 | 18–24 meses |
| SSS | 588+ | +288 | ~33–39 meses (≈3 anos) |

```txt
if rankScore <= 17:  rank = "E"
elif rankScore <= 29: rank = "D"
elif rankScore <= 47: rank = "C"
elif rankScore <= 83: rank = "B"
elif rankScore <= 155: rank = "A"
elif rankScore <= 299: rank = "S"
elif rankScore <= 587: rank = "SS"
else: rank = "SSS"
```

### 5.3 Teto de onboarding

O onboarding (EPIC-004) pode definir o Rank inicial, com teto **Rank B / RankScore 48** e **Level 1**. Ranks A e superiores só podem ser obtidos com treino real registrado no app.

### 5.4 Diminishing returns por Rank

| Rank atual | Multiplicador |
|---|---:|
| E, D, C | 1.00 |
| B | 0.90 |
| A | 0.80 |
| S | 0.70 |
| SS | 0.60 |

Os atributos continuam evoluindo normalmente; apenas o RankScore (e portanto o Rank) avança mais devagar.

### 5.5 Limite mensal saudável

| Perfil | Ganho mensal saudável |
|---|---:|
| Casual | 3–8 |
| Regular | 8–12 |
| Constante | 12–18 |
| Extremo | 18–24 |

Acima de 24 RankScore/mês, aplicar redução, validação ou diminishing returns.

### 5.6 Bônus de streak (secundário)

| Streak | Bônus de RankScore |
|---|---:|
| 7 dias | +1 |
| 30 dias | +3 |
| 90 dias | +8 |
| 180 dias | +15 |
| 365 dias | +35 |

Streak premia consistência e nunca pode ser a principal fonte de RankScore.

## 6. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-064 | Ganhar XP ao concluir exercícios | P0 | [Abrir](./US-064-ganhar-xp-ao-concluir-quests.md) |
| US-065 | Calcular XP por esforço, dificuldade e conclusão | P0 | [Abrir](./US-065-calcular-xp-esforco-dificuldade.md) |
| US-066 | Subir de level | P0 | [Abrir](./US-066-subir-de-level.md) |
| US-067 | Evoluir de Rank (E→SSS) via RankScore e curva exponencial | P0 | [Abrir](./US-067-evoluir-de-rank-rankscore.md) |
| US-068 | Evoluir os 6 atributos | P0 | [Abrir](./US-068-evoluir-os-6-atributos.md) |
| US-069 | Manter streak (com bônus controlado de RankScore) | P0 | [Abrir](./US-069-manter-streak.md) |
| US-070 | Preservar streak com regra clara de virada de dia | P0 | [Abrir](./US-070-streak-virada-de-dia.md) |
| US-071 | Receber feedback visual de level up e de atributo evoluído | P0 | [Abrir](./US-071-feedback-visual-levelup-atributo.md) |
| US-072 | Preservar progresso após trial ou assinatura expirada | P0 | [Abrir](./US-072-preservar-progresso-apos-expiracao.md) |
| US-130 | Acumular XP interno de atributo e subir o Level ao atingir 10 | P0 | [Abrir](./US-130-pontos-internos-atributo-levelup.md) |
| US-131 | Ver Sabedoria evoluir automaticamente ao completar qualquer treino | P0 | [Abrir](./US-131-sabedoria-automatica.md) |
| US-132 | Receber penalidade de XP por quest diária não completada | P0 | [Abrir](./US-132-penalidade-xp-quest-diaria.md) |
| US-133 | Ver quais atributos evoluíram na tela de recompensa | P0 | [Abrir](./US-133-atributos-evoluidos-recompensa.md) |
| US-134 | Visualizar barra de progresso interna de cada atributo no perfil | P1 | [Abrir](./US-134-barra-progresso-interna-atributo.md) |
| US-154 | Aplicar diminishing returns e limite mensal de RankScore | P0 | [Abrir](./US-154-diminishing-returns-limite-mensal.md) |
| US-155 | Proteger o RankScore contra ganho artificial e abuso | P0 | [Abrir](./US-155-protecao-abuso-rankscore.md) |

> **Alterações de backlog:** a US-067 foi expandida para a evolução de Rank baseada em RankScore e curva exponencial (antes apenas "evoluir de rank"). A US-069 passou a incluir o bônus controlado de streak no RankScore. As US-154 e US-155 são novas (diminishing returns/limite mensal e proteção contra abuso). O cálculo do Rank/RankScore inicial no onboarding fica no EPIC-004 (US-156), reutilizando o `calculateRank` e o teto definidos aqui.

## 7. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-009-001 | XP só deve ser concedido após conclusão válida do exercício. |
| RN-EPIC-009-002 | O mesmo exercício não pode gerar XP duplicado. |
| RN-EPIC-009-003 | Rank segue a progressão E, D, C, B, A, S, SS e SSS. |
| RN-EPIC-009-004 | Cada exercício concede XP interno de atributo conforme `ExerciseAttributeContribution` (EPIC-005), com 1 ou 2 atributos visíveis além de Sabedoria. |
| RN-EPIC-009-005 | Cada atributo visível impactado recebe de 1 a 4 XP internos, conforme a dificuldade efetiva montada para o exercício. |
| RN-EPIC-009-006 | Sabedoria recebe +1 XP interno automaticamente ao completar qualquer exercício, sem aparecer como atributo visível do exercício. |
| RN-EPIC-009-022 | Cada atributo possui Level (0–10) e XP interno (0–9). Ao acumular 10 XP internos, o Level sobe em 1 e o XP interno volta a 0, preservando excedente quando houver. |
| RN-EPIC-009-007 | RankScore é a soma dos pontos reais dos 6 atributos válidos para Rank. |
| RN-EPIC-009-008 | O Rank é recalculado por `calculateRank(rankScore)` sempre que o RankScore muda. |
| RN-EPIC-009-009 | A curva de Rank é aproximadamente exponencial; o SSS exige cerca de 3 anos de treino constante. |
| RN-EPIC-009-010 | O Rank máximo inicial pelo onboarding é B (RankScore 48); A+ exige treino real. |
| RN-EPIC-009-011 | A partir do Rank A, aplica-se diminishing returns ao ganho de RankScore. |
| RN-EPIC-009-012 | Ganho mensal de RankScore acima de ~24 sofre redução/validação. |
| RN-EPIC-009-013 | Streak pode dar bônus de RankScore, mas não pode ser a principal fonte. |
| RN-EPIC-009-014 | RankScore não pode ser comprado nem concedido por ações sem esforço real. |
| RN-EPIC-009-015 | Streak aumenta quando há quest concluída em dias consecutivos. |
| RN-EPIC-009-016 | Não completar a quest diária resulta em penalidade de XP progressiva, aplicada na virada de dia: 1 dia = -10 XP, 2 dias = -20 XP, 3 dias = -30 XP, e assim por diante. |
| RN-EPIC-009-017 | A penalidade de XP não desce o usuário abaixo de 0 XP. |
| RN-EPIC-009-018 | Falhar um dia não deve gerar punição visual agressiva. |
| RN-EPIC-009-019 | Dores e limitações superam permissões de Rank (segurança sempre supera Rank). |
| RN-EPIC-009-020 | Progresso e Rank devem permanecer salvos mesmo com acesso bloqueado. |
| RN-EPIC-009-021 | O Rank deve ser exibido como progresso, nunca como julgamento físico. |

## 8. Impactos técnicos

### Flutter

- Barras de XP e progresso de Rank (RankScore até o próximo Rank).
- Exibição de level, rank, streak e dos 6 atributos com seu Level atual.
- Barra de progresso interna de cada atributo (XP interno 0–9 como P1).
- Animações leves de level up, rank up e level up de atributo.
- Mensagens localizadas de conquista, sem punição visual agressiva.

### Backend

- Serviço de cálculo de XP (por exercício) e consolidação final da quest.
- Serviço de progressão de atributos (XP interno → level up).
- Serviço de progressão de Level.
- Serviço de RankScore e `calculateRank` (recálculo de Rank).
- Diminishing returns por Rank e limite mensal de RankScore.
- Bônus controlado de streak no RankScore.
- Proteção contra abuso/ganho artificial.
- Serviço de streak.
- Job de virada de dia (penalidade progressiva de XP).
- Atualização transacional de progresso.

### Banco de dados

Entidades principais:

- `HunterProgress` (XP, Level, Rank, RankScore, streak, ganho mensal de RankScore).
- `HunterAttributes` (campos `[attr]Level` e `[attr]Points`/`[attr]Xp` internos para os 6 atributos).
- `QuestLog`.
- `RankScoreLog` (auditoria de ganhos/reduções de RankScore: fonte, valor bruto, multiplicador, valor efetivo).

### Analytics

- `xp_earned` (`source`: exercise | dungeon | penalty, `amount`).
- `level_up`.
- `rank_changed`.
- `rank_score_changed`.
- `rank_diminishing_returns_applied`.
- `rank_progress_monthly_limit_reached`.
- `rank_streak_bonus_applied`.
- `rank_abuse_suspected`.
- `streak_updated`.
- `attribute_level_up` (`attribute`, `new_level`).
- `xp_penalty_applied` (`amount`).
- `daily_quest_missed`.

### QA

- Concluir quest diária e verificar XP geral, XP interno dos atributos visíveis e +1 XP interno de Sabedoria por exercício concluído.
- Concluir dungeon e verificar ganho de XP e atributos (sem penalidade).
- Verificar level up de atributo ao acumular 10 XP internos.
- Subir de Level de Hunter.
- Verificar RankScore = soma dos atributos e recálculo de Rank ao mudar.
- Validar curva: cada Rank exige o RankScore correto.
- Validar diminishing returns a partir do Rank A.
- Validar redução acima de 24 RankScore/mês.
- Validar bônus de streak no RankScore e que não é a fonte principal.
- Validar que A+ não é alcançável pelo onboarding (teto B/48).
- Não completar a quest diária por 1, 2 e 3 dias seguidos e verificar a progressão da penalidade.
- Verificar que penalidade não leva XP abaixo de 0.
- Verificar que segurança supera Rank (dor/limitação bloqueiam mesmo com Rank alto).
- Evitar XP/RankScore duplicado e ganho artificial.
- Preservar progresso e Rank após bloqueio.

## 9. Dependências

- EPIC-004 para Rank/RankScore inicial (teto B/48).
- EPIC-005 para a contribuição de atributos por exercício.
- EPIC-006 para conclusão/penalidade da quest e influência do Rank na geração (sem superar segurança).
- EPIC-008 para execução da quest e conclusão dos exercícios.
- EPIC-010 para exibição de Rank, nomes narrativos e desbloqueios.
- EPIC-011 para histórico.

## 10. Critérios de aceite do épico

- Quest concluída gera XP geral e XP interno de atributos conforme `ExerciseAttributeContribution`.
- Level evolui corretamente; XP interno de atributo acumula e sobe o Level ao atingir 10.
- RankScore é calculado a partir dos atributos e o Rank é recalculado por `calculateRank`.
- A curva é exponencial e o teto do onboarding (B/48) é respeitado.
- Diminishing returns e limite mensal funcionam nos Ranks altos.
- Streak concede bônus controlado de RankScore sem ser a fonte principal.
- Penalidade de XP é aplicada na virada de dia para quests diárias perdidas, sem deixar XP negativo.
- Segurança sempre supera Rank.
- Feedback visual aparece sem punição agressiva.
- Progresso e Rank não somem ao expirar acesso.

## 11. Decisão registrada

A gamificação deve recompensar consistência e progresso, nunca punir excessivamente. O Rank é derivado do RankScore (soma dos atributos reais), com curva exponencial que faz o SSS exigir cerca de 3 anos de treino constante. O onboarding pode iniciar até o Rank B; Ranks A+ exigem treino real. Diminishing returns, limite mensal e proteção contra abuso preservam a economia, e a penalidade de XP por daily perdida cresce de forma progressiva (-10 XP por dia consecutivo sem completar a quest) com piso de 0. A segurança sempre supera o Rank.
