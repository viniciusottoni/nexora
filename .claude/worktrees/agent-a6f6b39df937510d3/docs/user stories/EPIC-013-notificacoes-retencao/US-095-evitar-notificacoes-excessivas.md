---
title: US-095 — Evitar notificações excessivas
sidebar_position: 95
---

# US-095 — Evitar notificações excessivas

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-095 |
| Épico | EPIC-013 — Notificações e Retenção |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Integração principal | Firebase Cloud Messaging |
| Status | Planejada |

## 2. História do usuário

Como **usuário do AWAKEN**, quero **não receber notificações excessivas**, para **continuar vendo o app como útil e motivador, não irritante**.

## 3. Contexto

Notificação demais destrói confiança. O AWAKEN deve priorizar qualidade, contexto e frequência baixa, evitando empilhar lembretes no mesmo dia.

## 4. Objetivo

Criar regras de limite e elegibilidade para impedir múltiplas notificações desnecessárias no mesmo dia.

## 5. Escopo

### Entra nesta US

- Limite diário de notificações.
- Controle de última notificação enviada.
- Priorização entre lembrete, streak, trial e reativação.
- Bloqueio de envios redundantes.
- Registro de envio para auditoria básica.

### Fora desta US

- Motor avançado de CRM.
- Segmentação complexa.
- Testes A/B de mensagens.
- Campanhas promocionais pesadas.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O sistema deve evitar múltiplas notificações desnecessárias no mesmo dia. |
| RN-002 | Deve existir limite diário por usuário. |
| RN-003 | Notificações transacionais de trial podem ter prioridade sobre lembretes. |
| RN-004 | Streak em risco pode ter prioridade sobre lembrete comum. |
| RN-005 | Usuário sem consentimento não deve receber push. |
| RN-006 | Usuário com acesso ativo não deve receber comunicação de reativação. |
| RN-007 | Toda tentativa de envio deve registrar decisão: enviada, ignorada ou falhou. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Recebe dentro dos limites e consentimento. |
| Premium Mensal | Recebe dentro dos limites e consentimento. |
| Premium Anual | Recebe dentro dos limites e consentimento. |
| Trial expirado | Pode receber reativação com baixa frequência. |
| Assinatura expirada | Pode receber reativação com baixa frequência. |
| Visitante | Não recebe. |

## 8. Fluxo principal

1. Um job tenta enviar notificação.
2. Sistema verifica consentimento.
3. Sistema verifica acesso e tipo de notificação.
4. Sistema verifica limite diário e prioridade.
5. Se permitido, envia push.
6. Se não permitido, registra motivo de ignorar.

## 9. Fluxos alternativos

### 9.1. Limite atingido

Não enviar nova notificação e registrar motivo.

### 9.2. Notificação de maior prioridade

Enviar apenas a de maior prioridade no ciclo atual.

## 10. Estados esperados

- envio permitido;
- envio bloqueado por limite;
- envio bloqueado por consentimento;
- envio priorizado;
- falha de envio;
- decisão registrada.

## 11. Impacto Flutter

- Não exige tela própria no MVP.
- Pode refletir preferências de notificação quando existir.

## 12. Impacto Backend

- Serviço central de elegibilidade de notificação.
- Registro de envio/decisão.
- Priorização por tipo.
- Integração com jobs de push.

## 13. Impacto DB

Entidades/campos:

- NotificationPreference;
- NotificationLog;
- lastNotificationSentAt;
- notificationType;
- decisionStatus;
- decisionReason.

## 14. Impacto Gamificação

- Evita que streak e quest virem pressão excessiva.
- Mantém tom saudável da retenção.

## 15. Impacto Monetização

- Evita desgaste comercial.
- Protege percepção de valor do trial e assinatura.

## 16. Contrato interno sugerido

```txt
POST /internal/notifications/evaluate
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| notification_send_blocked_by_limit | Quando envio é bloqueado por limite. |
| notification_send_decision_logged | Quando decisão é registrada. |

## 18. Critérios de aceite

### CA-001 — Limite diário

Dado que o usuário já recebeu o limite diário,
Quando novo job tentar enviar push,
Então o envio deve ser bloqueado.

### CA-002 — Priorização

Dado que há lembrete comum e streak em risco no mesmo ciclo,
Quando avaliar notificações,
Então o sistema deve priorizar a mensagem mais importante.

## 19. Critérios de teste QA

- limite diário atingido;
- sem consentimento;
- streak vs lembrete comum;
- trial vs lembrete comum;
- usuário expirado;
- registro de decisão;
- textos PT-BR, EN e ES quando aplicável.

## 20. Decisão registrada

O AWAKEN deve usar notificações como apoio ao hábito, nunca como fonte de irritação ou pressão comercial excessiva.
