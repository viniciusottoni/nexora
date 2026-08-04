# ADR-022 · Observabilidade e correlação de requisições

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, DevOps |
| **Relacionados** | ADR-001, ADR-021, ADR-033 |
| **Requisitos afetados** | RNF-OBS-01 a 09 |

---

## Contexto

O suporte deste produto é remoto e distribuído: N servidores em N lojas, cada um em uma rede que não controlamos, com usuários não técnicos. Quando o dono liga dizendo *"o pedido não chegou na cozinha"*, precisamos responder rápido, sem acesso físico e sem depender de descrição verbal.

O trajeto de um pedido atravessa: navegador do cliente → API do edge → banco local → WebSocket → navegador do KDS → outbox → nuvem. Sem correlação, investigar isso é impossível.

Há também a dimensão do parque: precisamos saber, sem perguntar, quais lojas estão saudáveis.

## Decisão

**Logs estruturados em JSON, tracing distribuído com OpenTelemetry, `traceId` propagado de ponta a ponta e reportado ao usuário, e health check de cada instalação enviado à nuvem a cada 60 segundos.**

## Detalhamento

### Correlação

```
Navegador gera traceId ──► header traceparent (W3C Trace Context)
   │
   ├─► API edge      (span: http.request)
   │      ├─► banco  (span: db.query)
   │      └─► outbox (span: event.append)
   ├─► WebSocket     (span: rt.emit)  → mesmo traceId
   └─► sync push     (span: sync.batch) → nuvem herda o traceId
```

O mesmo `traceId` aparece no erro devolvido ao usuário (ADR-021). O suporte pede esse número e encontra o trajeto inteiro.

### Log estruturado

```json
{
  "level": "info",
  "time": "2026-07-31T20:47:12.334Z",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "spanId": "00f067aa0ba902b7",
  "tenantId": "018f...",
  "storeId": "018f...",
  "userId": "018f...",
  "deviceId": "018f...",
  "installationId": "018f...",
  "msg": "order.placed",
  "orderId": "018f...",
  "shortCode": "A47",
  "itemCount": 3,
  "durationMs": 87
}
```

Campos obrigatórios: `traceId`, `tenantId`, `time`, `level`, `msg`. **Proibido**: nome de cliente, telefone, endereço, token, senha, PIN (RNF-SEG-15).

### Níveis

| Nível | Uso |
|---|---|
| `error` | Falha que exige ação; vai para o Sentry |
| `warn` | Degradação (fallback de polling, atraso de sync, retry) |
| `info` | Evento de negócio relevante e ciclo de vida |
| `debug` | Apenas em desenvolvimento |

### Health check da instalação

```http
POST /v1/platform/heartbeat        (a cada 60 s)
{
  "installationId": "...",
  "version": "1.4.2",
  "uptimeSeconds": 384210,
  "db": { "ok": true, "sizeMb": 1840, "connections": 6 },
  "outbox": { "pending": 12, "oldestPendingSeconds": 4 },
  "sync": { "lastSuccessAt": "...", "lagSeconds": 3 },
  "realtime": { "connectedClients": 7 },
  "disk": { "usedPercent": 42 },
  "metricsWorker": { "lastRunAt": "..." }
}
```

Ausência de heartbeat por mais de 10 minutos em horário de operação gera alerta à Replay (RNF-OBS-07).

### Métricas técnicas

| Métrica | Tipo |
|---|---|
| `http_request_duration_seconds` | histograma, por rota |
| `order_to_kds_latency_seconds` | histograma — **a métrica-mestre do produto** |
| `outbox_pending_total` | gauge |
| `sync_lag_seconds` | gauge |
| `sync_batch_duration_seconds` | histograma |
| `websocket_connected_clients` | gauge |
| `metrics_worker_lag_seconds` | gauge |
| `db_query_duration_seconds` | histograma |

### Retenção

| Dado | Quente | Frio |
|---|---|---|
| Logs | 30 dias | 12 meses |
| Traces | 7 dias (amostrado) | — |
| Métricas técnicas | 90 dias | 13 meses agregado |

Amostragem de traces: 100% em erro e em rota crítica (criação de pedido); 10% no restante.

### Painel de saúde do parque

Uma tela lista todas as instalações com: nome, versão, último contato, atraso de sync, uso de disco, clientes conectados e estado geral (OK / DEGRADADO / FORA). É a primeira tela que o suporte abre.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Logs em texto simples | Fáceis de ler no terminal | Não pesquisáveis por campo; sem correlação | Investigação remota inviável |
| Só Sentry, sem tracing | Simples; barato | Vê o erro, não o trajeto | Insuficiente para latência distribuída |
| Enviar todos os logs do edge para a nuvem | Visibilidade total | Consome banda da loja; custo alto | Só `error` e `warn` sobem; `info` fica local com rotação |
| APM proprietário completo | Rico | Custo por host × N lojas | Desproporcional |

## Consequências

**Positivas**

- Suporte investiga remotamente com o `traceId` que o usuário informa
- Problema de instalação é detectado antes de o cliente ligar
- Latência pedido → KDS é medida em produção, não estimada
- Painel de saúde viabiliza operar dezenas de lojas com equipe pequena

**Negativas**

- Instrumentação é trabalho contínuo
- Armazenamento de logs e traces tem custo
- Risco de vazar dado pessoal em log se a disciplina falhar

**Mitigações**

- Instrumentação incluída na Definition of Done
- Amostragem de traces para conter custo
- Varredura automatizada de logs procurando padrões de dado pessoal (CPF, telefone, e-mail) — falha no CI
- Apenas `error` e `warn` trafegam da loja para a nuvem

## Como validar

- Um pedido gera trace completo, do navegador à nuvem, com um único `traceId`
- Varredura de logs sem ocorrência de dado pessoal
- Simulação de instalação offline dispara alerta em até 10 min
- `order_to_kds_latency_seconds` p95 visível no painel e abaixo de 2 s

## Revisitar quando

- O custo de observabilidade crescer desproporcionalmente ao parque
- Surgir necessidade de análise de sessão de usuário (session replay)
