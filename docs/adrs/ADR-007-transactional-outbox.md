# ADR-007 · Sincronização por transactional outbox

| | |
|---|---|
| **Status** | Substituído |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead |
| **Substituído por** | [ADR-040](./ADR-040-arquitetura-100-online-api-unica.md) |
| **Relacionados** | ADR-001, ADR-006, ADR-008, ADR-020, ADR-034 |
| **Requisitos afetados** | RF-OFF-02, RF-OFF-03, RF-OFF-07, RNF-OFF-02, RNF-PER-07/08 |

---

> ⚠️ **Substituído em 06/08/2026 pelo [ADR-040](./ADR-040-arquitetura-100-online-api-unica.md).** Sem edge, não há mais duas pontas a sincronizar — outbox, worker de envio e recepção idempotente entre edge e nuvem deixam de existir. `domain_event` e `audit_log` continuam vigentes (ADR-006), não são específicos deste padrão. Conteúdo mantido como registro histórico.

## Contexto

O ADR-001 estabeleceu que a loja opera offline e sincroniza depois. A pergunta que resta é: **como garantir que nenhum pedido se perca e nenhum duplique**?

O padrão ingênuo — salvar no banco e depois publicar em uma fila — tem uma janela de falha real: se o processo cair entre o commit e o publish, o evento existe no estado mas nunca chega à nuvem. Em uma loja com queda de energia no meio do serviço, isso não é hipótese teórica.

O inverso (publicar antes de salvar) é pior: pode publicar algo que a transação depois desfez.

## Forças em jogo

| Força | Descrição |
|---|---|
| Zero perda | RPO igual a zero para evento confirmado localmente (RNF-DIS-05) |
| Zero duplicação | Cozinha não pode receber duas pizzas por causa de reenvio |
| Ordem | Métrica e estado dependem de aplicar eventos na ordem certa |
| Retomada | Após 6 h offline, sincronizar 4.000 eventos em menos de 5 min |
| Simplicidade operacional | Não queremos operar um broker de mensagens em cada loja |

## Decisão

**Transactional outbox:** o evento é gravado na tabela `outbox` **dentro da mesma transação** do estado. Um worker lê o outbox em ordem de sequência e envia em lotes idempotentes, com cursor persistido nos dois lados.

## Detalhamento

### Escrita

```ts
await prisma.$transaction(async (tx) => {
  await tx.orderItem.update({ ... });        // estado
  const event = await tx.domainEvent.create({ ... });   // log (ADR-006)
  await tx.outbox.create({                    // fila de saída
    data: { eventId: event.id, status: 'PENDING', deviceSeq: nextSeq() },
  });
});
```

As três operações são atômicas. Se a transação falhar, nada existe. Se tiver sucesso, o evento **está** na fila — não há janela.

### Envio

```
Worker (a cada 2 s, ou imediato ao detectar evento novo)
  ├─ SELECT ... FROM outbox WHERE status='PENDING' ORDER BY device_seq LIMIT 500
  ├─ POST /v1/sync/push  (gzip, assinado por HMAC)
  ├─ resposta: acceptedUntilSeq
  └─ UPDATE outbox SET status='SYNCED' WHERE device_seq <= acceptedUntilSeq
```

### Recepção (nuvem)

```sql
INSERT INTO domain_event (id, ...) VALUES (...)
ON CONFLICT (id) DO NOTHING;     -- idempotência por chave primária
```

O `id` é UUIDv7 gerado **na origem** (ADR-016). Reenviar o mesmo lote não duplica nada.

### Parâmetros

| Parâmetro | Valor | Justificativa |
|---|---|---|
| Intervalo de push | 2 s | Painel do dono quase em tempo real |
| Intervalo de pull | 30 s | Catálogo muda pouco |
| Lote | 500 eventos ou 1 MB | Equilíbrio entre latência e overhead |
| Retry | Backoff exponencial 2s → 5 min | Não martelar servidor indisponível |
| Alerta de atraso | > 5 min | RNF-OFF-07 |
| Retenção de outbox sincronizado | 30 dias | Depuração e reprocessamento |

### Direção por domínio

| Dado | Direção | Conflito |
|---|---|---|
| Pedido, pagamento, caixa, mesa | Loja → Nuvem | Não (loja é dona) |
| Catálogo, preço, configuração, ficha técnica | Nuvem → Loja | Não (nuvem é dona) |
| Movimento de estoque | Ambos | Resolvido por ADR-008 |

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Publicar em fila após o commit | Simples | Janela de falha entre commit e publish | Perde evento em queda de energia — inaceitável |
| Broker em cada loja (Kafka, RabbitMQ) | Garantias fortes | Um broker por loja para operar e monitorar | Complexidade operacional desproporcional |
| Replicação lógica do PostgreSQL | Nativa; sem código | Acopla schemas; não permite filtrar nem transformar por domínio | Impediria direções diferentes por domínio |
| CRDTs / replicação bidirecional automática | Convergência automática | Semântica de negócio não é comutativa (autorização, estoque, caixa) | Complexidade muito alta e semanticamente errada aqui |
| Sincronizar estado em vez de eventos | Conceitualmente simples | Sobrescrita destrói informação; sem ordem nem auditoria | Perde tudo o que o ADR-006 construiu |

## Consequências

**Positivas**

- Entrega ao menos uma vez **mais** idempotência resulta, na prática, em exatamente uma vez
- Retomada automática após qualquer interrupção
- Sem infraestrutura adicional na loja — apenas uma tabela
- Totalmente auditável: dá para ver o que saiu, quando e o que a nuvem aceitou

**Negativas**

- Latência de segundos até a nuvem (aceitável: painel remoto não é tempo real crítico)
- Tabela `outbox` exige limpeza periódica
- Worker é um ponto de falha a monitorar
- Ordem global entre múltiplas lojas não existe (nem é necessária)

**Mitigações**

- Alerta se a fila passar de 500 pendentes ou o atraso passar de 5 min (RNF-OBS)
- Job de limpeza diário do outbox sincronizado
- Health check do worker reportado à nuvem a cada 60 s

## Como validar

- Teste de caos C-02: 4.000 eventos após 6 h offline sincronizam em menos de 5 min
- Teste de caos C-03: queda no meio do lote — retoma do último confirmado
- Teste de caos C-04: reenvio do mesmo lote — zero duplicados
- Query de integridade: nenhum outbox `PENDING` com mais de 10 min quando há conexão

## Revisitar quando

- Uma loja gerar volume que torne o lote de 500 insuficiente
- Surgir necessidade de sincronização entre lojas da mesma rede (multi-loja)
