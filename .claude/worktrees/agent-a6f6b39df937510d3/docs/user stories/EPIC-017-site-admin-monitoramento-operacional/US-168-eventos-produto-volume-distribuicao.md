---
title: US-168 — Visualizar eventos do produto por volume e distribuição
sidebar_position: 168
---

# US-168 — Visualizar eventos do produto por volume e distribuição

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-168 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto, Engenharia e QA |
| Plataforma | Web Admin (React) |
| Dependência | US-158, EPIC-014 |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **visualizar eventos do produto por volume e distribuição**, para **identificar uso real, anomalias e falhas de instrumentação no MVP**.

## 3. Objetivo

Expor uma visão administrativa dos eventos instrumentados no EPIC-014, com volume por período, distribuição por tipo e filtros básicos.

## 4. Escopo

### Entra nesta US

- Lista de eventos conhecidos pela taxonomia vigente.
- Volume por evento em período selecionado.
- Distribuição por plataforma, versão, ambiente e país/idioma quando disponível.
- Gráfico temporal por evento.
- Busca por nome de evento.
- Indicação de eventos sem volume recente.

### Fora desta US

- Criação de eventos pelo admin.
- Data warehouse.
- Testes A/B.
- Payload individual com dados pessoais.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A tela deve refletir a taxonomia atual de analytics. |
| RN-002 | Eventos devem ser exibidos de forma agregada. |
| RN-003 | Payloads sensíveis ou identificáveis não devem ser expostos. |
| RN-004 | Eventos sem volume recente devem ser visíveis para detectar quebra de tracking. |
| RN-005 | Ambiente e versão devem ser filtros quando disponíveis. |

## 6. Fluxo principal

1. Produto acessa a tela de eventos.
2. Sistema exibe volume dos principais eventos no período padrão.
3. Produto busca ou seleciona um evento.
4. Sistema mostra distribuição e tendência temporal.
5. Produto identifica anomalias ou ausência de eventos esperados.

## 7. Impacto Frontend React

- Página de eventos.
- Tabela de eventos, gráfico temporal e filtros.
- Estado para eventos sem dados.

## 8. Impacto Backend

- Endpoint agregado de eventos.
- Consulta à fonte de analytics/logs disponível.
- Normalização da taxonomia do EPIC-014.

## 9. Critérios de aceite

### CA-001 — Volume por evento

Dado que eventos foram registrados,
quando produto abrir a tela,
então deve ver volume por evento no período selecionado.

### CA-002 — Distribuição por versão

Dado que eventos possuem versão do app,
quando filtrar por versão,
então a distribuição deve refletir apenas aquela versão.

### CA-003 — Evento sem volume

Dado que um evento esperado não ocorreu no período,
quando consultar a tela,
então ele deve aparecer como sem volume ou ausente de forma explícita.

## 10. Decisão registrada

> A tela de eventos do admin serve para leitura agregada e validação operacional da instrumentação, não para exploração irrestrita de payloads.
