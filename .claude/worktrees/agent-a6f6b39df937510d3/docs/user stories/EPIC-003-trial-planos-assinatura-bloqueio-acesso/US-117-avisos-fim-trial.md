---
title: US-117 — Receber avisos de fim do trial
sidebar_position: 117
---

# US-117 — Receber avisos de fim do trial

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-117 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial |
| Plano | Trial |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Status do trial e notificações |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário em trial**,

quero **receber avisos quando meu teste estiver próximo do fim**,

para **decidir se vou assinar antes de perder acesso**.

---

## 3. Contexto

Avisos de fim de trial ajudam conversão e reduzem surpresa. Como P1, podem entrar no final do MVP ou logo após, desde que respeitem consentimento e evitem pressão excessiva.

---

## 4. Objetivo

Avisar o usuário sobre proximidade do fim do trial em momentos úteis, como 3 dias restantes, 1 dia restante e dia da expiração.

---

## 5. Escopo

### Entra nesta US

- Aviso in-app de fim de trial.
- Push notification se o usuário permitiu notificações.
- Mensagens claras e localizadas.
- CTA para visualizar planos.
- Evitar notificações para assinante ativo.

### Fora desta US

- Campanhas complexas.
- Descontos personalizados.
- Sequência avançada de CRM.
- E-mail marketing.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Avisos devem ser enviados apenas para usuários em trial. |
| RN-002 | Usuário assinante não deve receber aviso de fim de trial. |
| RN-003 | Push depende de permissão do usuário. |
| RN-004 | A mensagem deve ser informativa, sem urgência falsa. |
| RN-005 | O aviso deve levar para tela de planos quando houver CTA. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode receber aviso. |
| Premium Mensal | Não recebe aviso. |
| Premium Anual | Não recebe aviso. |
| Trial expirado | Recebe comunicação de reativação, não aviso de fim. |
| Assinatura expirada | Não recebe aviso de trial. |

---

## 8. Fluxo principal

1. Sistema identifica usuário em trial.
2. Calcula dias restantes.
3. Quando atinge marco definido, gera aviso.
4. Usuário recebe aviso in-app ou push.
5. Usuário toca no CTA e vê planos.

---

## 9. Fluxos alternativos

### 9.1. Push não permitido

O app deve usar apenas aviso in-app quando aplicável.

### 9.2. Usuário assinou antes do aviso

O aviso não deve ser enviado.

---

## 10. Estados esperados

- trial com 3 dias restantes;
- trial com 1 dia restante;
- trial expira hoje;
- push permitido;
- push negado;
- usuário convertido.

---

## 11. Impacto no Frontend Flutter

- Banner in-app.
- CTA para planos.
- Integração com push, se disponível.
- Textos localizados.

---

## 12. Impacto no Backend

- Identificar usuários em trial próximos da expiração.
- Agendar ou disparar aviso.
- Evitar duplicidade de aviso.

---

## 13. Impacto no Banco de Dados

Campos possíveis:

- trialEndsAt;
- lastTrialReminderSentAt;
- notificationPreference.

---

## 14. Impacto em Gamificação

- Ajuda a preservar continuidade de streak.
- Não altera XP ou rank.

---

## 15. Impacto em Monetização

- Ajuda conversão antes da expiração.
- Deve respeitar transparência e consentimento.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de aviso. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

Não obrigatório para o MVP se o aviso for calculado localmente com status do backend. Para push, usar serviço de notificações do EPIC-013.

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| trial_reminder_viewed | Quando aviso é visto. |
| trial_reminder_clicked | Quando usuário toca no CTA. |

---

## 19. Critérios de aceite

### CA-001 — Aviso no momento correto

Dado que faltam poucos dias para o trial acabar,

Quando o usuário abrir o app,

Então deve ver aviso claro sobre o fim do trial.

### CA-002 — Usuário assinante não recebe

Dado que o usuário assinou,

Quando chegar o marco de aviso,

Então nenhum aviso de fim de trial deve ser exibido.

---

## 20. Critérios de teste para QA

- 3 dias restantes;
- 1 dia restante;
- trial expira hoje;
- push permitido;
- push negado;
- usuário já assinante;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Avisos de fim de trial são P1 e devem ser úteis, transparentes e sem pressão enganosa.
