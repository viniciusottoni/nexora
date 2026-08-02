---
title: US-030 — Informar dores físicas
sidebar_position: 30
---

# US-030 — Informar dores físicas

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-030 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile.physicalPains |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **informar minhas dores físicas atuais**,

para **ajudar o AWAKEN a evitar exercícios que possam piorar desconfortos relatados**.

---

## 3. Contexto

Dores físicas são coletadas separadamente das limitações. Elas orientam a geração do treino, enquanto limitações físicas filtram exercícios contraindicados do catálogo.

---

## 4. Objetivo

Coletar regiões de dor atuais do usuário e salvar no perfil para apoiar a personalização da quest.

---

## 5. Escopo

### Entra nesta US

- Seleção múltipla de dores físicas.
- Opções: pescoço, ombro, pulso, costas, lombar e joelhos.
- Opção “não sinto dores”.
- Validação de consistência.
- Salvamento no perfil.
- Aviso de que o app não substitui orientação profissional.

### Fora desta US

- Diagnóstico médico.
- Tratamento fisioterapêutico.
- Prescrição clínica.
- Bloqueio absoluto de exercício, que pertence às limitações físicas da US-142.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O usuário deve informar dores ou escolher “não sinto dores”. |
| RN-002 | Dores físicas devem orientar a geração do treino. |
| RN-003 | Dores não substituem limitações físicas do catálogo. |
| RN-004 | Dados de dores não podem aparecer no card compartilhável. |
| RN-005 | O app deve exibir aviso de responsabilidade profissional. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode responder. |
| Usuário em Trial | Pode responder. |
| Premium Mensal | Pode responder. |
| Premium Anual | Pode responder. |
| Trial expirado | Não pode responder durante bloqueio. |
| Assinatura expirada | Não pode responder durante bloqueio. |

---

## 8. Fluxo principal

1. Usuário chega à etapa 8/8 do onboarding.
2. App exibe regiões de dor física.
3. Usuário seleciona uma ou mais regiões, ou escolhe “não sinto dores”.
4. App valida a resposta.
5. App salva as dores físicas no perfil.
6. Usuário segue para revisão final.

---

## 9. Fluxos alternativos

### 9.1. Sem dores

Se o usuário escolher “não sinto dores”, outras seleções devem ser limpas.

### 9.2. Múltiplas dores

O usuário pode selecionar mais de uma região.

---

## 10. Estados esperados

- pronto para seleção;
- dores selecionadas;
- sem dores;
- erro de consistência;
- salvando;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Tela de seleção múltipla.
- Opção “não sinto dores”.
- Aviso de responsabilidade.
- Textos localizados.

---

## 12. Impacto no Backend

- Validar dores permitidas.
- Salvar em UserProfile.
- Disponibilizar para geração de treino.

---

## 13. Impacto no Banco de Dados

Entidade: UserProfile.

Campo:

- physicalPains.

---

## 14. Impacto em Gamificação

- Pode ajustar o risco e intensidade da quest.
- Não concede XP diretamente.

---

## 15. Impacto em Monetização

- Reforça valor do trial pela personalização cuidadosa.
- Não altera assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Regiões de dor, aviso e validações. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |
| FR | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
PATCH /api/users/me/profile/onboarding
```

Request:

```json
{
  "physicalPains": ["shoulder", "lower_back"]
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| onboarding_step_completed | Quando dores físicas são salvas. |

---

## 19. Critérios de aceite

### CA-001 — Dores salvas

Dado que o usuário seleciona dores,

Quando avançar,

Então as dores devem ser salvas no perfil.

### CA-002 — Sem dores

Dado que o usuário seleciona “não sinto dores”,

Quando avançar,

Então o perfil deve ser salvo sem dores físicas.

### CA-003 — Opção exclusiva limpa outras seleções

Dado que o usuário selecionou uma ou mais regiões de dor,

Quando selecionar “não sinto dores”,

Então as demais seleções devem ser removidas automaticamente.

---

## 20. Critérios de teste para QA

- selecionar sem dores;
- selecionar uma dor;
- selecionar múltiplas dores;
- validar aviso de responsabilidade;
- validar consistência da opção sem dores;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Dores físicas orientam a geração do treino, mas não substituem limitações físicas usadas para filtrar exercícios contraindicados.
