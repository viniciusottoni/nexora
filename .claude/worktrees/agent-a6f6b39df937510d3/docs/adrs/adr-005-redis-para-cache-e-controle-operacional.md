# ADR-005 — Redis para cache e controle operacional

Status: Aceito

## Contexto

O AWAKEN terá operações que precisam de baixa latência e controle operacional: sessão, rate limit, cache de perfil, cache de quest diária, proteção contra chamadas repetidas e apoio a jobs leves. Usar apenas PostgreSQL para tudo aumentaria carga e complexidade nas consultas frequentes.

## Decisão

Usar Redis como cache e camada auxiliar de controle operacional.

## Implementação

- Usar Redis para cache de perfil e progressão de leitura frequente.
- Usar Redis para rate limit de login, geração de quest e chamadas de IA.
- Usar Redis para locks curtos em conclusão de quest, evitando processamento duplicado.
- Usar TTL em caches sensíveis.
- Nunca usar Redis como fonte final de dados críticos.
- Persistência oficial continua no PostgreSQL.

## Consequências

A API ganha performance e proteção contra abuso. A equipe deve tratar Redis como cache descartável, não como banco principal.

## Critérios de aceite

- A aplicação funciona mesmo após limpeza do cache.
- Dados oficiais continuam no PostgreSQL.
- Rate limit é aplicado em endpoints críticos.
- Locks têm TTL curto para evitar travamentos permanentes.
