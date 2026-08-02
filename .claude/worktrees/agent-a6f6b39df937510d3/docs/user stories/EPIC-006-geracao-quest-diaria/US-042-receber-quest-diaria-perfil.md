---
title: US-042 — Receber quest diária baseada no perfil
sidebar_position: 42
---

# US-042 — Receber quest diária baseada no perfil

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-042 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile, ExerciseCatalog |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **receber uma quest diária baseada no meu perfil**,

para **saber exatamente o que treinar hoje, de forma segura e compatível**.

---

## 3. Contexto

A quest diária é o core do loop de retenção. Ela combina objetivo, nível efetivo, tempo disponível, limitações e dores em um treino do dia, montado a partir do catálogo aprovado e dentro do orçamento de tempo.

---

## 4. Objetivo

Gerar e entregar uma quest diária (`type = daily`) compatível com o perfil, respeitando o tempo disponível e a prioridade de segurança.

---

## 5. Escopo

### Entra nesta US

- Geração da quest diária a partir do perfil.
- Respeito ao orçamento de tempo (aquecimento, execução, descanso, finalização).
- Composição de exercícios aprovados via filtro + pontuação (US-045/151).
- Exibição do treino do dia antes da edição.

### Fora desta US

- Cálculo do nível efetivo (US-150).
- Filtro eliminatório (US-045) e pontuação (US-151) em detalhe.
- Prescrição numérica por perfil (US-153).
- Edição da quest (EPIC-007).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A quest diária deve respeitar objetivo, nível efetivo, equipamentos, tempo, limitações e dores. |
| RN-002 | A duração estimada deve caber no tempo disponível, incluindo descanso e trocas. |
| RN-003 | Um treino de 10 minutos é uma micro quest com poucos exercícios. |
| RN-004 | Segurança tem prioridade sobre objetivo e recompensa. |
| RN-005 | Apenas exercícios aprovados podem compor a quest. |
| RN-006 | O Rank pode influenciar a dificuldade sugerida e elementos cosméticos da quest, mas nunca supera segurança, dores, limitações, tempo, equipamento ou nível efetivo (EPIC-009). |
| RN-007 | A quest diária gerada deve ser apresentada primeiro como notificação de sistema (exercícios + aviso de penalidade), e só passar a constar na lista de quests após confirmação do usuário (botão OK). |
| RN-008 | A notificação de quest gerada deve ser exibida no máximo uma vez por quest/dia/usuário. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não gera quest. |
| Usuário em Trial | Gera quest. |
| Premium Mensal | Gera quest. |
| Premium Anual | Gera quest. |
| Trial expirado | Não gera (US-043). |
| Assinatura expirada | Não gera (US-043). |

---

## 8. Fluxo principal

1. Usuário com acesso ativo faz login e entra na Home.
2. Sistema resolve nível efetivo e aplica filtro + pontuação.
3. Sistema seleciona exercícios dentro do orçamento de tempo e gera a quest diária.
4. App exibe a quest gerada como **notificação de sistema** (mesmo padrão visual e sonoro do popup de confirmação de "tornar-se jogador" no fim do onboarding — `AwakenSystemNotificationPage`), listando os exercícios da quest e o aviso de penalidade por não conclusão (US-129).
5. Usuário confirma no botão de OK da notificação.
6. Somente após a confirmação, a quest passa a constar na lista de quests do usuário.

---

## 8.1. Notificação de quest gerada

- Reusa o componente `AwakenSystemNotificationPage` (visual, animações e som `system-popup.mp3`) já usado na confirmação final do onboarding — mesma identidade visual/sonora, conteúdo diferente.
- Conteúdo da notificação: título da quest diária, lista de exercícios (nome + meta, ex.: `Push-ups [100]`), e aviso de penalidade (texto de alerta, cor de destaque) caso a quest não seja concluída no dia (RN-007).
- A notificação é exibida uma única vez por geração de quest (não reaparece ao navegar entre telas no mesmo dia).
- A quest só é exibida na lista de quests (tela/aba de quests) após o usuário confirmar (botão OK) na notificação. Antes disso, a quest já existe no backend (persistida — US-047), mas fica com um marcador de "não confirmada/não vista" no app.

---

## 9. Fluxos alternativos

### 9.1. Falha na geração principal

O sistema usa fallback por templates (US-046).

### 9.2. Catálogo insuficiente

O sistema amplia critérios com segurança (regressões) e registra para curadoria.

---

## 10. Estados esperados

- carregando geração;
- quest pronta, aguardando confirmação na notificação;
- quest confirmada (visível na lista de quests);
- erro com fallback;
- bloqueado por acesso expirado.

---

## 11. Impacto no Frontend Flutter

- Tela de quest do dia.
- Estados de carregamento, erro e bloqueio.
- Exibição de exercícios, séries, reps e descanso.
- Notificação de quest gerada na Home (reuso de `AwakenSystemNotificationPage`, visual e som `system-popup.mp3`), com lista de exercícios e aviso de penalidade.
- Botão OK da notificação confirma a quest e libera sua exibição na lista de quests.
- Controle de exibição única da notificação por quest/dia (flag local/local cache + estado no backend).

---

## 12. Impacto no Backend

- Serviço de geração da quest diária.
- Orquestração: nível efetivo → filtro → pontuação → seleção → prescrição.

---

## 13. Impacto no Banco de Dados

Entidades: `Quest`, `QuestExercise`.

Campos relevantes: `type=daily`, `userId`, `date`, `estimatedDurationMinutes`, lista de `QuestExercise`.

---

## 14. Impacto em Gamificação

- Concluir a quest concede XP e atributos (EPIC-008/009).

---

## 15. Impacto em Monetização

- Quest diária funcional é o principal motor de valor do trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Títulos e textos da quest. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/daily/generate
GET  /api/quests/daily/today
```

Response conceitual:

```json
{
  "questId": "qst_001",
  "type": "daily",
  "estimatedDurationMinutes": 30,
  "exercises": [ { "exerciseId": "exr_001", "sets": 3, "reps": 12 } ]
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| daily_quest_generated | Quando a quest diária é gerada. |
| daily_quest_notification_shown | Quando a notificação de quest gerada é exibida. |
| daily_quest_confirmed | Quando o usuário confirma (OK) a notificação e a quest entra na lista. |
| quest_viewed | Quando o usuário visualiza a quest. |

---

## 19. Critérios de aceite

### CA-001 — Quest compatível

Dado um usuário com acesso ativo e perfil salvo,

Quando solicitar a quest do dia,

Então deve receber um treino compatível dentro do tempo disponível.

### CA-002 — Micro quest

Dado que o usuário tem 10 minutos,

Quando a quest for gerada,

Então deve montar uma micro quest compatível com o tempo total.

### CA-003 — Notificação antes da lista

Dado que a quest diária foi gerada,

Quando o usuário entrar na Home,

Então deve ver a notificação de sistema com a lista de exercícios e o aviso de penalidade, e a quest não deve aparecer na lista de quests antes da confirmação.

### CA-004 — Confirmação libera a quest

Dado que a notificação de quest gerada está visível,

Quando o usuário tocar no botão OK,

Então a notificação deve fechar e a quest deve passar a constar na lista de quests.

---

## 20. Critérios de teste para QA

### Backend

- geração respeita objetivo, nível efetivo, tempo, limitações e dores;
- duração estimada cabe no tempo disponível;
- apenas exercícios aprovados entram.

### E2E

- usuário com trial ativo recebe a quest do dia;
- 10 min gera micro quest;
- falha aciona fallback;
- login → Home exibe notificação de sistema com exercícios e penalidade antes de qualquer outra coisa;
- quest não aparece na lista de quests antes do OK;
- quest aparece na lista de quests após o OK;
- notificação não reaparece ao navegar/reabrir a Home no mesmo dia.

---

## ✅ Decisão registrada

> A quest diária é montada a partir do perfil, dentro do orçamento de tempo e com prioridade de segurança, usando apenas exercícios aprovados.
>
> A quest gerada é sempre apresentada primeiro como notificação de sistema (mesmo padrão visual/sonoro do popup final do onboarding), com a lista de exercícios e o aviso de penalidade, e só passa a constar na lista de quests após confirmação do usuário.
