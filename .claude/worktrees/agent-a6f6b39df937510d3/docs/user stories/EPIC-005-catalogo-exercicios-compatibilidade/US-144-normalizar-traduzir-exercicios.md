---
title: US-144 — Normalizar e traduzir exercícios para PT-BR e enums internos
sidebar_position: 144
---

# US-144 — Normalizar e traduzir exercícios para PT-BR e enums internos

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-144 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR (saída principal), EN, ES, FR |
| Dependência principal | ExerciseRawImport → ExerciseCatalog |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **normalizar e traduzir os exercícios importados**,

para **criar registros consistentes em `ExerciseCatalog` com nomes PT-BR e enums internos**.

---

## 3. Contexto

A resposta da API vem em inglês e com vocabulário próprio (equipamentos, músculos, partes do corpo). Antes de classificar e aprovar, é preciso normalizar nomes, traduzir para PT-BR e mapear valores para os enums internos do AWAKEN.

---

## 4. Objetivo

Transformar registros `imported` em registros `normalized` no `ExerciseCatalog`, com nome PT-BR, slug, instruções e dicas traduzidas, e equipamentos/músculos mapeados para enums internos.

---

## 5. Escopo

### Entra nesta US

- Geração de `NamePtBr`, `Slug`, `DescriptionPtBr`, `InstructionsPtBr`, `TipsPtBr`.
- Preservação dos originais (`NameOriginal`, `InstructionsOriginal`).
- Mapeamento de equipamentos para enum interno.
- Mapeamento de músculos e partes do corpo para enums internos.
- Normalização de URLs de mídia (vídeo, imagem, GIF) a partir de `mediaBaseUrl`.

### Fora desta US

- Classificação de dificuldade/impacto (US-038), tags (US-040/145/146), atributos (US-147).
- Sanitização (US-148) e aprovação (US-149).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Todo exercício normalizado deve ter `NamePtBr` não vazio. |
| RN-002 | Os textos originais devem ser preservados junto às traduções. |
| RN-003 | Equipamentos devem ser mapeados para enum interno; valores desconhecidos vão para revisão. |
| RN-004 | Músculos e partes do corpo devem ser mapeados para enums internos. |
| RN-005 | URLs de mídia devem ser resolvidas de forma absoluta e válida. |
| RN-006 | Itens normalizados recebem status `normalized`. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Executa normalização. |
| Usuário final | Sem acesso direto. |

---

## 8. Fluxo principal

1. Sistema lê registros `imported` em `ExerciseRawImport`.
2. Traduz nome, descrição, instruções e dicas para PT-BR.
3. Mapeia equipamentos, músculos e partes do corpo para enums internos.
4. Resolve URLs de mídia.
5. Cria/atualiza `ExerciseCatalog` com status `normalized`.

---

## 9. Fluxos alternativos

### 9.1. Valor sem mapeamento

Equipamento/músculo sem correspondência é enviado para fila de revisão e o exercício não avança até resolução.

### 9.2. Tradução ausente

Se a tradução automática falhar, o item fica pendente de tradução e não é aprovado.

---

## 10. Estados esperados

- normalizando;
- normalizado;
- pendente de mapeamento;
- pendente de tradução;
- erro de normalização.

---

## 11. Impacto no Frontend Flutter

- Consumo posterior dos textos PT-BR; sem tela própria nesta US.

---

## 12. Impacto no Backend

- Serviço de normalização e tradução.
- Dicionários de mapeamento equipamento/músculo/parte do corpo → enum.
- Resolução de mídia.

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseCatalog`.

Campos: `NamePtBr`, `NameOriginal`, `Slug`, `DescriptionPtBr`, `InstructionsPtBr`, `InstructionsOriginal`, `TipsPtBr`, `RequiredEquipment`, `PrimaryMuscleGroups`, `SecondaryMuscleGroups`, `BodyParts`, `VideoUrl`, `ImageUrl`, `GifUrl`, `SanitizationStatus`.

---

## 14. Impacto em Gamificação

- Indireto: prepara o exercício para receber contribuição de atributos.

---

## 15. Impacto em Monetização

- Garante conteúdo localizado, percebido como qualidade no trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Saída principal das traduções. |
| EN | Mantido a partir do original. |
| ES | Chaves equivalentes (quando houver). |

---

## 17. Contrato de API sugerido

```txt
POST /api/admin/exercises/normalize
```

Request:

```json
{
  "importBatchId": "imp_2026_06_22_01"
}
```

Response conceitual:

```json
{
  "normalized": 90,
  "pendingMapping": 4,
  "pendingTranslation": 2
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_sanitized | Etapa subsequente; normalização é pré-requisito. |

---

## 19. Critérios de aceite

### CA-001 — Registro normalizado

Dado que um exercício foi importado,

Quando passar pela normalização,

Então deve gerar um `ExerciseCatalog` com `NamePtBr`, equipamentos e músculos mapeados e mídia resolvida.

### CA-002 — Valor sem mapeamento

Dado que um equipamento não tem enum correspondente,

Quando a normalização rodar,

Então o exercício vai para revisão e não avança.

---

## 20. Critérios de teste para QA

### Backend

- normalização gera `NamePtBr` e preserva originais;
- equipamentos/músculos desconhecidos vão para revisão;
- URLs de mídia ficam absolutas e válidas;
- status muda para `normalized`.

### E2E

- lote normalizado fica disponível para sanitização;
- textos PT-BR aparecem corretamente na instrução do exercício.

---

## ✅ Decisão registrada

> A normalização traduz para PT-BR e mapeia para enums internos, preservando os dados originais e bloqueando exercícios sem mapeamento até revisão.
