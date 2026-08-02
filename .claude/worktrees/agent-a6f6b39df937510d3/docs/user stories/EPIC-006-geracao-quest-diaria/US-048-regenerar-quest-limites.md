---
title: US-048 — Regenerar quest dentro de limites
sidebar_position: 48
---

# US-048 — Regenerar quest dentro de limites

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-048 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Quest |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **regenerar a quest dentro de limites justos**,

para **ajustar um treino ruim sem abusar do sistema**.

---

## 3. Contexto

Às vezes o treino gerado não agrada (tempo, foco, exercícios). A regeneração permite uma nova tentativa, mas com limite diário para preservar consistência e evitar "rolar" infinitamente até burlar a personalização.

---

## 4. Objetivo

Permitir regenerar a quest diária respeitando um limite e mantendo a personalização e a segurança.

---

## 5. Escopo

### Entra nesta US

- Regeneração da quest diária com limite por dia.
- Manutenção do perfil, segurança e tempo na nova geração.
- Registro do motivo de regeneração (US-049).

### Fora desta US

- Edição manual de exercícios (EPIC-007).
- Geração de dungeon (US-128).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A regeneração tem limite diário definido (1 regeneração gratuita por dia). |
| RN-002 | A nova quest mantém personalização, segurança e tempo. |
| RN-003 | Atingido o limite, novas regenerações são bloqueadas no dia, exceto via consumo do item "Pergaminho da Reforja" (RN-005). |
| RN-004 | A regeneração não pode contornar limitações/dores. |
| RN-005 | Atingido o limite, o app verifica o inventário do usuário pelo item "Pergaminho da Reforja". Se houver ao menos 1 unidade, o app pede confirmação explícita antes de consumi-la para regenerar além do limite. Se não houver, o app oferece ir à loja, onde o item chega destacado para compra. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Regenera dentro do limite. |
| Premium Mensal/Anual | Regenera dentro do limite. |
| Acesso expirado | Não gera (US-043). |

---

## 8. Fluxo principal

1. Usuário solicita regenerar a quest.
2. Sistema verifica o limite diário.
3. Se permitido, gera nova quest mantendo perfil e segurança.
4. Persiste a nova quest do dia.

---

## 9. Fluxos alternativos

### 9.1. Limite atingido

App informa que o limite foi atingido e mantém a quest atual, a menos que o usuário opte por usar o "Pergaminho da Reforja" (9.2).

### 9.2. Limite atingido com "Pergaminho da Reforja" disponível

Ao tentar regenerar após o limite, o app verifica o inventário do usuário pelo item "Pergaminho da Reforja". Se houver ao menos 1 unidade, o app pergunta explicitamente se o usuário quer consumir 1 unidade para regenerar mesmo assim. Se confirmado, o backend consome o item e entrega a nova quest; se cancelado, a quest atual é mantida e nada é consumido.

### 9.3. Limite atingido sem "Pergaminho da Reforja" disponível

Se o usuário não possuir o item, o app pergunta se ele quer ir à loja para comprá-lo. Se aceitar, o app navega para a loja com o "Pergaminho da Reforja" destacado para compra; se recusar, a quest atual é mantida.

---

## 10. Estados esperados

- regenerando;
- nova quest pronta;
- limite atingido (bloqueado);
- limite atingido, aguardando confirmação para consumir o "Pergaminho da Reforja";
- limite atingido, redirecionando à loja para comprar o item.

---

## 11. Impacto no Frontend Flutter

- Ação de regenerar com contador de tentativas.
- Mensagem ao atingir o limite.
- Diálogo de confirmação explícita antes de consumir o "Pergaminho da Reforja" (nunca consome sem confirmação do usuário).
- Diálogo oferecendo ir à loja quando o item não está disponível, com o item destacado na tela da loja ao aceitar.

---

## 12. Impacto no Backend

- Controle de limite de regeneração por dia.
- Reuso do pipeline de geração.
- Consulta e consumo do item "Pergaminho da Reforja" no inventário do usuário (ver ADR-022) quando a regeneração ocorre além do limite gratuito.

---

## 13. Impacto no Banco de Dados

Entidade: `Quest`.

Campos: `regenerationCount`, `date`, `generationReason`, `generationMethod`, `profileSnapshot`, `appliedFilters` (US-049).

Entidade: `InventoryItem` (ver ADR-022) — `userId`, `itemKey`, `quantity`. Usada para verificar/consumir o "Pergaminho da Reforja" (`itemKey = reforja_scroll`).

---

## 14. Impacto em Gamificação

- Regenerar não concede XP; conclusão sim.

---

## 15. Impacto em Monetização

- Equilíbrio entre flexibilidade e consistência protege o engajamento.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de limite. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/daily/regenerate
Body: { "useReforgeScroll": boolean }
```

Response (limite atingido, sem `useReforgeScroll`):

```json
{ "code": "REGENERATION_LIMIT_REACHED" }
```

Response (`useReforgeScroll: true` sem unidades disponíveis):

```json
{ "code": "REFORGE_SCROLL_NOT_AVAILABLE" }
```

```txt
GET /api/inventory/items/{itemKey}
GET /api/shop/items
POST /api/shop/items/{itemKey}/purchase
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| daily_quest_generated | Quando a regeneração entrega nova quest. |

---

## 19. Critérios de aceite

### CA-001 — Regeneração permitida

Dado que o usuário não atingiu o limite,

Quando regenerar,

Então deve receber uma nova quest personalizada e segura.

### CA-002 — Limite atingido

Dado que o limite diário foi atingido,

Quando tentar regenerar,

Então a ação deve ser bloqueada e a quest atual mantida.

### CA-003 — Uso do Pergaminho da Reforja além do limite

Dado que o limite diário foi atingido e o usuário possui ao menos 1 "Pergaminho da Reforja" no inventário,

Quando tentar regenerar e confirmar o consumo do item,

Então 1 unidade do item deve ser consumida e o usuário deve receber uma nova quest personalizada e segura.

### CA-004 — Cancelar o uso do Pergaminho da Reforja

Dado que o app perguntou se o usuário quer consumir o "Pergaminho da Reforja",

Quando o usuário cancelar,

Então nenhuma unidade deve ser consumida e a quest atual deve ser mantida.

### CA-005 — Sem Pergaminho da Reforja disponível

Dado que o limite diário foi atingido e o usuário não possui o "Pergaminho da Reforja",

Quando tentar regenerar,

Então o app deve perguntar se o usuário quer ir à loja e, se aceito, navegar até a loja com o item destacado para compra.

---

## 20. Critérios de teste para QA

### Backend

- limite diário é respeitado;
- nova quest mantém perfil/segurança/tempo;
- limitações/dores não são contornadas;
- regeneração além do limite só é aceita com `useReforgeScroll: true` e unidade disponível no inventário, consumindo exatamente 1 unidade;
- regeneração além do limite sem unidade disponível retorna `REFORGE_SCROLL_NOT_AVAILABLE`.

### E2E

- regenerar dentro do limite funciona;
- além do limite, é bloqueado;
- além do limite, com pergaminho disponível, pede confirmação antes de consumir e regenera ao confirmar;
- além do limite, sem pergaminho, oferece ir à loja com o item destacado;
- cancelar a confirmação do pergaminho não consome o item nem regenera.

---

## ✅ Decisão registrada

> A regeneração é permitida dentro de um limite diário, sempre mantendo personalização e segurança. Esgotado o limite, o usuário pode regenerar consumindo 1 unidade do item "Pergaminho da Reforja" do inventário, sempre com confirmação explícita antes do consumo; sem o item, o app oferece ir à loja comprá-lo.
