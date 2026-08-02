---
title: US-215 — Executar teste de carga e definir plano de capacidade do MVP
sidebar_position: 215
---

# US-215 — Executar teste de carga e definir plano de capacidade do MVP

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-215 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P0 |
| Fase | Bloqueador do teste aberto |
| Perfil principal | DevOps, QA, backend, Flutter e Produto |
| Plano | Todos |
| Dependência principal | Ambiente de staging, observabilidade, scripts de carga |
| Status | Planejada |

## 2. História do usuário

Como **dono do produto AWAKEN**,

quero **validar a capacidade mínima do MVP antes de abrir o app para usuários reais**,

para **não descobrir gargalos somente depois da publicação**.

## 3. Contexto

A meta de longo prazo é suportar dezenas ou centenas de milhares de pessoas online. O MVP não precisa nascer nesse tamanho, mas precisa ter teste de carga mínimo, metas claras e plano de evolução para crescer sem reescrita emergencial.

## 4. Objetivo

Executar teste de carga realista em staging, registrar resultados e definir o plano inicial de capacidade/autoscaling do MVP.

## 5. Escopo

### Entra nesta US

- Criar cenários de carga para fluxos críticos.
- Definir metas de p95, p99, taxa de erro e throughput.
- Rodar teste em staging com banco e Redis equivalentes ao plano inicial.
- Registrar gargalos encontrados.
- Definir capacidade inicial recomendada para API, Worker, PostgreSQL e Redis.
- Criar checklist go/no-go para teste aberto.

### Fora desta US

- Garantir centenas de milhares online no primeiro deploy.
- Teste de caos avançado.
- Benchmark público.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Teste aberto não deve ocorrer sem teste de carga mínimo documentado. |
| RN-002 | Fluxos críticos devem ter metas de latência e erro. |
| RN-003 | Resultado ruim deve gerar issues/USes antes do go-live. |
| RN-004 | Teste deve usar dados sintéticos, sem dados reais de usuário. |
| RN-005 | Plano de capacidade deve indicar quando escalar API, Worker, banco e Redis. |

## 7. Cenários mínimos

- Login e refresh.
- Consulta de status de acesso.
- Geração de quest diária.
- Consulta da quest do dia.
- Conclusão de exercício.
- Conclusão de quest.
- Consulta de loja.
- Consulta de histórico.
- Webhook/sync de assinatura em carga controlada.
- Jobs de notificação e penalidade com base simulada.

## 8. Metas iniciais sugeridas

| Métrica | Meta inicial MVP |
|---|---|
| Taxa de erro | menor que 1% em cenário nominal |
| p95 rotas simples | até 300 ms em staging saudável |
| p95 geração de quest | até 1500 ms sem IA externa pesada |
| p99 rotas simples | até 1000 ms em cenário nominal |
| Job diário | processar base simulada sem carregar tudo em memória |
| Redis hit rate em cache quente | acima de 80% nos caches alvo |

## 9. Fluxo principal

1. Preparar staging com configuração semelhante à produção inicial.
2. Popular dados sintéticos.
3. Executar scripts de carga.
4. Coletar métricas de API, banco, Redis e Worker.
5. Registrar resultado em documento.
6. Criar issues para gargalos.
7. Aprovar ou bloquear teste aberto.

## 10. Impacto no Backend

- Necessidade de endpoints estáveis e observáveis.
- Possível criação de seeds sintéticos para carga.
- Ajustes conforme gargalos encontrados.

## 11. Impacto no Flutter

- Fluxos críticos devem ser compatíveis com execução automatizada/manual de carga quando aplicável.
- App deve lidar com erro 429/5xx de forma amigável.

## 12. Impacto no DevOps

- Ambiente staging precisa representar produção inicial.
- Scripts de carga devem ficar versionados.
- Resultados devem ser registrados por data/commit.

## 13. Critérios de aceite

- Scripts de carga existem no repositório ou documentação.
- Dados sintéticos são usados.
- Relatório de teste contém data, commit, ambiente, cenários e resultados.
- Métricas p95/p99 e erro são registradas.
- Plano de capacidade inicial está documentado.
- Go/no-go do teste aberto é registrado.

## 14. Critérios de teste para QA

- executar cenário nominal;
- executar pico moderado;
- executar erro controlado de Redis ou banco em staging;
- validar comportamento do app sob 429/5xx;
- validar relatório final.

## ✅ Decisão registrada

O AWAKEN só deve abrir teste público após teste de carga mínimo documentado, com metas, resultado e plano de capacidade inicial do MVP.