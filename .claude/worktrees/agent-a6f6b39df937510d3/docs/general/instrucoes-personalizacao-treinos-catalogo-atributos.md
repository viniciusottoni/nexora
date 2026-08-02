# AWAKEN — Instruções de Personalização de Treinos, Catálogo de Exercícios e Atributos

> Documento de instrução para Product Owner, Backend, Frontend Flutter, IA de geração de treinos, banco de dados e QA.  
> Objetivo: definir como o onboarding interfere na geração de treinos, como os exercícios devem ser importados/sanitizados a partir da ExerciseDB / Ascend API e como cada exercício contribui para os atributos do personagem.

---

## 1. Objetivo deste documento

Este documento consolida as regras de produto e implementação para o sistema de personalização de treinos do **AWAKEN**.

Ele define:

- como cada resposta do onboarding interfere no treino;
- como cada resposta interfere na seleção dos exercícios;
- como o catálogo de exercícios deve ser estruturado;
- como importar exercícios da ExerciseDB / Ascend API;
- como sanitizar os exercícios importados;
- quais metadados cada exercício deve possuir;
- como cada exercício contribui para atributos do personagem;
- como funciona o XP de atributo;
- como o usuário evolui atributos reais;
- como o gerador deve filtrar exercícios de acordo com perfil, objetivo, dores e limitações;
- quais regras são obrigatórias para um exercício ficar disponível para geração de quests.

---

## 2. Princípio central de personalização

O onboarding **não deve escolher um treino fixo**.

O onboarding deve gerar:

1. filtros de segurança;
2. limites de intensidade;
3. nível efetivo do usuário;
4. prioridades por objetivo;
5. restrições por dor e limitação;
6. restrições por tempo disponível;
7. parâmetros iniciais de progressão;
8. pesos para escolha dos exercícios;
9. base inicial para evolução dos atributos.

A personalização real deve acontecer em duas etapas:

```txt
1. Personalização inicial baseada no onboarding.
2. Personalização contínua baseada no comportamento real do usuário.
```

O sistema deve aprender com:

- exercícios concluídos;
- exercícios pulados;
- exercícios substituídos;
- carga usada;
- repetições concluídas;
- tempo concluído;
- dor relatada;
- dificuldade percebida;
- RPE percebido;
- preferência por regressão ou progressão;
- aderência ao treino;
- streak;
- evolução semanal.

---

## 3. Base científica usada como referência

As regras abaixo devem seguir princípios consolidados de prescrição de exercício, especialmente:

- combinação de treino aeróbico e fortalecimento muscular;
- progressão gradual;
- controle de intensidade;
- respeito a dores e limitações;
- adaptação ao nível do usuário;
- uso de exercício de força para saúde, massa muscular, força e manutenção funcional;
- uso de cardio/condicionamento para capacidade cardiorrespiratória e gasto energético;
- uso de mobilidade, equilíbrio e core para controle corporal e prevenção de sobrecarga.

Referências conceituais usadas:

```txt
WHO — Physical Activity Guidelines
https://www.who.int/initiatives/behealthy/physical-activity

ACSM — Physical Activity Guidelines
https://acsm.org/education-resources/trending-topics-resources/physical-activity-guidelines/

ACSM — Progression Models in Resistance Training for Healthy Adults
https://pubmed.ncbi.nlm.nih.gov/19204579/

CDC — Adult BMI Categories
https://www.cdc.gov/bmi/adult-calculator/bmi-categories.html

NHS — Physical Activity Guidelines for Older Adults
https://www.nhs.uk/live-well/exercise/physical-activity-guidelines-older-adults/

ExerciseDB / Ascend API
https://github.com/exercisedb/exercisedb-api
https://docs.ascendapi.com/introduction
```

---

## 4. Regra de segurança máxima

O sistema nunca deve escolher um exercício apenas porque ele é bom para o objetivo.

A ordem de prioridade deve ser:

```txt
1. Segurança
2. Compatibilidade com limitações e dores
3. Compatibilidade com nível
4. Compatibilidade com tempo disponível
5. Compatibilidade com objetivo
6. Potencial de evolução
7. Variedade e aderência
8. Recompensa de XP e atributos
```

Regra obrigatória:

```txt
Gamificação nunca pode superar segurança.
```

O sistema não deve incentivar o usuário a "forçar" dor em troca de XP, rank, streak ou evolução de atributo.

---

# PARTE 1 — ONBOARDING E PERSONALIZAÇÃO

---

## 5. Introdução do onboarding

### Campo

```txt
Sem campo de formulário.
```

### Função

A introdução apenas inicia o fluxo.

### Interferência no treino

Não interfere diretamente no treino.

### Regra de produto

A tela deve explicar que as respostas impactam:

- segurança;
- nível do treino;
- tipos de exercício;
- intensidade;
- duração;
- progressão;
- XP;
- atributos;
- recomendações futuras.

### Decisão

```txt
A introdução não gera dados de treino, mas prepara o usuário para responder com sinceridade.
```

---

## 6. Objetivo principal — `goal`

### Opções

```txt
ganhar massa
perder peso
condicionamento
mais força
manter a forma
```

O objetivo principal define a prioridade fisiológica do treino.

---

### 6.1 `ganhar massa`

#### Interferência no treino

Priorizar:

- treino resistido;
- hipertrofia;
- progressão de volume;
- progressão de carga;
- exercícios multiarticulares;
- exercícios por grupo muscular;
- descanso suficiente;
- repetição controlada;
- consistência semanal.

#### Interferência nos exercícios

Preferir exercícios com:

```txt
goalTags: hypertrophy, strength
exerciseType: strength
movementPattern: squat, hinge, push, pull, lunge, carry
attributeFocus: strength, vitality, focus
```

Evitar como prioridade principal:

- cardio excessivo;
- circuitos muito longos;
- alto volume de condicionamento que atrapalhe recuperação;
- exercícios de baixa sobrecarga muscular como única base.

#### Atributos favorecidos

```txt
Força
Vitalidade
Foco
Sabedoria
```

---

### 6.2 `perder peso`

#### Interferência no treino

Priorizar:

- gasto energético;
- força full body;
- preservação de massa muscular;
- cardio seguro;
- condicionamento progressivo;
- circuitos de baixo ou médio impacto;
- aderência e frequência.

#### Interferência nos exercícios

Preferir exercícios com:

```txt
goalTags: fat_loss, conditioning, maintenance
exerciseType: strength, cardio, conditioning
calorieCostLevel: medium/high
impactLevel: compatible with user
```

Se houver sobrepeso, obesidade, sedentarismo ou dor articular, o sistema deve evitar:

- saltos;
- corrida intensa no início;
- burpee completo;
- pliometria;
- movimentos rápidos no solo;
- exercícios com alto impacto em joelho, tornozelo ou lombar.

#### Atributos favorecidos

```txt
Resistência
Vitalidade
Força
Agilidade
Sabedoria
```

---

### 6.3 `condicionamento`

#### Interferência no treino

Priorizar:

- capacidade cardiorrespiratória;
- resistência muscular;
- circuitos;
- exercícios por tempo;
- intervalos controlados;
- progressão de duração ou densidade.

#### Interferência nos exercícios

Preferir exercícios com:

```txt
goalTags: conditioning
exerciseType: cardio, conditioning, strength_endurance
attributeFocus: endurance, vitality, agility
```

Evitar:

- transformar todo treino em HIIT agressivo;
- alto impacto para usuários sedentários;
- exercícios complexos antes de técnica mínima.

#### Atributos favorecidos

```txt
Resistência
Vitalidade
Agilidade
Foco
Sabedoria
```

---

### 6.4 `mais força`

#### Interferência no treino

Priorizar:

- movimentos básicos;
- progressão objetiva;
- descanso maior;
- técnica;
- sobrecarga progressiva;
- menor número de exercícios principais;
- acessórios complementares.

#### Interferência nos exercícios

Preferir exercícios com:

```txt
goalTags: strength
exerciseType: strength
movementPattern: squat, hinge, push, pull, carry
technicalComplexity: compatible with experience
```

Para iniciantes, "força" deve significar:

```txt
técnica + controle + progressão segura
```

Para intermediários e avançados, pode significar:

```txt
mais carga + menos repetições + descanso maior + progressão planejada
```

#### Atributos favorecidos

```txt
Força
Foco
Vitalidade
Sabedoria
```

---

### 6.5 `manter a forma`

#### Interferência no treino

Priorizar equilíbrio:

- força;
- cardio moderado;
- mobilidade;
- core;
- variedade;
- aderência;
- baixo risco de dor excessiva.

#### Interferência nos exercícios

Preferir exercícios com:

```txt
goalTags: maintenance
exerciseType: strength, cardio, mobility, core
difficultyLevel: compatible
impactLevel: low/medium
```

#### Atributos favorecidos

```txt
Força
Resistência
Vitalidade
Foco
Sabedoria
```

---

## 7. Nível de experiência — `experienceLevel`

### Opções

```txt
sedentário
iniciante
intermediário
avançado
```

O nível de experiência controla:

- dificuldade;
- volume;
- intensidade;
- descanso;
- complexidade técnica;
- impacto;
- velocidade de progressão;
- permissões de exercício.

---

### 7.1 `sedentário`

#### Regra de treino

```txt
Baixo volume.
Baixa complexidade.
Baixo impacto.
Foco em aderência.
Foco em técnica.
Evitar falha muscular.
Evitar dor excessiva.
```

#### Prescrição inicial sugerida

```txt
RPE: 3 a 5
Séries: 1 a 2
Repetições: 6 a 12
Tempo por exercício: curto
Descanso: 45 a 90 segundos
Frequência: 2 a 3 quests por semana + movimento leve
```

#### Exercícios permitidos

- caminhada;
- agachamento assistido;
- sentar e levantar;
- flexão na parede;
- flexão inclinada;
- ponte de glúteo;
- remada com elástico leve;
- prancha curta no antebraço;
- mobilidade básica.

#### Exercícios a evitar

- burpee completo;
- salto;
- corrida intensa;
- HIIT agressivo;
- levantamento pesado;
- exercícios técnicos;
- movimentos rápidos no solo.

---

### 7.2 `iniciante`

#### Regra de treino

```txt
Full body.
Movimentos básicos.
Progressão lenta.
Foco em consistência.
Técnica antes de intensidade.
```

#### Prescrição inicial sugerida

```txt
RPE: 5 a 6
Séries: 2 a 3
Repetições: 8 a 15
Descanso: 45 a 90 segundos
Frequência: 2 a 3 treinos por semana
```

#### Exercícios permitidos

- agachamento livre ou assistido;
- flexão inclinada ou joelhos apoiados;
- remada baixa;
- supino máquina ou halter leve;
- ponte de glúteo;
- avanço curto;
- prancha;
- caminhada rápida;
- bike leve;
- mobilidade.

---

### 7.3 `intermediário`

#### Regra de treino

```txt
Volume moderado.
Progressão semanal.
Pode usar divisão simples.
Pode usar supersets.
Pode usar exercícios unilaterais.
Pode usar intervalos moderados.
```

#### Prescrição inicial sugerida

```txt
RPE: 6 a 8
Séries: 3 a 4
Repetições: 6 a 15
Descanso: 60 a 180 segundos conforme objetivo
Frequência: 3 a 4 treinos por semana
```

#### Exercícios permitidos

- exercícios básicos com carga;
- variações intermediárias;
- unilaterais;
- circuitos;
- cardio intervalado moderado;
- core intermediário.

---

### 7.4 `avançado`

#### Regra de treino

```txt
Maior volume.
Maior intensidade.
Progressão mais específica.
Periodização simples.
Maior liberdade de variação.
```

#### Prescrição inicial sugerida

```txt
RPE: 7 a 9
Séries: 3 a 5+
Repetições: 3 a 15 conforme objetivo
Descanso: maior para força, menor para condicionamento
Frequência: 4 a 5 treinos por semana
```

#### Exercícios permitidos

- compostos avançados;
- variações difíceis;
- maior carga;
- maior complexidade técnica;
- intervalos avançados;
- exercícios explosivos, se não houver contraindicação.

---

## 8. Há quanto tempo treina — `trainingDuration`

### Opções

```txt
não treino
menos de 1 mês
1 a 6 meses
6 a 12 meses
mais de 1 ano
mais de 3 anos
```

Este campo valida o nível informado e gera o `effectiveExperienceLevel`.

---

### Regras

| Resposta | Regra |
|---|---|
| não treino | Tratar como sedentário. |
| menos de 1 mês | Tratar como iniciante absoluto. |
| 1 a 6 meses | Iniciante em consolidação. |
| 6 a 12 meses | Iniciante avançado ou intermediário leve. |
| mais de 1 ano | Pode ser intermediário se desempenho confirmar. |
| mais de 3 anos | Pode liberar avançado se desempenho e ausência de limitações confirmarem. |

---

### Regra de conflito

Se houver conflito entre `experienceLevel` e `trainingDuration`, o sistema deve aplicar o nível mais seguro.

Exemplos:

```txt
experienceLevel: avançado
trainingDuration: não treino
effectiveExperienceLevel: sedentário
```

```txt
experienceLevel: intermediário
trainingDuration: menos de 1 mês
effectiveExperienceLevel: iniciante
```

---

## 9. Dados físicos básicos

### Campos

```txt
age
heightCm
weightKg
biologicalSex
```

Esses campos devem ajustar risco, impacto, progressão e estimativas, mas não devem gerar julgamento estético ou bloqueio indevido.

---

## 10. Idade — `age`

### Interferência

| Faixa | Regra |
|---|---|
| 16–17 | Tratar como adolescente. Priorizar técnica, coordenação e segurança. Evitar testes máximos. |
| 18–39 | Faixa adulta padrão. Personalização depende mais de objetivo, nível e dor. |
| 40–59 | Aumentar atenção a aquecimento, mobilidade, recuperação e progressão gradual. |
| 60+ | Priorizar força, equilíbrio, mobilidade, baixo impacto e recuperação. |

### Regras de segurança

Para usuários mais velhos, sedentários ou com condições médicas:

```txt
Exibir orientação de procurar avaliação profissional quando necessário.
Reduzir intensidade inicial.
Aumentar aquecimento.
Aumentar descanso.
Reduzir impacto.
Priorizar equilíbrio e mobilidade.
```

---

## 11. Altura e peso — `heightCm`, `weightKg`

### Cálculo

```txt
heightM = heightCm / 100
bmi = weightKg / (heightM * heightM)
```

### Uso do IMC

O IMC deve ser usado como triagem, não como diagnóstico.

| IMC aproximado | Regra |
|---|---|
| < 18.5 | Evitar excesso de cardio se objetivo não for condicionamento. Priorizar força, massa muscular e nutrição futura. |
| 18.5 a 24.9 | Sem restrição automática. |
| 25 a 29.9 | Reduzir impacto se sedentário ou com dor articular. |
| 30+ | Evitar saltos e alto impacto no início. Priorizar baixo impacto e progressão gradual. |

### Peso relativo em exercícios corporais

O peso corporal altera a dificuldade de exercícios como:

- flexão;
- agachamento;
- prancha;
- avanço;
- burpee;
- corrida;
- salto;
- mountain climber.

Regra:

```txt
Quanto maior o peso relativo e menor o nível, maior a chance de usar regressão.
```

Exemplo:

```txt
Flexão tradicional → flexão inclinada → flexão na parede.
```

---

## 12. Sexo biológico — `biologicalSex`

### Uso permitido

```txt
Estimativas energéticas futuras.
Cálculos nutricionais futuros.
Referências fisiológicas médias.
Alertas de saúde específicos, quando existirem.
```

### Uso proibido

```txt
Bloquear exercício por estereótipo.
Criar treino "masculino" ou "feminino" sem justificativa.
Assumir fragilidade.
Impedir progressão de força.
```

### Regra

No MVP de treino, `biologicalSex` deve ter baixo peso na seleção de exercícios.

A seleção deve depender principalmente de:

```txt
objetivo
nível
tempo de treino
dor
limitação
tempo disponível
desempenho real
```

---

## 13. Tipo de corpo atual — `bodyType`

### Opções

```txt
magro
normal
gordo
atlético/forte
```

Este campo deve ser tratado como autoimagem/proxy inicial, não como diagnóstico.

---

### Regras

| Resposta | Interferência |
|---|---|
| magro | Se objetivo for massa/força, priorizar hipertrofia e reduzir cardio excessivo. |
| normal | Usar objetivo como principal guia. |
| gordo | Preferir baixo impacto, força full body, cardio seguro e progressão gradual. |
| atlético/forte | Pode liberar variantes mais difíceis se experiência e histórico confirmarem. |

### Regra de conflito

Se `bodyType` conflitar com dados de execução, o sistema deve confiar mais em:

```txt
desempenho real
dor
limitação
nível efetivo
histórico de treino
```

---

## 14. Tempo disponível por treino — `availableMinutesPerWorkout`

### Opções

```txt
10
20
30
45
60
```

Esse campo define a arquitetura da sessão.

---

### Regras por tempo

| Tempo | Estrutura |
|---|---|
| 10 min | Micro quest. Poucos exercícios. Foco em consistência. |
| 20 min | Treino compacto. 3 a 5 exercícios. Pode usar circuito. |
| 30 min | Sessão padrão MVP. 5 a 7 exercícios. |
| 45 min | Treino completo. Aquecimento, principal e finalização. |
| 60 min | Treino completo com descanso adequado, maior volume ou divisão muscular. |

### Regra obrigatória

O tempo total deve incluir:

```txt
aquecimento
instruções
execução
descanso
troca de exercício
finalização
```

Um treino de 10 minutos não pode conter 8 exercícios com longos descansos.

---

## 15. Limitações físicas — `physicalLimitations`

### Tipo

```txt
Seleção múltipla.
A opção "não tenho limitações" limpa as outras.
```

### Função

Limitação física é filtro forte.

Se um exercício tiver tag incompatível com a limitação, ele deve ser:

```txt
removido
ou substituído por regressão segura
ou marcado como não elegível para aquele usuário
```

---

### Exemplos de regras por limitação

| Limitação | Evitar/regredir |
|---|---|
| Joelho | Saltos, agachamento profundo, avanço longo, corrida intensa, pliometria. |
| Lombar | Flexão lombar repetida, rotação carregada, terra pesado, sit-up, burpee. |
| Ombro | Overhead, handstand, flexão profunda, movimentos balísticos. |
| Punho | Flexão tradicional, prancha alta, mountain climber com punho estendido. |
| Tornozelo/pé | Corrida, salto, corda, avanço saltado. |
| Quadril | Avanço profundo, agachamento profundo, saltos, mobilidade agressiva. |
| Cervical | Abdominal puxando cabeça, ponte cervical, carga alta em trapézio. |
| Mobilidade reduzida | Movimentos sem apoio, saltos, exercícios complexos no solo. |

---

## 16. Dores físicas — `physicalPains`

### Tipo

```txt
Seleção múltipla.
A opção "não sinto dores" limpa as outras.
```

### Função

Dor física é ajuste imediato.

Limitação pode ser algo crônico ou estrutural.
Dor pode ser algo atual, transitório ou agudo.

O sistema deve tratar dor como prioridade alta.

---

### Regras por dor

| Dor | Regra |
|---|---|
| Joelho | Remover impacto e flexão profunda. |
| Lombar | Remover abdominais agressivos, saltos e flexão lombar repetida. |
| Ombro | Remover overhead e empurrar profundo. |
| Punho | Trocar apoio de mão por apoio de antebraço ou pegada neutra. |
| Tornozelo/pé | Remover corrida, salto e corda. |
| Pescoço/cervical | Evitar exercícios que forcem cervical. |

### Regra dinâmica

Se o usuário marcar dor durante a execução de um exercício, essa informação deve valer mais que o onboarding inicial.

```txt
Dor relatada durante treino > resposta antiga do onboarding.
```

---

## 17. Revisão final

A revisão final deve mostrar, nesta ordem:

```txt
objetivo
nível
tempo de treino
dados físicos
tipo de corpo
tempo disponível
limitações
dores
```

### Função

Antes de salvar, o sistema deve validar conflitos:

| Conflito | Ação |
|---|---|
| Avançado + não treino | Aplicar nível efetivo mais baixo. |
| Mais força + dor lombar | Remover cargas axiais pesadas no início. |
| Perder peso + IMC alto | Priorizar baixo impacto. |
| Ganhar massa + 10 min | Gerar micro quest e informar limitação prática. |
| Dor + exercício contraindicado | Bloquear exercício. |

---

# PARTE 2 — CATÁLOGO DE EXERCÍCIOS

---

## 18. Regra central do catálogo

Nenhum exercício deve entrar no gerador apenas porque veio da API externa.

A API externa é fonte bruta.

O AWAKEN precisa ter uma camada própria de:

```txt
normalização
tradução
sanitização
atributos
segurança
personalização
aprovação
```

---

## 19. Fonte dos exercícios

Os exercícios devem ser obtidos preferencialmente da:

```txt
ExerciseDB / Ascend API
```

A API deve fornecer dados suficientes para preencher a base interna, incluindo:

- nome;
- identificador externo;
- tipo de exercício;
- equipamentos;
- grupo muscular principal;
- músculos secundários;
- partes do corpo;
- instruções;
- dicas;
- imagem;
- vídeo;
- GIF;
- variações;
- exercícios relacionados;
- metadados de dificuldade, se disponíveis.

---

## 20. Fluxo de importação

```txt
1. Buscar exercícios na ExerciseDB / Ascend API.
2. Salvar resposta original em ExerciseRawImport.
3. Normalizar nomes, equipamentos, músculos e mídia.
4. Mapear grupos musculares para enums internos.
5. Criar registro em ExerciseCatalog.
6. Aplicar sanitização automática.
7. Aplicar enriquecimento de atributos.
8. Marcar como pending_review.
9. Revisar amostras ou regras críticas.
10. Aprovar como approved.
11. Liberar para geração de quests.
```

---

## 21. Não depender da API em tempo real

Regra recomendada para o MVP:

```txt
Importar e cachear os exercícios necessários.
Não depender da API externa em tempo real para gerar treino.
```

Motivos:

- evitar lentidão;
- evitar falha externa;
- reduzir custo;
- garantir consistência;
- permitir sanitização própria;
- permitir tags internas;
- permitir XP de atributos;
- permitir aprovação interna;
- permitir versionamento.

---

## 22. Atenção jurídica e de licença

Antes de copiar, armazenar ou distribuir mídia/dados da ExerciseDB / Ascend API, o projeto deve verificar:

```txt
termos de uso
licença
permissão de cache
permissão de redistribuição
limites de requisição
uso comercial
uso de imagens/vídeos/GIFs
obrigações de atribuição
```

O sistema deve manter rastreabilidade:

```txt
providerName
providerExerciseId
providerVersion
sourceUrl
importedAt
```

---

## 23. Tabela bruta — `ExerciseRawImport`

```txt
Id
ProviderName
ProviderExerciseId
ProviderVersion
RawJson
ImportedAt
ImportBatchId
SourceUrl
MediaBaseUrl
Status
ErrorMessage
```

### Status possíveis

```txt
imported
failed
normalized
pending_review
approved
rejected
deprecated
```

---

## 24. Tabela sanitizada — `ExerciseCatalog`

```txt
Id
ProviderName
ProviderExerciseId
NamePtBr
NameOriginal
Slug
DescriptionPtBr
OverviewOriginal
InstructionsPtBr
InstructionsOriginal
TipsPtBr
TipsOriginal
ExerciseType
MovementPattern
DifficultyLevel
TechnicalComplexity
ImpactLevel
Environment
RequiredEquipment
PrimaryMuscleGroups
SecondaryMuscleGroups
BodyParts
JointStressTags
ContraindicationTags
LimitationBlockTags
PainBlockTags
GoalTags
MinExperienceLevel
SuitableForSedentary
SuitableForBeginner
SuitableForIntermediate
SuitableForAdvanced
RegressionExerciseId
ProgressionExerciseId
RelatedExerciseIds
VideoUrl
ImageUrl
GifUrl
MediaLicenseInfo
SanitizationStatus
IsApprovedForWorkoutGeneration
CreatedAt
UpdatedAt
```

---

## 25. Campos essenciais do exercício

| Campo | Uso |
|---|---|
| `id` | Identificação interna. |
| `providerName` | Fonte do exercício. |
| `providerExerciseId` | ID externo. |
| `namePtBr` | Nome exibido ao usuário. |
| `nameOriginal` | Nome vindo da API. |
| `exerciseType` | força, cardio, mobilidade, flexibilidade, equilíbrio, core. |
| `environment` | casa, academia ou ambos. |
| `requiredEquipment` | equipamento necessário. |
| `primaryMuscleGroups` | músculos principais. |
| `secondaryMuscleGroups` | músculos secundários. |
| `movementPattern` | padrão de movimento. |
| `difficultyLevel` | dificuldade geral 1–5. |
| `technicalComplexity` | complexidade técnica 1–5. |
| `impactLevel` | impacto 0–5. |
| `jointStressTags` | articulações exigidas. |
| `contraindicationTags` | contraindicações práticas. |
| `goalTags` | objetivos compatíveis. |
| `minExperienceLevel` | nível mínimo. |
| `regressionExerciseId` | variação mais fácil. |
| `progressionExerciseId` | variação mais difícil. |
| `videoUrl` | vídeo de execução. |
| `imageUrl` | imagem demonstrativa. |
| `gifUrl` | GIF demonstrativo. |

---

## 26. Tags obrigatórias

### `goalTags`

```txt
hypertrophy
fat_loss
conditioning
strength
maintenance
```

### `movementPattern`

```txt
squat
hinge
horizontal_push
vertical_push
horizontal_pull
vertical_pull
lunge
carry
core_flexion
core_anti_extension
core_anti_rotation
locomotion
jump
balance
mobility
```

### `riskTags`

```txt
knee_high_stress
lumbar_high_stress
shoulder_high_stress
wrist_high_stress
ankle_high_stress
hip_high_stress
cervical_high_stress
high_impact
high_technical_complexity
requires_spotter
requires_load_control
```

### `accessibilityTags`

```txt
beginner_safe
sedentary_safe
low_impact
no_equipment
small_space
chair_supported
floor_required
wrist_neutral_possible
knee_friendly
back_friendly
```

---

## 27. Sanitização obrigatória

Todo exercício importado deve passar pelas validações abaixo.

| Validação | Regra |
|---|---|
| Nome | Não pode ser vazio, duplicado ou incompreensível. |
| Músculo principal | Deve existir pelo menos 1 grupo muscular principal. |
| Mídia | Deve ter vídeo, GIF ou imagem. Preferência: vídeo. |
| Instruções | Deve ter instrução mínima de execução. |
| Equipamento | Deve ser mapeado para enum interno. |
| Tipo | Deve ser força, cardio, mobilidade, flexibilidade, equilíbrio ou core. |
| Dificuldade | Deve receber nível 1–5. |
| Impacto | Deve receber impacto 0–5. |
| Articulações | Deve indicar articulações exigidas. |
| Limitações | Deve ter tags de bloqueio quando necessário. |
| Objetivo | Deve ter afinidade com pelo menos 1 objetivo do onboarding. |
| Atributos | Deve gerar Sabedoria + pelo menos 1 atributo adicional. |

---

## 28. Critérios para aprovação do exercício

Um exercício só pode ficar `approved` quando:

```txt
Tem nome PT-BR.
Tem grupo muscular principal.
Tem tipo de exercício.
Tem equipamento mapeado.
Tem mídia válida.
Tem instrução de execução.
Tem nível mínimo.
Tem impacto definido.
Tem tags de articulação.
Tem tags de limitação/dor quando necessário.
Tem goalTags.
Tem attributeContribution.
Tem wisdomXp >= 1.
Tem pelo menos um atributo além de Sabedoria com XP > 0.
```

---

# PARTE 3 — ATRIBUTOS E XP

---

## 29. Atributos do AWAKEN

Os atributos do personagem são:

```txt
Força
Agilidade
Resistência
Vitalidade
Foco
Sabedoria
```

---

## 30. Regra central de atributos

Todo exercício concluído deve contribuir para atributos.

Regra obrigatória:

```txt
Todo exercício dá +1 XP de Sabedoria.
Todo exercício deve contribuir com pelo menos 1 outro atributo.
Cada 10 XP em um atributo gera +1 ponto real naquele atributo.
```

---

## 31. Diferença entre XP geral e XP de atributo

| Tipo | Uso |
|---|---|
| XP geral | Evolui level, rank e progressão geral do Hunter. |
| XP de atributo | Evolui Força, Agilidade, Resistência, Vitalidade, Foco e Sabedoria. |

Exemplo:

```txt
Usuário concluiu flexão inclinada.

Ganha:
+20 XP geral
+2 XP de Força
+1 XP de Resistência
+1 XP de Foco
+1 XP de Sabedoria
```

---

## 32. Conversão de XP em ponto real

### Regra

```txt
attributeRealPoints += floor(attributeXp / 10)
attributeXp = attributeXp % 10
```

### Exemplo

```txt
Força atual: 12 pontos reais
XP de Força atual: 8/10

Exercício gera +5 XP de Força.

Novo XP bruto: 13
Pontos ganhos: floor(13 / 10) = 1
XP restante: 13 % 10 = 3

Resultado:
Força real: 13
XP de Força: 3/10
```

---

## 33. Tabela de atributos do usuário

### `UserAttributes`

```txt
UserId
StrengthPoints
StrengthXp
AgilityPoints
AgilityXp
EndurancePoints
EnduranceXp
VitalityPoints
VitalityXp
FocusPoints
FocusXp
WisdomPoints
WisdomXp
UpdatedAt
```

---

## 34. Tabela de histórico de ganho de atributos

### `UserAttributeXpLog`

```txt
Id
UserId
WorkoutId
QuestId
ExerciseId
AttributeName
XpEarned
Reason
CreatedAt
```

### `Reason`

```txt
exercise_completed
exercise_partially_completed
exercise_feedback_given
exercise_regression_selected
exercise_progression_completed
pain_feedback_given
manual_adjustment
```

---

## 35. Tabela de contribuição do exercício

### `ExerciseAttributeContribution`

```txt
Id
ExerciseCatalogId
PrimaryAttribute
StrengthXp
AgilityXp
EnduranceXp
VitalityXp
FocusXp
WisdomXp
IsAutoGenerated
ReviewedBy
ReviewedAt
CreatedAt
UpdatedAt
```

---

## 36. Validações de contribuição

```txt
WisdomXp deve ser sempre >= 1.
PrimaryAttribute não pode ser wisdom.
Pelo menos um atributo além de WisdomXp deve ser > 0.
Nenhum atributo comum deve gerar XP exagerado sem regra especial.
```

Limite recomendado no MVP:

```txt
Exercício comum: 1 a 3 XP no atributo principal.
Exercício complexo/intenso: até 4 XP, apenas para intermediário/avançado.
Sabedoria: sempre 1 XP fixo.
```

---

## 37. Matriz base de contribuição por tipo de exercício

| Tipo de exercício | Força | Agilidade | Resistência | Vitalidade | Foco | Sabedoria |
|---|---:|---:|---:|---:|---:|---:|
| Musculação pesada | 3 | 0 | 1 | 1 | 1 | 1 |
| Calistenia básica | 2 | 1 | 1 | 1 | 1 | 1 |
| Cardio moderado | 0 | 1 | 3 | 2 | 0 | 1 |
| HIIT seguro | 1 | 2 | 3 | 2 | 1 | 1 |
| Mobilidade | 0 | 1 | 0 | 1 | 2 | 1 |
| Alongamento | 0 | 0 | 0 | 1 | 2 | 1 |
| Core estático | 1 | 0 | 1 | 1 | 2 | 1 |
| Equilíbrio | 0 | 2 | 0 | 1 | 2 | 1 |
| Técnica/coordenação | 0 | 2 | 1 | 0 | 2 | 1 |
| Exercício respiratório | 0 | 0 | 1 | 2 | 2 | 1 |

---

## 38. Mapeamento automático por tipo de exercício

```txt
STRENGTH → Força
CARDIO → Resistência / Vitalidade
MOBILITY → Foco / Vitalidade
FLEXIBILITY → Foco / Vitalidade
BALANCE → Agilidade / Foco
CORE → Foco / Força / Resistência
PLYOMETRIC → Agilidade / Resistência / Vitalidade
```

---

## 39. Mapeamento automático por padrão de movimento

```txt
squat → Força + Vitalidade
hinge → Força + Foco
push → Força + Foco
pull → Força + Foco
lunge → Força + Agilidade
carry → Força + Vitalidade
run/walk/cycle → Resistência + Vitalidade
jump → Agilidade + Resistência
plank/anti_rotation → Foco + Resistência
mobility_flow → Foco + Vitalidade
```

---

## 40. Exemplos de contribuição

### Flexão inclinada

```json
{
  "name": "Flexão inclinada",
  "primaryAttribute": "strength",
  "attributeXp": {
    "strength": 2,
    "agility": 0,
    "endurance": 1,
    "vitality": 1,
    "focus": 1,
    "wisdom": 1
  }
}
```

### Agachamento livre

```json
{
  "name": "Agachamento livre",
  "primaryAttribute": "strength",
  "attributeXp": {
    "strength": 2,
    "agility": 1,
    "endurance": 1,
    "vitality": 1,
    "focus": 1,
    "wisdom": 1
  }
}
```

### Caminhada rápida

```json
{
  "name": "Caminhada rápida",
  "primaryAttribute": "endurance",
  "attributeXp": {
    "strength": 0,
    "agility": 0,
    "endurance": 2,
    "vitality": 2,
    "focus": 0,
    "wisdom": 1
  }
}
```

### Prancha

```json
{
  "name": "Prancha",
  "primaryAttribute": "focus",
  "attributeXp": {
    "strength": 1,
    "agility": 0,
    "endurance": 1,
    "vitality": 1,
    "focus": 2,
    "wisdom": 1
  }
}
```

### Polichinelo adaptado

```json
{
  "name": "Polichinelo adaptado",
  "primaryAttribute": "endurance",
  "attributeXp": {
    "strength": 0,
    "agility": 1,
    "endurance": 2,
    "vitality": 2,
    "focus": 0,
    "wisdom": 1
  }
}
```

---

# PARTE 4 — GERAÇÃO DE TREINO

---

## 41. Filtro eliminatório

Antes de pontuar exercícios, o gerador deve remover exercícios incompatíveis.

Um exercício deve ser removido quando:

```txt
exercise.minExperienceLevel > user.effectiveExperienceLevel
exercise.requiredEquipment não está disponível
exercise.timeCostSeconds estoura o tempo do treino
exercise.contraindicationTags conflita com physicalLimitations
exercise.contraindicationTags conflita com physicalPains
exercise.impactLevel é alto e usuário é sedentário com IMC alto
exercise.technicalComplexity é alta e usuário é sedentário/iniciante
exercise.isApprovedForWorkoutGeneration = false
```

---

## 42. Pontuação de exercício

Depois do filtro de segurança, o exercício pode receber pontuação.

Exemplo conceitual:

```txt
exerciseScore =
  goalAffinityScore * 0.30
+ levelMatchScore * 0.20
+ safetyScore * 0.25
+ timeFitScore * 0.10
+ varietyScore * 0.05
+ progressionFitScore * 0.10
```

Para o AWAKEN, `safetyScore` deve ter peso alto.

---

## 43. Pontuação com atributos

O gerador deve considerar atributos baixos do usuário.

Exemplo:

```txt
Usuário:
Objetivo: ganhar massa
Força: baixa
Dor: nenhuma
Tempo: 30 min

Resultado:
Aumentar prioridade de exercícios com StrengthXp alto.
```

Outro exemplo:

```txt
Usuário:
Objetivo: condicionamento
Resistência baixa
Dor no joelho
Nível: iniciante

Resultado:
Priorizar EnduranceXp/VitalityXp com low_impact e sem knee_high_stress.
```

---

## 44. Fórmula com atributo-alvo

Exemplo:

```txt
exerciseScore =
  goalAffinityScore * 0.25
+ safetyScore * 0.25
+ levelMatchScore * 0.15
+ targetAttributeScore * 0.15
+ timeFitScore * 0.10
+ varietyScore * 0.05
+ progressionFitScore * 0.05
```

### `targetAttributeScore`

Deve aumentar quando:

```txt
O exercício contribui para um atributo baixo do usuário.
O exercício contribui para o atributo mais ligado ao objetivo.
O exercício não conflita com dores/limitações.
```

---

## 45. Prescrição inicial por perfil

### Sedentário

```txt
Frequência inicial: 2–3 quests/semana de força + movimento leve nos outros dias
Intensidade: RPE 3–5
Séries: 1–2
Reps: 8–12 ou tempo curto
Descanso: 45–90s
Evitar: saltos, falha muscular, alta complexidade, alto impacto
Objetivo do app: criar hábito e evitar dor excessiva
```

### Iniciante

```txt
Frequência: 2–3 treinos/semana
Intensidade: RPE 5–6
Séries: 2–3
Reps: 8–15
Descanso: 45–90s
Estrutura: full body
Progressão: aumentar reps antes de aumentar dificuldade/carga
```

### Intermediário

```txt
Frequência: 3–4 treinos/semana
Intensidade: RPE 6–8
Séries: 3–4
Reps: 6–15 conforme objetivo
Descanso: 60–180s conforme objetivo
Estrutura: full body, upper/lower ou push/pull/legs simplificado
```

### Avançado

```txt
Frequência: 4–5 treinos/semana
Intensidade: RPE 7–9
Séries: 3–5+
Reps: 3–15 conforme objetivo
Descanso: maior para força, menor para condicionamento
Estrutura: periodização simples, progressão por bloco, deload se necessário
```

---

## 46. Regras por objetivo

### Ganhar massa

```txt
Prioridade: força/hipertrofia
Estrutura: grupos musculares principais
Reps iniciais: 8–15
Descanso: moderado
Cardio: baixo a moderado
Progressão: aumentar volume, reps, carga ou dificuldade
Evitar: excesso de circuitos longos se prejudicar recuperação
Atributos-alvo: Força, Vitalidade, Foco
```

### Perder peso

```txt
Prioridade: aderência + gasto energético + preservação muscular
Estrutura: força full body + cardio/condicionamento
Reps: 10–20 ou tempo
Descanso: menor, mas seguro
Cardio: moderado ou intervalado conforme nível
Progressão: aumentar duração, densidade ou complexidade
Evitar: saltos e HIIT agressivo em sedentários, dor articular ou IMC alto
Atributos-alvo: Resistência, Vitalidade, Força
```

### Condicionamento

```txt
Prioridade: capacidade cardiorrespiratória e resistência muscular
Estrutura: circuitos, intervalos, exercícios por tempo
Reps: por tempo ou alto número controlado
Descanso: curto/moderado
Progressão: aumentar tempo de trabalho, reduzir descanso ou elevar intensidade
Evitar: transformar todo treino em impacto alto
Atributos-alvo: Resistência, Vitalidade, Agilidade
```

### Mais força

```txt
Prioridade: movimentos básicos e progressão objetiva
Estrutura: poucos exercícios principais + acessórios
Reps: mais baixas em intermediários/avançados; moderadas em iniciantes
Descanso: maior
Progressão: carga, alavanca, amplitude ou dificuldade técnica
Evitar: fadiga metabólica excessiva antes dos exercícios principais
Atributos-alvo: Força, Foco, Vitalidade
```

### Manter a forma

```txt
Prioridade: equilíbrio e consistência
Estrutura: força + cardio leve/moderado + mobilidade
Volume: moderado
Progressão: lenta
Variedade: alta
Evitar: treinos muito agressivos que prejudiquem aderência
Atributos-alvo: Força, Resistência, Vitalidade, Foco
```

---

# PARTE 5 — EXECUÇÃO, FEEDBACK E EVOLUÇÃO

---

## 47. Dados coletados após cada exercício

Ao finalizar cada exercício, salvar:

```txt
exerciseId
plannedSets
plannedReps
plannedDurationSeconds
completedSets
completedReps
completedDurationSeconds
usedLoadKg
userRpe
painDuringExercise
painLocation
difficultyFeedback
skippedReason
substitutedExerciseId
formConfidence
```

---

## 48. Valores de feedback

### `painDuringExercise`

```txt
none
mild
moderate
strong
```

### `difficultyFeedback`

```txt
easy
ok
hard
impossible
```

### `formConfidence`

```txt
low
medium
high
```

---

## 49. Como o feedback altera o próximo treino

| Feedback | Ação |
|---|---|
| Completou tudo e marcou fácil | Aumentar reps, tempo, carga ou dificuldade. |
| Completou com RPE adequado | Manter ou progredir levemente. |
| Falhou sem dor | Reduzir volume ou escolher regressão. |
| Sentiu dor leve | Reduzir amplitude, trocar variação ou diminuir impacto. |
| Sentiu dor moderada/forte | Remover exercício e região de estresse temporariamente. |
| Pulou repetidamente | Substituir por alternativa mais aderente. |
| Sempre escolhe regressão | Recalibrar nível efetivo para baixo naquele padrão. |
| Sempre escolhe progressão | Recalibrar nível efetivo para cima naquele padrão. |

---

## 50. Conclusão válida para XP de atributo

O usuário ganha XP de atributo quando:

```txt
marcou o exercício como concluído
não marcou dor forte
não pulou todas as séries
executou pelo menos o mínimo definido
```

---

## 51. Conclusão parcial

```txt
Ganha XP proporcional.
Pode ganhar Sabedoria se tentou executar e registrou feedback útil.
```

Exemplo:

```txt
Completou 100%:
+100% dos XP de atributo

Completou 50%:
+50% dos XP de atributo, arredondado conforme regra do backend

Pulou:
+0 XP de atributo

Tentou, falhou e informou feedback:
+1 Sabedoria
```

---

## 52. Regra de Sabedoria

Sabedoria representa:

```txt
aprendizado técnico
consciência corporal
consistência
capacidade de ajustar o treino corretamente
atenção à execução
feedback honesto
```

### Ganha +1 XP de Sabedoria quando:

```txt
Conclui um exercício.
Tenta executar e registra feedback válido.
Troca para uma regressão recomendada.
Marca dor corretamente.
Assiste/inicia instrução de execução antes do exercício, se essa mecânica existir.
```

### Não ganha Sabedoria quando:

```txt
Pula o exercício sem motivo.
Marca conclusão falsa.
Cancela o treino inteiro sem executar nada.
```

---

# PARTE 6 — EXEMPLO COMPLETO DE EXERCÍCIO IMPORTADO

---

## 53. Dado bruto vindo da API

```json
{
  "exerciseId": "exr_41n2hZZdH9uyYFGZ",
  "name": "Lever Pec Deck Fly",
  "equipments": ["LEVERAGE MACHINE"],
  "bodyParts": ["CHEST"],
  "exerciseType": "STRENGTH",
  "targetMuscles": ["Pectoralis Major Clavicular Head"],
  "secondaryMuscles": ["Deltoid Anterior"],
  "videoUrl": "Lever-Pec-Deck-Fly-Chest.mp4",
  "instructions": [
    "Sit on the pec deck machine with your back firmly against the pad...",
    "Push the levers together slowly..."
  ]
}
```

---

## 54. Exercício sanitizado no AWAKEN

```json
{
  "providerName": "ExerciseDB/AscendAPI",
  "providerExerciseId": "exr_41n2hZZdH9uyYFGZ",
  "namePtBr": "Voador na máquina",
  "exerciseType": "strength",
  "movementPattern": "horizontal_push",
  "environment": ["academia"],
  "requiredEquipment": ["maquina_voador"],
  "primaryMuscleGroups": ["peitoral"],
  "secondaryMuscleGroups": ["ombro_anterior"],
  "difficultyLevel": 2,
  "technicalComplexity": 2,
  "impactLevel": 0,
  "jointStressTags": ["ombro"],
  "contraindicationTags": ["dor_ombro_aguda"],
  "goalTags": ["ganhar_massa", "mais_forca", "manter_a_forma"],
  "minExperienceLevel": "iniciante",
  "suitableForSedentary": false,
  "videoUrl": "Lever-Pec-Deck-Fly-Chest.mp4",
  "attributeContribution": {
    "primaryAttribute": "strength",
    "strengthXp": 2,
    "agilityXp": 0,
    "enduranceXp": 0,
    "vitalityXp": 1,
    "focusXp": 1,
    "wisdomXp": 1
  },
  "isApprovedForWorkoutGeneration": true
}
```

---

# PARTE 7 — REGRAS DE NEGÓCIO CONSOLIDADAS

---

## 55. Regras P0 para o MVP

| ID | Regra |
|---|---|
| RN-001 | Todo exercício deve ser importado, sanitizado e aprovado antes de ser usado. |
| RN-002 | Nenhum exercício sem mídia pode ser aprovado, salvo exceção manual. |
| RN-003 | Nenhum exercício sem grupo muscular principal pode ser aprovado. |
| RN-004 | Nenhum exercício sem instrução pode ser aprovado. |
| RN-005 | Todo exercício aprovado deve ter contribuição de atributos. |
| RN-006 | Todo exercício aprovado deve gerar pelo menos +1 XP de Sabedoria. |
| RN-007 | Todo exercício aprovado deve gerar XP em pelo menos 1 atributo além de Sabedoria. |
| RN-008 | Cada 10 XP em um atributo deve gerar +1 ponto real. |
| RN-009 | Limitações físicas bloqueiam exercícios incompatíveis. |
| RN-010 | Dores físicas bloqueiam ou rebaixam exercícios incompatíveis. |
| RN-011 | O sistema deve calcular `effectiveExperienceLevel`. |
| RN-012 | O nível efetivo deve ser conservador quando houver conflito. |
| RN-013 | O tempo disponível deve limitar o número de exercícios e descansos. |
| RN-014 | O objetivo define prioridade, mas não supera segurança. |
| RN-015 | O feedback real do usuário deve ajustar treinos futuros. |
| RN-016 | XP e atributos não devem incentivar execução com dor forte. |
| RN-017 | O sistema deve manter rastreabilidade do provider externo. |
| RN-018 | O app não deve depender da API externa em tempo real para gerar treino. |

---

## 56. Critérios de aceite

### CA-001 — Exercício importado

Dado que existe um exercício na ExerciseDB / Ascend API,  
quando o sistema executar a rotina de importação,  
então deve salvar o JSON original em `ExerciseRawImport`.

### CA-002 — Exercício sanitizado

Dado que um exercício foi importado,  
quando passar pela sanitização,  
então deve gerar um registro em `ExerciseCatalog` com nome, músculos, equipamento, mídia, dificuldade, impacto, tags e instruções.

### CA-003 — Exercício sem atributo

Dado que um exercício não possui contribuição de atributo,  
quando o sistema tentar aprová-lo,  
então a aprovação deve ser bloqueada.

### CA-004 — Sabedoria obrigatória

Dado que um exercício será aprovado,  
quando o sistema validar sua contribuição,  
então `WisdomXp` deve ser maior ou igual a 1.

### CA-005 — Atributo principal obrigatório

Dado que um exercício será aprovado,  
quando o sistema validar sua contribuição,  
então pelo menos um atributo além de Sabedoria deve ter XP maior que 0.

### CA-006 — Conversão de XP

Dado que um usuário possui 8 XP de Força,  
quando concluir exercício que gera 5 XP de Força,  
então deve ganhar +1 ponto real de Força e ficar com 3 XP restantes.

### CA-007 — Limitação física

Dado que o usuário marcou limitação no joelho,  
quando o sistema gerar treino,  
então exercícios com `knee_high_stress` e alto impacto devem ser removidos ou substituídos.

### CA-008 — Dor física

Dado que o usuário marcou dor lombar,  
quando o sistema gerar treino,  
então exercícios com `lumbar_high_stress` devem ser removidos ou substituídos.

### CA-009 — Tempo disponível

Dado que o usuário tem 10 minutos disponíveis,  
quando o sistema gerar uma quest,  
então deve montar uma micro quest compatível com o tempo total.

### CA-010 — Feedback real

Dado que o usuário marcou dor forte durante um exercício,  
quando o sistema gerar o próximo treino,  
então deve reduzir ou remover exercícios semelhantes.

---

# PARTE 8 — EVENTOS DE ANALYTICS

---

## 57. Eventos recomendados

| Evento | Quando dispara |
|---|---|
| `onboarding_completed` | Usuário conclui onboarding. |
| `profile_constraints_saved` | Perfil físico e limitações são salvos. |
| `exercise_import_started` | Importação de exercícios inicia. |
| `exercise_import_completed` | Importação finaliza. |
| `exercise_sanitized` | Exercício passa pela sanitização. |
| `exercise_approved` | Exercício fica disponível. |
| `exercise_rejected` | Exercício é rejeitado. |
| `workout_generated` | Quest/treino é gerado. |
| `exercise_started` | Usuário inicia exercício. |
| `exercise_completed` | Usuário conclui exercício. |
| `exercise_skipped` | Usuário pula exercício. |
| `exercise_pain_reported` | Usuário relata dor. |
| `attribute_xp_earned` | Usuário ganha XP de atributo. |
| `attribute_point_increased` | Atributo sobe +1 ponto real. |
| `wisdom_xp_earned` | Usuário ganha XP de Sabedoria. |

---

# PARTE 9 — DECISÃO FINAL

O AWAKEN deve tratar exercícios como objetos inteligentes, não apenas como nomes em uma lista.

Cada exercício precisa ser capaz de responder:

```txt
Para quem ele serve?
Para quem ele não serve?
Qual objetivo ele ajuda?
Qual músculo trabalha?
Qual articulação exige?
Qual equipamento precisa?
Qual nível mínimo exige?
Qual variação mais fácil existe?
Qual variação mais difícil existe?
Qual vídeo mostra execução?
Qual atributo ele evolui?
Quanto XP de atributo ele concede?
Ele é seguro para este usuário específico?
```

A decisão final é:

```txt
A ExerciseDB / Ascend API será usada como fonte bruta de exercícios.
O AWAKEN terá uma camada própria de sanitização, segurança, tradução, atributos e aprovação.
Nenhum exercício será usado em quest sem estar aprovado.
Todo exercício aprovado contribuirá com Sabedoria + pelo menos 1 atributo principal.
Cada 10 XP acumulados em um atributo aumentará +1 ponto real no personagem.
```

---

## 10. Checklist final para implementação

```txt
[ ] Criar ExerciseRawImport
[ ] Criar ExerciseCatalog
[ ] Criar ExerciseAttributeContribution
[ ] Criar UserAttributes
[ ] Criar UserAttributeXpLog
[ ] Criar rotina de importação ExerciseDB / Ascend API
[ ] Criar rotina de normalização
[ ] Criar rotina de sanitização
[ ] Criar rotina de enriquecimento de atributos
[ ] Criar validação de aprovação
[ ] Criar cálculo de effectiveExperienceLevel
[ ] Criar filtro por limitações físicas
[ ] Criar filtro por dores físicas
[ ] Criar pontuação por objetivo
[ ] Criar pontuação por atributo-alvo
[ ] Criar regra de conversão 10 XP = +1 ponto real
[ ] Criar eventos de analytics
[ ] Criar testes de QA para importação
[ ] Criar testes de QA para geração de treino
[ ] Criar testes de QA para ganho de atributos
[ ] Criar testes de QA para bloqueio por dor/limitação
```

---

*Documento consolidado para o projeto AWAKEN.*
