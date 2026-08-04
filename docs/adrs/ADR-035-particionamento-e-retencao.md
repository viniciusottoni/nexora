# ADR-035 · Particionamento e retenção do event store

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead |
| **Relacionados** | ADR-006, ADR-012, ADR-018 |
| **Requisitos afetados** | RNF-PER-04, RNF-MAN-04, RNF-LGP-05 |

---

## Contexto

O `domain_event` é a tabela que mais cresce no sistema — 3 a 8 mil eventos por dia por loja, cerca de 2 milhões por ano. Com 50 lojas em dois anos, chega a algo em torno de 200 milhões de linhas.

O volume em si não é problema para o PostgreSQL. O que se degrada é outra coisa: índices que não cabem mais em memória, `VACUUM` cada vez mais caro, consultas de drill-down lentas e a impossibilidade de descartar dado antigo sem um `DELETE` gigantesco que trava a tabela.

Há ainda uma exigência de privacidade: dados de cliente final devem ser anonimizados após 24 meses sem novo pedido (RNF-LGP-05), e parte deles vive no payload dos eventos.

## Decisão

**Particionamento por faixa mensal de `occurred_at`, com criação automática de partições futuras, arquivamento em object storage após 24 meses e anonimização por reescrita de payload.**

## Detalhamento

### Particionamento

```sql
CREATE TABLE domain_event (
  id           UUID NOT NULL,
  tenant_id    UUID NOT NULL,
  ...
  occurred_at  TIMESTAMPTZ NOT NULL,
  recorded_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (id, occurred_at)          -- a chave de partição entra na PK
) PARTITION BY RANGE (occurred_at);

CREATE TABLE domain_event_2026_07 PARTITION OF domain_event
  FOR VALUES FROM ('2026-07-01') TO ('2026-08-01');
```

> Detalhe importante: a chave de partição precisa fazer parte da chave primária. Isso significa que a deduplicação por `id` (ADR-007) passa a exigir também o `occurred_at` — que o evento sempre carrega, então não há perda funcional.

### Por que `occurred_at` e não `recorded_at`

Evento sincronizado com atraso pertence, semanticamente, ao mês em que **ocorreu**. Particionar por `recorded_at` colocaria eventos de julho na partição de agosto e quebraria a poda de partição nas consultas por período — que são justamente as mais frequentes no painel.

### Manutenção automática

```
Job mensal (dia 25)
  ├─ cria a partição do mês seguinte (e do subsequente, por margem)
  ├─ cria índices na partição nova
  ├─ arquiva partições com mais de 24 meses
  └─ verifica se há evento fora de faixa (não deveria haver)
```

Partição faltando causa erro de inserção — por isso duas partições futuras são mantidas sempre prontas.

### Índices por partição

```sql
CREATE INDEX ON domain_event_2026_07 (tenant_id, occurred_at DESC);
CREATE INDEX ON domain_event_2026_07 (tenant_id, aggregate_type, aggregate_id);
```

Índice por partição é muito menor e cabe em memória — é aqui que está o ganho real de desempenho.

### Arquivamento

```
Partição com mais de 24 meses
  ├─ exportada em Parquet, comprimida
  ├─ enviada ao object storage (s3://archive/events/<tenant>/<ano-mes>.parquet)
  ├─ verificação de integridade (contagem e checksum)
  └─ DETACH + DROP da partição       ← instantâneo, sem DELETE
```

`DROP` de partição é uma operação de metadados. Descartar 2 milhões de linhas leva milissegundos, contra horas de um `DELETE` com `VACUUM`.

Consulta a dado arquivado é excepcional e feita por processo manual de suporte.

### Retenção por tipo de dado

| Dado | Quente | Depois |
|---|---|---|
| `domain_event` | 24 meses | Parquet no object storage |
| `metric_hourly` | 24 meses | Consolidado em diário |
| `metric_daily` | Indefinido | Mantido (volume baixo) |
| `audit_log` | 5 anos | Arquivamento frio |
| `outbox` sincronizado | 30 dias (edge) | Purga |
| `idempotency_key` | 24 h | Purga |
| Dados de cliente final | 24 meses sem pedido | Anonimização |

Métrica agregada é pequena e é o que o dono realmente consulta em série histórica longa — por isso é mantida indefinidamente, enquanto o evento bruto é arquivado.

### Anonimização (LGPD)

Como o evento é imutável (ADR-006), a anonimização não apaga o evento: **reescreve o payload**, preservando estrutura, valores e horários.

```sql
UPDATE domain_event
SET payload = payload - 'customerName' - 'customerPhone' - 'address'
              || '{"anonymized": true}'::jsonb
WHERE tenant_id = $1
  AND payload->>'customerId' = $2;
```

O dado de negócio (valor, itens, tempos) permanece íntegro — as métricas históricas não se alteram. Apenas o vínculo com a pessoa desaparece.

> Exceção deliberada à imutabilidade, justificada por obrigação legal, executada por processo auditado e registrada em `audit_log`.

### No edge

O servidor local mantém apenas **12 meses** — a nuvem já tem tudo, e o disco da loja é limitado. Partições anteriores são simplesmente descartadas após confirmação de sincronização.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Tabela única sem partição | Simples | Índices insustentáveis; `DELETE` de dado antigo trava a tabela | Degradação garantida no segundo ano |
| Partição por tenant | Isolamento físico | Centenas de partições; consulta cross-tenant complicada | Não escala com o número de clientes |
| Partição por tenant e mês | Máxima granularidade | Explosão combinatória de partições | Manutenção inviável |
| Arquivar em banco separado | Consultável por SQL | Mais um banco para operar | Consulta a arquivo é rara |
| Nunca arquivar | Simples | Crescimento indefinido; custo crescente | Sem justificativa de negócio |
| Partição por semana | Menor por partição | Muitas partições; ganho marginal | Mês é o equilíbrio adequado |

## Consequências

**Positivas**

- Consultas por período podam partições automaticamente
- Índices menores, com melhor uso de memória
- Descarte de dado antigo é instantâneo
- `VACUUM` opera por partição, não na tabela inteira
- Anonimização preserva a integridade das métricas históricas

**Negativas**

- Chave primária composta muda o padrão de deduplicação
- Job de manutenção é ponto de falha (partição faltante quebra inserção)
- Consulta a dado arquivado exige processo manual
- Anonimização abre exceção controlada à imutabilidade

**Mitigações**

- Duas partições futuras sempre criadas com antecedência
- Alerta se a próxima partição não existir com 7 dias de folga
- Verificação de integridade após arquivamento, antes do `DROP`
- Anonimização executada por job auditado, nunca manualmente

## Como validar

- Consulta por período usa apenas as partições relevantes (verificado por `EXPLAIN`)
- Job mensal cria partições e não deixa lacuna
- Arquivamento preserva contagem e checksum antes do `DROP`
- Anonimização remove dado pessoal sem alterar nenhuma métrica agregada
- Teste de desempenho com 10 milhões de eventos: drill-down abaixo de 1 s

## Revisitar quando

- O volume por loja crescer a ponto de tornar a partição mensal grande demais
- Surgir necessidade frequente de consulta a dado arquivado
