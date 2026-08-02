---
title: US-024 — Informar nível de experiência
sidebar_position: 24
---

# US-024 — Informar nível de experiência

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-024 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile.experienceLevel |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **informar meu nível de experiência**,

para **não receber treinos difíceis ou fáceis demais**.

---

## 3. Contexto

A personalização real depende de entender o nível atual do usuário. Um iniciante precisa de variantes simples e menor volume; usuários mais experientes podem receber progressões mais exigentes.

---

## 4. Objetivo

Coletar o nível de experiência do usuário e usar essa informação para orientar dificuldade, volume e seleção de exercícios.

---

## 5. Escopo

### Entra nesta US

- Tela de seleção de nível.
- Opções simples e compreensíveis.
- Validação obrigatória.
- Salvamento no perfil.
- Uso futuro pela geração de quest.

### Fora desta US

- Teste físico real.
- Avaliação profissional.
- Diagnóstico de performance.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Nível de experiência é obrigatório. |
| RN-002 | O usuário deve escolher uma opção. |
| RN-003 | O nível deve influenciar dificuldade e volume dos treinos. |
| RN-004 | Iniciantes devem receber treino seguro e progressivo. |
| RN-005 | A resposta poderá ser editada depois, conforme US-034. |

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

1. Usuário chega à etapa de nível.
2. App exibe opções como iniciante, intermediário e avançado.
3. Usuário escolhe uma opção.
4. App salva a resposta.
5. App avança para próxima etapa.

---

## 9. Fluxos alternativos

### 9.1. Sem seleção

O app deve impedir avanço e pedir seleção de nível.

### 9.2. Usuário volta etapa

A opção escolhida deve permanecer marcada.

---

## 10. Estados esperados

- carregando;
- pronto para seleção;
- nível selecionado;
- erro de validação;
- salvando;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Tela de nível.
- Cards de opção.
- Validação de escolha.
- Persistência local temporária.
- Textos localizados.

---

## 12. Impacto no Backend

- Validar valor permitido.
- Salvar nível no perfil.
- Retornar perfil atualizado.

---

## 13. Impacto no Banco de Dados

Entidade: UserProfile.

Campo:

- experienceLevel.

Valores sugeridos:

- beginner;
- intermediate;
- advanced.

---

## 14. Impacto em Gamificação

- Pode influenciar dificuldade das quests.
- Pode influenciar progressão inicial.
- Não concede XP diretamente.

---

## 15. Impacto em Monetização

- Ajuda o trial demonstrar personalização real.
- Não altera regras de assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels dos níveis. |
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
  "experienceLevel": "beginner"
}
```

Response conceitual:

```json
{
  "experienceLevel": "beginner",
  "currentStep": "physical_data"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| onboarding_step_completed | Quando nível é salvo. |

---

## 19. Critérios de aceite

### CA-001 — Nível salvo

Dado que o usuário seleciona um nível,

Quando avançar,

Então o nível deve ser salvo no perfil.

### CA-002 — Validação obrigatória

Dado que nenhum nível foi selecionado,

Quando tentar avançar,

Então o app deve exibir validação.

---

## 20. Critérios de teste para QA

### Frontend (Flutter)

- selecionar cada nível e verificar estado selecionado;
- avançar sem selecionar e verificar mensagem de validação local;
- voltar etapa e verificar que seleção anterior permanece marcada;
- simular estado de carregamento (salvando);
- simular falha de conexão e verificar mensagem de erro;
- verificar textos em PT-BR, EN, ES e FR.

### Backend

- handler salva `experienceLevel` com cada valor do enum permitido;
- handler rejeita valor de `experienceLevel` fora do enum (retorna 400);
- validator rejeita `experienceLevel` nulo ou ausente;
- usuário sem acesso ativo recebe 403;
- usuário não autenticado recebe 401.

### API

- `PATCH /api/users/me/profile/onboarding` com `experienceLevel` válido retorna 200 e perfil atualizado;
- `PATCH` com `experienceLevel` inválido retorna 400 com mensagem localizada;
- `PATCH` sem autenticação retorna 401;
- `PATCH` com trial expirado retorna 403;
- `PATCH` idempotente: segunda chamada com mesmo valor não gera erro.

### E2E

- usuário com trial ativo seleciona nível e avança para próxima etapa;
- usuário tenta avançar sem seleção e permanece na tela com validação;
- usuário volta etapa, mantém seleção e avança novamente;
- fluxo completo em PT-BR, EN, ES e FR.

---

## ✅ Decisão registrada

> Nível de experiência é obrigatório para evitar treinos incompatíveis e garantir personalização real desde a primeira quest.
