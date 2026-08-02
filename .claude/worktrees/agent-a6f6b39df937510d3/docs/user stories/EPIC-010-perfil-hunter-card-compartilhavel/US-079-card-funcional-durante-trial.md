---
title: US-079 — Ter card funcional durante trial
sidebar_position: 79
---

# US-079 — Ter card funcional durante trial

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-079 |
| Épico | EPIC-010 — Perfil do Hunter e Card Compartilhável |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial |
| Plano | Trial |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Subscription.accessStatus e HunterCard |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário em trial**,

quero **ter um card funcional durante meu teste gratuito**,

para **experimentar o valor social e visual do AWAKEN antes de assinar**.

---

## 3. Contexto

O trial deve entregar valor real. O card funcional permite que o usuário experimente a recompensa visual, mas o visual premium pode ficar reservado para assinantes.

---

## 4. Objetivo

Permitir que usuários em trial gerem e compartilhem um card funcional, sem recursos premium visuais avançados.

---

## 5. Escopo

### Entra nesta US

- Card funcional no trial.
- Exibição de progresso real.
- Compartilhamento permitido.
- Visual base sem elementos premium exclusivos.
- Marca AWAKEN discreta.

### Fora desta US

- Card animado premium.
- Efeitos exclusivos de assinante.
- Customização avançada.
- Remoção de marca.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Usuário em trial pode gerar card funcional. |
| RN-002 | Card do trial deve mostrar dados reais de progresso. |
| RN-003 | Card do trial não deve exibir dados sensíveis. |
| RN-004 | Card do trial não deve usar visual premium reservado. |
| RN-005 | Trial expirado não deve gerar card completo. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode gerar. |
| Usuário em Trial | Pode gerar card funcional. |
| Premium Mensal | Pode gerar card completo. |
| Premium Anual | Pode gerar card completo/premium conforme US-080. |
| Trial expirado | Não pode gerar card funcional completo. |
| Assinatura expirada | Não pode gerar card completo. |

---

## 8. Fluxo principal

1. Usuário em trial acessa Perfil Hunter.
2. Toca em gerar card.
3. App monta card funcional com dados reais.
4. Usuário visualiza e pode compartilhar.

---

## 9. Fluxos alternativos

### 9.1. Trial expirado durante uso

O app deve bloquear geração e direcionar para paywall.

### 9.2. Dados de progresso inexistentes

O card deve usar estado inicial do Hunter.

---

## 10. Estados esperados

- trial ativo;
- card funcional pronto;
- progresso inicial;
- trial expirado;
- erro de geração.

---

## 11. Impacto no Frontend Flutter

- Variante visual do card para trial.
- Bloqueio de elementos premium.
- Captura e compartilhamento iguais ao fluxo base.

---

## 12. Impacto no Backend

- Retornar status de trial ativo.
- Fornecer dados reais de progresso.

---

## 13. Impacto no Banco de Dados

Usa dados de:

- Subscription;
- HunterProgress;
- HunterAttributes.

---

## 14. Impacto em Gamificação

- Demonstra recompensa visual durante trial.
- Aumenta motivação e chance de conversão.

---

## 15. Impacto em Monetização

- Trial entrega valor real.
- Visual premium fica como incentivo para assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels do card trial. |
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
  "accessStatus": "trial_active",
  "cardVariant": "trial"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| trial_card_generated | Quando card funcional do trial é gerado. |
| hunter_card_shared | Quando card é compartilhado. |

---

## 19. Critérios de aceite

### CA-001 — Card no trial

Dado que o usuário está em trial ativo,

Quando gerar card,

Então deve receber um card funcional.

### CA-002 — Sem premium no trial

Dado que o usuário está em trial,

Quando o card for gerado,

Então elementos premium exclusivos não devem aparecer.

---

## 20. Critérios de teste para QA

- gerar card no trial;
- compartilhar card no trial;
- validar ausência de visual premium;
- trial expirado;
- progresso inicial;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> O card do trial deve ser funcional e compartilhável, mas sem os elementos visuais premium reservados aos assinantes.
