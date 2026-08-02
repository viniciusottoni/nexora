---
title: US-210 — Separar API e Worker Hangfire com filas por carga
sidebar_position: 210
---

# US-210 — Separar API e Worker Hangfire com filas por carga

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-210 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção se houver teste aberto |
| Perfil principal | Backend, Worker, DevOps e Operação |
| Plano | Todos |
| Dependência principal | Hangfire, deploy, jobs recorrentes |
| Status | Planejada |

## 2. História do usuário

Como **usuário do AWAKEN**,

quero **que a API continue rápida mesmo quando rotinas de background estiverem rodando**,

para **não sentir lentidão por causa de notificações, importações ou tarefas recorrentes**.

## 3. Contexto

O servidor de jobs é registrado junto da aplicação da API. Ao escalar a API, cada réplica pode também processar tarefas em segundo plano. Para o MVP em produção aberta, a API deve ser stateless e as tarefas recorrentes devem rodar em processo separado.

## 4. Objetivo

Separar o processo web da API do processo Worker, permitindo escalar cada um de forma independente.

## 5. Escopo

### Entra nesta US

- Criar projeto/processo `Awaken.Worker` ou modo de execução separado.
- Remover processamento de jobs da API em produção.
- Manter painel operacional protegido.
- Configurar filas por tipo de carga.
- Documentar deploy separado de API e Worker.
- Configurar health check/readiness para ambos.

### Fora desta US

- Troca do Hangfire por outro sistema de fila.
- Kubernetes obrigatório.
- Autoscaling avançado por fila.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | API não deve executar tarefas pesadas em produção aberta. |
| RN-002 | Worker deve poder escalar independente da API. |
| RN-003 | Tipos diferentes de tarefa devem poder ter filas separadas. |
| RN-004 | Falha do Worker não deve derrubar API. |
| RN-005 | API deve continuar stateless para escalar horizontalmente. |

## 7. Filas sugeridas

- tarefas de negócio sensíveis;
- quests;
- notificações;
- importações;
- tarefas padrão.

## 8. Fluxo principal

1. API recebe requests dos usuários.
2. API agenda tarefas quando necessário.
3. Worker consome filas configuradas.
4. Cada fila tem concorrência e prioridade definidas.
5. Health checks indicam estado de API e Worker separadamente.

## 9. Impacto no Backend

- Criar projeto/processo Worker.
- Mover execução de jobs para Worker.
- Configurar filas e recorrências no Worker ou em bootstrap controlado.
- Garantir que API apenas registre o necessário para operação web.

## 10. Impacto no DevOps

- Deploy com dois serviços.
- Variáveis de ambiente compartilhadas com separação de responsabilidades.
- Logs e métricas separadas.
- Estratégia de restart independente.

## 11. Impacto no Banco

- Mesmo storage de jobs pode ser mantido no PostgreSQL no MVP.
- Monitorar locks e crescimento das tabelas operacionais.

## 12. Impacto no Flutter

Sem impacto direto.

## 13. Critérios de aceite

- API sobe sem executar tarefas pesadas em produção.
- Worker sobe e processa tarefas recorrentes.
- Filas estão configuradas por tipo de carga.
- API e Worker têm health checks próprios.
- Falha do Worker não derruba endpoints web.
- Documentação de deploy está atualizada.

## 14. Critérios de teste para QA

- API sem worker ativo;
- worker processando tarefa;
- fila de notificação;
- fila de importação;
- restart do worker;
- múltiplas réplicas da API sem duplicar workers indevidamente.

## ✅ Decisão registrada

API e Worker devem ser separados para o MVP em produção aberta, evitando que processamento em segundo plano degrade a experiência dos usuários.