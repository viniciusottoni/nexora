---
title: US-191 — UI de loja, inventário e carteira
sidebar_position: 191
---

# US-191 — UI de loja, inventário e carteira

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-191 |
| Épico | EPIC-019 — Fundação de Economia, Loja e Inventário |
| Prioridade | P1 |
| Fase | Fundação de economia (pós-MVP) |
| Perfil principal | Engenharia Flutter e Design |
| Dependência | ADR-023, US-186, US-187, US-188 |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **usuário**, quero **telas reais de loja, inventário e carteira ligadas ao backend**, para **ver meu saldo e o que existe na loja sem mocks enganosos**.

## 3. Objetivo

Transformar as telas mock de `features/shop` e `features/inventory` em UI real que lê catálogo, inventário e saldo do backend. Como o catálogo nasce vazio, as telas exibem empty state honesto. **Nenhum item concreto é adicionado.**

## 4. Escopo

### Entra nesta US

- Loja lendo catálogo real (US-188), com loading/erro/empty state.
- Inventário lendo itens reais (US-187), com empty state.
- Widget de saldo de Gold reutilizável (design system) lendo a carteira (US-186).
- Fluxo de compra ligado à orquestração (US-189): loading, sucesso, cancelamento, erro.
- Remoção de itens decorativos falsos e preços Gold mock.

### Fora desta US

- Definição visual de itens reais e cosméticos.
- Tela de extrato (US-192).
- Visão administrativa (US-193).

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A UI nunca calcula saldo nem credita inventário; só reflete o backend. |
| RN-002 | Catálogo/inventário vazios exibem empty state honesto, sem mock. |
| RN-003 | Todo estado de compra (loading/sucesso/cancelamento/erro) é tratado. |
| RN-004 | Textos visíveis são localizados (ADR-021), inclusive empty states. |
| RN-005 | Saldo e inventário atualizam após confirmação do servidor. |

## 6. Impacto Flutter

- Refatorar telas de loja e inventário para dados reais.
- Componente `GoldBalance` no design system.
- Estados de UI completos e localizados em pt-BR, EN e ES.

## 7. Impacto Backend

- Consumo dos endpoints de catálogo, inventário e carteira (sem novo endpoint).

## 8. Impacto DB

- Nenhum.

## 9. Critérios de aceite

### CA-001 — Empty state honesto

Dado catálogo e inventário vazios,
quando abrir loja e inventário,
então a UI exibe empty state localizado, sem itens mock.

### CA-002 — Saldo real

Dado a carteira do usuário,
quando abrir qualquer tela com saldo,
então o valor exibido corresponde ao backend.

### CA-003 — Estados de compra

Dado um fluxo de compra,
quando ocorrer loading, sucesso, cancelamento ou erro,
então a UI trata cada estado de forma clara.

## 10. Decisão registrada

> A UI de economia passa a ser real e honesta com empty state; itens concretos entram no épico de catálogo/itens (ADR-023).
