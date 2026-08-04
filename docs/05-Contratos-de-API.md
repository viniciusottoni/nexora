# 05 — Contratos de API
## Ecossistema Nexora

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Documento** | Contratos de API (REST + WebSocket) |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |
| **Depende de** | `03-Modelo-de-Dados.md`, `04-Catalogo-de-Eventos-e-Maquinas-de-Estado.md` |

---

## 1. Princípios

| # | Princípio |
|---|---|
| 1 | **Duas APIs, um contrato.** `Api.Edge` (loja) e `Api.Cloud` compartilham DTOs de `Nexora.Contracts`. O front não sabe com quem fala. |
| 2 | **REST para comando e consulta, WebSocket para reação.** Nada de polling em tela operacional. |
| 3 | **Idempotência obrigatória** em toda escrita: header `Idempotency-Key`. |
| 4 | **Versionamento em path:** `/v1/...`. Quebra de contrato exige `/v2`. |
| 5 | **Erros padronizados** (RFC 7807 — Problem Details). |
| 6 | **Tenant nunca vem do cliente** em rota autenticada — sempre do token. |
| 7 | Toda escrita retorna o **estado resultante**, evitando re-fetch. |

### 1.1 Base URLs

| Ambiente | URL |
|---|---|
| Edge (loja) | `https://edge.local/v1` |
| Nuvem | `https://api.<plataforma>.com.br/v1` |
| Público (cardápio/delivery) | `https://<dominio-do-cliente>/api/v1/public` |

---

## 2. Autenticação

### 2.1 Fluxos

| Perfil | Método | Token |
|---|---|---|
| Gestor, administrativo | E-mail + senha (+ 2FA opcional) | Access 15 min · Refresh 30 dias |
| Garçom, cozinha, caixa | **PIN** em dispositivo registrado | Access 8 h (turno) · vinculado ao `deviceId` |
| Cliente do salão | Token anônimo do QR Code | Escopo mínimo, expira com a sessão da mesa |
| Cliente de delivery | Telefone + código OTP | Access 30 dias |
| Edge server | Chave assimétrica + HMAC por requisição | Sem expiração; revogável |
| Admin de plataforma | E-mail + senha + 2FA obrigatório | Access 15 min · escopo especial auditado |

### 2.2 Endpoints

```http
POST /v1/auth/login
{ "email": "...", "password": "..." }
→ 200 { "accessToken": "...", "refreshToken": "...", "user": {...}, "tenant": {...} }

POST /v1/auth/pin
{ "pin": "4821", "deviceId": "..." }
→ 200 { "accessToken": "...", "user": {...}, "permissions": [...] }

POST /v1/auth/refresh
{ "refreshToken": "..." }
→ 200 { "accessToken": "...", "refreshToken": "..." }

POST /v1/auth/authorize          # autorização pontual de ação sensível
{ "action": "CANCEL_STARTED_ITEM", "pin": "9911", "context": { "orderItemId": "..." } }
→ 200 { "authorizationToken": "...", "expiresIn": 120, "authorizedBy": {...} }
```

O `authorizationToken` é enviado no header `X-Authorization-Token` da requisição que executa a ação sensível — o gerente digita o PIN no próprio dispositivo do operador, sem trocar de sessão.

### 2.3 Claims do JWT

```json
{
  "sub": "<userId>",
  "tid": "<tenantId>",
  "sid": "<storeId>",
  "roles": ["WAITER"],
  "perms": ["order:create","order:read","table:open"],
  "did": "<deviceId>",
  "exp": 1234567890
}
```

---

## 3. Convenções gerais

### 3.1 Cabeçalhos

| Header | Uso |
|---|---|
| `Authorization: Bearer <token>` | Obrigatório em rota autenticada |
| `Idempotency-Key: <uuid>` | Obrigatório em POST/PUT/PATCH |
| `X-Device-Id` | Identificação do terminal |
| `X-Client-Version` | Versão do app, para compatibilidade |
| `X-Authorization-Token` | Autorização de ação sensível |
| `X-Occurred-At` | Horário real do fato (essencial em operação offline) |

### 3.2 Erros

```json
{
  "type": "https://docs.<plataforma>/errors/insufficient-stock",
  "title": "Estoque insuficiente",
  "status": 422,
  "detail": "O insumo 'Mussarela' não possui saldo para produzir este item.",
  "instance": "/v1/orders/01919e.../items",
  "code": "STOCK_INSUFFICIENT",
  "meta": { "ingredientId": "...", "required": 0.35, "available": 0.12 }
}
```

| Código HTTP | Uso |
|---|---|
| 400 | Requisição malformada |
| 401 | Não autenticado |
| 403 | Sem permissão / falta autorização de perfil superior |
| 404 | Recurso inexistente (ou de outro tenant — resposta idêntica, por segurança) |
| 409 | Conflito de estado (transição inválida) |
| 422 | Regra de negócio violada |
| 429 | Rate limit |
| 503 | Dependência externa indisponível (ex.: gateway de pagamento) |

### 3.3 Paginação

```http
GET /v1/orders?limit=50&cursor=<opaco>&status=READY
→ { "data": [...], "meta": { "nextCursor": "...", "hasMore": true } }
```

Paginação por cursor, não por offset — lista de pedidos muda constantemente.

---

## 4. API pública (cardápio e delivery)

Sem autenticação. Tenant resolvido pelo host.

```http
GET /v1/public/branding
→ { "tenant": {...}, "branding": { colors, logo, fonts, texts, pwa } }

GET /v1/public/menu?channel=DINE_IN
→ { "categories": [ { id, name, products: [ { id, name, image, variants: [...], modifierGroups: [...], isAvailable, prepMinutes } ] } ] }

GET /v1/public/table/{qrToken}
→ { "table": {...}, "session": {...}, "sessionToken": "...", "currentItems": [...] }

POST /v1/public/orders            # pedido do cliente (mesa ou delivery)
Idempotency-Key: <uuid>
{
  "channel": "DINE_IN",
  "sessionToken": "...",
  "items": [
    {
      "variantId": "...", "quantity": 1, "notes": "sem cebola",
      "fractions": [ { "variantId": "<mussarela-g>", "weight": 0.5 },
                     { "variantId": "<calabresa-g>", "weight": 0.5 } ],
      "modifiers": [ { "modifierId": "<borda-catupiry>" } ]
    }
  ]
}
→ 201 { "order": {...}, "promisedAt": "...", "estimatedMinutes": 12 }

GET  /v1/public/orders/{id}/status
→ { "status": "IN_PRODUCTION", "items": [ { name, status, etaMinutes } ], "promisedAt": "..." }

POST /v1/public/table/{qrToken}/call-waiter
POST /v1/public/table/{qrToken}/request-bill   { "splitMode": "BY_PERSON", "people": 4 }
POST /v1/public/orders/{id}/rating             { "rating": 5, "comment": "..." }
```

---

## 5. API de operação (edge)

### 5.1 Mesas e comandas

```http
GET  /v1/tables                       # mapa de mesas com status e tempo
→ [ { id, label, area, status, session: { openedAt, minutesOpen, total, guestCount, waiter } } ]

POST /v1/tables/{id}/sessions         { "guestCount": 4 }
→ 201 { "session": {...} }

GET  /v1/sessions/{id}                # comanda completa
POST /v1/sessions/{id}/close
PATCH /v1/sessions/{id}/transfer      { "toTableId": "...", "itemIds": [...] }
```

### 5.2 Pedidos

```http
POST /v1/orders
Idempotency-Key: <uuid>
X-Occurred-At: 2026-07-31T20:47:12.334Z
{ "channel": "DINE_IN", "sessionId": "...", "items": [...] }
→ 201 { "order": {...}, "promisedAt": "..." }

GET   /v1/orders?status=IN_PRODUCTION&stationId=...
POST  /v1/orders/{id}/items                    # acrescentar item
PATCH /v1/orders/{id}/items/{itemId}/cancel
      X-Authorization-Token: <se já iniciado>
      { "reason": "CUSTOMER_REQUEST" }
POST  /v1/orders/{id}/cancel                   { "reason": "..." }
```

### 5.3 KDS

```http
GET /v1/kds/queue?stationId=...
→ {
    "items": [
      {
        "orderItemId": "...", "orderCode": "A47", "shortCode": "47",
        "productName": "Pizza G Mussarela / Calabresa",
        "quantity": 1, "modifiers": ["sem cebola"], "notes": "...",
        "status": "QUEUED",
        "placedAt": "...", "fireAt": "...", "elapsedSeconds": 214,
        "thresholdState": "WARNING",
        "table": "12", "channel": "DINE_IN",
        "priorityScore": 87, "priorityReason": "prazo em 3 min"
      }
    ],
    "allDay": [ { "productName": "Pizza G Mussarela", "pending": 12 } ],
    "bottleneck": { "slotsTotal": 5, "slotsUsed": 3, "queueSize": 8 }
  }

POST /v1/kds/items/{id}/advance      # AVANÇO POR UM TOQUE
{ "to": "FIRED" }                    # FIRED | IN_OVEN | OUT_OF_OVEN | READY
→ 200 { "item": {...}, "nextAction": "IN_OVEN" }

POST /v1/kds/items/{id}/refire       { "reason": "BURNED" }
POST /v1/kds/products/{variantId}/unavailable  { "reason": "OUT_OF_STOCK" }
GET  /v1/kds/history?shift=current
```

> `advance` sem informar `to` avança para o próximo estado natural — é o que permite o teclado numérico: digitar `47` + `Enter` avança o pedido 47.

### 5.4 Caixa

```http
POST /v1/cash-sessions/open           { "openingAmount": 200.00 }
GET  /v1/cash-sessions/current
POST /v1/cash-sessions/movements      { "type": "WITHDRAWAL", "amount": 500, "reason": "sangria" }

GET  /v1/sessions/{id}/bill?split=BY_PERSON&people=4
→ { "items": [...], "subtotal": 180.00, "serviceFee": 18.00, "total": 198.00,
    "split": [ { "person": 1, "amount": 49.50 } ] }

POST /v1/sessions/{id}/payments
Idempotency-Key: <uuid>
{ "payments": [ { "method": "CREDIT", "amount": 100.00, "provider": "CIELO", "providerRef": "..." },
                { "method": "PIX",    "amount": 98.00 } ] }
→ 201 { "session": { "status": "PAID" }, "receipt": { "url": "..." } }

POST /v1/sessions/{id}/discount
X-Authorization-Token: <se acima do limite>
{ "percent": 10, "reason": "cortesia" }

POST /v1/cash-sessions/{id}/close     { "countedAmount": 1843.50 }
→ 200 { "expected": 1850.00, "counted": 1843.50, "divergence": -6.50, "requiresJustification": true }
```

### 5.5 Estoque

```http
GET  /v1/ingredients?lowStock=true
POST /v1/purchases                    { "supplierId": "...", "items": [...] }
POST /v1/stock/waste                  { "ingredientId": "...", "quantity": 2.5, "reason": "EXPIRATION" }
POST /v1/inventory-counts             { "items": [ { "ingredientId": "...", "countedQty": 12.4 } ] }
→ 201 { "count": {...}, "divergences": [ { ingredient, expected, counted, costImpact } ] }

GET  /v1/recipes/{variantId}
PUT  /v1/recipes/{variantId}          { "items": [ { "ingredientId": "...", "quantity": 0.18, "uom": "KG", "wastePercent": 2 } ] }
GET  /v1/products/{variantId}/cost
→ { "cost": 8.42, "price": 45.00, "margin": 36.58, "marginPercent": 81.3, "breakdown": [...] }
```

### 5.6 Alertas (E-08)

```http
GET  /v1/alerts?status=open
→ { "alerts": [ { "id", "type", "severity", "entityType", "entityId", "message", "raisedAt",
                  "acknowledgedAt", "resolvedAt", "targetRoles", "targetUserId", "groupKey" } ] }

GET  /v1/alerts?grouped=true            # US-083 — agrupado por tipo/janela
→ { "groups": [ { "type", "count", "severity", "message", "firstRaisedAt", "lastRaisedAt", "alerts": [...] } ] }

POST /v1/alerts/{id}/acknowledge
POST /v1/alerts/{id}/resolve

GET  /v1/tenant/alert-routing           # US-082 — matriz de direcionamento, já resolvida
→ { "ORDER_LATE": { "roles": [...], "scope": "RESPONSIBLE", "escalateAfterSeconds": 120,
                     "groupWindowSeconds": 60 }, ... }
```

`GET /v1/alerts`/`POST .../acknowledge`/`.../resolve` existem tanto no edge (autoridade da
avaliação, US-080 §9) quanto na nuvem (alertas de gestão + consulta remota do gestor); `PATCH
/v1/tenant/alert-routing` só na nuvem (mesma autoridade de escrita de `/v1/tenant/thresholds`
acima).

---

## 6. API de gestão (nuvem)

### 6.1 Painel do dono

```http
GET /v1/dashboard/pulse
→ {
    "revenueToday": 4820.00, "revenueVsAvgPercent": 12.4,
    "ordersLate": 2,
    "avgMinutesLastHour": 11.3, "targetMinutes": 10,
    "tablesOccupied": 14, "tablesTotal": 20,
    "openAlerts": 3,
    "syncDelaySeconds": 4,
    "asOf": "2026-07-31T21:02:00Z"
  }

GET /v1/metrics/times?from=...&to=...&groupBy=hour&channel=DINE_IN
→ { "series": [ { "hour": "...", "orders": 24, "avgSeconds": 640, "p90Seconds": 980, "otd": 0.83 } ] }

GET /v1/metrics/sales?from=...&to=...&dimension=product|category|channel|operator
GET /v1/metrics/heatmap?weeks=8
GET /v1/metrics/menu-engineering?from=...&to=...
→ { "items": [ { variantId, name, quantity, revenue, cost, margin,
                 quadrant: "STAR|PLOWHORSE|PUZZLE|DOG" } ] }

GET /v1/metrics/cmv?period=2026-07
→ { "theoretical": 18420.00, "actual": 19870.00, "divergence": 1450.00,
    "divergencePercent": 7.9, "byIngredient": [...] }

GET /v1/metrics/{code}/drill-down?bucket=...    # RF-BI-11 — do número ao pedido
→ { "orders": [ { id, code, placedAt, totalSeconds, table, items } ] }
```

### 6.2 Financeiro

```http
GET  /v1/finance/summary?period=2026-07
→ { "revenue": {...}, "cmv": {...}, "labor": {...}, "fixed": {...},
    "primeCost": 0.612, "breakEven": 92400.00, "result": 14320.00 }

POST /v1/finance/entries              { "type": "EXPENSE", "categoryId": "...", "amount": 4500,
                                        "competenceDate": "2026-07-01", "isRecurring": true }
GET  /v1/finance/cashflow?months=6
POST /v1/finance/payroll              { "period": "2026-07", "items": [...] }
GET  /v1/finance/export?format=csv&period=2026-07
```

### 6.3 Configuração e marca

```http
GET   /v1/tenant/config
PATCH /v1/tenant/config               { "operation": { "serviceFeePercent": 12 } }
PATCH /v1/tenant/branding             { "colors": { "primary": "#C1121F" } }
POST  /v1/tenant/branding/logo        (multipart)
GET   /v1/tenant/thresholds
PATCH /v1/tenant/thresholds           { "orderWarnMinutes": 10 }
PATCH /v1/tenant/alert-routing        { "ORDER_LATE": { "groupWindowSeconds": 60 } }   # E-08/US-082/US-083

POST  /v1/notifications/subscribe     { "endpoint": "...", "keys": { "p256dh": "...", "auth": "..." } }
GET   /v1/notifications?status=unread # E-08/US-081 — central de notificações do usuário autenticado
```

### 6.4 Plataforma (Replay)

```http
POST /v1/platform/tenants             { "name": "...", "slug": "...", "plan": "...", "template": "PIZZERIA" }
→ 201 { "tenant": {...}, "installToken": "...", "installCommand": "./install.sh --tenant=... --token=..." }

GET  /v1/platform/installations
→ [ { tenantName, storeName, version, lastSeenAt, syncLagSeconds, health: "OK|DEGRADED|DOWN" } ]

POST /v1/platform/tenants/{id}/support-access   { "reason": "...", "durationMinutes": 60 }
→ 201 { "token": "...", "expiresAt": "..." }     # gera EVT-074, visível ao cliente

POST /v1/platform/tenants/{id}/import/menu      (multipart: planilha)
```

---

## 7. WebSocket

### 7.1 Conexão

```
wss://edge.local/rt?token=<jwt>
```

O servidor inscreve automaticamente nas salas conforme os claims:
`tenant:{id}` · `store:{id}` · `station:{id}` · `table:{id}` · `role:{papel}` · `user:{id}`

### 7.2 Mensagens do servidor

```json
{ "type": "order.placed",
  "data": { "orderId": "...", "code": "A47", "table": "12", "items": [...] },
  "occurredAt": "..." }

{ "type": "order.item.ready",
  "data": { "orderItemId": "...", "productName": "...", "table": "12" } }

{ "type": "product.unavailable",
  "data": { "variantId": "...", "reason": "OUT_OF_STOCK" } }

{ "type": "table.waiter_called",
  "data": { "tableId": "...", "label": "12" } }

{ "type": "alert.raised",
  "data": { "alertId": "...", "alertType": "ORDER_LATE", "severity": "HIGH", "entityType": "order",
            "entityId": "...", "message": "...", "groupKey": null, "count": null } }

{ "type": "alert.group_updated",
  "data": { "alertId": "...", "alertType": "ORDER_LATE", "severity": "HIGH", "groupKey": "...",
            "count": 5 } }

{ "type": "alert.resolved",
  "data": { "alertId": "...", "alertType": "ORDER_LATE" } }

{ "type": "sync.status",
  "data": { "online": false, "pendingEvents": 47, "lastSyncAt": "..." } }
```

> E-08: `alert.group_updated` (US-083 §10) nunca toca som — só atualiza a contagem de um grupo já
> aberto. `alert.resolved` (US-080 §4 "Resolução automática") tira o alerta da lista de pendentes
> do cliente sem exigir reconhecimento manual.

### 7.3 Mensagens do cliente

```json
{ "type": "subscribe",   "data": { "rooms": ["station:oven"] } }
{ "type": "ack",         "data": { "alertId": "..." } }
{ "type": "heartbeat" }
```

### 7.4 Resiliência

| Situação | Comportamento |
|---|---|
| Conexão cai | Reconexão com backoff (1s, 2s, 4s… teto 30s) |
| Reconexão | Cliente envia `lastEventId`; servidor reenvia o que perdeu |
| WebSocket indisponível | Fallback automático para polling a cada 5 s |
| Heartbeat | A cada 20 s; sem resposta em 60 s → reconecta |

> A cozinha nunca pode depender de uma única via de comunicação. O fallback de polling é requisito, não otimização.

---

## 8. API de sincronização

Usada exclusivamente entre edge e nuvem. Autenticação por chave assimétrica + HMAC.

```http
POST /v1/sync/push
X-Installation-Id: <uuid>
X-Signature: <hmac-sha256>
Content-Encoding: gzip
{
  "installationId": "...",
  "fromSeq": 148100,
  "toSeq": 148600,
  "events": [ { id, type, version, aggregateType, aggregateId, payload,
                actorId, deviceId, deviceSeq, occurredAt } ]
}
→ 200 {
    "acceptedUntilSeq": 148600,
    "duplicates": 3,
    "rejected": [ { "eventId": "...", "reason": "SCHEMA_INVALID" } ],
    "conflicts": [ { "eventId": "...", "resolution": "KEPT_REMOTE" } ]
  }

GET /v1/sync/pull?cursor=98220&limit=500
→ { "events": [...], "nextCursor": 98720, "hasMore": true }

GET /v1/sync/health
→ { "serverTime": "...", "expectedVersion": "1.4.2", "configVersion": 88 }
```

### 8.1 Garantias

| Garantia | Mecanismo |
|---|---|
| Sem perda | Outbox persistido na mesma transação do estado |
| Sem duplicação | Upsert por `event.id` (chave primária) |
| Ordem | `deviceSeq` monotônico por instalação |
| Retomada | Cursor persistido dos dois lados |
| Integridade | HMAC por requisição; rejeição registrada |
| Horário correto | `occurredAt` preservado; `recordedAt` atribuído na nuvem |

---

## 9. Idempotência

Toda escrita exige `Idempotency-Key`. O servidor guarda a chave por 24 h com a resposta original.

```
1ª chamada  → processa, grava (key → response), retorna 201
2ª chamada  → retorna a resposta gravada, com header Idempotent-Replay: true
```

**Por que é inegociável aqui:** em rede instável, o garçom toca "enviar", perde o sinal e toca de novo. Sem idempotência, a cozinha recebe duas pizzas.

---

## 10. Rate limiting

| Escopo | Limite |
|---|---|
| Rotas públicas por IP | 60 req/min |
| Criação de pedido público por sessão de mesa | 10 req/min |
| Autenticadas por usuário | 300 req/min |
| KDS advance por dispositivo | 120 req/min |
| Sync push por instalação | 60 req/min |

---

## 11. Documentação e contrato vivo

| Item | Ferramenta |
|---|---|
| Especificação | OpenAPI 3.1 gerado via `Microsoft.AspNetCore.OpenApi`/Swashbuckle a partir dos controllers ASP.NET Core |
| Publicação | `/docs` em ambiente não produtivo |
| Tipos do cliente | Gerados a partir do OpenAPI publicado por `Nexora.Contracts` — front e back nunca divergem |
| Validação | FluentValidation nos DTOs de `Nexora.Contracts`, tanto na entrada quanto na saída |
| Testes de contrato | Snapshot do OpenAPI versionado; PR que quebra contrato falha no CI |

---

*Documento 05 do pacote 004_DonaBetinha. Replay Studio.*
