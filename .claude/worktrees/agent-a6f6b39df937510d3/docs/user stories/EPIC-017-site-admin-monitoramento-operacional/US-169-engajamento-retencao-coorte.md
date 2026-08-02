---
title: US-169 — Visualizar engajamento e retenção por coorte
sidebar_position: 169
---

# US-169 — Visualizar engajamento e retenção por coorte

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-169 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto, Growth e Engenharia |
| Plataforma | Web Admin (React) |
| Dependência | US-161, US-168, EPIC-014 |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **visualizar engajamento e retenção por coorte**, para **entender se usuários ativam, retornam e usam as funcionalidades centrais do AWAKEN**.

## 3. Objetivo

Criar visão agregada de DAU/MAU, retenção por coorte, sessões e uso por funcionalidade com base nos eventos e dados operacionais existentes.

## 4. Escopo

### Entra nesta US

- Indicadores de DAU, MAU e razão DAU/MAU.
- Retenção D1, D7 e D30 quando houver janela suficiente.
- Coortes por data de cadastro, início de trial ou primeira quest.
- Sessões por usuário ativo.
- Uso por funcionalidade: onboarding, quest, execução, perfil, loja, configurações quando instrumentado.
- Filtros por período, plano, versão e ambiente.

### Fora desta US

- Modelos preditivos de churn.
- Segmentação comportamental avançada.
- Push automático de retenção.
- BI externo completo.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Métricas de engajamento devem ser agregadas. |
| RN-002 | Coortes devem deixar claro o evento de origem usado. |
| RN-003 | Retenção só deve aparecer quando houver janela temporal suficiente. |
| RN-004 | Dados pessoais individuais não devem ser exibidos nesta tela. |
| RN-005 | Mudança de filtro deve recalcular todos os indicadores relacionados. |

## 6. Fluxo principal

1. Produto acessa a tela de engajamento.
2. Sistema exibe DAU/MAU e retenção do período padrão.
3. Produto seleciona coorte de cadastro, trial ou primeira quest.
4. Sistema atualiza retenção e uso por funcionalidade.
5. Produto identifica queda ou melhoria de engajamento.

## 7. Impacto Frontend React

- Página de engajamento.
- Cards, gráfico de retenção/coorte e tabela de uso por funcionalidade.
- Filtros consistentes com dashboard e eventos.

## 8. Impacto Backend

- Endpoints agregados de engajamento.
- Cálculo de coortes a partir de eventos e dados de usuário.
- Cache ou pré-agregação se necessário para performance.

## 9. Critérios de aceite

### CA-001 — DAU/MAU visível

Dado que há usuários ativos,
quando produto abrir engajamento,
então deve ver DAU, MAU e razão DAU/MAU.

### CA-002 — Retenção por coorte

Dado que há usuários com janela D1 suficiente,
quando selecionar uma coorte,
então deve ver retenção agregada para essa coorte.

### CA-003 — Sem dados insuficientes

Dado que D30 ainda não tem janela temporal,
quando consultar retenção,
então o sistema deve indicar dados insuficientes em vez de exibir zero enganoso.

## 10. Decisão registrada

> Engajamento e retenção no admin serão agregados, acionáveis e honestos sobre janela de dados, evitando interpretações falsas no lançamento.
