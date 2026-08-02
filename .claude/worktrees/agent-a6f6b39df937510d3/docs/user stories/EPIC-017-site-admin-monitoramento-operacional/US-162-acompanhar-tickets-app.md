---
title: US-162 — Acompanhar tickets abertos pelo app
sidebar_position: 162
---

# US-162 — Acompanhar tickets abertos pelo app

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-162 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Suporte, Produto e Engenharia |
| Plataforma | Web Admin (React) |
| Dependência | US-158, EPIC-018 US-176 |
| Status | Planejada |

## 2. História do usuário

Como **suporte do AWAKEN**, quero **acompanhar tickets abertos pelo app**, para **responder problemas reais dos usuários sem depender de canais externos dispersos**.

## 3. Objetivo

Exibir no site admin a lista de tickets criados exclusivamente pelo app, com busca, filtros e acesso ao detalhe.

## 4. Escopo

### Entra nesta US

- Listagem de tickets abertos pelo app.
- Busca por usuário, assunto, categoria ou correlationId quando existir.
- Filtros por status, prioridade, categoria, origem e período.
- Detalhe do ticket com histórico, contexto seguro e anexos permitidos.
- Indicadores de volume por status e prioridade.
- Paginação e ordenação por data/prioridade.

### Fora desta US

- Criação de ticket pelo site admin.
- Chat em tempo real com usuário.
- SLA avançado e automações de resposta.
- Alteração de status e triagem, tratadas na US-163.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Tickets do MVP são abertos exclusivamente pelo app. |
| RN-002 | Site admin pode listar e consultar tickets, mas não criar ticket para usuário final. |
| RN-003 | Dados sensíveis devem ser minimizados no detalhe do ticket. |
| RN-004 | Filtros devem permitir priorizar tickets recentes e críticos. |
| RN-005 | Usuário comum não pode acessar dados de tickets pelo admin. |

## 6. Fluxo principal

1. Suporte acessa a tela de tickets.
2. Sistema lista tickets mais recentes ou mais prioritários.
3. Suporte aplica busca ou filtros.
4. Suporte abre o detalhe do ticket.
5. Sistema exibe histórico e contexto seguro para análise.

## 7. Impacto Frontend React

- Página de tickets.
- Tabela com filtros, busca, paginação e detalhe.
- Chips de status, prioridade e categoria.

## 8. Impacto Backend

- Endpoint admin read-only de tickets.
- Projeção segura do detalhe do ticket.
- Índices para busca por status, categoria, usuário e data.

## 9. Critérios de aceite

### CA-001 — Tickets listados

Dado que existem tickets abertos pelo app,
quando suporte acessar a tela,
então deve ver a lista paginada com status, prioridade, categoria e data.

### CA-002 — Filtros funcionam

Dado que há tickets de categorias diferentes,
quando suporte filtrar por categoria e status,
então a lista deve retornar apenas tickets correspondentes.

### CA-003 — Sem criação pelo admin

Dado que suporte está na tela de tickets,
quando procurar ação de criar ticket,
então essa ação não deve estar disponível no MVP.

## 10. Decisão registrada

> O app é a porta de entrada dos tickets; o site admin é a central de acompanhamento e consulta operacional.
