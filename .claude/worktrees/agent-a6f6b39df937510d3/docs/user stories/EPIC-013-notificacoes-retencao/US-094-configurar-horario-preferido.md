---
title: US-094 — Configurar horário preferido
sidebar_position: 94
---

# US-094 — Configurar horário preferido

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-094 |
| Épico | EPIC-013 — Notificações e Retenção |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Integração principal | Firebase Cloud Messaging |
| Status | Planejada |

## 2. História do usuário

Como **usuário com acesso ativo**, quero **configurar meu horário preferido de lembrete**, para **receber notificações em um momento útil para minha rotina**.

## 3. Contexto

Notificações são mais úteis quando respeitam a rotina do usuário. O horário preferido reduz incômodo e aumenta chance de retorno diário.

## 4. Objetivo

Permitir que o usuário defina e altere um horário preferido para lembretes de quest diária.

## 5. Escopo

### Entra nesta US

- Escolher horário preferido.
- Salvar preferência de horário.
- Usar horário em lembretes de quest diária.
- Permitir alteração futura.
- Respeitar timezone do usuário.

### Fora desta US

- Múltiplos horários por dia.
- Rotinas semanais avançadas.
- Calendário externo.
- Segmentação comportamental complexa.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Usuário deve poder definir horário preferido se a feature entrar no MVP. |
| RN-002 | O horário deve ser salvo por usuário. |
| RN-003 | Scheduler deve considerar timezone do usuário. |
| RN-004 | O horário preferido deve respeitar limite de notificações. |
| RN-005 | Usuário com permissão negada pode configurar, mas não receberá push até permitir. |
| RN-006 | Usuário com acesso expirado não deve receber lembrete de quest, mesmo com horário salvo. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode configurar com trial ativo. |
| Premium Mensal | Pode configurar. |
| Premium Anual | Pode configurar. |
| Trial expirado | Não recebe lembrete, mas preferência pode ficar preservada. |
| Assinatura expirada | Não recebe lembrete, mas preferência pode ficar preservada. |
| Visitante | Não configura. |

## 8. Fluxo principal

1. Usuário acessa configurações de notificações.
2. Seleciona horário preferido.
3. App salva preferência.
4. Backend armazena o horário e timezone.
5. Próximos lembretes usam o horário configurado.

## 9. Fluxos alternativos

### 9.1. Horário não configurado

Sistema usa horário padrão definido pelo produto.

### 9.2. Permissão negada

Salvar preferência, mas informar que notificações precisam ser permitidas.

## 10. Estados esperados

- horário padrão;
- horário personalizado;
- salvando;
- salvo;
- permissão negada;
- erro.

## 11. Impacto Flutter

- Tela/controle de horário preferido.
- Estado de permissão negada.
- Feedback de salvamento.
- Textos localizados.

## 12. Impacto Backend

- Persistir `preferredReminderTime`.
- Persistir timezone.
- Usar preferência no scheduler.
- Validar usuário e acesso quando enviar.

## 13. Impacto DB

Entidade: NotificationPreference.

Campos:

- userId;
- preferredReminderTime;
- timezone;
- updatedAt.

## 14. Impacto Gamificação

- Aumenta chance de conclusão diária.
- Não concede XP.

## 15. Impacto Monetização

- Melhora retenção sem aumentar ruído.

## 16. Contrato API sugerido

```txt
PATCH /api/notifications/preferences/reminder-time
```

Request conceitual:

```json
{
  "preferredReminderTime": "19:30",
  "timezone": "America/Recife"
}
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| notification_time_configured | Quando horário é salvo. |

## 18. Critérios de aceite

### CA-001 — Horário salvo

Dado que o usuário escolheu 19:30,
Quando salvar,
Então o backend deve armazenar esse horário para lembretes futuros.

### CA-002 — Horário usado

Dado que existe horário preferido,
Quando o lembrete diário for disparado,
Então deve respeitar esse horário no timezone do usuário.

## 19. Critérios de teste QA

- salvar horário;
- alterar horário;
- timezone correto;
- permissão negada;
- acesso expirado;
- fallback para horário padrão;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

O horário preferido existe para reduzir ruído e aumentar relevância, não para multiplicar notificações.
