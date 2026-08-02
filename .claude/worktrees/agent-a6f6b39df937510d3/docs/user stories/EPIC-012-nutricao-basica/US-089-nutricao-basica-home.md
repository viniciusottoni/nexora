---
title: US-089 — Visualizar nutrição básica na Home
sidebar_position: 89
---

# US-089 — Visualizar nutrição básica na Home

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-089 |
| Épico | EPIC-012 — Nutrição Básica |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **usuário com acesso ativo**, quero **ver a nutrição básica na Home**, para **acompanhar água e gasto calórico sem sair do fluxo principal do AWAKEN**.

## 3. Contexto

O card de nutrição básica deve aparecer na Home logo abaixo do card de rank e antes das quests, como um status diário de autocuidado no estilo RPG.

## 4. Objetivo

Exibir um card compacto de nutrição básica na Home, com barras visuais de água e gasto calórico estimado.

## 5. Escopo

### Entra nesta US

- Card de nutrição básica na Home.
- Posicionamento abaixo do card de rank.
- Posicionamento antes das quests.
- Barra de água.
- Barra/indicador de gasto calórico estimado.
- Estados de loading, erro, perfil incompleto e acesso bloqueado.

### Fora desta US

- Tela nutricional completa.
- Plano alimentar.
- Macros.
- Integrações externas.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O card deve aparecer na Home abaixo do card de rank. |
| RN-002 | O card deve aparecer antes das quests. |
| RN-003 | O card deve exibir água e gasto calórico estimado. |
| RN-004 | A interface deve indicar que os dados são básicos/estimados. |
| RN-005 | Usuário com acesso expirado deve ver estado bloqueado/CTA. |
| RN-006 | O card não deve ocupar mais destaque que a quest principal. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não visualiza. |
| Usuário em Trial | Visualiza com trial ativo. |
| Premium Mensal | Visualiza. |
| Premium Anual | Visualiza. |
| Trial expirado | Vê bloqueio/CTA. |
| Assinatura expirada | Vê bloqueio/CTA. |

## 8. Fluxo principal

1. Usuário abre a Home.
2. App carrega dados do perfil, nutrição e progresso.
3. Home renderiza card de rank.
4. Home renderiza card de nutrição básica.
5. Home renderiza área de quests.

## 9. Fluxos alternativos

### 9.1. Dados incompletos

Card exibe orientação para atualizar perfil.

### 9.2. Erro de nutrição

Card exibe estado discreto de erro sem quebrar a Home.

## 10. Estados esperados

- carregando;
- dados exibidos;
- perfil incompleto;
- acesso bloqueado;
- erro parcial.

## 11. Impacto Flutter

- Novo componente de card na Home.
- Barras visuais com estética RPG/dark.
- Layout responsivo.
- Integração com estado de acesso.

## 12. Impacto Backend

- Endpoint consolidado ou chamada para nutrição básica do dia.
- Retorno de água e calorias estimadas.
- Validação de acesso.

## 13. Impacto DB

Entidades:

- NutritionLog;
- UserProfile;
- Subscription.

## 14. Impacto Gamificação

- Funciona como status diário.
- Não concede XP no MVP.
- Pode reforçar sensação de preparo do Hunter.

## 15. Impacto Monetização

- Nutrição básica é P1 para usuários com acesso ativo.
- Acesso expirado direciona para assinatura.

## 16. Contrato API sugerido

```txt
GET /api/nutrition/basic/today
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| home_nutrition_card_viewed | Quando o card aparece na Home. |

## 18. Critérios de aceite

### CA-001 — Posição correta

Dado que o usuário abre a Home,
Quando o conteúdo carregar,
Então o card de nutrição deve aparecer abaixo do card de rank e antes das quests.

### CA-002 — Conteúdo mínimo

Dado que os dados estão disponíveis,
Quando o card for exibido,
Então deve mostrar água e gasto calórico estimado.

## 19. Critérios de teste QA

- posição abaixo do rank;
- posição antes das quests;
- dados carregados;
- perfil incompleto;
- acesso expirado;
- erro parcial;
- responsividade Android.

## 20. Decisão registrada

A nutrição básica deve aparecer na Home como status secundário do Hunter, sem competir com o card de rank nem com a quest principal.
