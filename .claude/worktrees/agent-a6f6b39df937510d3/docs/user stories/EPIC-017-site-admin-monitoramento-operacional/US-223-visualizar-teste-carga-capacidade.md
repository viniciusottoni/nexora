---
title: US-223 — Visualizar teste de carga e capacidade do MVP
sidebar_position: 223
---

# US-223 — Visualizar teste de carga e capacidade do MVP

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-223 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Origem | US-215 |
| Prioridade | P0 |
| Fase | Bloqueador do teste aberto |
| Perfil principal | Admin, Produto, DevOps, QA e Engenharia |
| Plataforma | Web Admin React + Backend Admin API + Staging |
| Status | Planejada |

## 2. História do usuário

Como **dono do produto e admin técnico**,

quero **visualizar o resultado dos testes de carga e o plano de capacidade do MVP**,

para **decidir com segurança se o app pode abrir para mais usuários**.

## 3. Contexto

A US-215 exige teste de carga documentado antes do teste aberto. O Admin deve exibir o resultado de forma simples: cenários executados, metas, resultado, gargalos e decisão go/no-go.

## 4. Objetivo

Criar tela de capacidade do MVP com histórico de testes de carga e readiness de escala.

## 5. Escopo

### Entra nesta US

- Histórico dos testes de carga executados.
- Resultado por cenário: login, refresh, quest, loja, histórico, jobs e assinatura/IAP.
- Comparativo entre meta e resultado: erro, p95, p99 e throughput.
- Indicação go/no-go.
- Lista de gargalos encontrados e status de correção.
- Plano de capacidade inicial: API, Worker, banco, Redis e CDN/storage.

### Fora desta US

- Executar teste de carga diretamente pelo Admin.
- Simulação avançada de custos.
- Previsão automática de crescimento.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Teste aberto exige pelo menos um teste de carga aprovado ou decisão explícita de risco. |
| RN-002 | Resultado deve indicar commit, ambiente, data e parâmetros do teste. |
| RN-003 | Falha em cenário crítico deve deixar readiness como bloqueado. |
| RN-004 | Dados usados no teste devem ser sintéticos. |
| RN-005 | Métricas devem ser comparadas com metas definidas. |

## 7. Indicadores mínimos

- Último teste executado.
- Status geral: aprovado, atenção, bloqueado ou sem dados.
- p95 e p99 por cenário.
- Taxa de erro por cenário.
- Throughput por cenário.
- Gargalos abertos.
- Capacidade inicial recomendada.

## 8. Fluxo principal

1. Admin acessa Capacidade MVP.
2. Sistema mostra último resultado.
3. Admin compara metas e resultados.
4. Admin abre detalhe de cenário.
5. Admin consulta gargalos e decisão go/no-go.

## 9. Impacto no Frontend

- Nova página ou aba `Capacidade MVP`.
- Cards de status geral.
- Tabela de cenários.
- Gráficos comparando meta x resultado.

## 10. Impacto no Backend

- Endpoint admin para registrar/ler resultados de teste de carga.
- Modelo de dados para cenário, resultado e decisão.
- Integração opcional com artefatos de CI/staging.

## 11. Critérios de aceite

- Admin visualiza último teste de carga.
- Admin visualiza resultado por cenário.
- Metas e resultados são comparados.
- Go/no-go aparece claramente.
- Gargalos ficam listados com status.
- Histórico de testes fica disponível.

## 12. Critérios de teste para QA

- teste aprovado;
- teste bloqueado;
- cenário crítico falho;
- ambiente sem teste;
- histórico com múltiplas execuções;
- exportação/registro do resultado.

## ✅ Decisão registrada

O Admin deve exibir a capacidade validada do MVP para que abertura pública seja uma decisão baseada em evidência, não em suposição.