---
title: US-102 — Identificar queda em funis críticos
sidebar_position: 102
---

# US-102 — Identificar queda em funis críticos

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-102 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto, Engenharia e QA |
| Integrações | Firebase Analytics |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **identificar queda em funis críticos**, para **priorizar correções que afetam ativação, retenção e conversão**.

## 3. Contexto

Após instrumentar eventos P0, o time precisa analisar pontos de perda em onboarding, trial, geração de quest, conclusão e assinatura.

## 4. Objetivo

Definir funis mínimos de acompanhamento do MVP com eventos já instrumentados.

## 5. Escopo

### Entra nesta US

- Funil de ativação: splash, login, trial, onboarding, primeira quest.
- Funil de quest: gerada, vista, iniciada, concluída.
- Funil comercial: planos vistos, plano escolhido, assinatura iniciada.
- Funil de retenção: quest concluída, streak atualizado, retorno no dia seguinte.

### Fora desta US

- BI avançado.
- Alertas em tempo real.
- Data warehouse.
- Testes A/B.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Funis críticos usam eventos padronizados do EPIC-014. |
| RN-002 | Análise inicial pode ser feita dentro do Firebase. |
| RN-003 | Quedas devem ser avaliadas sem dados pessoais sensíveis. |
| RN-004 | Funis P1 não bloqueiam lançamento se eventos P0 estiverem corretos. |

## 7. Funis sugeridos

| Funil | Eventos |
|---|---|
| Ativação | app_opened, splash_viewed, login_completed, trial_started, onboarding_completed, daily_quest_generated |
| Quest | daily_quest_generated, quest_viewed, quest_started, exercise_completed, quest_completed |
| Comercial | trial_plans_viewed, monthly_plan_selected, annual_plan_selected, subscription_started |
| Retenção | quest_completed, streak_updated, app_opened |

> Observação: `splash_viewed` e `quest_viewed` são os passos técnicos já instrumentados no app para apoiar análise dos funis de ativação e quest.

## 8. Impacto Flutter

- Garantir consistência dos eventos usados nos funis.
- Evitar eventos duplicados por reconstrução de tela.

## 9. Impacto Backend

- Logs ajudam a explicar quedas causadas por erro de API.
- Eventos comerciais podem ser conciliados com RevenueCat.

## 10. Impacto QA

- Validar sequência mínima dos funis.
- Validar ausência de duplicidade.
- Validar payloads obrigatórios.

## 11. Critérios de aceite

### CA-001 — Funil de quest mensurável

Dado que um usuário gera, inicia e conclui uma quest,
Quando os eventos forem consultados,
Então deve ser possível montar o funil da quest.

### CA-002 — Funil comercial mensurável

Dado que usuário visualiza planos e escolhe um plano,
Quando os eventos forem consultados,
Então deve ser possível medir abandono antes da assinatura.

## 12. Decisão registrada

Funis críticos são P1, mas devem nascer dos eventos P0 para permitir aprendizado rápido sem infraestrutura analítica pesada.
