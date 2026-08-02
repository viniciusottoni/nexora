---
title: US-014 — Entender teste gratuito e assinatura antes do onboarding
sidebar_position: 14
---

# US-014 — Entender teste gratuito e assinatura antes do onboarding

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-014 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Visitante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Tela de proposta comercial e roteamento inicial |
| Status | Planejada |

---

## 2. História do usuário

Como **visitante**,

quero **entender antes do onboarding que posso testar o AWAKEN por 7 dias e depois precisarei assinar**,

para **iniciar minha jornada com transparência e sem paywall surpresa**.

---

## 3. Contexto

O AWAKEN não terá plano gratuito permanente no MVP. Por isso, a comunicação comercial deve acontecer antes do onboarding, deixando claro que o trial dura 7 dias e que depois será necessário escolher plano mensal ou anual.

A tela pricing é o único canal para escolher o plano. A seleção feita ali deve ser salva para uso posterior, antes da criação da conta, e aplicada quando a conta estiver vinculada à compra.

---

## 4. Objetivo

Exibir uma tela clara e objetiva sobre o teste gratuito, os planos disponíveis e a regra de assinatura obrigatória após o período de teste.

Também garantir que a escolha do plano ocorra antes do cadastro, seja salva e siga para a etapa de autenticação/cadastro sem efetuar compra imediata.

---

## 5. Escopo

### Entra nesta US

- Tela de explicação do trial de 7 dias.
- Mensagem clara sobre assinatura após o trial.
- Apresentação resumida dos planos mensal e anual.
- Escolha do plano mensal ou anual na pricing.
- Salvamento da escolha para uso posterior.
- CTA para iniciar o teste gratuito.
- Textos localizados.
- Estado de erro caso a configuração de planos não carregue.

### Fora desta US

- Compra da assinatura.
- Início técnico do trial.
- Paywall pós-expiração.
- Tela completa de checkout.
- A/B test de preços.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A comunicação do trial deve aparecer antes do onboarding. |
| RN-002 | A tela deve informar que o teste gratuito dura 7 dias. |
| RN-003 | A tela deve informar que após o trial será necessário assinar para continuar. |
| RN-004 | A tela pricing deve ser o único canal de escolha do plano mensal ou anual. |
| RN-005 | A seleção feita na pricing deve ser salva para uso posterior quando a conta for criada. |
| RN-006 | A tela não deve sugerir existência de plano gratuito permanente. |
| RN-007 | O CTA principal deve levar ao início do trial ou ao fluxo necessário para isso. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode visualizar e escolher o plano na pricing. |
| Usuário em Trial | Pode visualizar em configurações ou tela de planos, se necessário. |
| Premium Mensal | Pode visualizar planos, mas não precisa iniciar trial. |
| Premium Anual | Pode visualizar planos, mas não precisa iniciar trial. |
| Trial expirado | Deve visualizar paywall específico, não apenas esta tela. |
| Assinatura expirada | Deve visualizar paywall específico, não apenas esta tela. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. Visitante abre o app após a splash.
2. App identifica que ainda não existe trial ativo nem assinatura.
3. App exibe tela de explicação do trial.
4. Usuário entende a regra de 7 dias e planos disponíveis.
5. Usuário escolhe mensal ou anual na pricing e a escolha é salva.
6. Usuário toca em iniciar teste gratuito.
7. App segue para o fluxo de autenticação ou início de trial, conforme ordem definida.

---

## 9. Fluxos alternativos

### 9.1. Configuração de planos indisponível

Se os dados de preço não carregarem, a tela deve manter a explicação do trial e permitir seguir com fallback seguro.

### 9.2. Usuário já autenticado

Se o usuário já estiver autenticado e sem trial iniciado, a tela deve permitir iniciar trial.

---

## 10. Estados de tela ou estados esperados

- carregando planos;
- conteúdo carregado;
- fallback de configuração;
- erro de conexão;
- CTA habilitado;
- CTA em processamento.

---

## 11. Impacto no Frontend Flutter

- Criar tela de proposta/trial.
- Criar cards simples de plano mensal e anual.
- Criar CTA de iniciar teste.
- Integrar com roteamento inicial.
- Localizar textos.

---

## 12. Impacto no Backend

- Pode consumir endpoint de configuração de planos.
- Deve permitir fallback caso a configuração remota não esteja disponível.

---

## 13. Impacto no Banco de Dados

Não há impacto direto nesta US.

---

## 14. Impacto em Gamificação

- Não concede XP.
- Prepara o usuário para a jornada de evolução.

---

## 15. Impacto em Monetização

- Define transparência do modelo comercial.
- Evita dark pattern.
- Ajuda conversão qualificada após trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Texto principal do MVP. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

### Endpoint

```txt
GET /api/app-config/plans?platform=android&locale=pt-BR
```

### Response conceitual

```json
{
  "trialDays": 7,
  "plans": ["monthly", "annual"]
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| trial_offer_viewed | Quando a tela de trial é exibida. |
| plans_viewed | Quando os planos mensal/anual são exibidos. |
| monthly_plan_selected | Quando o plano mensal é escolhido na pricing. |
| annual_plan_selected | Quando o plano anual é escolhido na pricing. |

---

## 19. Critérios de aceite

### CA-001 — Comunicação antes do onboarding

Dado que o usuário é visitante,

Quando abrir o app pela primeira vez,

Então deve ver a explicação do trial antes do onboarding.

### CA-002 — Sem plano gratuito permanente

Dado que a tela é exibida,

Quando o usuário ler a proposta,

Então deve ficar claro que o acesso gratuito dura 7 dias.

### CA-003 — CTA para trial

Dado que o usuário visualiza a tela,

Quando tocar em iniciar teste,

Então deve seguir para o fluxo correto.

---

## 20. Critérios de teste para QA

- visitante novo;
- usuário autenticado sem trial;
- falha ao carregar planos;
- textos em PT-BR, EN e ES;
- ausência de menção a plano gratuito permanente;
- evento `trial_offer_viewed`.

---

## ✅ Decisão registrada

> A regra comercial do AWAKEN deve ser comunicada antes do onboarding: 7 dias gratuitos e assinatura mensal ou anual obrigatória para continuar.
