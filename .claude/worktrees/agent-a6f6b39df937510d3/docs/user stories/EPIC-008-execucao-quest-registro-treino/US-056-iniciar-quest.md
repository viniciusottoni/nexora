---
title: US-056 — Iniciar quest
sidebar_position: 56
---

# US-056 — Iniciar quest

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-056 |
| Épico | EPIC-008 — Execução da Quest e Registro do Treino |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Quest gerada e acesso ativo |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **iniciar uma quest gerada**,

para **começar meu treino real dentro do AWAKEN**.

---

## 3. Contexto

A execução é o momento em que a quest vira treino. O usuário deve iniciar uma quest diária, dungeon ou raid de forma simples, clara e sem distrações.

---

## 4. Objetivo

Permitir iniciar uma quest válida e registrar seu estado como em andamento, garantindo acesso ativo e evitando duplicidade.

---

## 5. Escopo

### Entra nesta US

- Iniciar quest diária.
- Iniciar dungeon.
- Iniciar raid.
- Validar acesso ativo.
- Validar que a quest está pronta para execução.
- Registrar início da quest.
- Abrir tela de execução.

### Fora desta US

- Concluir quest.
- Cancelar quest.
- Cálculo final de recompensa.
- Integração com wearables.
- Sensor automático de execução.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Apenas usuário com trial ou assinatura ativa pode iniciar quest. |
| RN-002 | Quest só pode iniciar se estiver no estado gerada/pronta. |
| RN-003 | Quest já iniciada não deve ser iniciada novamente. |
| RN-004 | Quest deve possuir `type`: `daily`, `dungeon` ou `raid`. |
| RN-005 | Ao iniciar, registrar `startedAt`. |
| RN-006 | Usuário com acesso expirado deve ser bloqueado e direcionado ao paywall. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode iniciar. |
| Usuário em Trial | Pode iniciar com trial ativo. |
| Premium Mensal | Pode iniciar com assinatura ativa. |
| Premium Anual | Pode iniciar com assinatura ativa. |
| Trial expirado | Não pode iniciar. |
| Assinatura expirada | Não pode iniciar. |

---

## 8. Fluxo principal

1. Usuário visualiza a quest pronta.
2. Usuário toca em iniciar.
3. App envia solicitação de início.
4. Backend valida acesso e estado da quest.
5. Backend atualiza quest para em andamento.
6. App abre tela de execução no primeiro exercício.

---

## 9. Fluxos alternativos

### 9.1. Quest já iniciada

O sistema deve abrir a execução em andamento, sem criar novo início.

### 9.2. Acesso expirado

O sistema deve bloquear início e exibir CTA de assinatura.

### 9.3. Quest inválida

O sistema deve exibir erro controlado e não iniciar.

---

## 10. Estados esperados

- pronta para iniciar;
- iniciando;
- em andamento;
- já iniciada;
- acesso bloqueado;
- erro de início.

---

## 11. Impacto no Frontend Flutter

- CTA “Iniciar quest”.
- Estado de loading ao iniciar.
- Navegação para tela de execução.
- Bloqueio visual para acesso expirado.
- Textos localizados.

---

## 12. Impacto no Backend

- Endpoint para iniciar quest.
- Validação de acesso.
- Validação de estado da quest.
- Persistência de `startedAt` e status `in_progress`.

---

## 13. Impacto no Banco de Dados

Entidades:

- Quest;
- QuestExercise;
- Subscription.

Campos relevantes:

- Quest.type;
- Quest.status;
- Quest.startedAt.

---

## 14. Impacto em Gamificação

- Iniciar quest não concede XP.
- XP e atributos são concedidos por exercício concluído e/ou conclusão final conforme regras do EPIC-009.

---

## 15. Impacto em Monetização

- Execução é recurso protegido por trial/assinatura ativa.
- Acesso expirado deve ir para paywall.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos de início e bloqueio. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/{questId}/start
```

Response conceitual:

```json
{
  "questId": "uuid",
  "questType": "daily",
  "status": "in_progress",
  "startedAt": "2026-06-23T18:00:00Z"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| quest_started | Quando a quest inicia com sucesso. |
| access_blocked | Quando usuário sem acesso tenta iniciar. |

Propriedade obrigatória:

- `quest_type`: `daily`, `dungeon` ou `raid`.

---

## 19. Critérios de aceite

### CA-001 — Iniciar com acesso ativo

Dado que o usuário tem acesso ativo e quest pronta,

Quando tocar em iniciar,

Então a quest deve entrar em andamento.

### CA-002 — Bloqueio sem acesso

Dado que o acesso expirou,

Quando tentar iniciar,

Então a ação deve ser bloqueada.

---

## 20. Critérios de teste para QA

- iniciar daily;
- iniciar dungeon;
- iniciar raid;
- tentar iniciar quest já iniciada;
- tentar iniciar com acesso expirado;
- validar evento `quest_started`;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Iniciar quest apenas muda o estado para execução; recompensas só são concedidas conforme progresso e conclusão válidos.
