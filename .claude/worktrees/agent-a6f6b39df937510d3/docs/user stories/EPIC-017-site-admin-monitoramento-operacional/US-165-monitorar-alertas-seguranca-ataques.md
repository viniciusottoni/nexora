---
title: US-165 — Monitorar alertas de segurança e ataques
sidebar_position: 165
---

# US-165 — Monitorar alertas de segurança e ataques

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-165 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Segurança, Engenharia e Admin |
| Plataforma | Web Admin (React) |
| Dependência | US-159, US-160, US-166, EPIC-018 US-180/US-181 |
| Status | Planejada |

## 2. História do usuário

Como **responsável por segurança**, quero **monitorar alertas e sinais de ataque**, para **responder rapidamente a brute force, abuso de API e tentativas de acesso indevido**.

## 3. Objetivo

Centralizar sinais mínimos de segurança no site admin, incluindo falhas de login, rate-limit hits, negações RBAC, tokens inválidos, tráfego anômalo e suspeitas de scraping.

## 4. Escopo

### Entra nesta US

- Tela de alertas de segurança.
- Filtros por tipo, severidade, origem, IP mascarado, usuário afetado, ambiente e período.
- Indicadores de tentativas de login inválidas e bloqueios.
- Eventos de negação RBAC e rate limit.
- Detalhe seguro do alerta com ações realizadas.
- Ação administrativa de marcar como analisado.

### Fora desta US

- SIEM corporativo completo.
- Resolução automática de ataques.
- Bloqueio global complexo por regra customizada.
- Análise forense avançada.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Alertas devem priorizar sinais de brute force, abuso de API, tokens inválidos, RBAC negado e scraping. |
| RN-002 | IP e identificadores devem ser tratados com minimização e mascaramento quando aplicável. |
| RN-003 | Apenas admin autorizado pode visualizar alertas sensíveis. |
| RN-004 | Toda leitura ou mudança de status de alerta crítico deve ser auditada. |
| RN-005 | Alertas devem distinguir ambiente para evitar falso positivo operacional. |

## 6. Fluxo principal

1. Admin de segurança acessa a tela de segurança.
2. Sistema lista alertas recentes por severidade.
3. Admin filtra por tipo, origem ou período.
4. Admin abre detalhe do alerta.
5. Admin marca alerta como analisado quando houver avaliação.

## 7. Impacto Frontend React

- Página de segurança.
- Tabela de alertas com severidade e status.
- Detalhe com linha do tempo e ações auditadas.

## 8. Impacto Backend

- Endpoint admin de alertas de segurança.
- Ingestão/consulta de eventos de auth, rate limit, RBAC e API.
- AuditLog para leitura e atualização de alertas críticos.

## 9. Critérios de aceite

### CA-001 — Alertas visíveis

Dado que houve falhas de login repetidas,
quando admin abrir segurança,
então deve ver alerta com tipo, severidade, origem e período.

### CA-002 — Negações RBAC

Dado que um usuário comum tentou acessar endpoint admin,
quando o evento for registrado,
então deve aparecer como sinal de segurança ou auditoria consultável.

### CA-003 — Ação auditada

Dado que admin marca um alerta crítico como analisado,
quando a ação for salva,
então deve gerar auditoria administrativa.

## 10. Decisão registrada

> O site admin terá painel mínimo de segurança para sinais acionáveis do MVP, sem substituir uma plataforma SOC/SIEM avançada.
