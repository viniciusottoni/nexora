---
title: US-178 — Catálogo e sugestão de itens e valores
sidebar_position: 178
---

# US-178 — Catálogo e sugestão de itens e valores

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-178 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P1 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Produto e Engenharia |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **definir catálogo de itens e valores**, para **substituir mocks de loja por uma base mínima coerente**.

## 3. Objetivo

Criar catálogo mínimo de itens e slots com metadados, raridade, disponibilidade e vínculo futuro com produtos da loja.

## 4. Escopo

### Entra nesta US

- Catálogo de itens.
- Catálogo de slots de inventário.
- Nome, descrição, raridade, tipo e status.
- Sugestão de valores por produto.
- Remoção de preço mock em Gold.

### Fora desta US

- Moeda virtual emitida por jogo.
- Marketplace entre usuários.
- Balanceamento completo de economia.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Catálogo deve diferenciar item de recompensa e item de loja. |
| RN-002 | Valores reais devem ser configurados na loja/RevenueCat, não hardcoded como Gold. |
| RN-003 | Item inativo não deve aparecer. |
| RN-004 | Raridade deve ser informativa e usada na UI. |
| RN-005 | Catálogo deve suportar expansão futura. |

## 6. Impacto Flutter

- Tela de loja lê catálogo real/configurado.
- Remover mocks de preço em Gold.
- Exibir raridade e disponibilidade.

## 7. Impacto Backend

- Endpoint de catálogo.
- Modelo de item/produto.
- Validação de item ativo.
- Preparação para IAP real na US-179.

## 8. Impacto DB

Entidades sugeridas:

- ShopItem;
- ShopProduct;
- InventorySlotProduct.

## 9. Critérios de aceite

### CA-001 — Catálogo real

Dado que a loja é aberta,
quando carregar catálogo,
então deve exibir itens configurados e ativos, sem Gold mock.

### CA-002 — Item inativo oculto

Dado que um item está inativo,
quando carregar catálogo,
então ele não deve aparecer.

## 10. Decisão registrada

> O EPIC-018 trata itens e slots como produtos de loja; moeda virtual emitida por jogo fica deferida.
