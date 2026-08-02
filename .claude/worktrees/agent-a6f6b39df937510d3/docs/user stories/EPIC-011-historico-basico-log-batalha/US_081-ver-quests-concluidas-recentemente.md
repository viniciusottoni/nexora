---
title: US-081 — Ver quests concluídas recentemente
sidebar_position: 81
---

# US-081 — Ver quests concluídas recentemente

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-081 |
| Épico | EPIC-011 — Histórico Básico e Log de Batalha |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **usuário com acesso ativo**, quero **ver minhas quests concluídas recentemente**, para **perceber minha consistência e minha evolução no AWAKEN**.

## 3. Contexto

O histórico funciona como um log de batalha: uma lista simples e confiável das quests concluídas, reforçando continuidade sem adicionar gráficos avançados no MVP.

## 4. Objetivo

Exibir as quests concluídas recentemente, com tipo, data, XP recebido e itens ganhos quando aplicável.

## 5. Escopo

### Entra nesta US

- Listar quests concluídas recentemente.
- Exibir tipo da quest: diária, dungeon e raid quando existir log.
- Exibir data de conclusão.
- Exibir XP recebido.
- Exibir indicação de itens ganhos em dungeons.
- Exibir empty state quando não houver histórico.

### Fora desta US

- Gráficos avançados.
- Comparações semanais profundas.
- Exportação de dados.
- Relatórios para treinador.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Histórico deve exibir apenas quests válidas concluídas. |
| RN-002 | Quest cancelada não deve aparecer como concluída. |
| RN-003 | O tipo da quest deve vir de `QuestLog.questType`. |
| RN-004 | A lista deve ser limitada ou paginada. |
| RN-005 | Dados sensíveis do onboarding não devem aparecer no histórico. |
| RN-006 | Logs não devem ser apagados quando trial ou assinatura expirar. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode visualizar histórico. |
| Usuário em Trial | Pode visualizar histórico conforme regra do trial. |
| Premium Mensal | Pode visualizar histórico. |
| Premium Anual | Pode visualizar histórico. |
| Trial expirado | Pode visualizar estado limitado com CTA. |
| Assinatura expirada | Pode visualizar estado limitado com CTA. |

## 8. Fluxo principal

1. Usuário acessa Histórico / Log de Batalha.
2. App solicita os logs recentes.
3. Backend retorna quests concluídas válidas em ordem descrescente.
4. App exibe cards com tipo, data, XP e itens quando houver.

## 9. Fluxos alternativos

### 9.1. Sem histórico

Exibir empty state orientando o usuário a concluir a primeira quest.

### 9.2. Acesso expirado

Exibir histórico limitado com CTA de assinatura, sem apagar dados.

### 9.3. Erro de carregamento

Exibir erro controlado e permitir tentar novamente.

## 10. Estados esperados

- carregando;
- lista com dados;
- vazio;
- acesso limitado;
- erro de conexão;
- erro inesperado.

## 11. Impacto Flutter

- Tela de histórico/log de batalha.
- Lista de cards de QuestLog.
- Empty state.
- Estado limitado com CTA.
- Textos localizados PT-BR, EN e ES.

## 12. Impacto Backend

- Endpoint de histórico recente.
- Consulta ordenada e limitada/paginada.
- Validação de acesso.
- Filtro para logs válidos.

## 13. Impacto DB

Entidades:

- QuestLog;
- Quest;
- HunterProgress;
- HunterInventory.

Campos relevantes:

- questType;
- xpEarned;
- itemsEarned;
- completedAt.

## 14. Impacto Gamificação

- Reforça continuidade e sensação de jornada.
- Não concede XP apenas por visualizar histórico.

## 15. Impacto Monetização

- Trial mostra valor do registro de progresso.
- Acesso expirado recebe CTA sem perda dos dados.

## 16. Contrato API sugerido

```txt
GET /api/hunter/battle-log/recent
```

Response conceitual:

```json
{
  "items": [
    {
      "questLogId": "uuid",
      "questType": "daily",
      "xpEarned": 120,
      "completedAt": "2026-06-25T10:30:00Z",
      "itemsEarned": []
    }
  ]
}
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| battle_log_viewed | Quando histórico é exibido. |
| battle_log_empty_viewed | Quando empty state é exibido. |

## 18. Critérios de aceite

### CA-001 — Lista recente

Dado que o usuário possui quests concluídas,
Quando abrir o histórico,
Então deve ver as quests recentes ordenadas da mais nova para a mais antiga.

### CA-002 — Sem quests canceladas

Dado que existe quest cancelada,
Quando o histórico carregar,
Então ela não deve aparecer como concluída.

## 19. Critérios de teste QA

- histórico com daily;
- histórico com dungeon;
- histórico com raid, se houver;
- histórico vazio;
- acesso expirado;
- erro de conexão;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

O histórico recente deve ser simples, confiável e suficiente para reforçar a sensação de jornada registrada no MVP.
