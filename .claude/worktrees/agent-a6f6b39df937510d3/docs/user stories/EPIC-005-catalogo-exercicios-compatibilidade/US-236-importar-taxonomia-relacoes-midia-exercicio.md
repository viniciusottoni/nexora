---
title: US-236 — Importar taxonomia biomecânica, relações com score e mídia individual do exercício
sidebar_position: 236
---

# US-236 — Importar taxonomia biomecânica, relações com score e mídia individual do exercício

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-236 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável ao dado bruto; PT-BR/EN/ES/FR na exibição futura de nomes relacionados |
| Dependência principal | ExerciseCatalog (US-143/US-144 concluídas) |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **importar a taxonomia biomecânica, o grafo de relações com score (similares, substituições, progressões e regressões) e a mídia (GIF) individual de cada exercício a partir do dataset enriquecido**,

para **dar suporte à geração e à troca de exercícios de forma individual, com candidatos ranqueados e explicáveis, em vez de uma única variante fixa**.

---

## 3. Contexto

Além da API viva ExerciseDB / Ascend (US-143), o AWAKEN recebeu um dataset pré-processado (`exerciseData_complete_051426.json`, 1394 exercícios) contendo, para cada exercício: uma taxonomia biomecânica detalhada (`movementFamily`, `movementPattern`, `mechanic`, `forceType`, `planeOfMotion`, `laterality`, `bodyPosition`, `benchAngle`, `equipmentCategory`, `loadType`, `primaryRegion`, flags `isCompound`/`isUnilateral`/`isAssisted`/`isWeighted`, `signals` e `confidence`) e listas de exercícios relacionados (`similarExercises`, `substitutions`, `progressions`, `regressions`), cada um com `score`, `confidence` e `reasons` (e, exceto em `similarExercises`, `types` classificando o tipo de relação).

O dataset também vem acompanhado de mídia própria por exercício: um GIF de execução (`360/{id}.gif`), sob a licença do EULA assinado com o provider (`ExerciseDB_EULA_Updated_2026_signed.pdf`). O asset original também disponibiliza uma variante em 180p, mas o AWAKEN usará apenas a resolução 360.

Hoje o catálogo (US-039) só guarda uma única regressão e uma única progressão por exercício (`RegressionExerciseId`/`ProgressionExerciseId`). Isso é insuficiente para a geração individual de exercício exigida pelo ADR-012: quando o sistema precisa trocar um exercício específico (por dor, limitação, equipamento indisponível ou falta de progresso), ele precisa de **múltiplos candidatos ranqueados por score e confiança, com o motivo da sugestão**, não de um único link fixo.

---

## 4. Objetivo

Casar cada item do dataset com o `ExerciseCatalog` já normalizado (por `id`/`ProviderExerciseId`), persistir a taxonomia em `ExerciseTaxonomy`, persistir cada relação candidata em `ExerciseRelationship` e publicar o GIF em resolução 360 no storage de mídia, vinculando a URL ao exercício.

---

## 5. Escopo

### Entra nesta US

- Importação do dataset enriquecido versionado (arquivo local, distinto da API viva) com rastreabilidade (`datasetVersion`, `sourceFile`, `importedAt`).
- Casamento de cada item do dataset com o `ExerciseCatalog` existente pelo `id`.
- Persistência da taxonomia biomecânica em `ExerciseTaxonomy` (1:1 com o exercício).
- Persistência de múltiplos candidatos de `similarExercises`, `substitutions`, `progressions` e `regressions` em `ExerciseRelationship`, com `score`, `confidence`, `reasons` e `types`.
- Sincronização do candidato de maior score/confiança de progressão e regressão com os campos legados `ProgressionExerciseId`/`RegressionExerciseId` (US-039), para compatibilidade.
- Upload do GIF em resolução 360 para o storage de mídia (Cloudflare R2) e gravação da URL resultante no `ExerciseCatalog`. A variante 180p do asset original é descartada/não importada.
- Verificação de licença (EULA assinado) antes de publicar a mídia.
- Tratamento de itens sem correspondência no catálogo, sem interromper o lote.

### Fora desta US

- Importação da API viva ExerciseDB / Ascend (US-143) e normalização/tradução inicial (US-144).
- Classificação-base de grupo muscular, equipamento, dificuldade e objetivo (US-036/037/038/145/146) — a taxonomia aqui é um enriquecimento adicional, não substitui essas classificações.
- Lógica de pontuação e seleção de exercício em tempo de geração de quest (EPIC-006, US-151/152).
- Lógica de substituição em tempo real por dor/limitação (EPIC-007/008) — esta US apenas disponibiliza os candidatos ranqueados que essa lógica consome.
- Tradução dos textos livres de `reasons` para exibição ao usuário final.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O dataset enriquecido é importado como fonte adicional versionada, distinta da API viva (US-143), com rastreabilidade de `datasetVersion`/`sourceFile`/`importedAt`. |
| RN-002 | Cada item deve ser casado com um `ExerciseCatalog` existente pelo `id`; itens sem correspondência ficam pendentes e não bloqueiam os demais do lote. |
| RN-003 | `ExerciseTaxonomy` é 1:1 com o exercício; conflito com classificação já existente não é sobrescrito automaticamente — o exercício é sinalizado para revisão de divergência. |
| RN-004 | Cada relação (similar, substituição, progressão, regressão) é persistida como candidato independente em `ExerciseRelationship`, preservando múltiplos candidatos ordenáveis por `score`; nenhum candidato é descartado por haver outro de score maior. |
| RN-005 | O candidato de maior `score`/`confidence` de progressão e de regressão sincroniza os campos legados `ProgressionExerciseId`/`RegressionExerciseId` do `ExerciseCatalog`. |
| RN-006 | Nenhum exercício pode ser aprovado (`IsApprovedForWorkoutGeneration`) sem GIF válido em resolução 360, reforçando a RN-EPIC-005-005. |
| RN-007 | GIFs só podem ser publicados no storage de mídia após verificação da licença do EULA assinado com o provider. |
| RN-008 | O texto de `reasons` do dataset é de uso interno/admin; não pode ser exibido ao usuário final sem mapeamento para uma chave estruturada de i18n. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Executa a importação, revisa divergências e pendências. |
| Usuário final | Sem acesso direto; consome o resultado (GIF do exercício, candidatos de substituição) via geração, edição e troca de exercício. |

---

## 8. Fluxo principal

1. Sistema recebe o dataset enriquecido versionado e o diretório de mídia `360/`.
2. Para cada item, localiza o `ExerciseCatalog` correspondente pelo `id`.
3. Cria/atualiza `ExerciseTaxonomy` com os atributos biomecânicos do item.
4. Envia o GIF (360) do exercício para o storage de mídia e grava a URL resultante no `ExerciseCatalog`.
5. Para cada entrada de `similarExercises`, `substitutions`, `progressions` e `regressions`, resolve o exercício-alvo no catálogo e persiste um `ExerciseRelationship` com categoria, `types`, `score`, `confidence` e `reasons`.
6. Sincroniza os campos legados de maior score em `ExerciseCatalog`.
7. Finaliza o lote com relatório de itens processados e pendentes.

---

## 9. Fluxos alternativos

### 9.1. Exercício do dataset sem correspondência no catálogo

Se o `id` não existir em `ExerciseCatalog` (ainda não importado/aprovado), a taxonomia e as relações desse item ficam pendentes de resolução, sem interromper o restante do lote.

### 9.2. GIF ausente

O exercício não pode ser aprovado para geração de treino (RN-006) até o GIF 360 estar disponível.

### 9.3. Divergência de classificação

Se a taxonomia importada conflitar com uma classificação já registrada (US-036/037/038), o exercício é marcado para revisão manual em vez de ter o dado sobrescrito silenciosamente.

---

## 10. Estados esperados

- taxonomia importada;
- taxonomia pendente (sem correspondência no catálogo);
- relação resolvida;
- relação pendente (id de origem ou alvo não encontrado);
- mídia publicada;
- mídia pendente;
- divergência de classificação sinalizada.

---

## 11. Impacto no Frontend Flutter

- Tela de detalhe do exercício passa a exibir o GIF de execução (resolução única, 360).
- Tela de troca/substituição de exercício passa a listar candidatos ordenados por score, com indicação do tipo de relação (mais fácil, mais difícil, alternativa de equipamento etc.).

---

## 12. Impacto no Backend

- Job de importação do dataset enriquecido versionado, separado do job de importação da API viva (US-143).
- Resolvedor de `id` contra `ExerciseCatalog`.
- Uploader de mídia para Cloudflare R2 com checagem de licença.
- Serviço de consulta de candidatos por categoria (similar/substituição/progressão/regressão), ordenados por `score`, para consumo do motor de geração (EPIC-006) e da edição/substituição (EPIC-007).

---

## 13. Impacto no Banco de Dados

### `ExerciseTaxonomy` (novo, 1:1 com `ExerciseCatalog`)

`Id`, `ExerciseCatalogId`, `MovementFamily`, `MovementPattern`, `Mechanic`, `ForceType`, `PlaneOfMotion`, `Laterality`, `BodyPosition`, `BenchAngle`, `EquipmentCategory`, `LoadType`, `PrimaryRegion`, `IsCompound`, `IsUnilateral`, `IsAssisted`, `IsWeighted`, `Signals`, `Confidence`, `CreatedAt`, `UpdatedAt`.

### `ExerciseRelationship` (novo)

`Id`, `SourceExerciseCatalogId`, `TargetExerciseCatalogId`, `RelationCategory` (`similar` | `substitution` | `progression` | `regression`), `RelationTypes` (lista, ex.: `harder_alternative`, `easier_alternative`, `unilateral_progression`, `bilateral_regression`, `same_equipment_alternative`, `equipment_alternative`, `machine_alternative`), `Score`, `Confidence`, `Reasons`, `DatasetVersion`, `CreatedAt`.

### `ExerciseCatalog` (campo existente reaproveitado)

`GifUrl` passa a ser preenchido com a URL do GIF em resolução 360 publicado pela importação. Não há campo para resolução alternativa — apenas 360 é usada.

---

## 14. Impacto em Gamificação

- Indireto: candidatos de substituição/progressão com score alto preservam o padrão de movimento e, portanto, a intenção de atributo/XP do exercício original.

---

## 15. Impacto em Monetização

- Indireto: GIF de execução próprio por exercício reforça a percepção de qualidade e clareza mesmo no período de trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR/EN/ES/FR | Nomes dos exercícios relacionados seguem a tradução já feita na US-144; `reasons` não é exibido ao usuário final sem mapeamento (RN-008). |

---

## 17. Contrato de API sugerido

```txt
POST /api/admin/exercises/import-enriched-dataset
```

Request:

```json
{
  "datasetVersion": "051426",
  "sourceFile": "exerciseData_complete_051426.json"
}
```

Response conceitual:

```json
{
  "taxonomyApplied": 1390,
  "mediaUploaded": 1388,
  "relationshipsCreated": 8420,
  "pending": 4
}
```

```txt
GET /api/exercises/{id}/relationships?category=substitution
```

Response conceitual:

```json
[
  { "exerciseId": "1758", "name": "assisted sit-up", "score": 99.0, "confidence": "high" }
]
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_enriched_dataset_imported | Quando o lote de importação finaliza. |
| exercise_relationship_imported | Para cada candidato de relação persistido. |
| exercise_media_uploaded | Quando o GIF (360) é publicado no storage. |

---

## 19. Critérios de aceite

### CA-001 — Taxonomia aplicada

Dado um exercício existente no catálogo e presente no dataset,

Quando a importação rodar,

Então `ExerciseTaxonomy` deve ser criado/atualizado com os atributos biomecânicos do item.

### CA-002 — Relações com múltiplos candidatos

Dado um exercício com substitutos, progressões ou regressões no dataset,

Quando importado,

Então cada candidato deve ser persistido com `score`, `confidence` e `reasons`, sem descartar candidatos de score menor.

### CA-003 — Mídia obrigatória

Dado um exercício sem GIF em resolução 360,

Quando a aprovação for tentada,

Então deve ser bloqueada até a mídia estar disponível.

### CA-004 — Sem correspondência no catálogo

Dado um `id` do dataset sem exercício correspondente no catálogo,

Quando a importação rodar,

Então o item fica pendente e não interrompe o restante do lote.

---

## 20. Critérios de teste para QA

### Backend

- taxonomia é criada com todos os atributos biomecânicos do item;
- relações persistem múltiplos candidatos ordenáveis por `score`;
- sincronização dos campos legados aponta para o candidato de maior score;
- upload de mídia grava a URL do GIF em resolução 360;
- exercício sem GIF 360 não pode ser aprovado;
- item sem correspondência no catálogo não interrompe o lote.

### E2E

- tela de detalhe do exercício exibe o GIF correto;
- tela de troca de exercício sugere candidatos ordenados por score, sem vazar o texto em inglês de `reasons` ao usuário final.

---

## ✅ Decisão registrada

> O dataset enriquecido (taxonomia biomecânica, grafo de relações com score/confiança/motivos e GIF individual em resolução 360 por exercício) é importado como camada adicional sobre o `ExerciseCatalog` já normalizado, mantendo compatibilidade com os campos legados de regressão/progressão (US-039) e reforçando que nenhum exercício é aprovado sem mídia válida, respeitando a licença do EULA assinado com o provider. A variante 180p do asset original não é utilizada.
