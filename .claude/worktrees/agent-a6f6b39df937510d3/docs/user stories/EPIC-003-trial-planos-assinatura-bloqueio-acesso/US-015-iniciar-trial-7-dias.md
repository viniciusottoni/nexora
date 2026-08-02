---
title: US-015 — Iniciar teste gratuito de 7 dias
sidebar_position: 15
---

# US-015 — Iniciar teste gratuito de 7 dias

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-015 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Visitante ou usuário autenticado sem trial |
| Plano | Trial |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Usuário autenticado e controle de trial |
| Status | Planejada |

---

## 2. História do usuário

Como **visitante ou usuário recém-cadastrado**,

quero **iniciar meu teste gratuito de 7 dias**,

para **experimentar o AWAKEN antes de assinar um plano mensal ou anual**.

---

## 3. Contexto

O trial é a entrada real no produto. Após compreender a regra comercial, o usuário precisa iniciar o período gratuito para acessar onboarding, quests, XP, perfil e demais recursos do MVP.

---

## 4. Objetivo

Permitir que um usuário elegível inicie um único trial gratuito de 7 dias.

---

## 5. Escopo

### Entra nesta US

- CTA para iniciar trial.
- Verificação de elegibilidade.
- Criação do status `trial_active`.
- Definição de início e fim do trial.
- Redirecionamento para onboarding.
- Mensagem de sucesso.

### Fora desta US

- Assinatura mensal ou anual.
- Paywall pós-expiração.
- Notificações de fim de trial.
- Renovação de trial.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Cada usuário só pode iniciar um trial gratuito. |
| RN-002 | O trial deve durar 7 dias corridos. |
| RN-003 | O trial deve possuir data/hora de início e fim. |
| RN-004 | Usuário que já consumiu trial não pode iniciar outro. |
| RN-005 | Após iniciar trial, usuário deve seguir para onboarding se ainda não concluiu. |
| RN-006 | Iniciar trial não cria assinatura paga automaticamente. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode iniciar após criar conta ou autenticar. |
| Usuário autenticado sem trial | Pode iniciar, se elegível. |
| Usuário em Trial | Não pode iniciar outro trial. |
| Premium Mensal | Não precisa iniciar trial. |
| Premium Anual | Não precisa iniciar trial. |
| Trial expirado | Não pode reiniciar trial. |
| Assinatura expirada | Não pode reiniciar trial. |

---

## 8. Fluxo principal

1. Usuário toca em iniciar teste gratuito.
2. App verifica se usuário está autenticado.
3. Backend valida elegibilidade.
4. Backend cria trial de 7 dias.
5. App recebe status `trial_active`.
6. Usuário é direcionado para onboarding ou home.

---

## 9. Fluxos alternativos

### 9.1. Usuário não autenticado

O app deve direcionar para cadastro ou login antes de iniciar trial.

### 9.2. Usuário não elegível

O app deve exibir mensagem clara e direcionar para planos mensal/anual.

### 9.3. Falha de conexão

O app deve permitir nova tentativa sem iniciar trial duplicado.

---

## 10. Estados de tela ou estados esperados

- pronto para iniciar;
- autenticando;
- iniciando trial;
- trial iniciado;
- não elegível;
- erro de conexão;
- erro inesperado.

---

## 11. Impacto no Frontend Flutter

- CTA de início do trial.
- Estado de processamento.
- Tratamento de elegibilidade.
- Redirecionamento para onboarding/home.
- Textos localizados.

---

## 12. Impacto no Backend

- Endpoint para iniciar trial.
- Validação de elegibilidade.
- Registro de datas do trial.
- Garantia contra duplicidade.

---

## 13. Impacto no Banco de Dados

Entidade principal: Subscription.

Campos relevantes:

- userId;
- plan;
- status;
- trialStartedAt;
- trialEndsAt;
- trialConsumedAt.

---

## 14. Impacto em Gamificação

- Libera acesso aos recursos que geram XP e evolução.
- Não concede XP no ato de iniciar trial.

---

## 15. Impacto em Monetização

- É o início da jornada de conversão.
- Deve preservar transparência: trial não é plano gratuito permanente.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de trial iniciado e erro. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/subscriptions/trial/start
```

Request:

```json
{}
```

Response conceitual:

```json
{
  "accessStatus": "trial_active",
  "trialStartedAt": "2026-06-18T10:00:00Z",
  "trialEndsAt": "2026-06-25T10:00:00Z"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| trial_started | Quando o trial é iniciado com sucesso. |
| trial_start_failed | Quando tentativa de iniciar trial falha. |

---

## 19. Critérios de aceite

### CA-001 — Início válido

Dado que o usuário é elegível,

Quando iniciar o teste,

Então o sistema deve criar trial ativo por 7 dias.

### CA-002 — Usuário não elegível

Dado que o usuário já consumiu trial,

Quando tentar iniciar novo trial,

Então deve ser direcionado para planos pagos.

---

## 20. Critérios de teste para QA

- iniciar trial com usuário novo;
- tentar iniciar trial duplicado;
- iniciar sem autenticação;
- falha de conexão;
- validar datas de início e fim;
- validar redirecionamento para onboarding.

---

## ✅ Decisão registrada

> O trial é único por usuário, dura 7 dias e libera o uso do MVP sem criar assinatura paga automática.
