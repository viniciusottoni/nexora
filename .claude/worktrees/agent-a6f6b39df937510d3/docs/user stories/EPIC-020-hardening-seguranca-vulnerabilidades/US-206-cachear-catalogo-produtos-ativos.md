---
title: US-206 — Cachear catálogo aprovado e produtos ativos
sidebar_position: 206
---

# US-206 — Cachear catálogo aprovado e produtos ativos

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-206 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção |
| Perfil principal | Usuário em geração de quest, loja, backend e Redis |
| Plano | Trial, Mensal e Anual |
| Dependência principal | ExerciseCatalogRepository, ShopProductRepository, Redis |
| Status | Planejada |

## 2. História do usuário

Como **usuário que gera quests e consulta a loja**,

quero **que dados estáticos ou pouco mutáveis sejam carregados rapidamente**,

para **ter uma experiência fluida sem sobrecarregar o banco**.

## 3. Contexto

A geração de treino consulta o catálogo aprovado de exercícios e a loja consulta produtos ativos. Esses dados mudam pouco e são candidatos fortes a cache. No MVP, cachear esses dados reduz latência, custo de banco e risco de pico quando muitos usuários acessam ao mesmo tempo.

## 4. Objetivo

Cachear catálogo aprovado de exercícios e produtos ativos, com invalidação quando admin/importação alterar esses dados.

## 5. Escopo

### Entra nesta US

- Cache Redis para catálogo aprovado usado na geração de treino.
- Cache Redis para produtos ativos de loja.
- Projeções leves para evitar carregar entidades tracked.
- `AsNoTracking` em consultas de leitura.
- Invalidação por importação/admin/update.
- Métricas de hit/miss e tempo de geração.

### Fora desta US

- CDN de mídia, coberta na US-214.
- Novo catálogo de exercícios.
- Alteração dos produtos comerciais.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Catálogo aprovado pode ser cacheado porque não é estado financeiro do usuário. |
| RN-002 | Produtos ativos podem ser cacheados, mas compra sempre valida produto no backend. |
| RN-003 | Importação ou alteração admin deve invalidar cache. |
| RN-004 | Cache não pode retornar exercício não aprovado para geração. |
| RN-005 | Falha de cache deve cair para banco sem quebrar geração. |

## 7. Chaves sugeridas

```txt
exercise-catalog:approved:v1
shop-products:active:v1
```

TTL inicial recomendado:

```txt
10 a 60 minutos, com invalidação explícita em alterações admin
```

## 8. Fluxo principal

1. Serviço solicita catálogo aprovado ou produtos ativos.
2. Backend tenta ler do cache.
3. Se houver cache, usa projeção cacheada.
4. Se não houver cache, consulta banco com `AsNoTracking` e projeção leve.
5. Backend grava cache com TTL.
6. Alterações administrativas invalidam a chave correspondente.

## 9. Impacto no Backend

- Criar DTO/projeção para catálogo de geração.
- Ajustar `WorkoutGeneratorService` para usar cache.
- Ajustar `ShopProductRepository`/serviço de loja para usar cache.
- Adicionar invalidação nos fluxos de importação/admin.

## 10. Impacto no Banco

- Menos leituras repetidas em `exercise_catalogs` e `shop_products`.
- Avaliar índice composto para exercícios aprovados por ambiente/dificuldade/equipamento.

## 11. Impacto no Flutter

Sem impacto direto. Loja e geração devem responder mais rápido.

## 12. Critérios de aceite

- Geração de quest usa catálogo cacheado quando disponível.
- Produtos ativos usam cache quando disponível.
- Alteração admin invalida cache.
- Falha de Redis não impede funcionamento.
- Consultas de leitura usam `AsNoTracking`/projeção.
- Métricas de hit/miss existem.

## 13. Critérios de teste para QA

- cache miss inicial;
- cache hit posterior;
- alteração de produto invalida cache;
- importação de exercício invalida cache;
- Redis indisponível;
- geração de quest com catálogo grande.

## ✅ Decisão registrada

Catálogo aprovado e produtos ativos devem ser cacheados no MVP para reduzir pressão no banco e preparar crescimento sem comprometer a autoridade do backend.