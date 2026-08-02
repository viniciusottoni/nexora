---
title: US-140 — Informar há quanto tempo treina
sidebar_position: 140
---

# US-140 — Informar há quanto tempo treina

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-140 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile.trainingDuration |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **informar há quanto tempo treino**,

para **o AWAKEN ajustar melhor minha progressão inicial e evitar treinos incompatíveis**.

---

## 3. Contexto

O nível declarado mostra percepção atual do usuário, mas o tempo de prática ajuda a calibrar histórico real de treino. Essa etapa faz parte do fluxo fixo 3/8 aprovado nos wireframes.

---

## 4. Objetivo

Coletar o tempo de treino do usuário e salvar no perfil para apoiar a geração de quest e progressão inicial.

---

## 5. Escopo

### Entra nesta US

- Tela 3/8 do onboarding.
- Seleção única do tempo de treino.
- Opções: não treino, menos de 1 mês, 1 a 6 meses, 6 a 12 meses, mais de 1 ano, mais de 3 anos.
- Validação obrigatória.
- Salvamento no perfil.

### Fora desta US

- Teste físico prático.
- Histórico detalhado por modalidade.
- Avaliação profissional.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Tempo de treino é obrigatório. |
| RN-002 | O valor deve estar entre as opções predefinidas. |
| RN-003 | O dado deve complementar o nível de experiência. |
| RN-004 | O dado deve influenciar dificuldade e progressão inicial quando aplicável. |

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

1. Usuário chega à etapa 3/8.
2. App exibe opções de tempo de treino.
3. Usuário seleciona uma opção.
4. App salva a resposta.
5. App avança para etapa 4/8.

---

## 9. Fluxos alternativos

### 9.1. Sem seleção

O app deve impedir avanço e exibir validação.

### 9.2. Usuário volta etapa

A seleção anterior deve permanecer marcada.

---

## 10. Estados esperados

- pronto para seleção;
- opção selecionada;
- erro de validação;
- salvando;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Tela 3/8.
- Cards de seleção única.
- Barra de progresso.
- Validação local.
- Textos localizados.

---

## 12. Impacto no Backend

- Validar valor permitido.
- Salvar em UserProfile.
- Retornar 204 sem corpo; cliente re-consulta perfil se necessário.

---

## 13. Impacto no Banco de Dados

Entidade: UserProfile.

Campo:

- trainingDuration.

---

## 14. Impacto em Gamificação

- Pode calibrar dificuldade inicial da quest.
- Não concede XP diretamente.

---

## 15. Impacto em Monetização

- Ajuda o trial entregar personalização mais convincente.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Opções de tempo de treino. |
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
  "trainingDuration": "1_6_months"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| onboarding_step_completed | Quando etapa 3/8 é salva. |

---

## 19. Critérios de aceite

### CA-001 — Tempo salvo

Dado que o usuário seleciona tempo de treino,

Quando avançar,

Então o dado deve ser salvo no perfil.

### CA-002 — Validação obrigatória

Dado que nenhuma opção foi selecionada,

Quando tentar avançar,

Então o app deve exibir validação.

---

## 20. Critérios de teste para QA

- selecionar cada opção;
- avançar sem seleção;
- voltar e manter resposta;
- falha de conexão;
- textos em PT-BR, EN, ES e FR.

---

## ✅ Decisão registrada

> Tempo de treino é uma entrada obrigatória da etapa 3/8 e complementa o nível de experiência para personalizar a primeira quest.
