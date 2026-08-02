---
title: US-023 — Informar objetivo principal
sidebar_position: 23
---

# US-023 — Informar objetivo principal

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-023 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile.goal |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **informar meu objetivo principal**,

para **receber treinos coerentes com minha meta física**.

---

## 3. Contexto

O objetivo principal orienta a geração das quests. Sem essa informação, o sistema não consegue diferenciar treino para emagrecimento, ganho de força, condicionamento ou saúde geral.

---

## 4. Objetivo

Coletar o objetivo principal do usuário durante o onboarding e salvar essa informação no perfil.

---

## 5. Escopo

### Entra nesta US

- Tela de pergunta sobre objetivo principal.
- Opções objetivas e fáceis de entender.
- Seleção obrigatória de uma opção principal.
- Salvamento temporário ou definitivo no perfil.
- Textos localizados.

### Fora desta US

- Metas avançadas com números detalhados.
- Plano de treino completo.
- Nutrição personalizada.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O objetivo principal é obrigatório. |
| RN-002 | O usuário deve selecionar pelo menos uma opção principal. |
| RN-003 | As opções devem ser claras para iniciantes. |
| RN-004 | O objetivo deve influenciar a geração da quest diária. |
| RN-005 | O usuário poderá editar o objetivo depois, conforme US-034. |

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

1. Usuário chega à etapa de objetivo.
2. App exibe opções de objetivo.
3. Usuário seleciona uma opção.
4. App valida seleção.
5. App salva a resposta e avança para próxima etapa.

---

## 9. Fluxos alternativos

### 9.1. Usuário tenta avançar sem selecionar

O app deve exibir validação solicitando escolha de um objetivo.

### 9.2. Usuário volta etapa

A resposta selecionada deve permanecer marcada, se já foi informada.

---

## 10. Estados esperados

- carregando;
- pronto para seleção;
- opção selecionada;
- erro de validação;
- salvando;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Tela de seleção de objetivo.
- Cards ou botões de opção.
- Validação obrigatória.
- Estado local da resposta.
- Textos localizados.

---

## 12. Impacto no Backend

- Receber e validar valor de objetivo.
- Salvar no UserProfile.
- Retornar perfil atualizado.

---

## 13. Impacto no Banco de Dados

Entidade: UserProfile.

Campo:

- goal.

Valores sugeridos:

- lose_weight;
- gain_strength;
- build_muscle;
- improve_conditioning;
- health_and_consistency.

---

## 14. Impacto em Gamificação

- Pode influenciar classe futura do Hunter.
- Pode influenciar atributos priorizados.
- Não concede XP no onboarding.

---

## 15. Impacto em Monetização

- Disponível para usuários com acesso ativo.
- Ajuda o usuário perceber valor no trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels dos objetivos. |
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
  "goal": "gain_strength"
}
```

Response conceitual:

```json
{
  "goal": "gain_strength",
  "currentStep": "experience_level"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| onboarding_step_completed | Quando objetivo é salvo. |

---

## 19. Critérios de aceite

### CA-001 — Objetivo selecionado

Dado que o usuário seleciona um objetivo,

Quando avançar,

Então a resposta deve ser salva.

### CA-002 — Sem seleção

Dado que nenhum objetivo foi selecionado,

Quando tentar avançar,

Então o app deve exibir validação.

---

## 20. Critérios de teste para QA

### Frontend (Flutter)

- selecionar cada objetivo e verificar estado selecionado;
- avançar sem selecionar e verificar mensagem de validação local;
- voltar etapa e verificar que seleção anterior permanece marcada;
- simular estado de carregamento (salvando);
- simular falha de conexão e verificar mensagem de erro;
- verificar textos em PT-BR, EN, ES e FR.

### Backend

- handler salva `goal` com cada valor do enum permitido;
- handler rejeita valor de `goal` fora do enum (retorna 400);
- validator rejeita `goal` nulo ou ausente;
- usuário sem acesso ativo recebe 403;
- usuário não autenticado recebe 401.

### API

- `PATCH /api/users/me/profile/onboarding` com `goal` válido retorna 200 e perfil atualizado;
- `PATCH` com `goal` inválido retorna 400 com mensagem localizada;
- `PATCH` sem autenticação retorna 401;
- `PATCH` com trial expirado retorna 403;
- `PATCH` idempotente: segunda chamada com mesmo valor não gera erro.

### E2E

- usuário com trial ativo seleciona objetivo e avança para próxima etapa;
- usuário tenta avançar sem seleção e permanece na tela com validação;
- usuário volta etapa, mantém seleção e avança novamente;
- fluxo completo em PT-BR, EN, ES e FR.

---

## ✅ Decisão registrada

> Objetivo principal é dado obrigatório do onboarding e deve orientar a geração da quest diária.
