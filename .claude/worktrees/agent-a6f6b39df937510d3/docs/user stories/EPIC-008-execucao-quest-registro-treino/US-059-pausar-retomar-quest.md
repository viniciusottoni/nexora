---
title: US-059 — Pausar e retomar quest
sidebar_position: 59
---

# US-059 — Pausar e retomar quest

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-059 |
| Épico | EPIC-008 — Execução da Quest e Registro do Treino |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Quest.status |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário executando uma quest**,

quero **pausar e retomar meu treino**,

para **lidar com interrupções sem perder o progresso já registrado**.

---

## 3. Contexto

Pausar e retomar é útil, mas não é obrigatório para o MVP inicial. Por isso, a funcionalidade é P1 e deve ser simples, sem cronômetro avançado.

---

## 4. Objetivo

Permitir pausar uma quest em andamento e retomá-la mantendo exercícios já concluídos.

---

## 5. Escopo

### Entra nesta US

- Pausar quest em andamento.
- Retomar quest pausada.
- Preservar exercícios concluídos.
- Exibir estado pausado.
- Suportar daily, dungeon e raid.

### Fora desta US

- Cronômetro avançado.
- Pausa automática por sensor.
- Notificações complexas de retomada.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Apenas quest em andamento pode ser pausada. |
| RN-002 | Apenas quest pausada pode ser retomada. |
| RN-003 | Pausar não concede XP. |
| RN-004 | Retomar não concede XP. |
| RN-005 | Progresso já registrado deve ser preservado. |
| RN-006 | Quest concluída ou cancelada não pode ser pausada. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode pausar/retomar se feature habilitada. |
| Premium Mensal | Pode pausar/retomar se feature habilitada. |
| Premium Anual | Pode pausar/retomar se feature habilitada. |
| Trial expirado | Não inicia novas execuções; retomada segue regra vigente. |
| Assinatura expirada | Não inicia novas execuções; retomada segue regra vigente. |
| Visitante | Não pode pausar/retomar. |

---

## 8. Fluxo principal

1. Usuário está em uma quest em andamento.
2. Toca em pausar.
3. Sistema muda status para pausada.
4. Usuário retorna depois.
5. Toca em retomar.
6. Sistema volta status para em andamento.

---

## 9. Fluxos alternativos

### 9.1. Feature desabilitada

App não exibe ação de pausar.

### 9.2. Quest já encerrada

Backend rejeita pausa ou retomada.

---

## 10. Estados esperados

- em andamento;
- pausando;
- pausada;
- retomando;
- retomada;
- erro.

---

## 11. Impacto no Frontend Flutter

- Ação de pausar.
- Estado visual pausado.
- CTA de retomar.
- Preservação do progresso visual.

---

## 12. Impacto no Backend

- Endpoint para pausar.
- Endpoint para retomar.
- Validação de status.
- Persistência de timestamps opcionais.

---

## 13. Impacto no Banco de Dados

Entidade: Quest.

Campos possíveis:

- status;
- pausedAt;
- resumedAt;
- totalPausedSeconds, se necessário futuramente.

---

## 14. Impacto em Gamificação

- Pausar/retomar não concede XP.
- Não deve quebrar streak por si só.
- Recompensa depende de conclusão válida.

---

## 15. Impacto em Monetização

- P1 para melhorar experiência de usuários ativos.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos de pausar/retomar. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/{questId}/pause
POST /api/quests/{questId}/resume
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| quest_paused | Quando quest é pausada. |
| quest_resumed | Quando quest é retomada. |

---

## 19. Critérios de aceite

### CA-001 — Pausar quest

Dado que a quest está em andamento,

Quando usuário pausar,

Então o status deve virar pausada.

### CA-002 — Retomar quest

Dado que a quest está pausada,

Quando usuário retomar,

Então o status deve voltar para em andamento.

---

## 20. Critérios de teste para QA

- pausar daily;
- retomar daily;
- pausar dungeon;
- tentar pausar quest concluída;
- progresso preservado;
- feature desabilitada;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Pausar e retomar é P1, deve preservar progresso e não pode conceder recompensa por si só.
