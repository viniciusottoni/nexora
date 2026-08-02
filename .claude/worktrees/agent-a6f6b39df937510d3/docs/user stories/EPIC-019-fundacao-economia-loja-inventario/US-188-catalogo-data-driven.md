---
title: US-188 — Catálogo de loja orientado a dados
sidebar_position: 188
---

# US-188 — Catálogo de loja orientado a dados

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-188 |
| Épico | EPIC-019 — Fundação de Economia, Loja e Inventário |
| Prioridade | P1 |
| Fase | Fundação de economia (pós-MVP) |
| Perfil principal | Engenharia e Produto |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência | ADR-023, EPIC-018 US-178 |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **um catálogo de loja carregado de dados**, para **adicionar ou desativar itens sem deploy e sem código de domínio hardcoded**.

## 3. Objetivo

Substituir o `ShopCatalog` estático (ADR-022) por um catálogo orientado a dados sobre `shop_products`. Na entrega, o catálogo nasce **vazio ou apenas com os itens legados da ADR-022**, sem itens novos.

## 4. Escopo

### Entra nesta US

- Catálogo lido de `shop_products` (Key, Name, Description, Type, Rarity, IsActive, canal e preço quando aplicável).
- Suporte aos dois canais: preço em Gold (interno) e produto IAP (RevenueCat).
- Filtro por item ativo e disponível.
- Remoção do `ShopCatalog` estático e dos preços Gold hardcoded no domínio.
- Endpoint de catálogo retornando lista vazia quando não há itens ativos.

### Fora desta US

- Definição de itens reais e cosméticos.
- Regras de efeito dos itens.
- Promoções, descontos e temporadas.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Catálogo vem de dados, não de lista estática em código. |
| RN-002 | Item inativo ou indisponível não aparece. |
| RN-003 | Preço/produto vem do canal configurado (Gold ou IAP), nunca fixo em texto. |
| RN-004 | Catálogo vazio é estado válido e retorna lista vazia sem erro. |
| RN-005 | Raridade e tipo são expostos para a UI (informativos). |

## 6. Impacto Flutter

- Loja lê catálogo real do backend.
- Empty state quando o catálogo está vazio.
- Exibição de raridade, tipo e canal de preço.

## 7. Impacto Backend

- Repositório de catálogo sobre `shop_products`.
- Remoção do `ShopCatalog` estático.
- `GET /api/shop/catalog` orientado a dados.
- Vínculo de itens legados da ADR-022 ao novo catálogo.

## 8. Impacto DB

- Uso de `shop_products` (existente); inclusão de canal/preço Gold quando aplicável.

## 9. Critérios de aceite

### CA-001 — Catálogo vazio

Dado que não há itens ativos,
quando carregar o catálogo,
então deve retornar lista vazia e a UI exibe empty state.

### CA-002 — Sem hardcoded

Dado o domínio,
quando inspecionar o código,
então não deve existir lista estática de itens nem preço Gold fixo.

### CA-003 — Item inativo oculto

Dado um item inativo,
quando carregar o catálogo,
então ele não deve aparecer.

## 10. Decisão registrada

> O catálogo passa a ser orientado a dados e nasce sem itens fictícios; o conteúdo real entra no épico de catálogo/itens (ADR-023).
