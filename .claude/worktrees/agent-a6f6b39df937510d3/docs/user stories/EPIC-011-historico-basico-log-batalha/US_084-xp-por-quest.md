---
title: US-084 — Ver XP recebido em cada quest
sidebar_position: 84
---

# US-084 — Ver XP recebido em cada quest

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-084 |
| Épico | EPIC-011 — Histórico Básico e Log de Batalha |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **usuário do AWAKEN**, quero **ver o XP recebido em cada quest concluída**, para **entender quanto cada treino contribuiu para minha evolução**.

## 3. Contexto

O XP é uma das principais provas de progresso. No histórico, o usuário precisa ver o XP recebido por quest de forma clara e consistente com o valor aplicado no perfil.

## 4. Objetivo

Exibir o XP recebido em cada entrada do histórico, usando o valor registrado no QuestLog.

## 5. Escopo

### Entra nesta US

- Exibir XP por quest no card do histórico.
- Exibir XP total daquela conclusão.
- Exibir penalidade quando houver.
- Garantir consistência com HunterProgress.
- Suportar daily, dungeon e raid quando houver log.

### Fora desta US

- Fórmula detalhada de XP.
- Gráfico de evolução.
- Simulador de XP.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | XP exibido deve vir de `QuestLog.xpEarned`. |
| RN-002 | XP exibido deve bater com XP aplicado ao HunterProgress. |
| RN-003 | Quest cancelada não deve exibir XP completo como conclusão. |
| RN-004 | Quando houver penalidade, o histórico deve indicar de forma clara. |
| RN-005 | Frontend não deve recalcular XP. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode ver XP do histórico permitido. |
| Premium Mensal | Pode ver XP no histórico. |
| Premium Anual | Pode ver XP no histórico. |
| Trial expirado | Pode ver estado limitado com CTA. |
| Assinatura expirada | Pode ver estado limitado com CTA. |
| Visitante | Não pode visualizar. |

## 8. Fluxo principal

1. Usuário acessa histórico.
2. Backend retorna logs com `xpEarned`.
3. App exibe XP em cada card.
4. Usuário entende o impacto de cada quest.

## 9. Fluxos alternativos

### 9.1. XP zero

Exibir XP como zero apenas se o QuestLog registrar esse valor.

### 9.2. Penalidade aplicada

Exibir indicação simples de penalidade sem explicar fórmula complexa.

## 10. Estados esperados

- XP exibido;
- XP zero;
- XP com penalidade;
- acesso limitado;
- erro.

## 11. Impacto Flutter

- Label visual de XP no card.
- Formatação consistente.
- Indicação discreta de penalidade.
- Textos localizados.

## 12. Impacto Backend

- Retornar `xpEarned` em todas as entradas do histórico.
- Garantir consistência com cálculo aplicado.
- Não delegar cálculo ao frontend.

## 13. Impacto DB

Entidades:

- QuestLog;
- HunterProgress.

Campos:

- xpEarned;
- xpPenaltyApplied.

## 14. Impacto Gamificação

- Reforça percepção de recompensa.
- Mostra progressão por treino.
- Não concede XP por visualização.

## 15. Impacto Monetização

- Ajuda trial e assinantes a perceberem valor contínuo.

## 16. Contrato API sugerido

```txt
GET /api/hunter/battle-log/recent
```

Trecho do response:

```json
{
  "questLogId": "uuid",
  "xpEarned": 120,
  "xpPenaltyApplied": 20
}
```

`xpPenaltyApplied` é um campo numérico nulo/valor, não um booleano. Quando não houver penalidade, o backend retorna `null`.

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| battle_log_xp_viewed | Quando XP por quest é exibido. |

## 18. Critérios de aceite

### CA-001 — XP exibido

Dado que uma quest concluída gerou XP,
Quando o histórico carregar,
Então o XP deve aparecer no card da quest.

### CA-002 — Sem recálculo no frontend

Dado que o frontend recebe `xpEarned`,
Quando renderizar o histórico,
Então deve exibir o valor recebido sem recalcular.

## 19. Critérios de teste QA

- daily com XP;
- dungeon com XP;
- raid com XP, se houver;
- XP zero;
- penalidade aplicada;
- consistência com perfil Hunter;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

O XP exibido no histórico deve ser fonte de confiança e precisa refletir exatamente o valor registrado no QuestLog.
