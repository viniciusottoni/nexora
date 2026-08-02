---
title: US-017 — Visualizar benefícios dos planos mensal e anual
sidebar_position: 17
---

# US-017 — Visualizar benefícios dos planos mensal e anual

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-017 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Visitante, usuário em trial ou usuário com acesso expirado |
| Plano | Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Catálogo de planos e RevenueCat |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário do AWAKEN**,

quero **visualizar os benefícios dos planos mensal e anual de forma clara**,

para **decidir qual assinatura faz mais sentido para continuar minha evolução**.

---

## 3. Contexto

Após o trial, o usuário precisa escolher entre mensal e anual. A tela deve explicar benefícios e diferenças sem confundir, sem esconder preço e sem sugerir plano gratuito permanente.

A escolha de mensal ou anual deve acontecer na tela pricing, antes do cadastro, e ficar salva para ser aplicada quando a conta for criada.

---

## 4. Objetivo

Apresentar os planos pagos com preço, periodicidade, benefícios, CTA e indicação de melhor custo-benefício quando aplicável.

Também garantir que a tela seja o ponto único de escolha do revenue para o fluxo comercial do MVP.

---

## 5. Escopo

### Entra nesta US

- Card do plano mensal.
- Card do plano anual.
- Benefícios principais.
- Preço e periodicidade.
- Indicação de economia anual, se disponível.
- CTA para selecionar plano.
- Persistência da escolha feita na pricing.
- Fallback se preços não carregarem.

### Fora desta US

- Processamento da compra.
- Reativação de acesso.
- A/B test de preços.
- Cupons e promoções.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A tela deve mostrar mensal e anual. |
| RN-002 | A tela pricing é o único canal de escolha do plano e deve salvar a opção selecionada. |
| RN-003 | Preços devem vir da fonte configurada para a loja/plataforma quando disponível. |
| RN-004 | Benefícios devem ser claros e verdadeiros. |
| RN-005 | Plano anual pode ser destacado como melhor custo-benefício. |
| RN-006 | Não deve existir CTA para plano gratuito permanente. |
| RN-007 | Se preço não carregar, não deve exibir valor inventado. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode visualizar e escolher na pricing. |
| Usuário em Trial | Pode visualizar planos. |
| Premium Mensal | Pode visualizar plano anual como possível mudança futura, se aplicável. |
| Premium Anual | Pode visualizar status atual. |
| Trial expirado | Deve visualizar planos para reativar acesso. |
| Assinatura expirada | Deve visualizar planos para reativar acesso. |

---

## 8. Fluxo principal

1. Usuário acessa tela de planos.
2. App carrega ofertas disponíveis.
3. Exibe mensal e anual.
4. Usuário compara benefícios.
5. Usuário seleciona um plano na pricing e a escolha fica salva.
6. App segue para o cadastro e, depois, para o fluxo de assinatura correspondente.

---

## 9. Fluxos alternativos

### 9.1. Preços indisponíveis

O app deve explicar que não conseguiu carregar os preços e permitir tentar novamente.

### 9.2. Usuário já assinante

A tela deve indicar status atual e evitar compra duplicada indevida.

---

## 10. Estados de tela ou estados esperados

- carregando planos;
- planos carregados;
- preço indisponível;
- selecionando plano;
- erro de conexão;
- assinante ativo.

---

## 11. Impacto no Frontend Flutter

- Tela ou seção de planos.
- Cards mensal/anual.
- Destaque visual do anual.
- Estados de loading e erro.
- Integração com SDK de assinatura.
- Textos localizados.

---

## 12. Impacto no Backend

- Pode consumir configuração de planos.
- Pode validar status atual do usuário.
- Pode receber webhook futuro da plataforma de assinatura.

---

## 13. Impacto no Banco de Dados

Entidade principal: Subscription.

Campos relevantes:

- plan;
- status;
- expiresAt;
- revenueCatCustomerId.

---

## 14. Impacto em Gamificação

- Não altera XP.
- Permite continuidade da jornada após assinatura.

---

## 15. Impacto em Monetização

- É tela central para conversão.
- Deve deixar claro valor do mensal e anual.
- Deve evitar promessas falsas.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Preço, benefícios e CTA. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/app-config/plans?platform=android&locale=pt-BR
```

Response conceitual:

```json
{
  "plans": [
    { "id": "monthly", "label": "Mensal" },
    { "id": "annual", "label": "Anual" }
  ]
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| plans_viewed | Quando planos são exibidos. |
| monthly_plan_selected | Quando mensal é selecionado. |
| annual_plan_selected | Quando anual é selecionado. |

---

## 19. Critérios de aceite

### CA-001 — Planos exibidos

Dado que a tela de planos carrega com sucesso,

Quando o usuário visualizar a tela,

Então deve ver mensal e anual.

### CA-002 — Preço indisponível

Dado que os preços não carregaram,

Quando a tela for exibida,

Então o app não deve inventar valores.

---

## 20. Critérios de teste para QA

- visualizar mensal e anual;
- selecionar mensal;
- selecionar anual;
- testar preço indisponível;
- testar usuário já assinante;
- validar textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> A tela de planos deve ser simples, transparente e focada na escolha entre mensal e anual, sem mencionar plano gratuito permanente.
