---
title: US-082 — Ver histórico durante trial
sidebar_position: 82
---

# US-082 — Ver histórico durante trial

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-082 |
| Épico | EPIC-011 — Histórico Básico e Log de Batalha |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial |
| Plano | Trial |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **usuário em trial**, quero **ver meu histórico de quests concluídas**, para **perceber valor no AWAKEN antes de assinar**.

## 3. Contexto

O trial precisa demonstrar progresso real. O histórico durante o teste gratuito reforça que cada quest concluída fica registrada e ajuda na conversão sem criar bloqueios artificiais.

## 4. Objetivo

Permitir que usuário em trial ativo visualize o histórico básico das quests concluídas durante sua jornada inicial.

## 5. Escopo

### Entra nesta US

- Histórico básico durante trial ativo.
- Lista de quests concluídas no período disponível.
- XP recebido por quest.
- Tipo da quest.
- Itens ganhos em dungeon, quando houver.
- Empty state.

### Fora desta US

- Histórico completo avançado.
- Gráficos profundos.
- Exportação de dados.
- Relatórios para terceiros.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Usuário em trial ativo pode visualizar histórico básico. |
| RN-002 | Histórico deve refletir logs reais. |
| RN-003 | Trial expirado deve seguir estado limitado com CTA. |
| RN-004 | Logs criados no trial não devem ser apagados ao expirar. |
| RN-005 | Histórico durante trial não deve mostrar dados sensíveis. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode visualizar histórico básico. |
| Trial expirado | Visualiza estado limitado com CTA. |
| Premium Mensal | Usa regra de assinante. |
| Premium Anual | Usa regra de assinante. |
| Visitante | Não pode visualizar. |

## 8. Fluxo principal

1. Usuário em trial acessa histórico.
2. App valida trial ativo.
3. Backend retorna logs disponíveis.
4. App exibe histórico básico.
5. Usuário percebe XP, consistência e evolução.

## 9. Fluxos alternativos

### 9.1. Trial expirado

Exibir estado limitado com CTA para assinatura.

### 9.2. Sem quests concluídas

Exibir empty state convidando a concluir uma quest.

## 10. Estados esperados

- trial ativo;
- histórico com dados;
- histórico vazio;
- trial expirado;
- erro de carregamento.

## 11. Impacto Flutter

- Estado específico para trial ativo.
- CTA discreto para conhecer planos quando aplicável.
- Lista de logs compatível com assinantes.
- Textos localizados.

## 12. Impacto Backend

- Validar status de trial.
- Retornar logs conforme acesso.
- Não apagar logs após expiração.

## 13. Impacto DB

Entidades:

- QuestLog;
- Subscription;
- User.

## 14. Impacto Gamificação

- Ajuda o usuário a perceber evolução desde os primeiros dias.
- Não concede XP por visualização.

## 15. Impacto Monetização

- Demonstra valor do produto durante trial.
- Expiração direciona para assinatura sem apagar histórico.

## 16. Contrato API sugerido

```txt
GET /api/hunter/battle-log?scope=trial
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| trial_battle_log_viewed | Quando usuário em trial vê histórico. |
| trial_battle_log_limited_viewed | Quando trial expirado vê estado limitado. |

## 18. Critérios de aceite

### CA-001 — Trial ativo vê histórico

Dado que o usuário está em trial ativo,
Quando acessar histórico,
Então deve ver quests concluídas disponíveis.

### CA-002 — Trial expirado limitado

Dado que o trial expirou,
Quando acessar histórico,
Então deve ver estado limitado com CTA.

## 19. Critérios de teste QA

- histórico com trial ativo;
- histórico vazio no trial;
- trial expirado;
- logs preservados após expiração;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

O histórico durante trial deve mostrar valor real do AWAKEN e preservar registros, mesmo após expiração.
