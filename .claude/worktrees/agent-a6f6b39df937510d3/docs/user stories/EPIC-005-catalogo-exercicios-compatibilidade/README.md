---
title: EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade
sidebar_position: 5
---

# EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-005 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Sistema e usuário com acesso ativo |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejado |

## 2. Objetivo

Construir um catálogo interno de exercícios capaz de alimentar a geração de quests com segurança, importando exercícios da **ExerciseDB / Ascend API** como fonte bruta e aplicando uma camada própria de normalização, tradução, sanitização, enriquecimento de atributos e aprovação.

Nenhum exercício deve entrar no gerador apenas porque veio da API externa. A API é fonte bruta; o AWAKEN é a autoridade sobre quais exercícios estão aptos para gerar treino.

## 3. Contexto de produto

A crítica central contra apps concorrentes é gerar treino incompatível com o perfil do usuário. Este épico existe para impedir que o AWAKEN indique exercícios impossíveis ou inadequados, especialmente para iniciantes e pessoas sem equipamentos.

Para isso, cada exercício precisa ser um **objeto inteligente** capaz de responder: para quem serve, para quem não serve, qual objetivo ajuda, qual músculo trabalha, qual articulação exige, qual equipamento precisa, qual nível mínimo exige, quais variantes possui, qual mídia mostra a execução, quais atributos evolui e quanto XP de atributo concede.

## 4. Escopo

### Entra neste épico

- Importação de exercícios da ExerciseDB / Ascend API para base bruta (`ExerciseRawImport`).
- Rastreabilidade do provider e verificação de licença/atribuição de mídia.
- Normalização e tradução PT-BR (nomes, equipamentos, músculos, instruções, dicas, mídia).
- Catálogo sanitizado (`ExerciseCatalog`) com metadados completos.
- Classificação por tipo, grupo muscular, equipamento, ambiente, dificuldade, complexidade técnica e impacto.
- Tags obrigatórias: `goalTags`, `movementPattern`, `riskTags`, `accessibilityTags`, `jointStressTags`, `contraindicationTags`, `limitationBlockTags`, `painBlockTags`.
- Adequação por nível (`minExperienceLevel`, `suitableForSedentary/Beginner/Intermediate/Advanced`).
- Variantes de regressão e progressão.
- Contribuição de atributos por exercício (`ExerciseAttributeContribution`).
- Sanitização automática obrigatória e fluxo de aprovação (`pending_review` → `approved`).
- Instruções simples de execução com mídia.
- Fallback de catálogo quando a IA não gerar treino.
- Regras determinísticas (sem IA) de escolha dos exercícios por dia: mapa de divisão muscular por programa (US-237), rotação do dia pelo histórico (US-238), recuperação/anti-sobrecarga com base científica (US-239), composição do perfil-alvo do dia consumido pela geração (US-240) progressão semanal adaptativa por estado/rank/atributos (US-241) e orçamento de tempo determinístico do treino (US-242).

### Fora deste épico

- Vídeos próprios de execução (usa-se a mídia do provider, respeitada a licença).
- Catálogo avançado de academia profissional.
- Avaliação biomecânica.
- Prescrição médica ou fisioterapêutica.
- Dependência da API externa em tempo real na geração de treino (decisão: importar e cachear).

## 5. Pipeline de importação e aprovação

```txt
1. Buscar exercícios na ExerciseDB / Ascend API.
2. Salvar resposta original em ExerciseRawImport.
3. Normalizar nomes, equipamentos, músculos e mídia.
4. Mapear grupos musculares e equipamentos para enums internos.
5. Criar registro em ExerciseCatalog.
6. Aplicar sanitização automática.
7. Aplicar enriquecimento de atributos (ExerciseAttributeContribution).
8. Marcar como pending_review.
9. Revisar amostras ou regras críticas.
10. Aprovar como approved.
11. Liberar para geração de quests.
```

Regra de disponibilidade: nenhum exercício é usado em quest sem `isApprovedForWorkoutGeneration = true`.

## 6. Modelo de dados

### 6.1 `ExerciseRawImport` (fonte bruta)

`Id`, `ProviderName`, `ProviderExerciseId`, `ProviderVersion`, `RawJson`, `ImportedAt`, `ImportBatchId`, `SourceUrl`, `MediaBaseUrl`, `Status`, `ErrorMessage`.

Status possíveis: `imported`, `failed`, `normalized`, `pending_review`, `approved`, `rejected`, `deprecated`.

### 6.2 `ExerciseCatalog` (sanitizado)

`Id`, `ProviderName`, `ProviderExerciseId`, `NamePtBr`, `NameOriginal`, `Slug`, `DescriptionPtBr`, `InstructionsPtBr`, `InstructionsOriginal`, `TipsPtBr`, `ExerciseType`, `MovementPattern`, `DifficultyLevel`, `TechnicalComplexity`, `ImpactLevel`, `Environment`, `RequiredEquipment`, `PrimaryMuscleGroups`, `SecondaryMuscleGroups`, `BodyParts`, `JointStressTags`, `ContraindicationTags`, `LimitationBlockTags`, `PainBlockTags`, `GoalTags`, `RiskTags`, `AccessibilityTags`, `MinExperienceLevel`, `SuitableForSedentary`, `SuitableForBeginner`, `SuitableForIntermediate`, `SuitableForAdvanced`, `RegressionExerciseId`, `ProgressionExerciseId`, `RelatedExerciseIds`, `VideoUrl`, `ImageUrl`, `GifUrl`, `MediaLicenseInfo`, `SanitizationStatus`, `IsApprovedForWorkoutGeneration`, `CreatedAt`, `UpdatedAt`.

### 6.3 `ExerciseAttributeContribution` (atributos por exercício)

`Id`, `ExerciseCatalogId`, `PrimaryAttribute`, `StrengthXp`, `AgilityXp`, `EnduranceXp`, `VitalityXp`, `FocusXp`, `WisdomXp`, `IsAutoGenerated`, `ReviewedBy`, `ReviewedAt`, `CreatedAt`, `UpdatedAt`.

## 7. Tags obrigatórias

- `goalTags`: `hypertrophy`, `fat_loss`, `conditioning`, `strength`, `maintenance`.
- `movementPattern`: `squat`, `hinge`, `horizontal_push`, `vertical_push`, `horizontal_pull`, `vertical_pull`, `lunge`, `carry`, `core_flexion`, `core_anti_extension`, `core_anti_rotation`, `locomotion`, `jump`, `balance`, `mobility`.
- `riskTags`: `knee_high_stress`, `lumbar_high_stress`, `shoulder_high_stress`, `wrist_high_stress`, `ankle_high_stress`, `hip_high_stress`, `cervical_high_stress`, `high_impact`, `high_technical_complexity`, `requires_spotter`, `requires_load_control`.
- `accessibilityTags`: `beginner_safe`, `sedentary_safe`, `low_impact`, `no_equipment`, `small_space`, `chair_supported`, `floor_required`, `wrist_neutral_possible`, `knee_friendly`, `back_friendly`.

## 8. Contribuição de atributos

Os 6 atributos do AWAKEN são: Força, Agilidade, Resistência, Vitalidade, Foco e Sabedoria.

Regra obrigatória (alinhada ao documento de instruções de personalização):

```txt
Todo exercício aprovado deve gerar wisdomXp >= 1.
Todo exercício aprovado deve gerar XP > 0 em pelo menos 1 atributo além de Sabedoria.
primaryAttribute não pode ser wisdom.
Cada 10 XP acumulados em um atributo geram +1 ponto real (cálculo no EPIC-009).
```

Limites recomendados no MVP: exercício comum `1–3` XP no atributo principal; exercício complexo/intenso até `4` (apenas intermediário/avançado); Sabedoria sempre `1` XP fixo.

A contribuição é gerada automaticamente a partir da matriz por tipo de exercício e por padrão de movimento (ver documento de instruções, seções 37–39) e pode ser revisada manualmente.

> **Divergência registrada com o EPIC-009 (atual):** o EPIC-005 passa a adotar contribuição completa de atributos por exercício (vetor de XP por atributo, incluindo `wisdomXp`), conforme o documento de instruções. O EPIC-009 atual descreve o exercício impactando "1 ou 2 atributos" com Sabedoria concedida automaticamente por treino. Recomenda-se alinhar o EPIC-009 ao modelo deste épico em uma atualização posterior. Até lá, o EPIC-005 é a fonte de verdade para a estrutura `ExerciseAttributeContribution`.

## 9. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-035 | Manter catálogo interno de exercícios aprovados (fonte da geração, com fallback) | P0 | [Abrir](./US-035-catalogo-interno-aprovados.md) |
| US-143 | Importar exercícios da ExerciseDB / Ascend API para base bruta | P0 | [Abrir](./US-143-importar-exercicios-fonte-bruta.md) |
| US-144 | Normalizar e traduzir exercícios para PT-BR e enums internos | P0 | [Abrir](./US-144-normalizar-traduzir-exercicios.md) |
| US-036 | Classificar exercícios por grupo muscular e partes do corpo | P0 | [Abrir](./US-036-classificar-grupo-muscular.md) |
| US-037 | Classificar exercícios por equipamento e ambiente | P0 | [Abrir](./US-037-classificar-equipamento-ambiente.md) |
| US-038 | Classificar dificuldade, complexidade técnica e impacto | P0 | [Abrir](./US-038-classificar-dificuldade-complexidade-impacto.md) |
| US-145 | Mapear padrão de movimento e tags de objetivo | P0 | [Abrir](./US-145-mapear-movimento-objetivo.md) |
| US-040 | Mapear tags de risco, articulação e bloqueio por limitação/dor | P0 | [Abrir](./US-040-mapear-risco-bloqueio-limitacao-dor.md) |
| US-146 | Mapear acessibilidade e adequação por nível | P0 | [Abrir](./US-146-mapear-acessibilidade-nivel.md) |
| US-039 | Mapear variantes de regressão e progressão | P0 | [Abrir](./US-039-mapear-regressao-progressao.md) |
| US-147 | Definir contribuição de atributos por exercício | P0 | [Abrir](./US-147-contribuicao-atributos-exercicio.md) |
| US-148 | Sanitizar exercícios importados | P0 | [Abrir](./US-148-sanitizar-exercicios.md) |
| US-149 | Aprovar exercício para geração de quests | P0 | [Abrir](./US-149-aprovar-exercicio-geracao.md) |
| US-041 | Ver instruções simples do exercício com mídia | P0 | [Abrir](./US-041-ver-instrucoes-exercicio.md) |
| US-236 | Importar taxonomia biomecânica, relações com score e mídia individual do exercício | P0 | [Abrir](./US-236-importar-taxonomia-relacoes-midia-exercicio.md) |
| US-237 | Definir a divisão muscular de cada dia por tipo de programa (split map) | P0 | [Abrir](./US-237-definir-divisao-muscular-por-programa.md) |
| US-238 | Determinar o dia do programa por rotação sobre o histórico (último dia concluído) | P0 | [Abrir](./US-238-determinar-dia-programa-rotacao-historico.md) |
| US-239 | Aplicar regras científicas de recuperação muscular e anti-sobrecarga | P0 | [Abrir](./US-239-recuperacao-muscular-anti-sobrecarga.md) |
| US-240 | Compor o conjunto elegível do dia (split + rotação + recuperação) para a geração | P0 | [Abrir](./US-240-compor-conjunto-elegivel-do-dia.md) |
| US-241 | Progressão semanal adaptativa (sobrecarga progressiva por estado, rank e atributos) | P0 | [Abrir](./US-241-progressao-semanal-adaptativa.md) |
| US-242 | Orçamento de tempo do treino determinístico (duração estimada × tempo disponível) | P0 | [Abrir](./US-242-orcamento-tempo-treino-deterministico.md) |

> **Alterações de backlog:** as antigas US-036 e US-037 foram mantidas, mas expandidas (grupo muscular agora inclui partes do corpo; equipamento agora inclui ambiente). A antiga US-038 passou a cobrir também complexidade técnica e impacto. A antiga US-040 (contraindicações) foi expandida para todas as tags de risco e bloqueio. As US-143 a US-149 são novas e cobrem importação, normalização, padrões/tags de objetivo, acessibilidade/nível, contribuição de atributos, sanitização e aprovação. A US-236 é nova e cobre a importação de um dataset enriquecido adicional (`exerciseData_complete_051426.json`) com taxonomia biomecânica, grafo de relações com score/confiança/motivos (similares, substituições, progressões, regressões) e GIF individual por exercício (resolução 360, única usada), dando suporte à geração e troca de exercício de forma individual (ADR-012). As US-237 a US-240 são novas e definem as **regras de escolha dos exercícios personalizados de cada dia**: o mapa de divisão muscular por programa (`full_body`, `ab`, `abc`, `abcd`, `abcde`) na US-237, a rotação determinística do dia pelo histórico de treinos concluídos na US-238, as regras científicas de recuperação e anti-sobrecarga na US-239, e a composição do conjunto elegível do dia (perfil-alvo consumido pela geração da EPIC-006) na US-240 — tudo por algoritmo determinístico, sem IA. A US-241 é nova e adiciona a **progressão semanal adaptativa**: relê o estado atual do jogador (perfil que pode ter mudado, rank e atributos) e ajusta exercícios, séries, repetições e descanso por sobrecarga progressiva e autorregulação, para o usuário se sentir sempre minimamente desafiado e evoluir, sempre subordinada à segurança (US-045) e à recuperação (US-239). A US-242 é nova e define o **orçamento de tempo determinístico**: a fórmula de duração estimada (execução + descanso + aquecimento + transições + finalização) e a resolução do conflito objetivo/intensidade × tempo, garantindo que quantidade de exercícios, séries, repetições e descanso caibam no tempo configurado sem descaracterizar o objetivo.

## 10. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-005-001 | Todo exercício deve ser importado, normalizado, sanitizado e aprovado antes de ser usado na geração. |
| RN-EPIC-005-002 | Todo exercício deve informar equipamento necessário, mapeado para enum interno. |
| RN-EPIC-005-003 | Todo exercício deve ter dificuldade (1–5) e impacto (0–5) definidos. |
| RN-EPIC-005-004 | Todo exercício deve ter pelo menos um grupo muscular principal. |
| RN-EPIC-005-005 | Nenhum exercício sem mídia válida (vídeo, GIF ou imagem) pode ser aprovado, salvo exceção manual. |
| RN-EPIC-005-006 | Nenhum exercício sem instrução de execução pode ser aprovado. |
| RN-EPIC-005-007 | Exercícios com tags de risco/contraindicação devem ser filtráveis conforme limitações e dores do usuário. |
| RN-EPIC-005-008 | Todo exercício aprovado deve ter `ExerciseAttributeContribution` com `wisdomXp >= 1` e pelo menos 1 atributo além de Sabedoria com XP > 0. |
| RN-EPIC-005-009 | `primaryAttribute` não pode ser `wisdom`. |
| RN-EPIC-005-010 | O catálogo deve manter rastreabilidade do provider (`providerName`, `providerExerciseId`, `providerVersion`, `sourceUrl`, `importedAt`). |
| RN-EPIC-005-011 | Mídia e dados do provider só podem ser armazenados/redistribuídos após verificação de licença e atribuição. |
| RN-EPIC-005-012 | O catálogo aprovado deve permitir fallback caso a IA não gere treino. |
| RN-EPIC-005-013 | O app não deve depender da API externa em tempo real para gerar treino. |
| RN-EPIC-005-014 | A divisão muscular de cada dia por programa (`full_body`, `ab`, `abc`, `abcd`, `abcde`) é uma configuração determinística e versionada (US-237), coerente com a descrição publicada dos programas (US-231) e ancorada nos enums de grupo (US-036) e padrão de movimento (US-145/US-236). |
| RN-EPIC-005-015 | O dia do programa é o sucessor cíclico do último dia efetivamente concluído (US-062), calculado sem IA; regenerações não avançam o ponteiro e trocar de programa reinicia a rotação (US-238). |
| RN-EPIC-005-016 | A escolha diária deve respeitar recuperação muscular baseada em ciência (janela 24/48/72h, frequência ~2x/semana, volume recuperável por nível) e, no `full_body`, variar estímulo e alternar ênfase entre sessões (US-239). |
| RN-EPIC-005-017 | O perfil-alvo do dia (US-240) é determinístico e é insumo da geração (EPIC-006); a segurança (US-045) permanece soberana e o fallback (US-046) é acionado quando não há elegíveis coerentes suficientes. |
| RN-EPIC-005-018 | A geração deve sempre usar o estado atual do jogador (perfil editável, rank e atributos); mudanças de configuração recalibram a progressão na próxima geração (US-241). |
| RN-EPIC-005-019 | A progressão semanal é determinística e cientificamente embasada (sobrecarga progressiva, dupla progressão, autorregulação por RPE e deload periódico), ajustando exercícios, séries, repetições e descanso para manter o desafio, sempre dentro dos tetos de recuperação (US-239) e da segurança (US-045). |
| RN-EPIC-005-020 | Rank e atributos (EPIC-009) enviesam o vetor de progressão dentro dos limites de segurança/recuperação; a regressão por queda de desempenho é ajuste de treino e não penaliza XP/rank. |
| RN-EPIC-005-021 | A duração estimada da quest soma execução, descanso entre séries, aquecimento, transições e finalização, e nunca excede o tempo disponível configurado (US-242). |
| RN-EPIC-005-022 | No conflito objetivo/intensidade × tempo, preserva-se o piso de descanso do objetivo e ajusta-se primeiro a quantidade de exercícios e séries; densidade (superset/circuito) só para condicionamento/perda de peso e micro quest como último recurso (US-242). |

## 11. Impactos técnicos

### Flutter

- Tela ou componente de instrução do exercício (nome PT-BR, descrição, séries, repetições, tempo, descanso, mídia).
- Indicação de variante (regressão/progressão) quando houver.
- Exibição apenas de exercícios aprovados.

### Backend

- Rotina de importação da ExerciseDB / Ascend API com `ImportBatchId`.
- Rotina de normalização e tradução.
- Rotina de sanitização automática.
- Rotina de enriquecimento de atributos.
- Validação de aprovação e fluxo de status.
- Serviços de consulta e filtro por perfil.
- Regras para substituição de exercício por variante compatível.

### Banco de dados

Entidades principais: `ExerciseRawImport`, `ExerciseCatalog`, `ExerciseAttributeContribution` (ver seção 6).

### Analytics

- `exercise_import_started`.
- `exercise_import_completed`.
- `exercise_sanitized`.
- `exercise_approved`.
- `exercise_rejected`.
- Uso indireto em `daily_quest_generated`, `dungeon_generated`, `workout_edited` e `exercise_completed`.

### QA

- Importar lote e verificar `ExerciseRawImport`.
- Verificar normalização/tradução PT-BR e enums.
- Filtrar por equipamento, dificuldade, impacto e limitação física.
- Substituir exercício por variante compatível.
- Bloquear aprovação de exercício sem mídia, sem músculo principal, sem instrução ou sem contribuição de atributo válida.
- Ver instruções no idioma correto.

## 12. Dependências

- EPIC-004 para perfil do usuário (limitações, dores, nível, objetivo).
- EPIC-006 para geração da quest (consumidor do catálogo e do perfil-alvo do dia — US-240).
- EPIC-007 para edição e substituição.
- EPIC-009 para conversão de XP de atributo em pontos reais e para rank/atributos que enviesam a progressão semanal (US-241).
- EPIC-021 para o catálogo de programas e seleção com restrição por rank (US-231/US-232), base das regras de divisão por dia (US-237–240).
- US-062 (EPIC-008) para o histórico de treinos concluídos que alimenta a rotação do dia (US-238), o estado de recuperação (US-239) e o desempenho da progressão semanal (US-241).
- US-034 (EPIC-004) para a edição de perfil, cujas mudanças recalibram a progressão (US-241).
- US-028 (EPIC-004) para o tempo disponível por treino, insumo do orçamento de tempo determinístico (US-242).

## 13. Critérios de aceite do épico

- Pipeline importa, normaliza, sanitiza e aprova exercícios.
- Exercícios aprovados têm metadados completos, tags e contribuição de atributos válida.
- Sistema filtra exercícios incompatíveis por limitação e dor.
- Variantes de regressão/progressão são utilizáveis.
- Instruções e mídia são exibidas ao usuário.
- Catálogo aprovado consegue gerar treino real e servir de fallback.

## 14. Decisão registrada

A ExerciseDB / Ascend API é usada como fonte bruta. O AWAKEN mantém uma camada própria de normalização, tradução, sanitização, segurança, atributos e aprovação. Nenhum exercício é usado em quest sem estar aprovado. Todo exercício aprovado contribui com Sabedoria + pelo menos 1 atributo principal, e cada 10 XP acumulados em um atributo aumentam +1 ponto real no personagem. O catálogo é obrigatório para garantir personalização real e fallback estável.
