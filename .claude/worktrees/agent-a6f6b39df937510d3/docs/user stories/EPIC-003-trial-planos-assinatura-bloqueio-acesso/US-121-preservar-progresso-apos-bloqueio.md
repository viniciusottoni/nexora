---
title: US-121 — Preservar progresso após bloqueio
sidebar_position: 121
---

# US-121 — Preservar progresso após bloqueio

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-121 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Trial expirado e assinatura expirada |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Dados de progresso e status de acesso |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso bloqueado**,

quero **que meu progresso continue salvo**,

para **não perder minha evolução caso eu assine depois**.

---

## 3. Contexto

Bloquear acesso não deve apagar a jornada. Preservar XP, rank, atributos, streak e histórico aumenta confiança e cria incentivo para reativação.

---

## 4. Objetivo

Garantir que expiração do trial ou assinatura não remova dados de progresso do usuário.

---

## 5. Escopo

### Entra nesta US

- Preservar perfil do usuário.
- Preservar XP, rank, level e atributos.
- Preservar histórico e QuestLog.
- Preservar status comercial separado do progresso.
- Reexibir progresso após reativação.

### Fora desta US

- Exportação de dados.
- Exclusão de conta.
- Arquivamento avançado.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Expiração do trial não deve apagar progresso. |
| RN-002 | Expiração da assinatura não deve apagar progresso. |
| RN-003 | Bloqueio deve impedir novas ações, não remover dados históricos. |
| RN-004 | Após assinar, progresso deve ser restaurado visualmente. |
| RN-005 | Exclusão de conta é regra separada e não faz parte desta US. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Progresso é salvo normalmente. |
| Premium Mensal | Progresso é salvo normalmente. |
| Premium Anual | Progresso é salvo normalmente. |
| Trial expirado | Progresso fica preservado, mas uso fica bloqueado. |
| Assinatura expirada | Progresso fica preservado, mas uso fica bloqueado. |

---

## 8. Fluxo principal

1. Usuário conclui quests durante trial ou assinatura.
2. Sistema registra progresso.
3. Trial ou assinatura expira.
4. Sistema bloqueia recursos protegidos.
5. Dados de progresso permanecem salvos.
6. Usuário assina e recupera acesso aos dados.

---

## 9. Fluxos alternativos

### 9.1. Usuário fica bloqueado por muito tempo

Dados devem permanecer vinculados à conta, salvo política futura de retenção.

### 9.2. Usuário exclui conta

Exclusão segue regra própria da US-013.

---

## 10. Estados esperados

- acesso ativo;
- acesso bloqueado;
- progresso preservado;
- acesso reativado;
- conta excluída, fora desta US.

---

## 11. Impacto no Frontend Flutter

- Mostrar estado limitado sem apagar dados locais indevidamente.
- Após reativação, recarregar progresso.
- Mensagens claras: progresso salvo.

---

## 12. Impacto no Backend

- Separar status de acesso dos dados de progresso.
- Bloquear operações, não excluir histórico.
- Retornar progresso após reativação.

---

## 13. Impacto no Banco de Dados

Entidades preservadas:

- UserProfile;
- HunterProgress;
- HunterAttributes;
- QuestLog;
- Quest.

---

## 14. Impacto em Gamificação

- XP, rank, level, atributos e histórico permanecem.
- Streak futura pode ter regra própria, mas histórico não deve sumir.

---

## 15. Impacto em Monetização

- Preservar progresso aumenta incentivo para assinar depois.
- Não deve usar ameaça de perda falsa como pressão.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagem de progresso preservado. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

Não há endpoint exclusivo. A regra deve ser aplicada nos serviços de acesso e progresso.

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| access_blocked | Quando acesso é bloqueado sem apagar progresso. |
| access_restored | Quando progresso volta a ficar acessível. |

---

## 19. Critérios de aceite

### CA-001 — Progresso preservado

Dado que o trial expirou,

Quando o usuário assinar depois,

Então seu progresso anterior deve continuar disponível.

### CA-002 — Sem exclusão por bloqueio

Dado que a assinatura expirou,

Quando o acesso for bloqueado,

Então dados de progresso não devem ser apagados.

---

## 20. Critérios de teste para QA

- expirar trial com XP;
- expirar assinatura com histórico;
- reativar e validar progresso;
- validar perfil Hunter após reativação;
- validar que exclusão de conta é fluxo separado.

---

## ✅ Decisão registrada

> Bloqueio comercial impede uso, mas não apaga a evolução do usuário. O progresso deve permanecer salvo para reativação futura.
