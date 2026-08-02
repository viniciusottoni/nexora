---
title: US-016 — Registrar início e fim do trial de 7 dias
sidebar_position: 16
---

# US-016 — Registrar início e fim do trial de 7 dias

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-016 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Subscription e AccessStatus |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **registrar início e fim do trial de 7 dias**,

para **controlar corretamente o acesso gratuito temporário do usuário**.

---

## 3. Contexto

O trial é o elemento central do modelo comercial. O sistema precisa registrar datas de início e fim com precisão, impedir duplicidade e mudar o status de acesso quando o período terminar.

---

## 4. Objetivo

Garantir que todo trial tenha ciclo de vida controlado: criado, ativo, expirado e convertido, quando houver assinatura.

---

## 5. Escopo

### Entra nesta US

- Registrar `trialStartedAt`.
- Registrar `trialEndsAt`.
- Registrar status `trial_active`.
- Identificar expiração.
- Alterar status para `trial_expired`.
- Expor status para o app.

### Fora desta US

- Compra de assinatura.
- Notificações de fim de trial.
- Paywall visual.
- Reativação após pagamento.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | `trialEndsAt` deve ser exatamente 7 dias após `trialStartedAt`, conforme regra do backend. |
| RN-002 | O backend é a fonte de verdade do status do trial. |
| RN-003 | Trial expirado deve bloquear recursos protegidos. |
| RN-004 | Trial expirado não deve apagar progresso. |
| RN-005 | A mudança de status deve ser idempotente. |
| RN-006 | Diferenças de horário do aparelho não podem alterar a regra de expiração. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não possui trial registrado. |
| Usuário em Trial | Possui datas de trial. |
| Premium Mensal | Pode ter histórico de trial consumido. |
| Premium Anual | Pode ter histórico de trial consumido. |
| Trial expirado | Possui status expirado. |
| Assinatura expirada | Pode ter trial anterior consumido. |
| Sistema | Pode processar status. |

---

## 8. Fluxo principal

1. Trial é iniciado.
2. Backend salva data de início.
3. Backend calcula data de fim.
4. Backend retorna status ativo para o app.
5. Ao consultar status, backend verifica se o período terminou.
6. Se terminou, muda status para expirado.

---

## 9. Fluxos alternativos

### 9.1. Consulta após expiração

Se o usuário abrir o app após o fim do trial, o sistema deve retornar `trial_expired`.

### 9.2. Processamento repetido

Se a expiração for processada mais de uma vez, o resultado deve permanecer consistente.

---

## 10. Estados de tela ou estados esperados

- trial não iniciado;
- trial ativo;
- trial próximo do fim;
- trial expirado;
- assinatura ativa;
- acesso bloqueado.

---

## 11. Impacto no Frontend Flutter

- Consumir status do trial.
- Exibir estado correto.
- Direcionar para onboarding, home ou paywall.
- Evitar depender do relógio local como fonte de verdade.

---

## 12. Impacto no Backend

- Persistir datas de trial.
- Calcular expiração.
- Retornar status de acesso.
- Processar expiração de forma segura.

---

## 13. Impacto no Banco de Dados

Entidade principal: Subscription.

Campos relevantes:

- trialStartedAt;
- trialEndsAt;
- trialConsumedAt;
- status;
- accessStatus.

---

## 14. Impacto em Gamificação

- Trial expirado bloqueia novas quests.
- Progresso já conquistado deve permanecer salvo.

---

## 15. Impacto em Monetização

- Expiração correta é essencial para conversão.
- Usuário expirado deve ser conduzido para plano mensal ou anual.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de expiração. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/subscriptions/status
```

Response conceitual:

```json
{
  "accessStatus": "trial_active",
  "trialStartedAt": "2026-06-18T10:00:00Z",
  "trialEndsAt": "2026-06-25T10:00:00Z",
  "daysRemaining": 7
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| trial_expired | Quando o status muda para expirado. |
| access_blocked | Quando recursos são bloqueados após expiração. |

---

## 19. Critérios de aceite

### CA-001 — Datas registradas

Dado que o trial foi iniciado,

Quando o backend salvar a assinatura temporária,

Então deve registrar início e fim do trial.

### CA-002 — Expiração correta

Dado que a data final passou,

Quando o status for consultado,

Então deve retornar trial expirado.

---

## 20. Critérios de teste para QA

- validar início e fim do trial;
- simular expiração;
- validar bloqueio após expiração;
- validar que progresso permanece;
- validar status em fusos diferentes;
- validar consulta repetida.

---

## ✅ Decisão registrada

> O backend é a fonte de verdade para datas e status do trial. O app não deve depender do relógio local para liberar ou bloquear acesso.

---

## 21. Adendo — Localização de datas em UTC (recorte do trial)

> Adicionado na revisão de arquitetura de 2026-06-28. Este adendo cobre **apenas** o ciclo de vida do trial. A auditoria de UTC no restante do código é tratada na **US-172 (EPIC-018)**.

### 21.1. Regras complementares

| ID | Regra |
|---|---|
| RN-007 | `trialStartedAt`, `trialEndsAt`, `trialConsumedAt` são persistidos em UTC (`timestamptz`) e expostos em ISO-8601 com sufixo `Z`. |
| RN-008 | O cálculo de expiração usa exclusivamente `IDateTimeService.UtcNow` (nunca `DateTime.Now`, nunca o relógio do app). |
| RN-009 | `daysRemaining` é derivado no backend a partir de UTC; o app apenas exibe, e pode converter para o fuso local **só para apresentação**. |
| RN-010 | A virada de dia do trial segue a mesma âncora UTC já usada pelos jobs recorrentes (`TimeZoneInfo.Utc`), garantindo consistência com penalidade/streak. |

### 21.2. Pontos de código a validar nesta US

- `Subscription`/entidade de trial: campos de data em UTC e cálculo de fim via serviço de tempo.
- `GET /api/subscriptions/status`: serialização com `Z` e `daysRemaining` calculado no servidor.
- Flutter `features/subscriptions`: nenhuma decisão de acesso baseada em `DateTime.now()`; exibição de "dias restantes" vinda do backend.

### 21.3. Critérios de aceite adicionais

- **CA-003** — Dado um aparelho com relógio adiantado/atrasado, quando o status do trial for consultado, então a expiração permanece correta (decidida em UTC pelo backend).
- **CA-004** — Dado o status retornado, então todas as datas vêm em UTC ISO-8601 com `Z`, e `daysRemaining` é coerente com elas.
