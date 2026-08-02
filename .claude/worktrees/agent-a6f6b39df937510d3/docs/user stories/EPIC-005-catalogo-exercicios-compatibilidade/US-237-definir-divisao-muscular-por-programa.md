---
title: US-237 — Definir a divisão muscular de cada dia por tipo de programa (split map)
sidebar_position: 237
---

# US-237 — Definir a divisão muscular de cada dia por tipo de programa (split map)

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-237 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR/EN/ES/FR apenas nos rótulos exibidos do dia (ex.: "Empurrar", "Puxar", "Pernas") |
| Dependência principal | US-231 (catálogo de programas), US-036 (grupo muscular), US-145 (padrão de movimento), US-236 (taxonomia) |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **conhecer, de forma determinística e legível por máquina, quais grupos musculares e padrões de movimento cada dia (letra) de cada programa (Full Body, AB, ABC, ABCD, ABCDE) deve treinar**,

para **saber exatamente qual é o alvo muscular do dia antes de filtrar e pontuar exercícios, sem depender de IA e mantendo coerência com a descrição publicada de cada programa (US-231)**.

---

## 3. Contexto

A US-231 define o **catálogo de programas** (`programKey`, nome, rank mínimo, categoria e descrição resumida), e a US-232 permite o usuário **selecionar** um programa. Porém, nenhuma US traduz a descrição textual de cada programa ("Push + Pull com pernas integradas", "Clássico das academias com maior divisão muscular", etc.) em um **mapa muscular executável** que a geração de quest (EPIC-006) possa consumir.

Sem esse mapa, o gerador não sabe que "hoje é o dia B do ABC = Puxar (costas + bíceps)". Esta US cria a **fonte de verdade da divisão (split map)**: para cada `programKey`, a lista ordenada de dias (letras), e para cada dia, os grupos musculares-alvo, os padrões de movimento-alvo e o papel do dia (empurrar/puxar/pernas/corpo inteiro).

O mapa é ancorado nas classificações já existentes no catálogo — `PrimaryMuscleGroups`/`bodyPart`/`target` (US-036) e `MovementPattern` (US-145/US-236) — para que o alvo do dia seja diretamente comparável a cada exercício aprovado. A escolha das divisões segue divisões clássicas e cientificamente aceitas (Push/Pull/Legs, Upper/Lower integrado, splits por grupo), sem inventar terminologia nova.

Esta US define **o que cada dia treina**; **qual dia é hoje** (rotação sobre o histórico) é a US-238; **as regras de recuperação/anti-sobrecarga** são a US-239; e **a composição final do conjunto elegível do dia** é a US-240.

---

## 4. Objetivo

Persistir uma configuração versionada `TrainingProgramSplit` (1:1 com cada `programKey`) e `TrainingSplitDay` (N dias por programa), definindo, por dia: letra, rótulo i18n, papel, grupos musculares primários e secundários-alvo, padrões de movimento-alvo e faixa de exercícios sugerida — coerente com a descrição da US-231.

---

## 5. Escopo

### Entra nesta US

- Definição canônica do split map dos 5 programas base: `full_body`, `ab`, `abc`, `abcd`, `abcde`.
- Estrutura `TrainingProgramSplit` (por programa) e `TrainingSplitDay` (por dia/letra).
- Mapeamento de cada dia para `targetMuscleGroups` (enum interno de US-036) e `targetMovementPatterns` (enum de US-145).
- Papel do dia (`role`: `full_body`, `push`, `pull`, `legs`, `chest`, `back`, `shoulders`, `arms`, `legs_focus`).
- Marcação de núcleo/`core` como finalizador permitido por dia.
- Rótulos i18n dos dias (chave estruturada, sem texto solto).
- Seed determinístico e versionado (`splitMapVersion`).

### Fora desta US

- Determinar o dia atual do usuário a partir do histórico (US-238).
- Regras de recuperação e anti-sobrecarga (US-239).
- Composição do conjunto elegível e orçamento de volume do dia (US-240).
- Filtro eliminatório de segurança (EPIC-006, US-045) e pontuação (US-151).
- Programas `perfect_2` e `system` da US-231 (tratados fora do split clássico; podem reusar esta estrutura em US futura).
- Prescrição de séries/reps (US-153).

---

## 6. Divisão muscular canônica (split map)

> Grupos referenciam os enums de grupo muscular (US-036) e `bodyPart`/`target` do catálogo; padrões referenciam `movementPattern` (US-145/US-236). `core` é sempre permitido como finalizador curto (1 exercício), salvo dias cuja função já é core.

### 6.1. `full_body` — Corpo inteiro em cada sessão

| Dia | Letra | Papel | Grupos-alvo (primários) | Padrões-alvo |
|---|---|---|---|---|
| 1 | FB | `full_body` | Peito, Costas, Pernas (quadríceps + posterior/glúteo), Ombros, Core | Um de cada: empurrar (`horizontal_push`/`vertical_push`), puxar (`horizontal_pull`/`vertical_pull`), agachamento/avanço (`squat`/`lunge`), dobradiça (`hinge`), `core_*` |

Full Body possui **um único dia lógico** repetido a cada sessão: todo padrão principal é tocado uma vez. A variação diária de ênfase (empurrar/puxar/pernas) e a rotação de exercícios para respeitar recuperação são responsabilidade da US-239, não do split map.

### 6.2. `ab` — Push + Pull com pernas integradas

| Dia | Letra | Papel | Grupos-alvo (primários) | Padrões-alvo |
|---|---|---|---|---|
| 1 | A | `push` | Peito, Ombros, Tríceps, Quadríceps, Panturrilha | `horizontal_push`, `vertical_push`, `squat`, `lunge`, `core_*` |
| 2 | B | `pull` | Costas, Bíceps, Deltoide posterior, Posterior de coxa, Glúteo | `horizontal_pull`, `vertical_pull`, `hinge`, `core_*` |

As pernas ficam **integradas** entre os dois dias: quadríceps/panturrilha no dia de empurrar, posterior/glúteo no dia de puxar (dobradiça), conforme a descrição da US-231.

### 6.3. `abc` — Push / Pull / Legs (clássico das academias)

| Dia | Letra | Papel | Grupos-alvo (primários) | Padrões-alvo |
|---|---|---|---|---|
| 1 | A | `push` | Peito, Ombros, Tríceps | `horizontal_push`, `vertical_push`, `core_*` |
| 2 | B | `pull` | Costas, Bíceps, Deltoide posterior, Trapézio | `horizontal_pull`, `vertical_pull` |
| 3 | C | `legs` | Quadríceps, Posterior de coxa, Glúteo, Panturrilha, Core | `squat`, `hinge`, `lunge`, `core_*` |

### 6.4. `abcd` — Divisão por grupo (intermediário/avançado)

| Dia | Letra | Papel | Grupos-alvo (primários) | Padrões-alvo |
|---|---|---|---|---|
| 1 | A | `chest` | Peito, Tríceps | `horizontal_push`, `vertical_push` |
| 2 | B | `back` | Costas, Bíceps, Deltoide posterior | `horizontal_pull`, `vertical_pull` |
| 3 | C | `legs` | Quadríceps, Posterior de coxa, Glúteo, Panturrilha | `squat`, `hinge`, `lunge` |
| 4 | D | `shoulders` | Ombros, Trapézio, Core (antebraço/braço acessório) | `vertical_push`, `carry`, `core_*` |

### 6.5. `abcde` — Divisão avançada de alto volume (por grupo)

| Dia | Letra | Papel | Grupos-alvo (primários) | Padrões-alvo |
|---|---|---|---|---|
| 1 | A | `chest` | Peito, Tríceps (assistência) | `horizontal_push`, `vertical_push` |
| 2 | B | `back` | Costas, Trapézio, Deltoide posterior | `horizontal_pull`, `vertical_pull` |
| 3 | C | `legs` | Quadríceps, Posterior de coxa, Glúteo, Panturrilha | `squat`, `hinge`, `lunge` |
| 4 | D | `shoulders` | Ombros, Trapézio | `vertical_push`, elevações laterais/posteriores |
| 5 | E | `arms` | Bíceps, Tríceps, Antebraço, Core | isolamento de braço, `core_*` |

---

## 7. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Cada `programKey` clássico (`full_body`, `ab`, `abc`, `abcd`, `abcde`) deve ter exatamente 1, 2, 3, 4 e 5 dias respectivamente. |
| RN-002 | Cada `TrainingSplitDay` deve ter ao menos um grupo muscular primário-alvo e ao menos um padrão de movimento-alvo. |
| RN-003 | O split map é a única fonte de verdade sobre o alvo muscular por dia; a geração (EPIC-006) não pode inferir alvo por outro meio. |
| RN-004 | O mapa deve ser coerente com a descrição publicada do programa na US-231; divergência bloqueia o seed. |
| RN-005 | Os grupos e padrões-alvo referenciam apenas enums já existentes no catálogo (US-036, US-145); valor fora do enum bloqueia o seed. |
| RN-006 | `full_body` possui um único dia lógico; a variação de ênfase entre sessões é responsabilidade da US-239. |
| RN-007 | O split map é versionado (`splitMapVersion`); alterar a divisão gera nova versão sem quebrar quests já geradas. |
| RN-008 | `core` é permitido como finalizador curto em qualquer dia, exceto quando o próprio dia já é de core/abdômen. |
| RN-009 | Programas fora do escopo clássico (`perfect_2`, `system`) não são obrigados a ter split map nesta US e não devem bloquear os demais. |

---

## 8. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Define e versiona o split map (seed). |
| Usuário final | Sem acesso direto; consome indiretamente via divisão exibida na tela de programas (US-232) e via geração de quest. |

---

## 9. Fluxo principal

1. Sistema carrega a configuração de programas (US-231).
2. Para cada `programKey` clássico, cria `TrainingProgramSplit` e seus `TrainingSplitDay` conforme a seção 6.
3. Valida coerência com a descrição da US-231 e com os enums de grupo/padrão (RN-004, RN-005).
4. Persiste com `splitMapVersion`.
5. Disponibiliza consulta "dias de um programa" e "alvo de um dia" para US-238 e US-240.

---

## 10. Fluxos alternativos

### 10.1. Enum inexistente

Se um grupo/padrão-alvo não existir no enum do catálogo, o seed do programa falha e é reportado, sem persistir dado parcial.

### 10.2. Divergência com a descrição da US-231

Se o mapa contrariar a descrição publicada (ex.: AB sem pernas integradas), o seed é bloqueado para revisão.

### 10.3. Programa sem split clássico

`perfect_2`/`system` seguem sem split map obrigatório e não interrompem o seed dos demais.

---

## 11. Estados esperados

- split map versionado e ativo;
- seed bloqueado por enum inválido;
- seed bloqueado por divergência com US-231;
- programa sem split clássico (ignorado com aviso).

---

## 12. Impacto no Frontend Flutter

- A tela de seleção de programas (US-232) passa a exibir a divisão real (dias e alvo muscular por dia) a partir do split map, em vez de texto fixo.
- Rótulos dos dias vêm de chave i18n (ex.: `program.day.push`, `program.day.pull`, `program.day.legs`).

---

## 13. Impacto no Backend

- Seed/configuração `TrainingProgramSplit` + `TrainingSplitDay`.
- Serviço de consulta: `getDays(programKey)` e `getDayTarget(programKey, dayKey)`.
- Validação de coerência (US-231, enums de US-036/US-145).

---

## 14. Impacto no Banco de Dados

### `TrainingProgramSplit` (novo, 1:1 com programa)

`Id`, `ProgramKey`, `SplitMapVersion`, `DayCount`, `IsActive`, `CreatedAt`, `UpdatedAt`.

### `TrainingSplitDay` (novo, N por programa)

`Id`, `TrainingProgramSplitId`, `DayIndex` (1..N), `DayKey` (`A`..`E` ou `FB`), `LabelI18nKey`, `Role` (`full_body`|`push`|`pull`|`legs`|`chest`|`back`|`shoulders`|`arms`), `TargetMuscleGroups` (lista de enum US-036), `SecondaryMuscleGroups` (lista), `TargetMovementPatterns` (lista de enum US-145), `AllowsCoreFinisher` (bool), `MinExercises`, `MaxExercises`, `CreatedAt`, `UpdatedAt`.

---

## 15. Impacto em Gamificação

- Indireto: um alvo muscular claro por dia garante que a distribuição de XP de atributos (US-147) reflita o foco real do dia.

---

## 16. Impacto em Monetização

- Indireto: divisão coerente e explicável aumenta a confiança percebida no programa durante o trial.

---

## 17. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR/EN/ES/FR | Rótulos dos dias e papéis vêm de chaves i18n; os enums de grupo/padrão são internos e não traduzidos no dado. |

---

## 18. Contrato de API sugerido

```txt
GET /api/training-programs/{programKey}/split
```

Response conceitual:

```json
{
  "programKey": "abc",
  "splitMapVersion": "v1",
  "days": [
    {
      "dayKey": "A",
      "role": "push",
      "labelI18nKey": "program.day.push",
      "targetMuscleGroups": ["chest", "shoulders", "triceps"],
      "targetMovementPatterns": ["horizontal_push", "vertical_push"],
      "allowsCoreFinisher": true
    },
    {
      "dayKey": "B",
      "role": "pull",
      "labelI18nKey": "program.day.pull",
      "targetMuscleGroups": ["back", "biceps", "rear_delts", "traps"],
      "targetMovementPatterns": ["horizontal_pull", "vertical_pull"],
      "allowsCoreFinisher": false
    },
    {
      "dayKey": "C",
      "role": "legs",
      "labelI18nKey": "program.day.legs",
      "targetMuscleGroups": ["quadriceps", "hamstrings", "glutes", "calves"],
      "targetMovementPatterns": ["squat", "hinge", "lunge"],
      "allowsCoreFinisher": true
    }
  ]
}
```

---

## 19. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| training_split_map_seeded | Quando o split map é semeado/versionado. |
| training_split_map_validation_failed | Quando o seed falha por enum inválido ou divergência com US-231. |

---

## 20. Critérios de aceite

### CA-001 — Split map dos 5 programas

Dado o seed de programas,

Quando a configuração for aplicada,

Então `full_body`, `ab`, `abc`, `abcd` e `abcde` devem ter 1, 2, 3, 4 e 5 dias respectivamente, cada dia com grupos e padrões-alvo válidos.

### CA-002 — Coerência com a US-231

Dado o programa `ab`,

Quando o split map for consultado,

Então o dia A deve conter empurrar + pernas (quadríceps) e o dia B deve conter puxar + pernas (posterior/glúteo), refletindo "pernas integradas".

### CA-003 — Enum inválido bloqueia seed

Dado um dia com grupo-alvo fora do enum do catálogo,

Quando o seed rodar,

Então deve falhar e reportar sem persistir dado parcial.

---

## 21. Critérios de teste para QA

### Backend

- os 5 programas geram a quantidade correta de dias;
- cada dia tem ao menos 1 grupo e 1 padrão-alvo válidos;
- AB integra pernas nos dois dias; ABC segue Push/Pull/Legs;
- grupo/padrão fora do enum bloqueia o seed;
- versionamento não quebra quests já geradas.

### E2E

- a tela de programas (US-232) exibe a divisão real por dia a partir do split map, nos três idiomas.

---

## ✅ Decisão registrada

> A divisão muscular de cada dia de cada programa é uma configuração determinística e versionada (`TrainingProgramSplit`/`TrainingSplitDay`), ancorada nos enums de grupo muscular (US-036) e padrão de movimento (US-145/US-236) e coerente com a descrição publicada dos programas (US-231). Ela é a fonte de verdade sobre **o que** cada dia treina; **qual** dia é hoje (US-238), **a recuperação** (US-239) e **a composição do elegível** (US-240) são responsabilidades separadas.
