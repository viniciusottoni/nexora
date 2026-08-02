---
title: US-170 — Gerar relatórios operacionais do admin
sidebar_position: 170
---

# US-170 — Gerar relatórios operacionais do admin

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-170 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto, Suporte, Engenharia e Admin |
| Plataforma | Web Admin (React) |
| Dependência | US-161, US-162, US-164, US-166, US-167, US-168, US-169 |
| Status | Planejada |

## 2. História do usuário

Como **time operacional**, quero **gerar relatórios administrativos consolidados**, para **acompanhar saúde do MVP, pendências e evolução sem montar consultas manuais toda vez**.

## 3. Objetivo

Disponibilizar relatórios operacionais simples, exportáveis e auditáveis com base nos dados já disponíveis no EPIC-017.

## 4. Escopo

### Entra nesta US

- Relatório de operação diária: usuários, DAU, tickets, erros e alertas.
- Relatório de suporte: tickets por status, prioridade, categoria e tempo em aberto.
- Relatório técnico: erros por severidade, componente e ambiente.
- Relatório de produto: eventos, engajamento e retenção agregada.
- Filtros por período e ambiente.
- Exportação CSV de recortes permitidos.
- Auditoria de geração/exportação.

### Fora desta US

- Ferramenta complexa de BI.
- Agendamento automático de relatórios.
- Envio por email.
- Relatórios financeiros avançados.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Relatórios devem consolidar apenas dados já disponíveis no admin. |
| RN-002 | Exportações devem respeitar perfil administrativo e minimização de dados. |
| RN-003 | Toda exportação deve gerar auditoria. |
| RN-004 | Relatório não deve expor senha, token, segredo ou payload sensível. |
| RN-005 | Métricas sem fonte confiável devem aparecer como indisponíveis, não como zero. |

## 6. Fluxo principal

1. Admin acessa relatórios.
2. Admin escolhe tipo de relatório e período.
3. Sistema consolida dados disponíveis.
4. Admin visualiza resumo e tabelas.
5. Admin exporta recorte permitido, gerando auditoria.

## 7. Impacto Frontend React

- Página de relatórios.
- Seleção de tipo, período e ambiente.
- Visualização resumida e exportação.

## 8. Impacto Backend

- Endpoints agregados para relatórios.
- Reuso dos mesmos contratos de dashboard, tickets, bugs, eventos e engajamento.
- Controle de autorização por perfil.

## 9. Critérios de aceite

### CA-001 — Relatório operacional

Dado que existem dados do período,
quando admin gerar relatório diário,
então deve ver usuários, DAU, tickets, erros e alertas agregados.

### CA-002 — Exportação auditada

Dado que admin exporta um relatório,
quando o CSV for gerado,
então deve existir registro de auditoria da exportação.

### CA-003 — Métrica indisponível

Dado que uma métrica não tem fonte configurada,
quando o relatório carregar,
então deve aparecer como indisponível, sem quebrar o relatório.

## 10. Decisão registrada

> Relatórios do admin são recortes operacionais simples e seguros, construídos sobre dados já disponíveis, sem criar uma plataforma de BI no MVP.
