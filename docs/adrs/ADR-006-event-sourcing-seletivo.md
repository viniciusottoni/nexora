# ADR-006 · Event sourcing seletivo, não completo

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO |
| **Relacionados** | ADR-007, ADR-012, ADR-034, ADR-035 |
| **Requisitos afetados** | RF-PED-02, RF-PED-03, RF-AUD-01, RF-BI-*, RF-OFF-* |

---

## Contexto

Três exigências do produto convergem para eventos:

1. **Métrica total** — cada etapa cronometrada, com rastreabilidade do número até a origem (RF-BI-11)
2. **Auditoria** — quem fez o quê, quando, com valores antes e depois (RF-AUD)
3. **Sincronização** — algo precisa trafegar entre loja e nuvem de forma ordenada e idempotente (ADR-007)

Eventos resolvem as três de uma só vez. Mas event sourcing **puro** — em que o estado é sempre reconstruído por replay — adicionaria complexidade desproporcional a consultas triviais como "quais mesas estão abertas agora", que o KDS e o mapa de salão fazem dezenas de vezes por minuto.

## Decisão

**Modelo híbrido:** tabelas de estado tradicionais **e** um log `domain_event` append-only. Toda transição grava as duas coisas **na mesma transação**.

- O **estado** serve a operação (consulta rápida e simples)
- O **log** serve métrica, auditoria e sincronização

## Detalhamento

```ts
async function fireItem(itemId: string, actor: Actor, ctx: Ctx) {
  return withTenant(ctx.tenantId, async (tx) => {
    // 1. estado
    const item = await tx.orderItem.update({
      where: { id: itemId },
      data: { status: 'FIRED', firedAt: ctx.occurredAt },
    });

    // 2. evento — MESMA transação
    await appendEvent(tx, {
      type: 'order.item.fired',
      aggregateType: 'OrderItem',
      aggregateId: itemId,
      payload: { stationId: item.stationId, operatorId: actor.id },
      actorId: actor.id,
      deviceId: ctx.deviceId,
      occurredAt: ctx.occurredAt,
    });

    return item;
  });
}
```

### Regra normativa

> **Nenhuma transição de estado pode ocorrer sem emitir seu evento correspondente** (doc. 04). Estado sem evento é métrica perdida, auditoria incompleta e dado que não sincroniza.

### O que gera evento

| Gera evento | Não gera evento |
|---|---|
| Toda transição de máquina de estado | Consultas |
| Toda ação sensível (desconto, cancelamento, ajuste) | Login de leitura |
| Todo movimento de estoque | Cálculos derivados |
| Todo pagamento e fechamento | Renderização |

### Reconstrução

O estado **não** é reconstruído por replay em operação normal. Mas o log permite:

- Recalcular qualquer agregado de métrica do zero (ADR-012)
- Auditar divergência entre estado e histórico
- Calcular métrica nova sobre histórico já existente, sem nova coleta

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Event sourcing puro (CQRS completo) | Auditoria perfeita; estado sempre derivável | Toda consulta operacional exige projeção; complexidade alta | Custo desproporcional para consultas triviais em time pequeno |
| Apenas estado, sem log | Simples | Sem métrica retroativa, sem auditoria completa, sem unidade de sincronização | Falha nos três requisitos centrais |
| Log apenas para auditoria (tabela separada) | Simples | Duplicaria o mecanismo para sincronização e métrica | Três mecanismos onde um resolve |
| CDC (change data capture) do banco | Sem código de aplicação | Captura mudança física, não intenção de negócio; sem autor nem contexto | Perde `actorId`, `deviceId` e semântica — inútil para auditoria e métrica |

## Consequências

**Positivas**

- Consulta operacional continua simples e rápida
- Auditoria vem de graça, sem mecanismo próprio
- Métrica nova pode ser calculada sobre histórico existente
- Sincronização ganha unidade natural de transporte
- Rastreabilidade do indicador até o evento (RF-BI-11)

**Negativas**

- Dupla escrita exige disciplina — é possível gravar estado sem evento
- Volume de eventos cresce (3 a 8 mil por dia por loja) e exige particionamento
- Payload duplica informação já presente no estado

**Mitigações**

- Teste de integração verifica emissão em **toda** transição (doc. 10, §4.2)
- Verificação diária de integridade: estado sem evento correspondente gera alerta (RNF-OBS-08)
- Particionamento mensal e política de retenção em ADR-035
- Payload contém apenas o delta, não o objeto inteiro (doc. 04, R4)

## Como validar

```sql
-- nenhum pedido sem evento de origem
SELECT o.id FROM "order" o
LEFT JOIN domain_event e ON e.aggregate_id = o.id AND e.type = 'order.placed'
WHERE e.id IS NULL;
```

Roda diariamente; resultado não vazio é incidente S1.

## Revisitar quando

- O volume de eventos tornar a dupla escrita um gargalo de throughput
- Surgir necessidade de reconstrução de estado por replay em operação normal
