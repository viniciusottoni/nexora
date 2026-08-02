---
title: EPIC-006 — Geração de Quests (Diária e Dungeon)
sidebar_position: 6
---

# EPIC-006 — Geração de Quests (Diária e Dungeon)

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-006 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Usuário em Trial ou assinante |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejado |

## 2. Objetivo

Gerar quests de treino compatíveis e seguras com o perfil do usuário: a quest diária (principal, obrigatória para o loop de progressão) e as dungeons (side quests opcionais, escolhidas antes da ativação como `solo` ou `grupo`). As dungeons aparecem nas segundas, quartas e sextas, ou quando o usuário usa uma `Pedra de Dungeon`. A geração respeita objetivo, nível efetivo, equipamentos, tempo disponível, limitações físicas e dores, aplicando um filtro eliminatório de segurança antes de qualquer pontuação.

## 3. Contexto de produto

A quest diária é o coração do loop de retenção do AWAKEN. Ela transforma treino em missão e precisa funcionar todos os dias para usuários com acesso ativo. Se a geração falhar, deve existir fallback por templates. As dungeons complementam o loop como treinos pontuais e opcionais que o usuário pode ativar nas segundas, quartas e sextas, ou fora dessa janela com `Pedra de Dungeon`, podendo também conceder itens ao completar. Antes da ativação, o usuário precisa definir se a dungeon será `solo` ou `grupo`.

A regra de produto mais importante deste épico: **gamificação nunca pode superar segurança**. O sistema nunca escolhe um exercício apenas porque é bom para o objetivo; a ordem de prioridade é segurança, compatibilidade com limitações/dores, nível, tempo, objetivo, potencial de evolução, variedade e, por último, recompensa de XP/atributos.

## 4. Escopo

### Entra neste épico

- Geração de quest diária (tipo `daily`).
- Geração de dungeons (tipo `dungeon`, side quests opcionais, com modo `solo` ou `grupo` definido antes da ativação e janela semanal própria).
- Cálculo do nível efetivo (`effectiveExperienceLevel`) para a geração.
- Filtro eliminatório de segurança (nível, equipamento, tempo, limitações, dores, impacto, complexidade, aprovação).
- Pontuação de exercícios com peso alto de segurança.
- Pontuação por atributo-alvo (priorizar atributos baixos e ligados ao objetivo).
- Prescrição inicial por perfil e objetivo (RPE, séries, reps, descanso, frequência).
- Arquitetura da sessão por tempo disponível (orçamento de tempo).
- Bloqueio de geração para trial ou assinatura expirada.
- Fallback por templates.
- Persistência da quest do dia.
- Regeneração limitada da quest diária.
- Auditoria da geração (motivo e respeito ao perfil).
- Penalidade de XP por quest diária não completada.

### Fora deste épico

- Planejamento mensal avançado.
- Periodização profissional completa.
- Master Quests.
- Raids (apenas em grupo — Pós-MVP).
- Treino social ou competitivo.
- Definição do catálogo e dos atributos por exercício (EPIC-005).
- Cálculo de conversão de XP em ponto real (EPIC-009).

## 5. Pipeline de geração

```txt
1. Resolver nível efetivo (effectiveExperienceLevel) a partir do perfil.
2. Filtro eliminatório de segurança (remover exercícios incompatíveis).
3. Pontuar exercícios elegíveis (segurança com peso alto).
4. Ajustar pontuação por atributo-alvo (atributos baixos e ligados ao objetivo).
5. Selecionar exercícios respeitando o orçamento de tempo.
6. Aplicar prescrição inicial por perfil e objetivo (séries, reps, descanso, RPE).
7. Persistir a quest do dia.
8. Em caso de falha, usar fallback por templates compatíveis.
9. Registrar motivo/auditoria da geração.
```

### 5.1 Filtro eliminatório

Um exercício é removido quando:

```txt
exercise.minExperienceLevel > user.effectiveExperienceLevel
exercise.requiredEquipment não está disponível
exercise.timeCost estoura o tempo do treino
exercise.contraindicationTags/limitationBlockTags conflita com physicalLimitations
exercise.painBlockTags conflita com physicalPains
exercise.impactLevel alto e usuário sedentário com IMC alto
exercise.technicalComplexity alta e usuário sedentário/iniciante
exercise.isApprovedForWorkoutGeneration = false
```

### 5.2 Pontuação

```txt
exerciseScore =
  goalAffinityScore     * 0.25
+ safetyScore           * 0.25
+ levelMatchScore       * 0.15
+ targetAttributeScore  * 0.15
+ timeFitScore          * 0.10
+ varietyScore          * 0.05
+ progressionFitScore   * 0.05
```

`safetyScore` tem peso alto; `targetAttributeScore` aumenta quando o exercício contribui para um atributo baixo do usuário e/ou para o atributo mais ligado ao objetivo, sem conflitar com dores/limitações.

## 6. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-042 | Receber quest diária baseada no perfil (com orçamento de tempo) | P0 | [Abrir](./US-042-receber-quest-diaria-perfil.md) |
| US-150 | Calcular nível efetivo (effectiveExperienceLevel) para a geração | P0 | [Abrir](./US-150-calcular-nivel-efetivo.md) |
| US-045 | Filtrar exercícios incompatíveis por segurança (filtro eliminatório) | P0 | [Abrir](./US-045-filtro-eliminatorio-seguranca.md) |
| US-151 | Pontuar e selecionar exercícios com prioridade de segurança | P0 | [Abrir](./US-151-pontuar-selecionar-exercicios.md) |
| US-152 | Priorizar atributos-alvo e atributos baixos do usuário | P1 | [Abrir](./US-152-priorizar-atributos-alvo.md) |
| US-153 | Aplicar prescrição inicial por perfil e objetivo | P0 | [Abrir](./US-153-prescricao-inicial-perfil-objetivo.md) |
| US-044 | Receber quest personalizada durante trial ou assinatura | P0 | [Abrir](./US-044-quest-personalizada-trial-assinatura.md) |
| US-043 | Bloquear geração para acesso expirado | P0 | [Abrir](./US-043-bloquear-geracao-acesso-expirado.md) |
| US-046 | Usar fallback por templates | P0 | [Abrir](./US-046-fallback-templates.md) |
| US-047 | Salvar a quest do dia | P0 | [Abrir](./US-047-salvar-quest-do-dia.md) |
| US-048 | Regenerar quest dentro de limites | P1 | [Abrir](./US-048-regenerar-quest-limites.md) |
| US-049 | Registrar motivo e auditoria da geração | P1 | [Abrir](./US-049-registrar-motivo-auditoria.md) |
| US-127 | Ativar uma dungeon como side quest opcional | P0 | [Abrir](./US-127-ativar-dungeon-side-quest.md) |
| US-128 | Gerar dungeon compatível com perfil e equipamentos | P0 | [Abrir](./US-128-gerar-dungeon-compativel.md) |
| US-129 | Aplicar penalidade de XP por quest diária não completada | P0 | [Abrir](./US-129-penalidade-xp-quest-nao-completada.md) |

> **Alterações de backlog:** a antiga US-045 ("impedir exercícios incompatíveis") foi expandida para o filtro eliminatório completo (nível efetivo, equipamento, tempo, limitações, dores, impacto, complexidade e aprovação). A US-049 passou a englobar a auditoria da geração. As US-150 a US-153 são novas e cobrem nível efetivo, pontuação, atributo-alvo e prescrição por perfil/objetivo. As demais US foram mantidas e ajustadas.

## 7. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-006-001 | Apenas usuários com trial ativo ou assinatura ativa podem gerar quest. |
| RN-EPIC-006-002 | A quest deve respeitar equipamentos disponíveis (padrão: treino sem equipamento). |
| RN-EPIC-006-003 | A quest deve respeitar limitações físicas E dores informadas. |
| RN-EPIC-006-004 | A duração estimada deve respeitar o tempo disponível, incluindo aquecimento, execução, descanso e finalização. |
| RN-EPIC-006-005 | A geração deve usar o `effectiveExperienceLevel`, conservador em caso de conflito. |
| RN-EPIC-006-006 | Segurança tem prioridade máxima: gamificação nunca supera segurança. |
| RN-EPIC-006-007 | O filtro eliminatório roda antes da pontuação; só exercícios aprovados entram. |
| RN-EPIC-006-008 | A pontuação deve dar peso alto à segurança e considerar atributos-alvo. |
| RN-EPIC-006-009 | Se a IA falhar, o sistema deve usar template compatível. |
| RN-EPIC-006-010 | A quest do dia deve ser persistida para evitar perda ao fechar o app. |
| RN-EPIC-006-011 | Dungeons são opcionais e podem ser ativadas nas segundas, quartas e sextas, ou fora dessa janela com `Pedra de Dungeon`; não substituem a quest diária. |
| RN-EPIC-006-012 | Não completar a quest diária resulta em penalidade de XP progressiva na virada de dia: 1 dia = -10 XP, 2 dias = -20 XP, 3 dias = -30 XP, e assim por diante. |
| RN-EPIC-006-013 | A penalidade de XP por quest diária perdida é aplicada na virada de dia, apenas se o usuário tiver acesso ativo. |
| RN-EPIC-006-014 | Dor relatada durante a execução prevalece sobre o onboarding na próxima geração. |
| RN-EPIC-006-015 | O Rank pode influenciar a quest (dificuldade sugerida, cosméticos, desafios opcionais, tipo de Master Quest – Pós-MVP), mas nunca supera segurança, dores, limitações, tempo, equipamento ou nível efetivo. |
| RN-EPIC-006-016 | Rank alto não libera automaticamente exercícios perigosos: segurança sempre supera Rank. |
| RN-EPIC-006-017 | Antes da ativação, toda dungeon deve ter o modo definido: `solo` ou `grupo`. |
| RN-EPIC-006-018 | A `Pedra de Dungeon`, quando usada, é consumida para liberar a dungeon fora da janela semanal. |

## 8. Impactos técnicos

### Flutter

- Tela de quest do dia.
- Estado de carregamento da geração.
- Estado de erro com fallback ou tentar novamente.
- Estado bloqueado por trial ou assinatura expirada.
- Exibição de treino gerado antes da edição (séries, reps, descanso por exercício).
- Indicação de dungeon liberada por calendário ou por `Pedra de Dungeon`.

### Backend

- Serviço de geração de quest (diária e dungeon, com campo `type`).
- Cálculo do `effectiveExperienceLevel`.
- Filtro eliminatório de segurança.
- Motor de pontuação (segurança, objetivo, nível, atributo-alvo, tempo, variedade, progressão).
- Prescrição inicial por perfil e objetivo.
- Validação de janela semanal de dungeon e consumo de item.
- Integração com IA, quando aplicável, e fallback por template.
- Registro/auditoria da quest gerada.
- Job de virada de dia para penalidade de XP.

### Banco de dados

Entidades principais: `Quest`, `QuestExercise`, `ExerciseCatalog`, `ExerciseAttributeContribution`, `UserProfile`, `Subscription`.

### Analytics

- `daily_quest_generated`.
- `dungeon_generated`.
- `quest_generation_blocked`.
- `quest_generation_failed`.
- `quest_viewed`.

### QA

- Gerar quest diária com e sem equipamentos.
- Gerar quest para sedentário/iniciante e validar filtro de nível/impacto/complexidade.
- Validar filtro por limitação E por dor.
- Validar respeito ao tempo disponível (10 min = micro quest).
- Validar prioridade de segurança sobre objetivo/atributo.
- Validar seleção de modo `solo` ou `grupo` antes da ativação da dungeon.
- Validar ativação de dungeon nas segundas, quartas e sextas.
- Validar ativação fora da janela semanal apenas com `Pedra de Dungeon`.
- Validar bloqueio após trial expirado.
- Validar fallback por template.
- Validar persistência da quest diária.
- Validar penalidade de XP progressiva quando a daily não foi completada por 1, 2 e 3 dias seguidos.
- Validar que dungeon não substitui a quest diária.

## 9. Dependências

- EPIC-003 para status de acesso.
- EPIC-004 para perfil (objetivo, nível, tempo, limitações, dores, dados físicos).
- EPIC-005 para catálogo aprovado, tags e contribuição de atributos.
- EPIC-009 para XP, penalidade e conversão de atributos.

## 10. Critérios de aceite do épico

- Usuário com acesso ativo recebe quest segura e compatível.
- Usuário bloqueado não recebe quest.
- O filtro eliminatório remove exercícios incompatíveis por nível, equipamento, tempo, limitação, dor, impacto, complexidade e não aprovados.
- A pontuação prioriza segurança e considera atributos-alvo.
- A prescrição respeita perfil, objetivo e tempo disponível.
- Fallback funciona quando a geração principal falha.
- Quest fica salva no dia.
- Penalidade de XP é aplicada na virada de dia para quests diárias perdidas.

## 11. Decisão registrada

A quest diária é o core do MVP. Ela deve ser confiável, segura, compatível com o perfil e protegida pelo status comercial do usuário. A geração sempre coloca segurança acima da gamificação: o filtro eliminatório roda antes da pontuação, e a recompensa de XP/atributos nunca incentiva execução com dor.
