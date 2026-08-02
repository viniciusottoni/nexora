---
title: US-122 — Impedir reinício indevido de trial
sidebar_position: 122
---

# US-122 — Impedir reinício indevido de trial

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-122 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | User, Subscription e regra de elegibilidade |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **impedir que o mesmo usuário reinicie trial indevidamente**,

para **proteger o modelo comercial do AWAKEN**.

---

## 3. Contexto

O trial gratuito deve ser uma oportunidade real de teste, mas não pode ser reiniciado indefinidamente na mesma conta. O backend precisa guardar o consumo do trial e validar elegibilidade antes de iniciar novo período.

---

## 4. Objetivo

Garantir que cada conta tenha no máximo um trial gratuito de 7 dias.

---

## 5. Escopo

### Entra nesta US

- Verificação de elegibilidade antes de iniciar trial.
- Registro de trial consumido.
- Bloqueio de novo trial na mesma conta.
- Mensagem clara para usuário não elegível.
- Direcionamento para planos pagos.

### Fora desta US

- Detecção avançada antifraude por dispositivo.
- Bloqueio por CPF ou documento.
- Análise manual de abuso.
- Regras complexas de risco.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Cada conta pode iniciar apenas um trial. |
| RN-002 | Trial iniciado deve marcar a conta como trial consumido. |
| RN-003 | Trial expirado não pode ser reiniciado. |
| RN-004 | Assinante anterior não pode iniciar novo trial na mesma conta. |
| RN-005 | Usuário não elegível deve ser direcionado para plano mensal ou anual. |
| RN-006 | A validação deve ocorrer no backend. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode tentar iniciar trial após criar conta. |
| Usuário autenticado sem trial | Pode iniciar se elegível. |
| Usuário em Trial | Não pode iniciar outro trial. |
| Trial expirado | Não pode reiniciar trial. |
| Premium Mensal | Não pode iniciar trial. |
| Premium Anual | Não pode iniciar trial. |
| Assinatura expirada | Não pode iniciar trial. |
| Sistema | Valida elegibilidade. |

---

## 8. Fluxo principal

1. Usuário solicita início de trial.
2. Backend consulta se a conta já consumiu trial.
3. Se não consumiu, backend inicia trial.
4. Se já consumiu, backend nega a solicitação.
5. App exibe mensagem clara e direciona para planos.

---

## 9. Fluxos alternativos

### 9.1. Usuário em trial tenta iniciar novamente

Sistema deve retornar status atual sem criar novo período.

### 9.2. Usuário com trial expirado tenta iniciar novamente

Sistema deve negar e orientar assinatura.

### 9.3. Falha de conexão

App deve permitir nova tentativa sem criar registros duplicados.

---

## 10. Estados esperados

- elegível;
- trial iniciado;
- trial já ativo;
- trial já consumido;
- não elegível;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Tratar resposta de não elegível.
- Exibir mensagem clara.
- Direcionar para planos pagos.
- Evitar duplicar solicitação ao tocar várias vezes.

---

## 12. Impacto no Backend

- Validar elegibilidade.
- Persistir consumo do trial.
- Garantir idempotência.
- Retornar erro funcional claro.

---

## 13. Impacto no Banco de Dados

Entidades:

- User;
- Subscription.

Campos relevantes:

- trialStartedAt;
- trialEndsAt;
- trialConsumedAt;
- accessStatus.

---

## 14. Impacto em Gamificação

- Não altera progresso.
- Impede uso prolongado gratuito sem assinatura.

---

## 15. Impacto em Monetização

- Protege o modelo de assinatura obrigatória após trial.
- Direciona usuários não elegíveis para mensal/anual.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagem de trial já utilizado. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/subscriptions/trial/start
```

Erro esperado:

```json
{
  "code": "TRIAL_ALREADY_USED",
  "message": "Seu teste gratuito já foi utilizado. Escolha um plano para continuar.",
  "correlationId": "uuid"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| trial_start_blocked | Quando sistema impede novo trial. |
| plans_viewed | Quando usuário é direcionado aos planos. |

---

## 19. Critérios de aceite

### CA-001 — Usuário elegível

Dado que a conta nunca iniciou trial,

Quando solicitar início,

Então o trial deve ser criado.

### CA-002 — Trial já consumido

Dado que a conta já usou trial,

Quando tentar iniciar novamente,

Então o sistema deve bloquear e direcionar para planos.

### CA-003 — Idempotência

Dado que o usuário toca várias vezes no CTA,

Quando a solicitação for processada,

Então apenas um trial deve existir.

---

## 20. Critérios de teste para QA

- usuário novo elegível;
- usuário com trial ativo;
- usuário com trial expirado;
- usuário assinante;
- múltiplos toques no CTA;
- falha de conexão;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> O trial do AWAKEN é único por conta. Após consumido, o usuário deve escolher plano mensal ou anual para continuar.
