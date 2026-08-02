---
title: US-063 — Ver tela de recompensa
sidebar_position: 63
---

# US-063 — Ver tela de recompensa

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-063 |
| Épico | EPIC-008 — Execução da Quest e Registro do Treino |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | QuestLog e RewardSummary |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário que concluiu uma quest**,

quero **ver uma tela de recompensa clara e motivadora**,

para **sentir o impacto do treino na minha evolução**.

---

## 3. Contexto

A tela de recompensa é o fechamento emocional da quest. Ela precisa mostrar XP geral, XP interno de atributos, pontos visíveis de atributo concedidos quando houver conversão, streak e itens quando aplicável, com visual épico e sem poluir a experiência.

---

## 4. Objetivo

Exibir o resultado da quest concluída com clareza, reforçando progressão e motivando retorno no dia seguinte.

---

## 5. Escopo

### Entra nesta US

- Exibir XP ganho.
- Exibir XP interno ganho nos atributos impactados.
- Exibir pontos visíveis de atributos concedidos quando houver conversão de 10 XP internos.
- Exibir impacto no streak.
- Exibir itens ganhos em dungeons quando aplicável.
- Exibir resultado de raid com `questType = raid`.
- CTA para voltar ao início/perfil/histórico.

### Fora desta US

- Animações complexas premium.
- Card compartilhável, tratado no EPIC-010.
- Histórico detalhado, tratado no EPIC-011.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Tela deve usar dados reais do QuestLog. |
| RN-002 | Tela deve exibir `questType`: daily, dungeon ou raid. |
| RN-003 | Dungeon deve exibir itens ganhos quando houver. |
| RN-004 | Quest diária sem item não deve exibir área vazia de itens. |
| RN-005 | Recompensa não deve ser recalculada no frontend. |
| RN-006 | Tela deve evitar exageros que confundam XP real. |
| RN-007 | A tela deve diferenciar XP interno de atributo de ponto visível de atributo. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode ver recompensa de quest concluída. |
| Premium Mensal | Pode ver recompensa. |
| Premium Anual | Pode ver recompensa. |
| Trial expirado | Pode visualizar recompensa já registrada, conforme regra de histórico. |
| Assinatura expirada | Pode visualizar recompensa já registrada, conforme regra de histórico. |
| Visitante | Não pode visualizar. |

---

## 8. Fluxo principal

1. Usuário conclui quest.
2. Backend retorna resumo de recompensa.
3. App exibe tela de recompensa.
4. Usuário visualiza XP geral, XP interno de atributos, pontos visíveis de atributo, streak e itens quando houver.
5. Usuário toca em CTA para continuar.

---

## 9. Fluxos alternativos

### 9.1. Sem itens ganhos

Tela deve ocultar seção de itens.

### 9.2. Erro ao carregar recompensa

App deve exibir erro controlado e permitir tentar novamente ou voltar ao início.

---

## 10. Estados esperados

- carregando recompensa;
- recompensa exibida;
- sem itens;
- com itens;
- erro de recompensa;
- recompensa já vista.

---

## 11. Impacto no Frontend Flutter

- Tela de recompensa.
- Componentes de XP geral, atributos, streak e itens.
- Visual dark, épico e motivador.
- CTA de continuidade.
- Textos localizados.

---

## 12. Impacto no Backend

- Retornar resumo de recompensa baseado no QuestLog.
- Não recalcular recompensa no frontend.
- Garantir dados consistentes para daily, dungeon e raid.

---

## 13. Impacto no Banco de Dados

Entidades:

- QuestLog;
- HunterProgress;
- HunterAttributes;
- HunterInventory.

---

## 14. Impacto em Gamificação

- Reforça progressão após treino.
- Mostra ganhos reais.
- Dungeons podem exibir itens.
- Raids seguem o mesmo contrato base.

---

## 15. Impacto em Monetização

- Recompensa visual aumenta retenção e percepção de valor do trial/assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos de recompensa. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/quests/{questId}/reward-summary
```

Response conceitual:

```json
{
  "questType": "dungeon",
  "xpEarned": 220,
  "attributeXpEarned": {
    "strength": 9,
    "vitality": 5,
    "wisdom": 7
  },
  "attributePointsGranted": {
    "strength": 1,
    "vitality": 0,
    "wisdom": 0
  },
  "streakDays": 5,
  "itemsEarned": [
    {
      "code": "scroll_reforge",
      "name": "Pergaminho da Reforja"
    }
  ]
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| reward_screen_viewed | Quando tela de recompensa é exibida. |

Propriedades:

- `quest_type`;
- `xp_earned`;
- `items_earned_count`.

---

## 19. Critérios de aceite

### CA-001 — Recompensa exibida

Dado que a quest foi concluída,

Quando a tela abrir,

Então deve mostrar XP geral, XP interno de atributos, pontos visíveis concedidos quando houver, e streak.

### CA-002 — Itens de dungeon

Dado que uma dungeon concedeu itens,

Quando a recompensa for exibida,

Então os itens devem aparecer na tela.

### CA-003 — Sem recalcular no app

Dado que o frontend recebe o resumo,

Quando renderizar,

Então deve usar os dados do backend sem recalcular recompensa.

---

## 20. Critérios de teste para QA

- recompensa de daily sem item;
- recompensa de dungeon com item;
- recompensa de raid;
- validar XP exibido;
- validar XP interno de atributos exibido;
- validar pontos visíveis de atributos exibidos quando houver conversão;
- validar streak;
- erro de carregamento;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> A tela de recompensa deve fechar a quest com clareza e motivação, usando dados reais do QuestLog sem recalcular recompensas no frontend.
