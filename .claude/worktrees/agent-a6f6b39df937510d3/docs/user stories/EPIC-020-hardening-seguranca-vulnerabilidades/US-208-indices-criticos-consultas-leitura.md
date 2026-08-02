---
title: US-208 — Criar índices críticos e otimizar consultas de leitura
sidebar_position: 208
---

# US-208 — Criar índices críticos e otimizar consultas de leitura

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-208 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção |
| Perfil principal | Backend, PostgreSQL, QA e DevOps |
| Plano | Todos |
| Dependência principal | EF Core, PostgreSQL, repositórios e migrations |
| Status | Planejada |

## 2. História do usuário

Como **usuário do AWAKEN**,

quero **que telas de quest, perfil, histórico, loja e notificações carreguem rápido**,

para **usar o app sem travamentos mesmo quando a base crescer**.

## 3. Contexto

Algumas consultas frequentes ainda carregam entidades completas, usam tracking onde não há escrita e precisam de índices alinhados ao padrão real de busca. O MVP precisa sair com consultas críticas otimizadas antes de aumentar o volume de usuários.

## 4. Objetivo

Criar índices críticos, remover leituras perigosas sem paginação e padronizar consultas de leitura com `AsNoTracking` e projeções.

## 5. Escopo

### Entra nesta US

- Mapear queries críticas do MVP.
- Criar índices para filtros e ordenações reais.
- Usar `AsNoTracking` em leitura.
- Usar projeções DTO quando não houver update.
- Remover ou restringir métodos `GetAllAsync` perigosos.
- Criar testes mínimos de consulta.
- Validar plano de execução em queries críticas.

### Fora desta US

- Read replica.
- Sharding.
- Mudança de banco.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Consulta de leitura não deve usar tracking sem necessidade. |
| RN-002 | Listagem deve ter paginação ou limite explícito. |
| RN-003 | Query crítica deve ter índice compatível com filtro e ordenação. |
| RN-004 | Repositórios não devem expor busca total sem caso de uso controlado. |
| RN-005 | Migration de índice deve ser revisada para impacto em produção. |

## 7. Áreas prioritárias de índice

- Histórico de batalha por usuário e data de conclusão.
- Quests diárias por usuário, tipo e data.
- Quests pendentes de verificação por tipo, data e status.
- Preferências de notificação com push ativo.
- Logs de notificação por usuário, tipo e data.
- Produtos ativos da loja por status e chave.
- Catálogo aprovado por status, ambiente, dificuldade e nome.
- Assinaturas por usuário, status e expiração.

## 8. Fluxo principal

1. Listar endpoints e jobs críticos.
2. Mapear queries executadas.
3. Ajustar repositórios para projeção e leitura sem tracking.
4. Criar migration com índices.
5. Validar plano de execução.
6. Rodar testes de regressão.

## 9. Impacto no Backend

- Ajustes em repositórios de quest, battle log, catálogo, loja, notificação, assinatura e progressão.
- Criação de DTOs/projeções internas.
- Remoção ou restrição de consultas sem limite.

## 10. Impacto no Banco

- Novas migrations de índice.
- Ganho esperado de leitura nas rotas e jobs críticos.
- Pequeno custo adicional em escrita, aceitável para o MVP.

## 11. Impacto no Flutter

Sem impacto contratual se as respostas forem mantidas. Deve reduzir latência percebida.

## 12. Critérios de aceite

- Queries críticas usam índice compatível.
- Leituras sem update usam `AsNoTracking`.
- Buscas totais perigosas são removidas, protegidas ou não usadas em produção.
- Battle log, quest diária, loja e jobs têm consultas otimizadas.
- Migration de índices é criada.
- Testes de regressão passam.

## 13. Critérios de teste para QA

- carga simulada em battle log;
- geração de quest com catálogo grande;
- consulta de loja;
- job de notificação;
- job de penalidade;
- comparação básica de tempo antes/depois em staging.

## ✅ Decisão registrada

O MVP deve sair com índices e consultas de leitura compatíveis com os principais fluxos reais, evitando varreduras e tracking desnecessário.