---
title: US-221 — Monitorar rotinas, workers e atualizações operacionais
sidebar_position: 221
---

# US-221 — Monitorar rotinas, workers e atualizações operacionais

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-221 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Origem | US-207, US-210, US-211 |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção |
| Perfil principal | Admin, DevOps e Backend |
| Plataforma | Web Admin React + Backend Admin API + Worker |
| Status | Planejada |

## 2. História do usuário

Como **admin técnico do AWAKEN**,

quero **monitorar rotinas recorrentes, workers e atualizações operacionais**,

para **prevenir atraso de notificações, falhas de progressão, acúmulo de filas e problemas durante deploy**.

## 3. Contexto

As US-207, US-210 e US-211 tornam rotinas recorrentes paginadas, separam API/Worker e controlam atualizações operacionais fora do startup normal da API. O Admin precisa visualizar se essas rotinas estão saudáveis e se alguma delas está atrasada, falhando ou processando volume anormal.

## 4. Objetivo

Criar visão operacional de rotinas recorrentes, workers, filas e atualizações técnicas controladas.

## 5. Escopo

### Entra nesta US

- Status dos workers ativos.
- Lista de rotinas recorrentes e última execução.
- Duração, volume processado, sucesso, falha e atraso por rotina.
- Status de filas por tipo de carga.
- Histórico de atualizações operacionais: pendente, sucesso, falha e duração.
- Alertas visuais para rotina atrasada ou fila acumulada.
- Link para logs e audit log relacionados.

### Fora desta US

- Executar atualização operacional manual pelo Admin.
- Editar agenda de rotina pelo Admin no MVP.
- Substituir painel nativo do sistema de jobs.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Rotina atrasada deve aparecer em destaque. |
| RN-002 | Falha recorrente deve gerar status crítico. |
| RN-003 | Worker indisponível deve deixar a área crítica. |
| RN-004 | Atualização operacional com falha deve bloquear readiness do MVP. |
| RN-005 | Admin não deve executar ações sensíveis por essa tela no MVP. |

## 7. Indicadores mínimos

- Workers online/offline.
- Rotinas agendadas.
- Última execução por rotina.
- Próxima execução por rotina.
- Duração média e última duração.
- Itens processados no último lote.
- Falhas recentes.
- Fila acumulada.
- Última atualização operacional controlada.

## 8. Fluxo principal

1. Admin acessa Rotinas e Workers.
2. Sistema exibe status dos workers e filas.
3. Sistema lista rotinas recorrentes.
4. Admin identifica atraso ou falha.
5. Admin abre detalhe e navega para logs/alertas relacionados.

## 9. Impacto no Frontend

- Nova página `Rotinas e Workers` ou seção em Saúde do MVP.
- Cards de worker/fila.
- Tabela de rotinas com status e duração.
- Timeline de execuções recentes.

## 10. Impacto no Backend

- Endpoint admin de leitura de rotinas e workers.
- Agregação de métricas do sistema de jobs.
- Registro seguro de execuções operacionais.

## 11. Critérios de aceite

- Admin visualiza workers ativos.
- Admin visualiza última e próxima execução das rotinas.
- Falhas e atrasos ficam destacados.
- Fila acumulada fica visível.
- Histórico de atualização operacional aparece sem permitir ação sensível.
- Links para logs/auditoria funcionam.

## 12. Critérios de teste para QA

- worker ativo;
- worker parado;
- rotina bem-sucedida;
- rotina com falha;
- rotina atrasada;
- fila acumulada;
- atualização operacional com sucesso e falha.

## ✅ Decisão registrada

O Admin deve expor saúde de rotinas, workers e atualizações operacionais para prevenir falhas invisíveis no MVP.