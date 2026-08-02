---
title: US-060 — Cancelar quest em andamento
sidebar_position: 60
---

# US-060 — Cancelar quest em andamento

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-060 |
| Épico | EPIC-008 — Execução da Quest e Registro do Treino |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Quest.status |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário executando uma quest**,

quero **cancelar a quest em andamento**,

para **encerrar um treino que não conseguirei concluir sem gerar recompensa indevida**.

---

## 3. Contexto

O usuário pode precisar interromper o treino. O cancelamento deve ser claro, exigir confirmação e não conceder recompensa completa.

---

## 4. Objetivo

Permitir cancelar uma quest em andamento, preservando o que já foi registrado e impedindo conclusão/recompensa final indevida.

---

## 5. Escopo

### Entra nesta US

- Cancelar quest em andamento.
- Confirmar intenção antes de cancelar.
- Registrar `cancelledAt`.
- Impedir conclusão posterior.
- Não conceder XP completo de quest.
- Manter registros parciais conforme regra definida.

### Fora desta US

- Reembolso de item.
- Reabertura de quest cancelada.
- Análise de motivo avançada.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Apenas quest em andamento ou pausada pode ser cancelada. |
| RN-002 | Quest cancelada não pode ser concluída depois. |
| RN-003 | Quest cancelada não deve conceder recompensa completa. |
| RN-004 | Sistema deve registrar data/hora de cancelamento. |
| RN-005 | Cancelamento deve exigir confirmação do usuário. |
| RN-006 | Quest cancelada deve indicar `xpPenaltyApplied` no log quando houver registro. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode cancelar quest própria em andamento. |
| Premium Mensal | Pode cancelar quest própria em andamento. |
| Premium Anual | Pode cancelar quest própria em andamento. |
| Trial expirado | Não inicia novas quests; pode ter bloqueios conforme estado vigente. |
| Assinatura expirada | Não inicia novas quests; pode ter bloqueios conforme estado vigente. |
| Visitante | Não pode cancelar. |

---

## 8. Fluxo principal

1. Usuário está em uma quest em andamento.
2. Toca em cancelar.
3. App exibe confirmação.
4. Usuário confirma cancelamento.
5. Backend altera status para cancelada.
6. App exibe estado de quest cancelada.

---

## 9. Fluxos alternativos

### 9.1. Usuário desiste do cancelamento

App fecha confirmação e mantém quest em andamento.

### 9.2. Quest já concluída

Backend deve rejeitar cancelamento.

---

## 10. Estados esperados

- em andamento;
- confirmação de cancelamento;
- cancelando;
- cancelada;
- cancelamento rejeitado;
- erro.

---

## 11. Impacto no Frontend Flutter

- Ação de cancelar.
- Modal de confirmação.
- Mensagem sobre perda de recompensa completa.
- Estado final de cancelada.

---

## 12. Impacto no Backend

- Endpoint para cancelar quest.
- Validação de status.
- Persistência de `cancelledAt`.
- Impedir conclusão posterior.

---

## 13. Impacto no Banco de Dados

Entidades:

- Quest;
- QuestLog, se houver log de cancelamento.

Campos:

- Quest.status;
- Quest.cancelledAt;
- QuestLog.xpPenaltyApplied.

---

## 14. Impacto em Gamificação

- Não concede recompensa final completa.
- Evita abuso de XP.
- Pode manter XP parcial já concedido por exercícios, conforme regra final do EPIC-009.

---

## 15. Impacto em Monetização

- Não altera assinatura.
- Mantém integridade da experiência paga/trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Confirmação e aviso de cancelamento. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/{questId}/cancel
```

Response conceitual:

```json
{
  "questId": "uuid",
  "status": "cancelled",
  "cancelledAt": "2026-06-23T18:40:00Z"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| quest_cancelled | Quando quest é cancelada. |

Propriedades:

- `quest_type`;
- `completed_exercises_count`.

---

## 19. Critérios de aceite

### CA-001 — Cancelamento confirmado

Dado que a quest está em andamento,

Quando o usuário confirmar cancelamento,

Então a quest deve virar cancelada.

### CA-002 — Sem recompensa completa

Dado que a quest foi cancelada,

Quando o sistema calcular recompensa,

Então não deve conceder recompensa completa da quest.

---

## 20. Critérios de teste para QA

- cancelar daily;
- cancelar dungeon;
- cancelar raid;
- desistir do cancelamento;
- tentar concluir quest cancelada;
- validar ausência de recompensa final completa;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Cancelamento deve ser permitido, mas não pode gerar recompensa completa nem permitir conclusão posterior da mesma quest.
