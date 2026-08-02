---
title: EPIC-004 — Onboarding e Perfil Inicial do Hunter
sidebar_position: 4
---

# EPIC-004 — Onboarding e Perfil Inicial do Hunter

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-004 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Usuário em Trial ou assinante |
| Planos impactados | Trial, Mensal e Anual |
| Status | Implementado (backend + Flutter); bloqueio de quest pendente de EPIC-006 |

## 2. Objetivo

Coletar as informações necessárias para gerar treinos compatíveis com a realidade do usuário em um fluxo de 8 etapas simples e rápidas, respeitando objetivo, experiência, histórico de treino, dados físicos, tipo de corpo, tempo disponível, limitações físicas e dores.

## 3. Contexto de produto

O onboarding é uma das partes mais importantes do AWAKEN. Ele precisa ser detalhado o bastante para personalizar o treino, mas simples o bastante para não cansar o usuário. O fluxo tem exatamente 8 telas, com progresso visível (1/8 a 8/8). Como o trial dura 7 dias, o onboarding precisa levar rapidamente à primeira quest.

## 4. Fluxo do onboarding (8 etapas)

| Etapa | Pergunta | Tipo | Opções / Campos |
|---|---|---|---|
| 1/8 | Qual é o seu objetivo? | Seleção única | Ganhar massa, Perder peso, Ter condicionamento, Ter mais força, Manter a forma |
| 2/8 | Qual é o seu nível de experiência? | Seleção única | Sedentário (Não treina), Iniciante (Treina às vezes), Intermediário (Treina com uma certa frequência), Avançado (Treina quase ou todo dia) |
| 3/8 | Há quanto tempo você treina? | Seleção única | Não treino, Menos de 1 mês, 1 à 6 meses, 6 à 12 meses, Mais de 1 ano, Mais de 3 anos |
| 4/8 | Sobre você... | Campos de entrada | Idade, Altura, Peso, Sexo biológico |
| 5/8 | Como está seu corpo hoje? | Seleção visual (2×2) | Corpo magro, Corpo "normal", Corpo gordo, Corpo atlético/forte |
| 6/8 | Qual o seu tempo disponível? | Seleção única | 5-10 min, 10-20 min, 20-30 min, 30-40 min, 40-50 min |
| 7/8 | Você tem alguma limitação física? | Seleção múltipla | A definir (ex.: hérnia de disco, problema no joelho, não consigo fazer impacto, etc.) |
| 8/8 | Quais as suas dores físicas? | Seleção múltipla | Pescoço, Ombro, Pulso, Costas, Lombar, Joelhos |

> A etapa 5/8 exibe silhuetas visuais para facilitar a identificação do tipo de corpo atual. Esta informação será usada para personalização da quest e do perfil visual.
>
> A etapa 7/8 (Limitações físicas) serve para filtrar exercícios do catálogo que não podem ser realizados pelo usuário com segurança. As opções exatas serão definidas em conjunto com o catálogo de exercícios (EPIC-005).
>
> Equipamentos e local de treino **não são coletados no onboarding**. O sistema gera quests compatíveis por padrão com treino sem equipamento, podendo ser ajustado pelo usuário no perfil após o onboarding.

## 5. Escopo

### Entra neste épico

- Início do onboarding após comunicação do trial.
- As 8 etapas definidas no fluxo acima.
- Barra de progresso (1/8 a 8/8).
- Navegação para frente e para trás entre etapas.
- Validação por etapa antes de avançar.
- Revisão e confirmação do perfil antes de salvar.
- Edição futura do perfil.

### Fora deste épico

- Avaliação médica.
- Anamnese profissional completa.
- Diagnóstico clínico.
- Plano nutricional completo.
- Seleção de equipamentos no onboarding (Pós-MVP ou configurações de perfil).
- Seleção de local de treino no onboarding.
- Personalização avançada por IA fora do MVP.

## 6. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-022 | Iniciar onboarding após entender trial e planos | P0 | [Abrir](./US-022-iniciar-onboarding-apos-trial-planos.md) |
| US-023 | Informar objetivo principal | P0 | [Abrir](./US-023-informar-objetivo-principal.md) |
| US-024 | Informar nível de experiência | P0 | [Abrir](./US-024-informar-nivel-experiencia.md) |
| US-140 | Informar há quanto tempo treina | P0 | [Abrir](./US-140-informar-tempo-de-treino.md) |
| US-025 | Informar idade, altura, peso e sexo biológico | P0 | [Abrir](./US-025-informar-dados-fisicos-basicos.md) |
| US-141 | Selecionar tipo de corpo atual (seleção visual) | P0 | [Abrir](./US-141-selecionar-tipo-corpo-atual.md) |
| US-028 | Informar tempo disponível por treino | P0 | [Abrir](./US-028-informar-tempo-disponivel-treino.md) |
| US-142 | Informar limitações físicas para filtro do catálogo | P0 | [Abrir](./US-142-informar-limitacoes-fisicas-catalogo.md) |
| US-030 | Informar dores físicas | P0 | [Abrir](./US-030-informar-dores-fisicas.md) |
| US-032 | Revisar perfil antes de concluir | P0 | [Abrir](./US-032-revisar-perfil-antes-concluir.md) |
| US-033 | Salvar perfil inicial | P0 | [Abrir](./US-033-salvar-perfil-inicial.md) |
| US-034 | Editar perfil após onboarding | P1 | [Abrir](./US-034-editar-perfil-apos-onboarding.md) |
| US-156 | Calcular Rank e RankScore iniciais ao concluir o onboarding | P0 | [Abrir](./US-156-calcular-rank-inicial-onboarding.md) |

> As US-026 (local de treino), US-027 (equipamentos), US-029 (dias por semana) e US-031 (preferências) foram removidas do escopo com base no fluxo definido pelos wireframes. Essas informações poderão ser coletadas em configurações de perfil no pós-MVP.
>
> A US-156 deriva atributos iniciais, RankScore e Rank inicial a partir das respostas do onboarding, com teto Rank B / RankScore 48 e Level 1. A curva e o `calculateRank` pertencem ao EPIC-009.

## 7. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-004-001 | Onboarding só deve ser concluído por usuário com acesso ativo. |
| RN-EPIC-004-002 | Objetivo, nível de experiência, tempo de treino, dados físicos, tipo de corpo, tempo disponível, limitações físicas e dores são as entradas mínimas para gerar quest. |
| RN-EPIC-004-003 | Limitações físicas informadas devem filtrar exercícios contraindicados do catálogo na geração de treino. |
| RN-EPIC-004-004 | Dores físicas informadas devem influenciar a geração e validação do treino (exercícios de risco para as áreas afetadas são filtrados). |
| RN-EPIC-004-005 | O tipo de corpo atual é usado para personalização visual do perfil e como dado complementar para a geração de treino. |
| RN-EPIC-004-006 | O usuário deve poder navegar entre as etapas para corrigir respostas antes de concluir. |
| RN-EPIC-004-007 | O usuário deve revisar o perfil antes de salvar. |
| RN-EPIC-004-008 | O perfil deve poder ser editado posteriormente por usuário com acesso ativo. |
| RN-EPIC-004-009 | Sexo biológico é campo de entrada livre; nenhuma opção predefinida é imposta. |
| RN-EPIC-004-010 | Ao concluir o onboarding, o sistema deriva atributos iniciais, RankScore e Rank inicial, com Level 1. |
| RN-EPIC-004-011 | O Rank inicial pelo onboarding tem teto Rank B (RankScore máximo 48); Ranks A+ exigem treino real (EPIC-009). |

## 8. Impactos técnicos

### Flutter

- Fluxo multi-step de 8 etapas com barra de progresso (1/8 a 8/8).
- Componentes de seleção única, seleção múltipla, campos de entrada e seleção visual (silhuetas).
- Validações por etapa antes de avançar.
- Navegação para frente e para trás.
- Tela de revisão final.
- Persistência de estado parcial, se possível.

### Backend

- Endpoint para salvar perfil inicial.
- Endpoint para atualizar perfil.
- Validação dos campos obrigatórios das 8 etapas.
- Normalização de respostas para geração de treino.

### Banco de dados

Entidade principal: UserProfile.

Campos relevantes:

- goal (objetivo: ganhar_massa | perder_peso | condicionamento | mais_forca | manter_forma).
- experienceLevel (sedentario | iniciante | intermediario | avancado).
- trainingDuration (nao_treino | menos_1_mes | 1_6_meses | 6_12_meses | mais_1_ano | mais_3_anos).
- age.
- heightCm.
- weightKg.
- biologicalSex (campo livre).
- bodyType (magro | normal | gordo | atletico_forte).
- availableMinutesPerWorkout (5_10 | 10_20 | 20_30 | 30_40 | 40_50).
- physicalLimitations (lista de limitações físicas que filtram o catálogo; opções a definir com EPIC-005).
- physicalPains (lista: pescoço | ombro | pulso | costas | lombar | joelhos).
- onboardingCompletedAt.

### Analytics

- `onboarding_started`.
- `onboarding_step_completed` (com propriedade `step`: 1 a 8).
- `onboarding_completed`.

### QA

- Fluxo completo das 8 etapas.
- Campos obrigatórios validados por etapa.
- Navegar para trás e alterar respostas.
- Selecionar tipo de corpo (silhuetas) em etapa 5.
- Selecionar múltiplas limitações físicas em etapa 7.
- Selecionar múltiplas dores físicas em etapa 8.
- Verificar que exercícios contraindicados são filtrados conforme limitações informadas.
- Salvar perfil.
- Tentar gerar quest sem onboarding completo.
- Testes em PT-BR, EN e ES.

## 9. Dependências

- EPIC-001.
- EPIC-002.
- EPIC-003.
- EPIC-005 para compatibilidade com catálogo e definição das opções de limitações físicas.
- EPIC-006 para geração da quest.
- EPIC-009 para o cálculo de Rank/RankScore (`calculateRank`, curva e teto) usado no Rank inicial.

## 10. Critérios de aceite do épico

- Usuário completa onboarding de 8 etapas sem ambiguidades.
- Barra de progresso reflete etapa atual.
- Perfil salvo contém dados suficientes para gerar quest.
- Sistema bloqueia quest se onboarding estiver incompleto.
- Limitações físicas filtram exercícios contraindicados do catálogo.
- Dores físicas ficam disponíveis para filtro do treino.
- Usuário consegue revisar antes de concluir.
- Usuário consegue editar perfil após onboarding.

## 11. Decisão registrada

O onboarding do AWAKEN é um fluxo fixo de 8 etapas, definido pelos wireframes aprovados. Local de treino e equipamentos não são coletados no onboarding — o sistema gera quests sem equipamento por padrão. Limitações físicas (etapa 7) e dores físicas (etapa 8) são coletadas separadamente: limitações filtram o catálogo de exercícios, dores orientam a geração do treino. Essa decisão garante segurança na personalização sem comprometer a velocidade do onboarding.
