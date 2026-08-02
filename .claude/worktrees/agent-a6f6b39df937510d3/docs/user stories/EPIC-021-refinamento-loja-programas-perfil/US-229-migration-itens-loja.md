---
title: US-229 — Criar migration inicial de itens da loja
sidebar_position: 229
---

# US-229 — Criar migration inicial de itens da loja

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-229 |
| Épico | EPIC-021 — Refinamento de Loja, Programas e Perfil |
| Prioridade | P0 |
| Fase | Refinamento funcional pós-fundação de economia |
| Perfil principal | Produto, Engenharia e QA |
| Dependências | EPIC-019, EPIC-020 |
| Status | Planejada |

## 2. História do usuário

Como **time de produto e engenharia**, quero **criar os primeiros itens da loja por migration**, para **popular o catálogo real do AWAKEN sem depender de mocks no Flutter**.

## 3. Contexto

O EPIC-019 entrega a fundação de economia, mas deixa itens concretos fora de escopo. Esta US cria o catálogo inicial com chaves estáveis, preços em Gold, raridade, tipo, limite e status ativo.

## 4. Objetivo

Criar uma migration/seed idempotente com os primeiros itens da loja e suas informações básicas para compra, exibição e uso.

## 5. Escopo

### Entra nesta US

- Criar itens consumíveis iniciais.
- Criar itens cosméticos iniciais.
- Criar packs iniciais.
- Criar produtos de slots de inventário.
- Criar pacotes de Gold quando a fundação de compra de Gold estiver disponível.
- Usar chaves estáveis de catálogo.
- Manter catálogo ativo/inativo por dado, não por mock.

### Fora desta US

- Balanceamento avançado de economia.
- Temporadas.
- Marketplace.
- Upload de assets pelo usuário.

## 6. Catálogo inicial sugerido

### Consumíveis

| Item | Chave sugerida | Preço Gold | Raridade | Limite |
|---|---:|---:|---|---|
| Pergaminho da Reforja | scroll_reforge | 150 | Incomum | 1 uso/dia |
| Pergaminho da Substituição | scroll_substitution | 90 | Comum | 2 usos/dia |
| Bússola da Dungeon | dungeon_compass | 120 | Incomum | 1 uso/dia |
| Chave da Dungeon | dungeon_key | 250 | Épico | 1 uso/dia |
| Selo de Proteção | protection_seal | 100 | Incomum | Máx. 2 ativos |
| Tônico de Recuperação | recovery_tonic | 70 | Comum | 2 usos/semana |
| Amuleto de Retorno | return_amulet | 220 | Épico | 1 uso/semana |
| Poção de Foco | focus_potion | 120 | Incomum | 1 uso/dia |
| Poção de Foco Grande | focus_potion_large | 260 | Épico | 1 uso/semana |
| Poção da Sorte | luck_potion | 90 | Comum | 1 uso/dia |
| Pedra da Dungeon | dungeon_stone | 120 | Incomum | 1 uso/dia |

### Cosméticos

| Item | Chave sugerida | Preço Gold | Raridade |
|---|---:|---:|---|
| Moldura Especial - Rank E | frame_rank_e | 250 | Comum |
| Moldura Especial - Rank D | frame_rank_d | 350 | Comum |
| Moldura Especial - Rank C | frame_rank_c | 500 | Incomum |
| Moldura Especial - Rank B | frame_rank_b | 750 | Raro |
| Moldura Especial - Rank A | frame_rank_a | 1000 | Raro |
| Moldura Especial - Rank S | frame_rank_s | 1500 | Épico |
| Moldura Especial - Rank SS | frame_rank_ss | 2000 | Épico |
| Moldura Especial - Rank SSS | frame_rank_sss | 3000 | Lendário |
| Aura | aura_default | 600 | Épico |
| Fundo: Portal | background_portal | 450 | Incomum |
| Fundo: Dungeon | background_dungeon | 600 | Raro |
| Fundo: Sombras do Hunter | background_hunter_shadows | 900 | Épico |
| Pergaminho de Renomeação | scroll_rename | 200 | Comum |
| Pergaminho da Classe | scroll_class_change | 350 | Incomum |

### Packs iniciais

| Pack | Chave sugerida | Preço Gold | Raridade |
|---|---:|---:|---|
| Pack Striker | pack_striker | 1500 | Incomum |
| Pack Runner | pack_runner | 1500 | Incomum |
| Pack Guardian | pack_guardian | 1500 | Incomum |
| Pack Shadow | pack_shadow | 1800 | Raro |
| Pack Reawakened | pack_reawakened | 2000 | Raro |

### Slots de inventário

| Produto | Chave sugerida | Preço Gold | Faixa |
|---|---:|---:|---|
| +5 slots | inventory_slots_05_10_15 | 300 | 10-15 |
| +5 slots | inventory_slots_05_15_20 | 500 | 15-20 |
| +5 slots | inventory_slots_05_20_25 | 800 | 20-25 |
| +5 slots | inventory_slots_05_25_30 | 1200 | 25-30 |

## 7. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Migration deve ser idempotente por chave de item/produto. |
| RN-002 | Item ativo aparece na loja; item inativo não aparece. |
| RN-003 | Preço em Gold vem do catálogo do backend. |
| RN-004 | Flutter não deve conter lista hardcoded dos itens. |
| RN-005 | Todo item deve ter tipo, raridade, descrição, preço e regra de uso associada. |
| RN-006 | Packs concedem apenas itens/avatares/classes previamente cadastrados. |

## 8. Impacto Backend

- Criar migration de catálogo inicial.
- Criar seed idempotente.
- Usar enums/chaves estáveis.
- Expor catálogo via endpoint existente ou novo endpoint de loja.

## 9. Impacto Flutter

- Loja passa a renderizar itens recebidos do backend.
- Remover mocks visuais como fonte de verdade.
- Manter estados de loading, erro e vazio.

## 10. Impacto DB

Tabelas esperadas conforme EPIC-019:

- shop_items;
- shop_products;
- item_usage_rules;
- item_effects;
- inventory_slot_products.

## 11. Critérios de aceite

### CA-001 — Migration cria catálogo

Dado que a migration foi executada,
quando consultar o catálogo,
então os itens iniciais devem existir com chave, preço, raridade e status.

### CA-002 — Migration idempotente

Dado que a migration roda mais de uma vez,
quando consultar os itens,
então não deve existir duplicidade por chave.

### CA-003 — Loja sem mock

Dado que o app abre a loja,
quando carregar os dados,
então os itens devem vir do backend, não de lista local hardcoded.

## 12. Decisão registrada

O primeiro catálogo real da loja nasce por migration/seed idempotente e substitui os mocks visuais como fonte de verdade.
