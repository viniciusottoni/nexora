---
title: US-205 — Cachear status de acesso e reduzir consultas por request
sidebar_position: 205
---

# US-205 — Cachear status de acesso e reduzir consultas por request

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-205 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção |
| Perfil principal | Usuário autenticado, backend, Redis e assinatura |
| Plano | Trial, Mensal, Anual e Assinatura expirada |
| Dependência principal | ActiveAccessMiddleware, User, Subscription, Redis |
| Status | Planejada |

## 2. História do usuário

Como **usuário autenticado do AWAKEN**,

quero **que o app valide meu acesso rapidamente sem sobrecarregar o banco a cada ação**,

para **manter o sistema responsivo mesmo com muitos usuários online**.

## 3. Contexto

O middleware de acesso ativo consulta usuário e assinatura em praticamente toda requisição autenticada. Isso é seguro funcionalmente, mas cria carga repetitiva no banco. Para o MVP em produção, o status de acesso deve usar cache curto e invalidação controlada.

## 4. Objetivo

Reduzir queries repetidas por request autenticada, mantendo o backend como autoridade de acesso.

## 5. Escopo

### Entra nesta US

- Criar cache Redis para status de acesso por usuário.
- Usar TTL curto e seguro.
- Invalidar cache quando assinatura, trial, usuário ou acesso forem alterados.
- Evitar cache permanente para estado financeiro.
- Medir cache hit/miss.
- Criar testes para cache válido, cache expirado e invalidação.

### Fora desta US

- Transformar Redis em fonte de verdade.
- Mudança comercial de trial/assinatura.
- Read replica.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O PostgreSQL continua sendo a fonte de verdade. |
| RN-002 | Cache de status de acesso deve ter TTL curto. |
| RN-003 | Alteração de assinatura deve invalidar cache do usuário. |
| RN-004 | Usuário bloqueado/deletado deve invalidar cache. |
| RN-005 | Falha no Redis não pode liberar acesso indevido. |
| RN-006 | Cache não pode conter dados sensíveis desnecessários. |

## 7. Chaves sugeridas

```txt
access-status:{userId}
subscription-status:{userId}
```

TTL inicial recomendado:

```txt
30 a 120 segundos
```

## 8. Fluxo principal

1. Requisição autenticada chega ao backend.
2. Middleware tenta ler status de acesso no Redis.
3. Se houver cache válido, usa o status cacheado.
4. Se não houver cache, consulta banco, calcula status e grava cache curto.
5. Se acesso estiver expirado, retorna 403.
6. Se acesso estiver ativo, segue para controller.

## 9. Fluxos alternativos

- Redis indisponível: backend consulta banco e não concede acesso por fallback inseguro.
- Assinatura atualizada: cache é removido.
- Usuário deletado/bloqueado: cache é removido.

## 10. Impacto no Backend

- Ajustar `ActiveAccessMiddleware`.
- Criar serviço de cache de acesso.
- Invalidar cache nos handlers de assinatura, trial e usuário.
- Criar métricas de hit/miss.

## 11. Impacto no Banco

- Menos leituras em `users` e `subscriptions`.
- Nenhuma migration obrigatória.

## 12. Impacto no Flutter

Sem impacto visual direto. O app deve continuar tratando 403 como acesso bloqueado.

## 13. Critérios de aceite

- Segunda request autenticada do mesmo usuário usa cache quando válido.
- Alteração de assinatura invalida cache.
- Redis indisponível não libera acesso indevido.
- Métrica de hit/miss é registrada.
- Testes cobrem ativo, expirado, cache miss, cache hit e invalidação.

## 14. Critérios de teste para QA

- usuário trial ativo;
- usuário trial expirado;
- assinatura ativa;
- assinatura expirada;
- alteração de assinatura;
- Redis fora do ar em ambiente controlado;
- carga com múltiplas requests do mesmo usuário.

## ✅ Decisão registrada

Status de acesso passa a ser cacheado por curto período para reduzir carga repetitiva no banco, sem transformar Redis em fonte de verdade.