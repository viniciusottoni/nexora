---
title: US-080 — Ter visual premium no card
sidebar_position: 80
---

# US-080 — Ter visual premium no card

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-080 |
| Épico | EPIC-010 — Perfil do Hunter e Card Compartilhável |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Assinante mensal ou anual |
| Plano | Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Subscription.accessStatus e HunterCard |
| Status | Planejada |

---

## 2. História do usuário

Como **assinante do AWAKEN**,

quero **ter um visual premium no meu card**,

para **sentir que minha assinatura entrega uma recompensa visual superior**.

---

## 3. Contexto

O visual premium do card é P1 porque aumenta valor percebido, mas não é obrigatório para o MVP funcional. Deve ser simples, elegante e não prejudicar performance.

---

## 4. Objetivo

Disponibilizar uma variante premium do card para usuários com assinatura ativa.

---

## 5. Escopo

### Entra nesta US

- Variante visual premium do card.
- Bordas, brilho ou destaque por rank.
- Selo de assinante ou S-Rank, se aprovado pelo produto.
- Manter dados reais de progresso.
- Manter ausência de dados sensíveis.

### Fora desta US

- Card animado premium completo.
- Editor avançado.
- Vários templates.
- Marketplace visual.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Visual premium só deve aparecer para assinatura ativa. |
| RN-002 | Trial usa card funcional, não premium. |
| RN-003 | Assinatura expirada remove acesso ao visual premium. |
| RN-004 | Visual premium não pode alterar dados de progresso. |
| RN-005 | Dados sensíveis continuam proibidos no card. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode acessar. |
| Usuário em Trial | Não acessa visual premium. |
| Premium Mensal | Pode acessar visual premium, se habilitado. |
| Premium Anual | Pode acessar visual premium, se habilitado. |
| Trial expirado | Não acessa. |
| Assinatura expirada | Não acessa. |

---

## 8. Fluxo principal

1. Assinante acessa Perfil Hunter.
2. App identifica assinatura ativa.
3. Usuário gera card.
4. App aplica variante visual premium.
5. Usuário visualiza e compartilha o card.

---

## 9. Fluxos alternativos

### 9.1. Assinatura expirada

O app deve voltar para card bloqueado ou funcional conforme regra comercial vigente.

### 9.2. Recurso premium desabilitado

Se a feature não estiver habilitada, assinante usa card completo padrão.

---

## 10. Estados esperados

- assinante ativo;
- card premium pronto;
- recurso desabilitado;
- assinatura expirada;
- erro de geração.

---

## 11. Impacto no Frontend Flutter

- Variante visual premium do card.
- Condicional por status de assinatura.
- Captura da imagem premium.
- Fallback para card padrão.

---

## 12. Impacto no Backend

- Retornar status de plano ativo.
- Retornar variante de card permitida.

---

## 13. Impacto no Banco de Dados

Usa dados de:

- Subscription;
- HunterProgress;
- HunterAttributes.

Campo opcional futuro:

- cardVariant.

---

## 14. Impacto em Gamificação

- Aumenta recompensa visual.
- Pode destacar rank e evolução.
- Não altera XP, rank ou atributos.

---

## 15. Impacto em Monetização

- Reforça benefício premium.
- Pode incentivar conversão do trial para assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Selo ou labels premium. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/hunter/card-data
```

Response parcial:

```json
{
  "accessStatus": "subscription_active",
  "cardVariant": "premium"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| premium_card_generated | Quando card premium é gerado. |
| hunter_card_shared | Quando card premium é compartilhado. |

---

## 19. Critérios de aceite

### CA-001 — Premium para assinante

Dado que o usuário possui assinatura ativa,

Quando gerar o card,

Então pode receber visual premium se a feature estiver habilitada.

### CA-002 — Trial sem premium

Dado que o usuário está em trial,

Quando gerar card,

Então não deve receber visual premium.

### CA-003 — Expirado sem premium

Dado que a assinatura expirou,

Quando gerar card,

Então o visual premium não deve estar disponível.

---

## 20. Critérios de teste para QA

- mensal ativo;
- anual ativo;
- trial ativo;
- assinatura expirada;
- feature premium desabilitada;
- ausência de dados sensíveis;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Visual premium do card é P1 e deve funcionar como benefício visual para assinantes ativos, sem alterar dados reais de progresso.
