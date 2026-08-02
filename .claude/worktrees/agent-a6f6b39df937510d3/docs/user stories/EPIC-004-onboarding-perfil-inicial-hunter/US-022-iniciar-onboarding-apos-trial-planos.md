---
title: US-022 — Iniciar onboarding após entender trial e planos
sidebar_position: 22
---

# US-022 — Iniciar onboarding após entender trial e planos

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-022 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | EPIC-003 — Trial e status de acesso |
| Status | Planejada |

---

## 2. História do usuário

Como **novo usuário com acesso ativo**,

quero **iniciar o onboarding após entender o trial e os planos**,

para **configurar meu perfil com transparência antes de receber minha primeira quest**.

---

## 3. Contexto

O AWAKEN deve evitar paywall surpresa. O usuário só deve entrar no onboarding depois de entender o modelo comercial, iniciar o trial ou possuir assinatura ativa.

---

## 4. Objetivo

Abrir o fluxo de onboarding apenas para usuários autenticados com acesso ativo e que ainda não tenham concluído o perfil inicial.

---

## 5. Escopo

### Entra nesta US

- Iniciar o fluxo de onboarding.
- Validar status de acesso antes de entrar.
- Exibir introdução curta do onboarding.
- Mostrar progresso das etapas.
- Permitir continuar de onde parou, se aplicável.

### Fora desta US

- Perguntas específicas do perfil.
- Geração da quest.
- Compra de assinatura.
- Edição futura do perfil.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Visitante não pode iniciar onboarding sem autenticação. |
| RN-002 | Usuário sem trial ativo ou assinatura ativa não pode iniciar onboarding. |
| RN-003 | Usuário com onboarding já concluído não deve refazer onboarding automaticamente. |
| RN-004 | Se o usuário sair no meio do onboarding, o app pode preservar progresso parcial quando viável. |
| RN-005 | O fluxo deve ser claro, curto e orientado à primeira quest. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode iniciar. |
| Usuário em Trial | Pode iniciar. |
| Premium Mensal | Pode iniciar. |
| Premium Anual | Pode iniciar. |
| Trial expirado | Não pode iniciar; deve ir para paywall. |
| Assinatura expirada | Não pode iniciar; deve ir para paywall. |

---

## 8. Fluxo principal

1. Usuário conclui autenticação e inicia trial ou possui assinatura ativa.
2. App verifica que onboarding ainda não foi concluído.
3. App exibe tela inicial do onboarding.
4. Usuário toca em começar.
5. App segue para a primeira pergunta do perfil.

---

## 9. Fluxos alternativos

### 9.1. Usuário sem acesso ativo

O app deve redirecionar para paywall ou tela de planos.

### 9.2. Onboarding já concluído

O app deve direcionar para Home/Quest, não para o início do onboarding.

---

## 10. Estados esperados

- verificando acesso;
- onboarding disponível;
- onboarding bloqueado;
- onboarding já concluído;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Rota de onboarding.
- Guard por status de acesso.
- Tela de introdução.
- Barra ou indicador de progresso.
- Textos localizados.

---

## 12. Impacto no Backend

- Fornecer status de onboarding do usuário.
- Validar status de acesso em endpoints protegidos.

---

## 13. Impacto no Banco de Dados

Entidade principal: UserProfile.

Campos:

- onboardingStartedAt;
- onboardingCompletedAt;
- currentOnboardingStep.

---

## 14. Impacto em Gamificação

- Prepara a criação do perfil Hunter.
- Não concede XP.

---

## 15. Impacto em Monetização

- Respeita a regra de trial/assinatura antes do onboarding.
- Evita dark pattern.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos do onboarding. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/users/me/onboarding-status
```

Response conceitual:

```json
{
  "onboardingCompleted": false,
  "currentStep": "goal"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| onboarding_started | Quando usuário inicia onboarding. |

---

## 19. Critérios de aceite

### CA-001 — Usuário com acesso ativo

Dado que o usuário possui trial ou assinatura ativa,

Quando abrir o onboarding pela primeira vez,

Então deve conseguir iniciar o fluxo.

### CA-002 — Usuário sem acesso

Dado que o trial expirou,

Quando tentar iniciar onboarding,

Então deve ser redirecionado para paywall.

---

## 20. Critérios de teste para QA

- trial ativo;
- assinatura ativa;
- trial expirado;
- onboarding já concluído;
- falha de conexão;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> O onboarding só deve começar após transparência comercial e com acesso ativo, garantindo coerência com o modelo trial-first do AWAKEN.
