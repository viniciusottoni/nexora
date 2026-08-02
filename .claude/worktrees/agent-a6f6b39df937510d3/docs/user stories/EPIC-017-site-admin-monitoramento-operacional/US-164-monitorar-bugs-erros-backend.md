---
title: US-164 — Monitorar bugs e erros do backend
sidebar_position: 164
---

# US-164 — Monitorar bugs e erros do backend

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-164 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Engenharia, Suporte e Produto |
| Plataforma | Web Admin (React) |
| Dependência | US-158, EPIC-014 |
| Status | Planejada |

## 2. História do usuário

Como **engenharia do AWAKEN**, quero **monitorar bugs e erros do backend no site admin**, para **reduzir tempo de diagnóstico e priorizar correções antes que afetem mais usuários**.

## 3. Objetivo

Exibir falhas de backend e bugs operacionais normalizados por severidade, status, componente, ambiente, origem e data.

## 4. Escopo

### Entra nesta US

- Lista de bugs e erros do backend.
- Filtros por severidade, status, componente, ambiente, origem e período.
- Detalhe com mensagem sanitizada, correlationId, stack resumida segura e ocorrência.
- Agrupamento de ocorrências semelhantes.
- Link entre erro e ticket quando houver relação.
- Atualização de status de acompanhamento técnico.

### Fora desta US

- APM completo.
- Tracing distribuído avançado.
- Correção automática de bugs.
- Registro manual de bug interno, tratado na US-171.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Erros exibidos não podem conter token, senha ou payload sensível. |
| RN-002 | Severidade deve orientar priorização operacional. |
| RN-003 | CorrelationId deve ser exibido quando disponível. |
| RN-004 | Ambientes devem ser diferenciados para evitar confusão entre dev, staging e produção. |
| RN-005 | Agrupamento não pode ocultar ocorrência crítica recente. |

## 6. Fluxo principal

1. Engenharia acessa a tela de bugs.
2. Sistema lista erros recentes por severidade.
3. Engenharia filtra por ambiente, componente ou período.
4. Engenharia abre detalhe de uma falha.
5. Sistema exibe dados sanitizados para investigação.

## 7. Impacto Frontend React

- Página de bugs/erros.
- Tabela com severidade, status, componente e ambiente.
- Detalhe com correlationId e histórico de ocorrências.

## 8. Impacto Backend

- Endpoint admin de erros normalizados.
- Sanitização de logs antes da exposição.
- Integração com logs/crash/observabilidade do EPIC-014.

## 9. Critérios de aceite

### CA-001 — Erros listados por severidade

Dado que existem erros de backend,
quando engenharia acessar a tela,
então deve ver severidade, status, componente, ambiente e data.

### CA-002 — Detalhe seguro

Dado que um erro contém payload sensível no log original,
quando exibido no admin,
então o detalhe deve omitir ou mascarar esse conteúdo.

### CA-003 — Busca por correlationId

Dado que suporte recebeu um correlationId,
quando pesquisar no admin,
então deve localizar a ocorrência relacionada quando existir.

## 10. Decisão registrada

> O painel de bugs mostra falhas de backend de forma operacional e segura, priorizando investigação rápida sem expor dados sensíveis.
