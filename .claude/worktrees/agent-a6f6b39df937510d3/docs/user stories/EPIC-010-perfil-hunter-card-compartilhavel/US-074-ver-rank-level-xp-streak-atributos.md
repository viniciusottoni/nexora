---
title: US-074 — Ver rank, level, XP, streak e atributos
sidebar_position: 74
---

# US-074 — Ver rank, level, XP, streak e atributos

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-074 |
| Épico | EPIC-010 — Perfil do Hunter e Card Compartilhável |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados |PT-BR, EN, ES, FR |
| Dependência principal | HunterProgress e HunterAttributes |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **ver meu rank, level, XP, streak e os 6 atributos com seus respectivos levels**,

para **entender minha evolução de forma clara e motivadora**.

---

## 3. Contexto

A força do AWAKEN está em transformar treino em progressão. O usuário precisa ver seu avanço de forma simples e visual, sem depender apenas de números brutos.

---

## 4. Objetivo

Exibir os principais indicadores de gamificação no Perfil Hunter: rank, level, XP, streak e atributos.

---

## 5. Escopo

### Entra nesta US

- Exibir rank atual e o nome narrativo opcional (ex.: Caçador, Elite).
- Exibir o progresso de RankScore até o próximo Rank.
- Exibir level atual.
- Exibir XP atual e XP para próximo level.
- Exibir streak atual.
- Exibir 6 atributos: Força, Agilidade, Resistência, Vitalidade, Foco e Sabedoria.
- Exibir level de cada atributo.

### Fora desta US

- Cálculo dos atributos.
- Histórico detalhado de XP.
- Ranking social.
- Badges avançados.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Todos os valores exibidos devem vir do progresso real do usuário. |
| RN-002 | XP deve mostrar progresso para o próximo level. |
| RN-003 | Streak deve refletir a regra oficial do EPIC-009. |
| RN-004 | Os 6 atributos devem ser exibidos com seus levels. |
| RN-005 | Acesso expirado não deve exibir perfil completo. |
| RN-006 | O Rank deve mostrar o progresso de RankScore até o próximo Rank (EPIC-009). |
| RN-007 | O nome narrativo é opcional e não substitui o Rank principal (E→SSS). |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode visualizar. |
| Usuário em Trial | Pode visualizar dados funcionais. |
| Premium Mensal | Pode visualizar completo. |
| Premium Anual | Pode visualizar completo. |
| Trial expirado | Visualiza estado limitado. |
| Assinatura expirada | Visualiza estado limitado. |

---

## 8. Fluxo principal

1. Usuário acessa Perfil Hunter.
2. App carrega dados de progresso.
3. App exibe rank, level, XP e streak.
4. App exibe os 6 atributos com level.
5. Usuário entende seu estágio atual de evolução.

---

## 9. Fluxos alternativos

### 9.1. Usuário sem progresso

Exibir valores iniciais e mensagem motivacional para começar a primeira quest.

### 9.2. Falha ao carregar dados

Exibir erro controlado e ação de tentar novamente.

---

## 10. Estados esperados

- carregando;
- progresso carregado;
- progresso inicial;
- acesso limitado;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Componentes de rank, XP bar, level e streak.
- Lista/cards dos 6 atributos.
- Animações leves de progresso, se performático.
- Textos localizados.

---

## 12. Impacto no Backend

- Endpoint de progresso agregado.
- Retorno dos 6 atributos.
- Validação de acesso.

---

## 13. Impacto no Banco de Dados

Entidades:

- HunterProgress;
- HunterAttributes;
- Subscription.

Campos principais:

- rank;
- level;
- xp;
- xpToNextLevel;
- streakDays;
- strengthLevel;
- agilityLevel;
- enduranceLevel;
- vitalityLevel;
- focusLevel;
- wisdomLevel.

---

## 14. Impacto em Gamificação

- Exibe a progressão central do produto.
- Aumenta motivação e retenção.
- Não altera valores por si só.

---

## 15. Impacto em Monetização

- Trial vê valor do sistema.
- Assinante recebe visual completo.
- Expirado vê bloqueio e CTA.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels de atributos e progresso. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/hunter/progress
```

Response conceitual:

```json
{
  "rank": "E",
  "rankNarrativeName": "Desperto",
  "rankScore": 14,
  "rankScoreToNext": 18,
  "level": 3,
  "xp": 240,
  "xpToNextLevel": 500,
  "streakDays": 4,
  "attributes": {
    "strength": 2,
    "agility": 1,
    "endurance": 3,
    "vitality": 2,
    "focus": 1,
    "wisdom": 1
  }
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| hunter_progress_viewed | Quando bloco de progresso é exibido. |

---

## 19. Critérios de aceite

### CA-001 — Indicadores exibidos

Dado que o usuário possui acesso ativo,

Quando acessar o perfil,

Então deve ver rank, level, XP, streak e os 6 atributos.

### CA-002 — Dados reais

Dado que os dados foram carregados,

Quando exibidos,

Então devem corresponder ao progresso salvo.

---

## 20. Critérios de teste para QA

- progresso inicial;
- progresso intermediário;
- streak zero;
- todos os atributos visíveis;
- acesso expirado;
- erro de conexão;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Rank, level, XP, streak e os 6 atributos são os indicadores centrais do Perfil Hunter e devem refletir sempre o progresso real do usuário.
