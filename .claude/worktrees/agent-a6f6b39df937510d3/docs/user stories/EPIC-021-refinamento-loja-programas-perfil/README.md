---
title: EPIC-021 — Refinamento de Loja, Programas e Perfil
sidebar_position: 21
---

# EPIC-021 — Refinamento de Loja, Programas e Perfil

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-021 |
| Fase | Refinamento funcional pós-fundação de economia |
| Prioridade | P0 / P1 |
| Perfil principal | Usuário em Trial, Premium Mensal, Premium Anual, Produto, Engenharia e QA |
| Planos impactados | Trial, Mensal e Anual |
| Dependências | EPIC-004, EPIC-007, EPIC-010, EPIC-019, EPIC-020 |
| Status | Planejado |

## 2. Objetivo

Refinar a experiência de loja, programas de treino e perfil do Hunter após a fundação de economia, garantindo catálogo inicial de itens via migration, regras de uso, seleção de programas por rank, inventário inicial vazio, avatar padrão controlado e moldura especial do plano anual.

## 3. Contexto

O EPIC-019 entrega a fundação de economia, carteira, inventário, catálogo e compra. O EPIC-021 introduz os primeiros itens concretos, suas regras de uso, programas de treino com restrições por rank e refinamentos visuais/funcionais do perfil.

## 4. Escopo

### Entra neste épico

- Migration com os primeiros itens da loja.
- Regras de uso e efeito dos itens no sistema.
- Programas de treino e restrições por rank.
- Tela de seleção de programas com detalhes antes de confirmar.
- Inventário padrão vazio para novo usuário.
- Avatar padrão quando não houver imagem do Google.
- Seleção de avatar apenas entre avatares disponíveis no sistema.
- Bloqueio de upload externo de avatar.
- Moldura dourada e brilho suave para plano anual.

### Fora deste épico

- Marketplace entre usuários.
- Upload livre de imagens.
- Temporadas ou passe de batalha.
- Balanceamento avançado de economia.
- Editor avançado de programas.

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-229 | Criar migration inicial de itens da loja | P0 | [Abrir](./US-229-migration-itens-loja.md) |
| US-230 | Aplicar regras de uso e efeito dos itens | P0 | [Abrir](./US-230-regras-itens-loja.md) |
| US-231 | Definir programas e restrições por rank | P0 | [Abrir](./US-231-programas-restricoes-rank.md) |
| US-232 | Selecionar programa em tela com detalhes | P0 | [Abrir](./US-232-tela-selecao-programas.md) |
| US-233 | Criar inventário padrão vazio | P0 | [Abrir](./US-233-inventario-padrao-vazio.md) |
| US-234 | Usar avatar padrão e permitir apenas avatares disponíveis | P1 | [Abrir](./US-234-avatar-padrao-controlado.md) |
| US-235 | Exibir moldura dourada animada para plano anual | P1 | [Abrir](./US-235-moldura-dourada-plano-anual.md) |

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-021-001 | Os primeiros itens da loja devem nascer por migration e não por mock no Flutter. |
| RN-EPIC-021-002 | Todo item deve ter chave estável, tipo, raridade, preço em Gold, status e regra de uso. |
| RN-EPIC-021-003 | O backend é a fonte de verdade para compra, consumo, limite diário/semanal e efeito de item. |
| RN-EPIC-021-004 | Novo usuário inicia com inventário vazio, sem itens gratuitos implícitos. |
| RN-EPIC-021-005 | Programas devem respeitar rank mínimo e categoria do usuário. |
| RN-EPIC-021-006 | Usuário só pode selecionar programa permitido para seu rank atual. |
| RN-EPIC-021-007 | Avatar pode usar imagem Google quando existir; sem imagem, usar avatar padrão do sistema. |
| RN-EPIC-021-008 | Avatar editável deve vir apenas de catálogo interno de avatares disponíveis. |
| RN-EPIC-021-009 | Upload externo de avatar fica bloqueado no MVP. |
| RN-EPIC-021-010 | Plano anual exibe moldura dourada com brilho suave no Perfil Hunter/Card. |

## 7. Impactos técnicos

### Flutter

- Loja lê itens reais do backend.
- Inventário mostra empty state real.
- Tela de programas com cards expansíveis e CTA de seleção.
- Perfil permite troca apenas por avatares internos disponíveis.
- Card/Perfil do plano anual renderiza moldura dourada animada com brilho discreto.

### Backend

- Migration/seed de catálogo inicial.
- Handlers de efeito e consumo de item.
- Validação de limite de uso por dia/semana.
- Catálogo de programas com rank mínimo.
- Inventário inicial vazio.
- Catálogo de avatares internos.

### Banco de dados

- ShopProducts/ShopItems iniciais.
- ItemEffects/UsageRules ou configuração equivalente.
- TrainingPrograms.
- UserInventory vazio por padrão.
- UserAvatarSelection.
- Annual frame entitlement/flag visual.

### QA

- Validar migration idempotente.
- Validar compra/consumo de itens.
- Validar limites diários e semanais.
- Validar bloqueio de programa por rank.
- Validar inventário vazio em novo usuário.
- Validar avatar padrão e bloqueio de upload.
- Validar moldura anual apenas para Premium Anual.

## 8. Dependências

- EPIC-019 para fundação de economia, inventário, catálogo e ledger.
- EPIC-020 para validação server-side e atomicidade de Gold/pedido/inventário.
- EPIC-007 para alteração de tipo/programa de treino.
- EPIC-010 para Perfil Hunter e card compartilhável.

## 9. Critérios de aceite do épico

- Itens iniciais existem no banco após migration.
- Loja não depende de mock para exibir itens iniciais.
- Cada item tem regra de uso documentada e validada pelo backend.
- Programas aparecem com detalhes e bloqueio por rank.
- Novo usuário começa com inventário vazio.
- Avatar padrão aparece quando não houver imagem Google.
- Usuário não consegue carregar avatar externo.
- Plano anual exibe moldura dourada com brilho suave.

## 10. Decisão registrada

O EPIC-021 é o refinamento que transforma a fundação de economia e perfil em uma experiência concreta: catálogo inicial de itens, regras reais, programas controlados por rank, inventário honesto, avatar seguro e benefício visual claro para o plano anual.
