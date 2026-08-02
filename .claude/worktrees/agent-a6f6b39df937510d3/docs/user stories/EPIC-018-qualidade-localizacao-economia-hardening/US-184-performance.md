---
title: US-184 — Performance
sidebar_position: 184
---

# US-184 — Performance

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-184 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P1 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Engenharia e QA |
| Plataforma | Flutter Android + Backend .NET 10 |
| Status | Planejada |

## 2. História do usuário

Como **usuário do AWAKEN**,
quero **que o app carregue rápido e responda bem**,
para **não perder motivação durante quests, loja, perfil ou histórico**.

## 3. Objetivo

Melhorar performance percebida e técnica em telas críticas, payloads, catálogo e mídia de exercícios.

## 4. Escopo

### Entra nesta US

- Definir metas p95 para APIs críticas.
- Reduzir payloads desnecessários.
- Aplicar lazy loading em listas/mídias.
- Cachear catálogo quando seguro.
- Otimizar GIFs/imagens de exercícios via CDN/storage.
- Evitar rebuilds pesados em Flutter.

### Fora desta US

- Otimização prematura de toda tela.
- Reescrita completa do app.
- Infra de observabilidade avançada.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Telas críticas devem carregar com feedback visual rápido. |
| RN-002 | Catálogo de exercício não deve baixar mídia pesada sem necessidade. |
| RN-003 | APIs críticas devem ter payload mínimo suficiente. |
| RN-004 | Cache não pode gerar dados incorretos de assinatura, inventário ou progresso. |
| RN-005 | Performance deve ser validada em Android alvo do MVP. |

## 6. Impacto Flutter

- Lazy loading de imagens/GIFs.
- Cache local quando seguro.
- Reduzir rebuilds.
- Skeleton/loading states.
- Paginação em listas longas.

## 7. Impacto Backend

- Otimizar payloads.
- Paginar endpoints.
- Índices e cache.
- Medir p95 em rotas críticas.

## 8. Impacto QA

- Medir Home.
- Medir geração de quest.
- Medir execução de treino.
- Medir histórico.
- Medir catálogo com mídia.

## 9. Critérios de aceite

### CA-001 — Mídia sob demanda

Dado que uma lista possui exercícios com GIF,
quando a tela carregar,
então mídia pesada deve ser carregada sob demanda.

### CA-002 — Payload otimizado

Dado que o app consulta histórico,
quando receber resposta,
então deve vir paginada e sem campos desnecessários.

## 10. Decisão registrada

> Performance do MVP deve priorizar experiência percebida nas rotas críticas, sem otimização complexa prematura.
