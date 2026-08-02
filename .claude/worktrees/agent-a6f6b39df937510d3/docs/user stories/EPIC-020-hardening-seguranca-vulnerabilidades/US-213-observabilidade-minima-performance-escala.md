---
title: US-213 — Implantar observabilidade mínima de performance e escala
sidebar_position: 213
---

# US-213 — Implantar observabilidade mínima de performance e escala

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-213 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção |
| Perfil principal | DevOps, backend, Flutter, QA e Produto |
| Plano | Todos |
| Dependência principal | Serilog, OpenTelemetry, health checks, dashboards |
| Status | Planejada |

## 2. História do usuário

Como **responsável pelo MVP em produção**,

quero **enxergar latência, erros, filas, banco, cache e falhas do app**,

para **tomar decisão antes que usuários percebam degradação**.

## 3. Contexto

Sem observabilidade mínima, não há como saber se o app suporta o volume real, quais endpoints estão lentos ou se jobs estão atrasando. O MVP precisa de métricas e logs acionáveis antes do teste aberto.

## 4. Objetivo

Implantar observabilidade mínima para API, Worker, banco, Redis, jobs e app mobile.

## 5. Escopo

### Entra nesta US

- Métricas de HTTP: RPS, p50, p95, p99, taxa de erro e status code.
- Métricas de banco: conexões, tempo de query e falhas.
- Métricas de Redis: hit/miss e indisponibilidade.
- Métricas de jobs: duração, processados, sucesso, falha e atraso.
- Logs estruturados com correlationId.
- Health e readiness separados.
- Dashboard mínimo de produção/staging.
- Alertas mínimos para erro elevado e latência alta.

### Fora desta US

- Observabilidade corporativa completa.
- SIEM avançado.
- APM pago obrigatório.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Produção deve expor health check e readiness. |
| RN-002 | Toda request deve ter correlationId. |
| RN-003 | Métricas de p95/p99 devem existir para endpoints críticos. |
| RN-004 | Jobs devem registrar duração e resultado. |
| RN-005 | Alertas mínimos devem existir antes do teste aberto. |
| RN-006 | Logs não podem conter dados sensíveis. |

## 7. Endpoints/fluxos críticos

- Login, registro e refresh.
- Status de acesso.
- Geração e consulta da quest diária.
- Conclusão de exercício e quest.
- Loja, assinatura e IAP.
- Jobs de notificação e penalidade.
- Suporte e health checks.

## 8. Fluxo principal

1. Request entra com correlationId.
2. API registra métrica de duração e status.
3. Queries e cache registram métricas relevantes.
4. Jobs registram início, fim, duração e contadores.
5. Dashboard exibe indicadores.
6. Alertas disparam em degradação.

## 9. Impacto no Backend

- Configurar OpenTelemetry ou alternativa.
- Ajustar logs estruturados.
- Adicionar métricas customizadas para cache/jobs.
- Separar health de readiness.

## 10. Impacto no Flutter

- Garantir crash/error reporting.
- Enviar versão do app em headers/eventos quando aplicável.
- Correlacionar falhas relevantes sem dados sensíveis.

## 11. Impacto no DevOps

- Criar dashboard staging/prod.
- Definir alertas mínimos.
- Guardar histórico de métricas.

## 12. Critérios de aceite

- Dashboard mostra RPS, erro, p95 e p99 da API.
- Dashboard mostra status de banco e Redis.
- Jobs registram duração e resultado.
- Health/readiness estão separados.
- Alertas mínimos estão configurados.
- Logs possuem correlationId.
- Dados sensíveis não aparecem nos logs.

## 13. Critérios de teste para QA

- request bem-sucedida aparece em métrica;
- request com erro aparece em métrica;
- job executado aparece no dashboard/log;
- Redis indisponível em staging gera sinal observável;
- banco indisponível em staging afeta readiness;
- alerta de erro elevado dispara em teste controlado.

## ✅ Decisão registrada

O MVP não deve abrir sem observabilidade mínima. Performance que não é medida não pode ser garantida.