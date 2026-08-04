# ADR-020 · Idempotência obrigatória em toda escrita

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead |
| **Relacionados** | ADR-007, ADR-016 |
| **Requisitos afetados** | RF-OFF-03, RNF-OFF-03 |

---

## Contexto

O cenário é concreto e frequente: o garçom toca "enviar pedido", o Wi-Fi oscila, a tela não confirma, ele toca de novo. Sem idempotência, a cozinha recebe **duas pizzas**.

O mesmo vale para pagamento (cobrar duas vezes), abertura de caixa, baixa de estoque e sincronização de lote. Em rede instável — que é a premissa do produto — o reenvio não é exceção, é rotina.

## Decisão

**Toda operação de escrita (POST, PUT, PATCH) exige o header `Idempotency-Key`.** O servidor armazena a chave com a resposta original por 24 horas e retorna a mesma resposta em reenvio.

## Detalhamento

### Contrato

```http
POST /v1/orders
Idempotency-Key: 018f2c4a-7b3e-7000-8000-1a2b3c4d5e6f
{ ... }

1ª chamada  → processa, grava (key → resposta), retorna 201
2ª chamada  → retorna a resposta gravada
              Idempotent-Replay: true
```

A chave é gerada **pelo cliente**, no momento em que a intenção nasce — não a cada tentativa de envio. Se o usuário toca "enviar" duas vezes, é a mesma intenção, logo a mesma chave.

### Armazenamento

```sql
CREATE TABLE idempotency_key (
  key            TEXT PRIMARY KEY,
  tenant_id      UUID NOT NULL,
  endpoint       TEXT NOT NULL,
  request_hash   TEXT NOT NULL,
  response_status INT,
  response_body  JSONB,
  status         TEXT NOT NULL,      -- IN_PROGRESS | COMPLETED
  created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
  expires_at     TIMESTAMPTZ NOT NULL
);
```

### Casos tratados

| Caso | Comportamento |
|---|---|
| Mesma chave, mesmo payload | Retorna a resposta original |
| Mesma chave, payload diferente | `422 IDEMPOTENCY_KEY_REUSED` — indica erro do cliente |
| Chave em processamento (concorrência) | `409 REQUEST_IN_PROGRESS`, cliente reenvia com backoff |
| Chave expirada (> 24 h) | Trata como requisição nova |
| Requisição original falhou com 5xx | Não armazena — permite nova tentativa real |

O `request_hash` existe justamente para detectar reuso indevido: reaproveitar chave com corpo diferente é bug do cliente e precisa falhar de forma visível, não silenciosa.

### Implementação

```ts
@Injectable()
export class IdempotencyInterceptor implements NestInterceptor {
  // 1. lê o header (obrigatório em métodos de escrita)
  // 2. tenta INSERT com status IN_PROGRESS  → se conflitar, resolve conforme a tabela acima
  // 3. executa o handler
  // 4. grava resposta e marca COMPLETED
}
```

### Duas camadas de idempotência

Este ADR trata da **camada de API**. A **camada de sincronização** (ADR-007) tem sua própria idempotência, pela chave primária do evento:

```sql
INSERT INTO domain_event (id, ...) VALUES (...) ON CONFLICT (id) DO NOTHING;
```

São mecanismos complementares: um protege a requisição HTTP, o outro protege o transporte de eventos.

### Escopo

| Método | Exige chave |
|---|---|
| POST | Sim |
| PATCH, PUT | Sim |
| DELETE | Sim (soft delete é escrita) |
| GET, HEAD | Não |

Exceção: endpoints de sincronização, que usam o mecanismo do ADR-007.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Sem idempotência, confiar no cliente | Simples | Duplicação garantida em rede instável | Inaceitável na premissa do produto |
| Deduplicação por conteúdo (hash do payload) | Sem header | Dois pedidos legitimamente idênticos seriam colapsados | Mesa pede duas pizzas iguais — é intenção real |
| Deduplicação por janela de tempo | Simples | Heurística; erra nos dois sentidos | Não determinístico |
| Idempotência só em pagamento | Menos esforço | Pedido duplicado é igualmente grave | Meia solução |
| ID do recurso gerado pelo cliente como chave | Elegante | Não cobre operações sem recurso novo (fechar caixa) | Cobertura parcial |

## Consequências

**Positivas**

- Reenvio nunca duplica pedido, pagamento ou movimento de estoque
- Cliente pode reenviar com segurança sem lógica própria de verificação
- Reuso indevido de chave falha de forma visível, não silenciosa
- Habilita a fila offline do cliente (ADR-027) sem risco

**Negativas**

- Tabela adicional com limpeza periódica
- Uma escrita extra por requisição
- Cliente precisa gerar e persistir a chave junto da intenção

**Mitigações**

- Limpeza diária de chaves expiradas
- Índice em `expires_at` para purga eficiente
- Cliente gera a chave no momento em que a ação é enfileirada, e a mantém entre tentativas

## Como validar

- Teste: mesma chave duas vezes → um único pedido, `Idempotent-Replay: true` na segunda
- Teste: mesma chave com payload diferente → 422
- Teste: duas requisições concorrentes com a mesma chave → uma processa, outra recebe 409
- Cenário C-10: dois dispositivos avançam o mesmo item — um único avanço

## Revisitar quando

- O volume de chaves exigir armazenamento em Redis em vez de tabela
