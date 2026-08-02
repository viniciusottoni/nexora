---
title: US-083 — Ver histórico completo como assinante
sidebar_position: 83
---

# US-083 — Ver histórico completo como assinante

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-083 |
| Épico | EPIC-011 — Histórico Básico e Log de Batalha |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Assinante mensal ou anual |
| Plano | Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **assinante do AWAKEN**, quero **ver meu histórico completo de quests concluídas**, para **acompanhar minha jornada além do resumo recente**.

## 3. Contexto

O histórico completo é P1 porque amplia valor percebido, mas o MVP pode começar com uma lista recente. Para assinantes, o sistema deve estar preparado para consulta paginada de logs antigos.

## 4. Objetivo

Permitir que assinantes acessem histórico completo/paginado de quests concluídas, mantendo performance e consistência.

## 5. Escopo

### Entra nesta US

- Histórico completo ou paginado para assinantes.
- Filtro simples por tipo de quest como P1 opcional.
- Paginação ou carregamento incremental.
- Exibição de XP, data e itens quando houver.
- Preservação de logs antigos.

### Fora desta US

- Dashboard avançado.
- Gráficos complexos.
- Exportação de dados.
- Comparativos profundos.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Histórico completo é P1. |
| RN-002 | Assinante ativo pode consultar histórico completo/paginado. |
| RN-003 | Acesso expirado pode ver estado limitado com CTA. |
| RN-004 | Logs não devem ser apagados por expiração. |
| RN-005 | Consulta deve ser paginada ou limitada para proteger performance. |
| RN-006 | XP exibido deve bater com XP aplicado. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Premium Mensal | Pode ver histórico completo/paginado. |
| Premium Anual | Pode ver histórico completo/paginado. |
| Usuário em Trial | Usa regra de histórico durante trial. |
| Trial expirado | Estado limitado com CTA. |
| Assinatura expirada | Estado limitado com CTA. |
| Visitante | Não pode visualizar. |

## 8. Fluxo principal

1. Assinante acessa histórico.
2. App solicita primeira página de logs.
3. Backend retorna registros ordenados por conclusão.
4. Usuário rola a lista.
5. App carrega próximas páginas quando necessário.

## 9. Fluxos alternativos

### 9.1. Assinatura expirada

Exibir estado limitado com CTA de reativação.

### 9.2. Sem registros antigos

Exibir lista recente ou empty state.

## 10. Estados esperados

- carregando página;
- lista paginada;
- fim da lista;
- vazio;
- acesso limitado;
- erro.

## 11. Impacto Flutter

- Lista com paginação/carregamento incremental.
- Estado de fim de histórico.
- Estado limitado para acesso expirado.
- Textos localizados.

## 12. Impacto Backend

- Endpoint paginado.
- Validação de assinatura ativa.
- Ordenação por `completedAt`.
- Índices para performance.

## 13. Impacto DB

Entidade principal:

- QuestLog.

Índices sugeridos:

- userId + completedAt;
- userId + questType.

## 14. Impacto Gamificação

- Reforça longa jornada e consistência.
- Não concede XP por visualização.

## 15. Impacto Monetização

- Histórico completo é benefício P1 para assinantes.
- Expirado preserva dados e direciona para reativação.

## 16. Contrato API sugerido

```txt
GET /api/hunter/battle-log?page=1&pageSize=20
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| full_battle_log_viewed | Quando assinante vê histórico completo. |
| battle_log_page_loaded | Quando nova página é carregada. |

## 18. Critérios de aceite

### CA-001 — Assinante vê histórico completo

Dado que o usuário é assinante ativo,
Quando acessar histórico completo,
Então deve conseguir navegar pelos logs paginados.

### CA-002 — Expirado limitado

Dado que a assinatura expirou,
Quando acessar histórico completo,
Então deve ver estado limitado com CTA.

## 19. Critérios de teste QA

- assinante mensal;
- assinante anual;
- múltiplas páginas;
- lista vazia;
- assinatura expirada;
- performance com muitos logs;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

Histórico completo é P1 e deve ser preparado com paginação para assinantes sem comprometer performance do MVP.
