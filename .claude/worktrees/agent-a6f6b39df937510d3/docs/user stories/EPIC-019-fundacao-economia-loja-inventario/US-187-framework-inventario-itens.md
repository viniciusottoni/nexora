---
title: US-187 — Framework genérico de inventário, itens e slots
sidebar_position: 187
---

# US-187 — Framework genérico de inventário, itens e slots

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-187 |
| Épico | EPIC-019 — Fundação de Economia, Loja e Inventário |
| Prioridade | P1 |
| Fase | Fundação de economia (pós-MVP) |
| Perfil principal | Engenharia |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência | ADR-023, ADR-022 |
| Status | Planejada |

## 2. História do usuário

Como **engenharia**, quero **um framework genérico de inventário, chaves de item e slots**, para **adicionar itens futuros apenas com dados, sem mudar schema**.

## 3. Objetivo

Generalizar o inventário mínimo da ADR-022 numa base extensível: chaves estáveis, tipos de item e slots de inventário. Esta US **não cria itens concretos nem suas regras de efeito/consumo**.

## 4. Escopo

### Entra nesta US

- Consolidação de `InventoryItem` (`UserId`, `ItemKey`, `Quantity`) como base genérica.
- Registro de chaves estáveis (`ItemKeys`) e tipo de item (consumível, slot, cosmético) como metadado.
- Conceito de slot de inventário (`InventorySlot`) como estrutura, sem efeito.
- Operações genéricas de obter/incrementar quantidade.
- Pontos de extensão para handlers de efeito/consumo futuros (interface sem implementações concretas).

### Fora desta US

- Itens concretos e cosméticos.
- Regras de efeito e de consumo de cada item.
- Exposição de um endpoint genérico de "consumir item" (mantém a restrição da ADR-022).

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Chaves de item são estáveis; tradução/apresentação fica no app (ADR-021). |
| RN-002 | A estrutura deve suportar novos itens sem alteração de schema. |
| RN-003 | Tipo de item é metadado informativo nesta fase, sem efeito implementado. |
| RN-004 | Não há endpoint genérico de consumo; consumo permanece em fluxos específicos. |
| RN-005 | Quantidade de item nunca fica negativa. |

## 6. Impacto Flutter

- Modelos de inventário/slot lendo do backend.
- Renderização por `itemKey` e tipo, com fallback genérico.

## 7. Impacto Backend

- `InventoryItem` e `InventorySlot` como base genérica.
- Catálogo de `ItemKeys` e enum de tipo.
- Interface de handler de efeito (sem implementações concretas).
- Reutilização pela concessão de compra (US-189).

## 8. Impacto DB

Entidades sugeridas:

- InventoryItem (`inventory_items`, índice único `(UserId, ItemKey)`);
- InventorySlot (`inventory_slots`, estrutura).

## 9. Critérios de aceite

### CA-001 — Item inexistente

Dado que o usuário nunca obteve um item,
quando consultar o inventário,
então a quantidade deve ser 0 sem erro.

### CA-002 — Extensão sem schema

Dado um novo `ItemKey`,
quando registrá-lo no catálogo de chaves,
então o item deve ser representável no inventário sem migração de schema.

## 10. Decisão registrada

> O inventário vira base genérica e extensível; itens concretos e suas regras ficam para o épico de catálogo/itens (ADR-023).
