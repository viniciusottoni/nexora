# ADR-012 · Agregados pré-calculados para o painel

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO |
| **Relacionados** | ADR-006, ADR-018, ADR-034, ADR-035 |
| **Requisitos afetados** | RF-BI-01 a 14, RNF-PER-04 |

---

## Contexto

O painel do dono precisa responder em menos de 3 segundos (RNF-PER-04), sobre um histórico que chega a milhões de eventos, com comparativos de período e percentis.

Ao mesmo tempo, RF-BI-11 exige que **todo indicador permita navegar até o pedido de origem em no máximo 3 cliques**. Ou seja: precisamos de velocidade no agregado e de rastreabilidade no detalhe.

Há ainda uma complicação específica da arquitetura offline: eventos podem chegar **horas depois** de terem ocorrido. Um agregado calculado apenas de forma incremental ficaria permanentemente errado para os períodos afetados.

## Decisão

**Tabelas de agregação mantidas incrementalmente por um worker, com recálculo completo noturno do dia anterior.**

O painel lê agregado. O drill-down consulta o evento apenas quando o usuário abre o número.

## Detalhamento

### Camadas de agregação

| Tabela | Granularidade | Uso |
|---|---|---|
| `metric_hourly` | tenant × loja × hora × canal | Pulso, mapa de calor, tempos por faixa |
| `metric_daily` | tenant × loja × dia | Comparativos, tendências |
| `metric_product_daily` | tenant × produto × dia | Curva ABC, engenharia de cardápio |
| `metric_operator_daily` | tenant × operador × dia | Produtividade |

### Fluxo

```
domain_event ──► MetricAggregationWorker (BackgroundService, incremental, a cada 30 s)
                      │
                      ▼
                metric_hourly ──► metric_daily ──► Painel
                      ▲
        Job noturno (03h): recalcula o dia anterior por completo
```

`MetricAggregationWorker` é um `BackgroundService`/`IHostedService` registrado no `Program.cs` da `Api.Cloud`, análogo ao processador BullMQ do desenho original — a diferença é que a fila deixa de existir como componente separado: o worker consulta o `domain_event` diretamente em polling de 30 s, sem depender de um broker de fila.

### Por que o recálculo noturno é obrigatório

```
20h03  pedido criado offline
21h15  evento sincronizado
```

O worker incremental processaria esse evento às 21h15, mas ele pertence à hora das 20h (ADR-018, ADR-034). O recálculo noturno reprocessa o dia inteiro a partir do event store e corrige qualquer defasagem — **sem ele, todo indicador por faixa horária ficaria errado em loja com internet instável**.

### Agregados são descartáveis

```bash
# se um bug corromper os números, basta reprocessar
dotnet run --project Nexora.Tools.MetricsRebuild -- --tenant=<id> --from=2026-07-01 --to=2026-07-31
```

Essa propriedade é o que torna a decisão segura: o event store é a verdade; o agregado é cache.

### Drill-down

```
GET /v1/metrics/times?groupBy=hour        → agregado (rápido)
GET /v1/metrics/times/drill-down?bucket=20h → pedidos daquela hora (evento)
GET /v1/orders/{id}                         → carimbos completos
```

Três chamadas, três cliques — cumpre RF-BI-11.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Consulta direta ao event store | Sempre correto; sem worker | Lento e caro a cada abertura do painel | Não atende ao requisito de 3 s |
| Materialized views | Nativo; simples | Refresh completo é caro; incremental é limitado no Postgres | Não lida bem com chegada tardia de evento |
| Data warehouse dedicado | Escala muito | Infraestrutura, ETL e custo adicionais | Desproporcional à volumetria (doc. 03, §14) |
| Apenas incremental, sem recálculo | Mais simples | Números permanentemente errados após sync atrasado | Falha exatamente no cenário que o produto precisa suportar |
| Cálculo sob demanda com cache | Simples | Primeira consulta lenta; invalidação complexa | Pior experiência sem ganho real |

## Consequências

**Positivas**

- Painel responde em menos de 3 s mesmo com histórico grande
- Correção automática de dados que chegaram atrasados
- Agregados recalculáveis do zero: bug de cálculo não deixa cicatriz permanente
- Métrica nova pode ser retroagida sobre o histórico existente

**Negativas**

- Dado do dia corrente pode ficar alguns segundos atrás
- Worker é ponto de falha a monitorar
- Espaço adicional em disco (pequeno)
- Complexidade de manter duas rotas de cálculo (incremental e completa)

**Mitigações**

- Painel exibe explicitamente o momento da última atualização e o atraso de sincronização (RF-BI-14)
- Alerta se o worker parar por mais de 15 min (RNF-OBS)
- Teste noturno D-05 compara agregado com recálculo direto dos eventos

## Como validar

- Teste D-05: agregado horário igual ao recálculo direto
- Teste D-06: evento sincronizado com atraso aparece na hora de ocorrência
- RNF-PER-04 medido: p95 do painel abaixo de 3 s
- Drill-down alcança o pedido em 3 interações

## Revisitar quando

- O parque ultrapassar ~100 lojas e a carga analítica competir com a operacional na mesma instância
- Surgir necessidade de análise ad hoc livre pelo cliente (aí sim, warehouse)
