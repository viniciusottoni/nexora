---
title: US-219 — Aprimorar Segurança com diagnóstico preventivo
sidebar_position: 219
---

# US-219 — Aprimorar Segurança com diagnóstico preventivo

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-219 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Origem | US-198, US-199, US-200, US-201 |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção |
| Perfil principal | Admin, Segurança, Suporte e Engenharia |
| Plataforma | Web Admin React + Backend Admin API |
| Status | Planejada |

## 2. História do usuário

Como **admin de segurança**,

quero **ver sinais preventivos de autenticação, autorização, limites e uso administrativo sensível**,

para **agir antes que uma falha ou abuso vire incidente crítico**.

## 3. Contexto

A tela atual de Segurança já lista alertas por tipo, severidade, status, origem, ambiente e permite marcar como analisado. Porém, após as US-198 a US-201, a visualização precisa ser mais preventiva: padrões de falha, tendência, usuários afetados, endpoints mais atingidos, motivo de bloqueio e correlação com audit log.

## 4. Objetivo

Evoluir a tela de Segurança para diagnóstico preventivo, não apenas lista de alertas.

## 5. Escopo

### Entra nesta US

- Cards de tendência por tipo de alerta.
- Gráfico de alertas por hora/dia.
- Agrupamento por origem mascarada, usuário afetado, endpoint e ambiente.
- Destaque para aumento repentino de falhas de login, tokens inválidos, limite atingido e autorização negada.
- Drilldown para audit log e usuário.
- Ações de triagem: marcar analisado, classificar falso positivo, adicionar nota e vincular a bug/incidente.

### Fora desta US

- Bloqueio automático avançado.
- SIEM corporativo.
- Investigação forense completa.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Dados sensíveis devem permanecer mascarados. |
| RN-002 | Alertas críticos devem aparecer antes de alertas baixos. |
| RN-003 | Aumento repentino deve gerar destaque visual. |
| RN-004 | Toda ação de triagem deve gerar auditoria. |
| RN-005 | Falso positivo não apaga o alerta; apenas muda sua classificação. |

## 7. Indicadores mínimos

- Alertas abertos por severidade.
- Falhas de autenticação por período.
- Limites atingidos por endpoint.
- Autorizações negadas por recurso.
- Alertas por ambiente.
- Top origens mascaradas.
- Tempo médio até análise.

## 8. Fluxo principal

1. Admin acessa Segurança.
2. Sistema exibe resumo preventivo acima da tabela.
3. Admin identifica tendência ou pico.
4. Admin abre grupo ou alerta individual.
5. Admin registra análise e navega para audit log/usuário/incidente.

## 9. Impacto no Frontend

- Evoluir `SecurityPage` atual.
- Adicionar cards, gráfico temporal e agrupamentos.
- Melhorar modal de detalhe com timeline e vínculos.

## 10. Impacto no Backend

- Criar endpoints agregados de segurança.
- Retornar séries temporais e agrupamentos seguros.
- Permitir classificação de alerta além de analisado.

## 11. Critérios de aceite

- Tela mostra resumo preventivo além da tabela.
- Admin visualiza tendência temporal de alertas.
- Admin agrupa por endpoint, ambiente, usuário e origem mascarada.
- Ação de triagem gera auditoria.
- Não há exposição de token, senha, IP completo ou payload sensível.

## 12. Critérios de teste para QA

- pico de falha de login;
- muitos limites atingidos;
- autorização negada;
- alerta crítico;
- falso positivo;
- drilldown para audit log.

## ✅ Decisão registrada

A página de Segurança deve evoluir de listagem reativa para diagnóstico preventivo, conectando alertas, tendências e auditoria.