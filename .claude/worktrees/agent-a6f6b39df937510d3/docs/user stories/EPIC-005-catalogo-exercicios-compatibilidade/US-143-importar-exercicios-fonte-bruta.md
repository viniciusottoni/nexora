---
title: US-143 — Importar exercícios da ExerciseDB / Ascend API para base bruta
sidebar_position: 143
---

# US-143 — Importar exercícios da ExerciseDB / Ascend API para base bruta

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-143 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (processo interno) |
| Dependência principal | ExerciseRawImport |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **importar exercícios da ExerciseDB / Ascend API e salvar a resposta original**,

para **ter uma fonte bruta rastreável que alimente o catálogo interno**.

---

## 3. Contexto

A API externa é a fonte bruta de exercícios. Antes de qualquer transformação, a resposta original deve ser preservada em `ExerciseRawImport` para rastreabilidade, reprocessamento e auditoria, sem depender da API em tempo real na geração.

---

## 4. Objetivo

Executar a rotina de importação por lote, salvar o JSON original e registrar metadados de rastreabilidade do provider.

---

## 5. Escopo

### Entra nesta US

- Chamada à ExerciseDB / Ascend API por lote (`ImportBatchId`).
- Persistência do JSON original em `ExerciseRawImport`.
- Registro de `providerName`, `providerExerciseId`, `providerVersion`, `sourceUrl`, `mediaBaseUrl`, `importedAt`.
- Tratamento de erro por item (`failed` + `errorMessage`).
- Verificação de licença/atribuição antes de armazenar mídia.

### Fora desta US

- Normalização e tradução (US-144).
- Sanitização (US-148) e aprovação (US-149).
- Geração de treino em tempo real a partir da API.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A resposta original deve ser salva em `ExerciseRawImport` antes de qualquer transformação. |
| RN-002 | Cada importação deve registrar rastreabilidade completa do provider. |
| RN-003 | Itens com falha recebem status `failed` e `errorMessage`, sem interromper o lote. |
| RN-004 | Mídia/dados só podem ser armazenados após verificação de termos de uso, licença, cache e atribuição. |
| RN-005 | A importação roda em background, nunca no caminho de geração de quest. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Executa importação. |
| Usuário final | Sem acesso direto. |

---

## 8. Fluxo principal

1. Job de importação inicia com `ImportBatchId`.
2. Sistema busca exercícios na ExerciseDB / Ascend API.
3. Para cada item, salva `RawJson` e metadados em `ExerciseRawImport` com status `imported`.
4. Itens com erro recebem `failed` + `errorMessage`.
5. Lote é finalizado e disponibilizado para normalização.

---

## 9. Fluxos alternativos

### 9.1. Falha de rede/limite de requisição

Respeitar limites do provider, aplicar retry/backoff e marcar itens pendentes para o próximo lote.

### 9.2. Licença incompatível

Se a licença não permitir cache/redistribuição da mídia, importar apenas dados textuais e sinalizar restrição em `mediaLicenseInfo`.

---

## 10. Estados esperados

- importação iniciada;
- item importado;
- item com falha;
- lote concluído;
- restrição de licença detectada.

---

## 11. Impacto no Frontend Flutter

- Sem impacto direto.

---

## 12. Impacto no Backend

- Cliente da ExerciseDB / Ascend API.
- Job de importação por lote com idempotência por `providerExerciseId`.
- Persistência em `ExerciseRawImport`.

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseRawImport`.

Campos: `ProviderName`, `ProviderExerciseId`, `ProviderVersion`, `RawJson`, `ImportedAt`, `ImportBatchId`, `SourceUrl`, `MediaBaseUrl`, `Status`, `ErrorMessage`.

---

## 14. Impacto em Gamificação

- Indireto: habilita futura contribuição de atributos.

---

## 15. Impacto em Monetização

- Reduz custo e dependência externa ao cachear exercícios.

---

## 16. Impacto em Internacionalização

- Não aplicável no processo de importação; tradução ocorre na US-144.

---

## 17. Contrato de API sugerido

```txt
POST /api/admin/exercises/import
```

Request:

```json
{
  "provider": "ExerciseDB/AscendAPI",
  "batchSize": 100
}
```

Response conceitual:

```json
{
  "importBatchId": "imp_2026_06_22_01",
  "imported": 96,
  "failed": 4
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_import_started | Quando a importação inicia. |
| exercise_import_completed | Quando a importação finaliza. |

---

## 19. Critérios de aceite

### CA-001 — JSON original salvo

Dado que existe um exercício na ExerciseDB / Ascend API,

Quando o sistema executar a importação,

Então deve salvar o JSON original em `ExerciseRawImport` com rastreabilidade do provider.

### CA-002 — Falha isolada

Dado que um item falha na importação,

Quando o lote continuar,

Então o item recebe `failed` + `errorMessage` sem interromper os demais.

---

## 20. Critérios de teste para QA

### Backend

- importação salva `RawJson` e metadados completos;
- reimportar mesmo `providerExerciseId` não duplica registro;
- item inválido recebe status `failed`;
- restrição de licença marca `mediaLicenseInfo`.

### E2E

- lote de importação conclui e fica disponível para normalização;
- geração de quest não chama a API externa em tempo real.

---

## ✅ Decisão registrada

> A resposta original do provider é sempre preservada em `ExerciseRawImport` com rastreabilidade e verificação de licença, antes de qualquer transformação.
