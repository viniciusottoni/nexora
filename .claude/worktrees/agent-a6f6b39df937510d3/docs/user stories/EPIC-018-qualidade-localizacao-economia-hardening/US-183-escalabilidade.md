---
title: US-183 — Escalabilidade
sidebar_position: 183
---

# US-183 — Escalabilidade

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-183 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P1 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Engenharia Backend |
| Plataforma | Backend .NET 10 + PostgreSQL + Redis |
| Status | Planejada |

## 2. História do usuário

Como **time de engenharia**,
quero **preparar o backend para escalar sem estado local crítico**,
para **suportar crescimento de usuários sem reescrita emergencial**.

## 3. Objetivo

Revisar caminhos quentes e preparar arquitetura para réplicas, cache, filas e índices sem aumentar custo desnecessário no MVP.

## 4. Escopo

### Entra nesta US

- Garantir API stateless.
- Revisar uso de cache Redis para dados quentes.
- Preparar consultas para read replica futura.
- Criar/validar índices críticos.
- Identificar tarefas candidatas a fila assíncrona.
- Documentar limites iniciais de escala.

### Fora desta US

- Kubernetes no MVP.
- Migração imediata para microsserviços.
- Data warehouse.
- Particionamento prematuro sem necessidade.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | API não deve depender de estado local em memória para regra crítica. |
| RN-002 | Consultas de usuário/dia e histórico devem ter índices adequados. |
| RN-003 | Cache não pode ser fonte de verdade para progresso financeiro ou inventário. |
| RN-004 | Operações pesadas podem ser movidas para fila futura. |
| RN-005 | Escalabilidade deve preservar baixo custo inicial. |

## 6. Impacto Backend

- Revisar services com estado local.
- Revisar queries de quest, battle log, assinatura, inventário e notificações.
- Planejar cache e invalidação.
- Preparar documentação operacional.

## 7. Impacto DB

- Índices por userId/data.
- Índices por quest status.
- Índices por logs recentes.
- Revisão de queries N+1.

## 8. Impacto QA

- Teste com massa de dados.
- Validar paginação.
- Validar comportamento com cache desligado.
- Validar consistência de inventário e assinatura.

## 9. Critérios de aceite

### CA-001 — API stateless

Dado que há múltiplas instâncias da API,
quando usuário executa fluxo crítico,
então o resultado não deve depender de memória local da instância.

### CA-002 — Consultas críticas indexadas

Dado que histórico e quest usam userId/data,
quando consultar com massa maior,
então devem usar índices definidos.

## 10. Decisão registrada

> O AWAKEN deve escalar de forma incremental: stateless, índices e cache antes de complexidade operacional maior.
