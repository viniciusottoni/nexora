---
title: US-220 — Visualizar performance, cache, banco e Redis
sidebar_position: 220
---

# US-220 — Visualizar performance, cache, banco e Redis

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-220 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Origem | US-205, US-206, US-208, US-213 |
| Prioridade | P0 |
| Fase | Bloqueador do teste aberto |
| Perfil principal | Admin, DevOps, Backend e Produto |
| Plataforma | Web Admin React + Backend Admin API + Observabilidade |
| Status | Planejada |

## 2. História do usuário

Como **admin técnico do AWAKEN**,

quero **visualizar performance da API, banco, Redis e caches críticos**,

para **detectar gargalos antes que usuários percebam lentidão**.

## 3. Contexto

As US-205, US-206, US-208 e US-213 criam cache, índices e observabilidade mínima. O Admin deve exibir esses sinais de forma acessível: latência, erro, uso de cache, saúde do banco, Redis e rotas críticas.

## 4. Objetivo

Criar uma tela de performance operacional com foco em prevenção de lentidão e saturação.

## 5. Escopo

### Entra nesta US

- Cards de p95, p99, erro, RPS e tempo médio por rota crítica.
- Status de banco e Redis.
- Hit/miss de cache de status de acesso.
- Hit/miss de cache de catálogo e produtos.
- Lista de endpoints mais lentos.
- Sinais de consultas sem índice ou lentas quando disponíveis.
- Filtro por ambiente e período.

### Fora desta US

- Profiling avançado no Admin.
- Otimização automática de queries.
- APM completo dentro do produto.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Rotas críticas devem aparecer com p95 e taxa de erro. |
| RN-002 | Cache sem métricas deve aparecer como sem dados. |
| RN-003 | Queda brusca de hit rate deve gerar alerta visual. |
| RN-004 | Banco ou Redis indisponível deve deixar o painel crítico. |
| RN-005 | Dados devem ser agregados e não expor payload de usuário. |

## 7. Indicadores mínimos

- p95 e p99 da API.
- Erros 4xx e 5xx por rota.
- RPS por rota crítica.
- Cache hit rate por domínio.
- Latência de banco.
- Latência de Redis.
- Top endpoints lentos.
- Última coleta de métricas.

## 8. Fluxo principal

1. Admin acessa Performance.
2. Sistema carrega métricas agregadas.
3. Admin identifica endpoints lentos ou cache ineficiente.
4. Admin abre detalhe de rota/domínio.
5. Admin navega para relatório ou incidente relacionado.

## 9. Impacto no Frontend

- Criar página `Performance` ou aba dentro de Relatórios/Health.
- Usar Recharts já disponível no web admin.
- Criar cards e tabelas de indicadores.

## 10. Impacto no Backend

- Endpoint agregado de performance.
- Integração com métricas da US-213.
- Expor dados resumidos por ambiente/período.

## 11. Critérios de aceite

- Admin visualiza p95/p99 e erro por rota crítica.
- Admin visualiza hit/miss dos caches críticos.
- Admin visualiza status de banco e Redis.
- Endpoint lento fica destacado.
- Dados sem coleta aparecem como sem dados.
- Não há payload sensível na tela.

## 12. Critérios de teste para QA

- rota saudável;
- rota lenta;
- erro elevado;
- cache com hit alto;
- cache com hit baixo;
- Redis indisponível;
- banco com latência alta.

## ✅ Decisão registrada

O Admin deve permitir diagnóstico rápido de performance e cache para evitar lentidão no MVP em produção.