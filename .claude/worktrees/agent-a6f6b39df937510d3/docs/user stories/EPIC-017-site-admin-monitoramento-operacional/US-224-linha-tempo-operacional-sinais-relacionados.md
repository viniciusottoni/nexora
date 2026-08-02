---
title: US-224 — Visualizar linha do tempo operacional com sinais relacionados
sidebar_position: 224
---

# US-224 — Visualizar linha do tempo operacional com sinais relacionados

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-224 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Origem | US-203, US-213 e diagnósticos da EPIC-020 |
| Prioridade | P1 |
| Fase | MVP em produção / evolução preventiva |
| Perfil principal | Admin, Suporte, Segurança, Engenharia e Produto |
| Plataforma | Web Admin React + Backend Admin API |
| Status | Planejada |

## 2. História do usuário

Como **admin analisando um problema**,

quero **ver uma linha do tempo com sinais relacionados de alertas, tickets, eventos, métricas e usuário afetado**,

para **entender impacto e próxima ação sem navegar manualmente por várias telas isoladas**.

## 3. Contexto

O Admin já possui Dashboard, Segurança, Audit Log, Eventos, Tickets, Bugs e Relatórios. Com as US-194 a US-215, haverá mais sinais técnicos. O risco é cada tela mostrar dados isolados. Para diagnóstico real, o Admin precisa conectar sinais por período, usuário, recurso e ambiente.

## 4. Objetivo

Criar uma visão operacional correlacionada para acelerar diagnóstico e prevenção.

## 5. Escopo

### Entra nesta US

- Linha do tempo unificada por usuário, ambiente, recurso ou período.
- Associação entre alerta, evento administrativo, erro operacional, ticket, evento de produto e métrica.
- Card de impacto estimado: usuários afetados, recursos afetados, período e severidade.
- Sugestão de próxima ação operacional segura.
- Links cruzados entre telas existentes.
- Filtros por ambiente, severidade, usuário, recurso e período.

### Fora desta US

- IA automática de causa raiz.
- Resolução automática.
- Chat operacional em tempo real.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A linha do tempo deve usar apenas dados seguros e minimizados. |
| RN-002 | Dados pessoais devem ser mascarados quando não forem essenciais. |
| RN-003 | Relação incerta deve ser marcada como provável, não definitiva. |
| RN-004 | Toda ação administrativa tomada a partir da tela deve gerar auditoria. |
| RN-005 | Links cruzados devem respeitar permissões do admin. |

## 7. Indicadores mínimos

- Linha do tempo de sinais relacionados.
- Usuários afetados estimados.
- Recursos afetados.
- Alertas relacionados.
- Tickets relacionados.
- Bugs/incidentes relacionados.
- Identificador de correlação quando disponível.

## 8. Fluxo principal

1. Admin abre detalhe de alerta, ticket, bug ou métrica.
2. Admin clica em `Ver linha do tempo`.
3. Sistema mostra sinais relacionados.
4. Admin visualiza impacto e contexto.
5. Admin toma ação ou registra observação.

## 9. Impacto no Frontend

- Criar drawer/página de linha do tempo operacional.
- Adicionar botão `Ver linha do tempo` em Segurança, Audit Log, Bugs, Tickets e Performance.
- Criar componente visual reutilizável.

## 10. Impacto no Backend

- Endpoint de sinais relacionados.
- Busca segura por período, recurso, usuário e identificador de correlação.
- Resposta agregada e sanitizada.

## 11. Critérios de aceite

- Admin consegue abrir linha do tempo a partir de alerta.
- Timeline mostra sinais relacionados.
- Impacto estimado aparece de forma clara.
- Relação incerta é indicada como provável.
- Dados sensíveis são mascarados.
- Links respeitam permissões.

## 12. Critérios de teste para QA

- alerta com identificador de correlação;
- ticket relacionado;
- usuário afetado;
- bug relacionado;
- nenhum sinal relacionado encontrado;
- admin sem permissão para parte dos dados.

## ✅ Decisão registrada

O Admin deve evoluir de telas isoladas para linha do tempo operacional, reduzindo tempo de diagnóstico e prevenção.