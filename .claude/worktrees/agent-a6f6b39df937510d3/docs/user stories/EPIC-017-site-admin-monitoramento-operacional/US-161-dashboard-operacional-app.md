---
title: US-161 — Visualizar dashboard operacional do app
sidebar_position: 161
---

# US-161 — Visualizar dashboard operacional do app

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-161 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Admin, Produto, Suporte e Engenharia |
| Plataforma | Web Admin (React) |
| Dependência | US-158, US-159, US-160, EPIC-014 |
| Status | Planejada |

## 2. História do usuário

Como **time operacional do AWAKEN**, quero **ver um dashboard com os principais sinais do app**, para **identificar rapidamente saúde, uso, receita e pendências antes do release**.

## 3. Objetivo

Consolidar em uma tela inicial os indicadores mínimos de operação: usuários, DAU, tickets abertos, MRR, atividade recente e top eventos.

## 4. Escopo

### Entra nesta US

- Cards de total de usuários, DAU, tickets abertos e MRR.
- Gráfico de usuários ativos diários por período.
- Feed de atividade recente operacional.
- Lista de top eventos do produto.
- Filtros por período e ambiente quando aplicável.
- Estados de loading, erro e vazio.

### Fora desta US

- BI avançado.
- Métricas financeiras detalhadas.
- Alertas automáticos complexos.
- Edição de dados pelo dashboard.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Dashboard deve usar dados agregados e minimizados. |
| RN-002 | MRR deve ser exibido apenas quando houver fonte confiável. |
| RN-003 | Dados pessoais não devem aparecer nos cards agregados. |
| RN-004 | Período padrão deve priorizar visão recente do release. |
| RN-005 | Falha parcial de uma métrica não deve quebrar o dashboard inteiro. |

## 6. Fluxo principal

1. Admin acessa o dashboard.
2. Sistema carrega indicadores agregados.
3. Admin ajusta período ou ambiente.
4. Sistema atualiza cards, gráfico, feed e top eventos.
5. Admin navega para módulos específicos a partir dos sinais exibidos.

## 7. Impacto Frontend React

- Página de dashboard.
- Cards de indicador, gráfico temporal, feed e lista ranqueada.
- Tratamento de erro parcial por bloco.

## 8. Impacto Backend

- Endpoint agregado de dashboard admin.
- Consultas a usuários, tickets, assinaturas, eventos e logs.
- Sanitização de payloads para consumo administrativo.

## 9. Critérios de aceite

### CA-001 — Indicadores essenciais

Dado que existem dados operacionais,
quando o admin abrir o dashboard,
então deve ver total de usuários, DAU, tickets abertos e MRR.

### CA-002 — Top eventos visível

Dado que eventos do produto foram instrumentados,
quando o dashboard carregar,
então deve exibir os eventos mais frequentes no período selecionado.

### CA-003 — Falha parcial tolerada

Dado que uma fonte de métrica falha,
quando o dashboard carregar,
então os demais blocos devem continuar visíveis com indicação de erro no bloco afetado.

## 10. Decisão registrada

> O dashboard do admin prioriza sinais agregados e acionáveis para operação do MVP, sem virar ferramenta de BI complexa.
