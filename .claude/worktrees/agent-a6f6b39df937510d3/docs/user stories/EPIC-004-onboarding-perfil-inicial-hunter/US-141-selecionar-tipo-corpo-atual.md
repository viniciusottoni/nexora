---
title: US-141 — Selecionar tipo de corpo atual
sidebar_position: 141
---

# US-141 — Selecionar tipo de corpo atual

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-141 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile.bodyType |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **selecionar visualmente meu tipo de corpo atual**,

para **facilitar a personalização inicial da quest e do perfil visual**.

---

## 3. Contexto

A etapa 5/8 usa silhuetas para reduzir atrito e facilitar identificação. O dado complementa peso/altura e ajuda na personalização visual e de treino.

---

## 4. Objetivo

Permitir que o usuário selecione uma das quatro silhuetas de tipo de corpo atual.

---

## 5. Escopo

### Entra nesta US

- Tela 5/8 com seleção visual 2x2.
- Opções: corpo magro, corpo normal, corpo gordo, corpo atlético/forte.
- Seleção única obrigatória.
- Salvamento no perfil.
- Textos localizados e respeitosos.

### Fora desta US

- Avaliação corporal automática.
- Upload de foto.
- Diagnóstico estético.
- Comparações negativas.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Tipo de corpo é obrigatório na etapa 5/8. |
| RN-002 | O usuário deve selecionar apenas uma opção. |
| RN-003 | A linguagem deve ser respeitosa e não depreciativa. |
| RN-004 | O dado pode complementar geração de quest e perfil visual. |
| RN-005 | O dado não deve ser exibido em card compartilhável sem decisão explícita futura. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode responder. |
| Usuário em Trial | Pode responder. |
| Premium Mensal | Pode responder. |
| Premium Anual | Pode responder. |
| Trial expirado | Não pode responder. |
| Assinatura expirada | Não pode responder. |

---

## 8. Fluxo principal

1. Usuário chega à etapa 5/8.
2. App exibe quatro silhuetas.
3. Usuário seleciona uma opção.
4. App salva a resposta.
5. App avança para etapa 6/8.

---

## 9. Fluxos alternativos

### 9.1. Sem seleção

O app deve impedir avanço e pedir seleção.

### 9.2. Acessibilidade visual

A seleção deve ter label textual além da silhueta.

---

## 10. Estados esperados

- silhuetas carregadas;
- opção selecionada;
- erro de validação;
- salvando;
- fallback sem imagem;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Grid 2x2 de seleção visual.
- Labels acessíveis.
- Estado selecionado.
- Fallback textual.
- Textos localizados.

---

## 12. Impacto no Backend

- Validar valor permitido.
- Salvar em UserProfile.
- Retornar perfil atualizado.

---

## 13. Impacto no Banco de Dados

Entidade: UserProfile.

Campo:

- bodyType.

---

## 14. Impacto em Gamificação

- Pode influenciar personalização visual do Hunter.
- Não concede XP diretamente.

---

## 15. Impacto em Monetização

- Ajuda o trial parecer visualmente personalizado.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels das silhuetas. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
PATCH /api/users/me/profile/onboarding
```

Request:

```json
{
  "bodyType": "athletic_strong"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| onboarding_step_completed | Quando etapa 5/8 é salva. |

---

## 19. Critérios de aceite

### CA-001 — Tipo salvo

Dado que o usuário seleciona uma silhueta,

Quando avançar,

Então o tipo de corpo deve ser salvo no perfil.

### CA-002 — Acessibilidade

Dado que o usuário usa leitor de tela,

Quando navegar pelas opções,

Então cada silhueta deve ter descrição textual.

---

## 20. Critérios de teste para QA

- selecionar cada tipo;
- avançar sem seleção;
- validar labels de acessibilidade;
- fallback sem imagem;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Tipo de corpo atual deve ser coletado por seleção visual respeitosa, com fallback textual e sem exposição pública automática.
