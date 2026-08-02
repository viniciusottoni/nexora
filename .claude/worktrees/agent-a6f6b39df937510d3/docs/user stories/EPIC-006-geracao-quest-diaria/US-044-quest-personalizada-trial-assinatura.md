---
title: US-044 — Receber quest personalizada durante trial ou assinatura
sidebar_position: 44
---

# US-044 — Receber quest personalizada durante trial ou assinatura

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-044 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile, Subscription |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário em trial ou assinante**,

quero **receber quests personalizadas**,

para **perceber valor real antes e depois de assinar**.

---

## 3. Contexto

A personalização precisa ser perceptível desde o trial: o treino reflete objetivo, nível, tempo, limitações e dores do usuário, e não um plano genérico. Isso sustenta a conversão e a retenção.

---

## 4. Objetivo

Garantir que a quest gerada seja personalizada pelo perfil em todos os planos com acesso ativo.

---

## 5. Escopo

### Entra nesta US

- Personalização por objetivo, nível efetivo, tempo, limitações e dores.
- Mesma qualidade de personalização em trial e em assinatura.
- Mensagem clara de que o treino é personalizado.

### Fora desta US

- Detalhe do filtro/pontuação (US-045/151).
- Bloqueio por acesso expirado (US-043).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A personalização deve ser idêntica em trial e assinatura ativa. |
| RN-002 | A quest deve refletir o perfil do usuário, não um template genérico (exceto em fallback). |
| RN-003 | A personalização não pode violar segurança, limitações ou dores. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não recebe. |
| Usuário em Trial | Recebe quest personalizada. |
| Premium Mensal | Recebe quest personalizada. |
| Premium Anual | Recebe quest personalizada. |
| Trial/Assinatura expirados | Não recebem (US-043). |

---

## 8. Fluxo principal

1. Usuário com acesso ativo solicita a quest.
2. Sistema gera quest personalizada pelo perfil.
3. App exibe a quest com indicação de personalização.

---

## 9. Fluxos alternativos

### 9.1. Fallback

Se a geração principal falhar, usar template compatível (US-046) mantendo a maior personalização possível.

---

## 10. Estados esperados

- carregando;
- quest personalizada pronta;
- fallback aplicado;
- bloqueado por acesso.

---

## 11. Impacto no Frontend Flutter

- Exibição da quest com selo/indicação de personalização.

---

## 12. Impacto no Backend

- Geração baseada no perfil para todos os planos ativos.

---

## 13. Impacto no Banco de Dados

Entidades: `Quest`, `UserProfile`, `Subscription`.

---

## 14. Impacto em Gamificação

- Quest personalizada concede XP/atributos ao concluir (EPIC-009).

---

## 15. Impacto em Monetização

- Valor percebido no trial é o principal driver de conversão.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens da quest. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/quests/daily/today
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| daily_quest_generated | Quando a quest personalizada é gerada. |

---

## 19. Critérios de aceite

### CA-001 — Personalização no trial

Dado um usuário em trial com perfil salvo,

Quando solicitar a quest,

Então deve receber um treino personalizado pelo perfil.

### CA-002 — Paridade trial/assinatura

Dado o mesmo perfil em trial e em assinatura,

Quando gerar a quest,

Então a personalização deve ser equivalente.

---

## 20. Critérios de teste para QA

### Backend

- personalização aplicada a trial e assinatura;
- fallback mantém o máximo de personalização;
- segurança/limitações respeitadas.

### E2E

- usuário em trial percebe personalização;
- assinante recebe a mesma qualidade.

---

## ✅ Decisão registrada

> A personalização é igual em trial e assinatura ativa e sempre respeita a segurança; é o principal motor de valor percebido.
