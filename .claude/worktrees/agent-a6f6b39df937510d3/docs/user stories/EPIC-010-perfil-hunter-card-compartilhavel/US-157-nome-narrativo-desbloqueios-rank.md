---
title: US-157 — Exibir nome narrativo e desbloqueios por Rank
sidebar_position: 157
---

# US-157 — Exibir nome narrativo e desbloqueios por Rank

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-157 |
| Épico | EPIC-010 — Perfil do Hunter e Card Compartilhável |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterProgress (Rank) |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **ver o nome narrativo do meu Rank e os desbloqueios correspondentes**,

para **sentir o tom anime/RPG e ser recompensado visualmente pela progressão**.

---

## 3. Contexto

Os Ranks têm nomes narrativos opcionais (Desperto, Aprendiz, Caçador, Elite, Ascendente, Despertado, Monarca, Lenda Viva) e desbloqueios cosméticos por Rank. No MVP, os desbloqueios são visuais (emblemas, efeitos, nome narrativo); desbloqueios ligados a Master Quests, card animado e eventos avançados são Pós-MVP.

---

## 4. Objetivo

Exibir o nome narrativo do Rank atual e os desbloqueios cosméticos disponíveis para aquele Rank, mantendo o Rank principal (E→SSS) sempre claro.

---

## 5. Escopo

### Entra nesta US

- Mapa de nomes narrativos por Rank.
- Exibição do nome narrativo junto ao Rank principal.
- Desbloqueios cosméticos por Rank (MVP: emblemas/efeitos visuais, nome narrativo).
- Indicação do que será desbloqueado no próximo Rank.

### Fora desta US

- Master Quests, card animado e eventos avançados (Pós-MVP).
- Cálculo do Rank (EPIC-009).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O nome narrativo é opcional e não substitui o Rank principal (E→SSS). |
| RN-002 | Desbloqueios por Rank são cosméticos e não alteram a segurança do treino. |
| RN-003 | Desbloqueios não podem expor dados sensíveis. |
| RN-004 | Desbloqueios ligados a Master Quests/eventos são Pós-MVP. |
| RN-005 | O mapa narrativo segue: E Desperto, D Aprendiz, C Caçador, B Elite, A Ascendente, S Despertado, SS Monarca, SSS Lenda Viva. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Vê nome narrativo e desbloqueios do seu Rank. |
| Premium Mensal/Anual | Vê nome narrativo e desbloqueios; cosméticos premium como P1. |
| Acesso expirado | Estado limitado. |

---

## 8. Fluxo principal

1. Usuário acessa o perfil.
2. App lê o Rank atual.
3. Exibe o nome narrativo e os desbloqueios daquele Rank.
4. Indica o próximo desbloqueio.

---

## 9. Fluxos alternativos

### 9.1. Rank inicial

Exibe os desbloqueios do Rank de partida (E–B).

### 9.2. Rank up

Ao subir de Rank, novos desbloqueios e nome narrativo passam a ser exibidos.

---

## 10. Estados esperados

- nome narrativo e desbloqueios exibidos;
- próximo desbloqueio indicado;
- estado limitado.

---

## 11. Impacto no Frontend Flutter

- Componente de nome narrativo e lista de desbloqueios.
- Indicação de próximo desbloqueio por Rank.

---

## 12. Impacto no Backend

- Mapa de nomes narrativos e desbloqueios por Rank.
- Retorno do Rank atual e desbloqueios.

---

## 13. Impacto no Banco de Dados

Entidade: `HunterProgress`.

Campos: `rank` (mapeado para nome narrativo e desbloqueios em configuração).

---

## 14. Impacto em Gamificação

- Reforça identidade e recompensa visual por Rank.

---

## 15. Impacto em Monetização

- Cosméticos por Rank aumentam engajamento; premium como P1.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Nomes narrativos e rótulos de desbloqueio. |
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
  "rank": "C",
  "rankNarrativeName": "Caçador",
  "unlocks": ["variacoes_treino", "master_quests_pequenas"],
  "nextRankUnlocks": ["metas_semanais", "emblemas_melhores"]
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| hunter_profile_viewed | Quando o perfil exibe nome narrativo/desbloqueios. |
| rank_changed | Quando novos desbloqueios passam a valer. |

---

## 19. Critérios de aceite

### CA-001 — Nome narrativo

Dado um usuário em Rank C,

Quando acessar o perfil,

Então deve ver o Rank C e o nome narrativo "Caçador".

### CA-002 — Desbloqueios cosméticos

Dado um usuário em determinado Rank,

Quando o perfil for exibido,

Então deve mostrar os desbloqueios cosméticos daquele Rank, sem expor dados sensíveis.

---

## 20. Critérios de teste para QA

### Frontend (Flutter)

- nome narrativo correto por Rank;
- desbloqueios exibidos por Rank;
- próximo desbloqueio indicado;
- Rank principal sempre visível;
- textos em PT-BR, EN, ES.

---

## ✅ Decisão registrada

> O perfil exibe o nome narrativo opcional e os desbloqueios cosméticos por Rank, mantendo o Rank principal (E→SSS) claro; desbloqueios de Master Quests/eventos são Pós-MVP.
