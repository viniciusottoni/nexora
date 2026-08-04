# E-09 · Auditoria

|  |  |
|---|---|
| **Fase** | 1 — MVP |
| **Histórias** | 2 |
| **Pontos** | 13 |
| **Sprints previstas** | Sprint 7 |
| **Aplicações afetadas** | api-edge, api-cloud, web-admin |
| **Pacotes do monorepo** | packages/db, packages/domain |

---

## 1. Objetivo do épico

Entregar a trilha imutável exigida na descoberta (item 7.4 do briefing: **"Sim."**) e reforçada na seção 5.4.

A trilha registra **quem** fez, **quando**, **o que mudou** (valor anterior e novo), **de onde** (dispositivo) e **em qual estabelecimento**. Cobre, no mínimo: cancelamentos, descontos, alterações de preço, movimentações de estoque, ajustes financeiros, abertura e fechamento de caixa e alterações de permissão.

A imutabilidade não é convenção — é imposta por permissão de banco. `UPDATE` e `DELETE` são revogados na tabela, o que torna a alteração impossível para a aplicação, não apenas proibida.

A trilha também é insumo de vários indicadores de gestão (cancelamentos, descontos, desvios): é infraestrutura de confiança, não burocracia.

## 2. Valor entregue

- Rastreabilidade completa de toda ação sensível
- Imutabilidade garantida pelo banco, não pela aplicação
- Base dos indicadores de cancelamento, desconto e desvio
- Evidência em caso de divergência ou suspeita
- Requisito de conformidade e de confiança do cliente

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-090](./US-090-Trilha-imutavel-de-acoes-sensiveis.md) | Trilha imutavel de acoes sensiveis | M | 8 | RF-AUD-01, RF-AUD-02, RF-AUD-04 |
| [US-091](./US-091-Consulta-e-filtro-da-trilha.md) | Consulta e filtro da trilha | M | 5 | RF-AUD-03 |

## 4. Ordem de execução recomendada

1. US-090 — trilha imutável
2. US-091 — consulta e filtro

## 5. Dependências do épico

**Depende de:** E-00  
**Habilita:** E-05, E-10, E-12

## 6. Definition of Done do épico

- [ ] Trilha cobrindo todas as ações sensíveis listadas no RF-AUD-02
- [ ] `UPDATE` e `DELETE` revogados na tabela por permissão de banco
- [ ] Tentativa de alteração recusada pelo banco, verificada por teste
- [ ] Consulta filtrável pelo gestor, com desempenho adequado
- [ ] Trilha funcionando integralmente offline

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Ação sensível esquecida sem registro na trilha | Média | Alto | Lista fechada no RF-AUD-02; teste de cobertura por ação |
| Volume da trilha degradar consultas ao longo do tempo | Média | Médio | Particionamento e retenção definidos no ADR-035 |

---

*Épico E-09 · Pacote 004_DonaBetinha · Replay Studio.*