---
title: US-116 — Visualizar dias restantes do trial
sidebar_position: 116
---

# US-116 — Visualizar dias restantes do trial

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-116 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial |
| Plano | Trial |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Subscription status |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário em trial**,

quero **visualizar quantos dias gratuitos ainda tenho**,

para **saber quando precisarei assinar para continuar usando o AWAKEN**.

---

## 3. Contexto

A contagem de dias restantes reforça transparência e evita surpresa no fim do trial. O usuário deve perceber a regra comercial sem pressão excessiva.

---

## 4. Objetivo

Exibir de forma clara a quantidade de dias restantes do trial em pontos adequados da interface.

---

## 5. Escopo

### Entra nesta US

- Calcular dias restantes com base no backend.
- Exibir indicador de trial ativo.
- Exibir mensagem de proximidade do fim.
- Manter linguagem transparente.
- Atualizar quando o status mudar.

### Fora desta US

- Push notification.
- Paywall pós-expiração.
- Compra de plano.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Dias restantes devem vir de cálculo baseado no backend. |
| RN-002 | O app não deve depender somente do relógio local. |
| RN-003 | Usuário fora do trial não deve ver contador de trial ativo. |
| RN-004 | Quando chegar a 0, o app deve tratar como trial expirado. |
| RN-005 | A mensagem deve ser clara e sem urgência falsa. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode visualizar contador. |
| Premium Mensal | Não visualiza contador de trial. |
| Premium Anual | Não visualiza contador de trial. |
| Trial expirado | Visualiza paywall, não contador ativo. |
| Assinatura expirada | Não visualiza contador de trial. |

---

## 8. Fluxo principal

1. Usuário em trial abre o app.
2. App consulta status de assinatura.
3. Backend retorna data final e dias restantes.
4. App exibe o contador.
5. Quando o trial termina, app direciona para paywall.

---

## 9. Fluxos alternativos

### 9.1. Erro ao consultar status

App deve exibir estado controlado e tentar novamente.

### 9.2. Trial expirado durante uso

App deve atualizar status e bloquear recursos protegidos na próxima validação.

---

## 10. Estados de tela ou estados esperados

- trial ativo;
- 7 a 4 dias restantes;
- 3 a 1 dias restantes;
- último dia;
- expirado;
- erro de status.

---

## 11. Impacto no Frontend Flutter

- Componente de contador.
- Mensagem localizada.
- Atualização de estado global.
- Tratamento de expiração.

---

## 12. Impacto no Backend

- Retornar `trialEndsAt` e `daysRemaining`.
- Garantir cálculo consistente.

---

## 13. Impacto no Banco de Dados

Entidade: Subscription.

Campos:

- trialStartedAt;
- trialEndsAt;
- accessStatus.

---

## 14. Impacto em Gamificação

- Não altera XP.
- Ajuda o usuário a planejar continuidade da jornada.

---

## 15. Impacto em Monetização

- Aumenta consciência de conversão sem surpresa.
- Pode levar usuário a assinar antes do fim do trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Texto de dias restantes. |
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
  "daysRemaining": 3
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| trial_day_count_viewed | Quando contador é exibido. |

---

## 19. Critérios de aceite

### CA-001 — Contador visível

Dado que o usuário está em trial,

Quando abrir tela com status comercial,

Então deve ver dias restantes.

### CA-002 — Expiração

Dado que o trial acabou,

Quando status for atualizado,

Então o contador deve sair e o paywall deve aparecer.

---

## 20. Critérios de teste para QA

- trial com 7 dias;
- trial com 3 dias;
- último dia;
- trial expirado;
- erro de status;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> O usuário em trial deve saber quantos dias gratuitos restam, com cálculo confiável vindo do backend.
