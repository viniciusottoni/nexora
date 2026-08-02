---
title: US-233 — Criar inventário padrão vazio
sidebar_position: 233
---

# US-233 — Criar inventário padrão vazio

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-233 |
| Épico | EPIC-021 — Refinamento de Loja, Programas e Perfil |
| Prioridade | P0 |
| Fase | Refinamento funcional pós-fundação de economia |
| Perfil principal | Novo usuário |
| Dependências | EPIC-019, EPIC-020 |
| Status | Planejada |

## 2. História do usuário

Como **novo usuário do AWAKEN**, quero **começar com inventário vazio**, para **entender que meus itens virão de compras, recompensas ou regras explícitas do sistema**.

## 3. Contexto

O inventário deve ser honesto. A conta nova não deve ganhar itens implícitos por mock, seed visual ou fallback local.

## 4. Objetivo

Garantir que todo novo usuário tenha inventário inicial vazio, com empty state adequado no app e sem itens criados automaticamente fora de regra explícita.

## 5. Escopo

### Entra nesta US

- Criar estrutura de inventário do usuário quando necessário.
- Garantir ausência de itens iniciais implícitos.
- Exibir empty state no app.
- Permitir que compras/recompensas futuras adicionem itens.
- Cobrir trial, mensal e anual.

### Fora desta US

- Bônus de onboarding.
- Pack gratuito de boas-vindas.
- Temporadas.
- Recompensa diária inicial.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Novo usuário inicia com inventário vazio. |
| RN-002 | Nenhum item deve ser criado por mock no Flutter. |
| RN-003 | Inventário vazio deve exibir mensagem honesta. |
| RN-004 | Itens só entram por compra, recompensa, concessão administrativa auditável ou regra explícita futura. |
| RN-005 | Inventário vazio não bloqueia uso básico do app. |

## 7. Fluxo principal

1. Usuário cria conta e conclui acesso inicial.
2. Backend cria ou prepara inventário vazio.
3. Usuário abre inventário.
4. App consulta backend.
5. Backend retorna lista vazia.
6. App mostra empty state.

## 8. Impacto Backend

- Garantir inventário inicial sem itens.
- Endpoint retorna lista vazia corretamente.
- Não aplicar seed de item por usuário.

## 9. Impacto Flutter

- Empty state de inventário.
- Sem fallback visual com itens fake.
- CTA opcional para loja quando acesso ativo.

## 10. Critérios de aceite

### CA-001 — Novo usuário sem itens

Dado que um novo usuário foi criado,
quando abrir inventário,
então a lista deve estar vazia.

### CA-002 — Sem mock

Dado que o backend retorna inventário vazio,
quando o app renderizar,
então não deve exibir itens fake.

## 11. Decisão registrada

O inventário inicial do AWAKEN é vazio por padrão; todo item precisa de origem rastreável.
