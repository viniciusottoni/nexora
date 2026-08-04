# ADR-008 · Saldo de estoque derivado de movimentos

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO |
| **Relacionados** | ADR-006, ADR-007 |
| **Requisitos afetados** | RF-EST-04 a 08, RN-007, RN-008, RNF-OFF-08 |

---

## Contexto

Com a arquitetura local-first (ADR-001), quase todos os dados têm dono único: pedido nasce na loja, catálogo nasce na nuvem. Não há conflito.

**Estoque é a exceção.** Considere:

```
20h05  Loja (offline)  → produz 12 pizzas → baixa 2,16 kg de mussarela
20h30  Nuvem (online)  → comprador registra entrada de 20 kg de mussarela
21h15  Loja reconecta  → sincroniza
```

Se sincronizássemos **saldo**, teríamos dois números conflitantes e qualquer resolução (último vence, maior vence, média) destruiria informação real.

Esse é também o dado mais sensível do produto: dele saem o CMV, o custo por produto e a margem — os números que respondem *"como está a saúde financeira"*.

## Decisão

**Nunca sincronizar saldo. Sincronizar movimentos.**

`ingredient.current_stock` é um campo **materializado por conveniência de leitura**, sempre recalculável a partir de `stock_movement`. Cada movimento tem identidade própria (UUIDv7) e é idempotente.

## Detalhamento

```sql
-- a verdade
SELECT COALESCE(SUM(quantity), 0) AS saldo
FROM stock_movement
WHERE tenant_id = $1 AND ingredient_id = $2;

-- o materializado, recalculado periodicamente e após cada sync
UPDATE ingredient SET current_stock = (...) WHERE id = $2;
```

### Tipos de movimento

| Tipo | Sinal | Origem |
|---|---|---|
| `PURCHASE` | + | Nuvem |
| `PRODUCTION` | − | Edge (baixa por ficha técnica) |
| `WASTE` | − | Ambos |
| `ADJUSTMENT` | ± | Ambos (com autorização) |
| `COUNT` | ± | Contagem cíclica |
| `TRANSFER` | ± | Entre lojas (futuro) |
| `RETURN` | + | Devolução a fornecedor |

### O conflito deixa de existir

No exemplo do contexto, após a sincronização existem simplesmente **dois movimentos**:

```
20h05  PRODUCTION  −2,160 kg   (origem: edge)
20h30  PURCHASE   +20,000 kg   (origem: nuvem)
```

O saldo é a soma. Não há conflito — há apenas ordem de aplicação, e a ordem correta é dada por `occurredAt` (ADR-034).

### Momento da baixa

Definido em RN-007: a baixa ocorre na **conclusão da produção** do item (`order.item.ready`), não no lançamento do pedido. Motivo: pedido cancelado antes de iniciar não deve consumir insumo; item cancelado depois de iniciado gera `WASTE`, não estorno (RN-008).

### Baixa proporcional em meio a meio

```
item com frações [Mussarela 0,5 · Calabresa 0,5]
  → baixa 0,5 × ficha(Mussarela)  +  0,5 × ficha(Calabresa)
```

Este é o ponto em que a maioria dos sistemas de pizzaria erra, e é o que faz o CMV ficar incorreto de forma silenciosa.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Sincronizar o saldo | Simples; menos linhas | Sobrescrita destrói informação; impossível auditar | Erro conceitual — o saldo é derivado, não é fato |
| Last-write-wins no saldo | Trivial | Perde todas as baixas feitas offline | Destruiria o CMV |
| Bloquear operação de estoque offline | Sem conflito | A loja pararia de produzir sem internet | Viola ADR-001 |
| Reserva otimista com compensação | Preciso | Complexidade alta; exige coordenação online | Desnecessário: movimentos já resolvem |

## Consequências

**Positivas**

- O único conflito real de sincronização deixa de existir
- Auditabilidade completa: toda alteração de estoque tem origem, autor e horário
- CMV teórico × real fica confiável — é a métrica mais reveladora do produto
- Contagem cíclica passa a ser apenas mais um tipo de movimento

**Negativas**

- Tabela de movimentos cresce continuamente
- O campo materializado pode ficar momentaneamente defasado
- Consulta de saldo histórico exige agregação

**Mitigações**

- Recálculo do materializado após cada lote de sync e em job noturno
- Agregação mensal por insumo para consultas históricas
- Query de integridade diária compara materializado com a soma dos movimentos

## Como validar

```sql
-- integridade: materializado igual à soma dos movimentos
SELECT i.id, i.current_stock, COALESCE(SUM(m.quantity),0) AS calculado
FROM ingredient i
LEFT JOIN stock_movement m ON m.ingredient_id = i.id
GROUP BY i.id, i.current_stock
HAVING i.current_stock <> COALESCE(SUM(m.quantity),0);
```

Teste de caos C-06: compra na nuvem e baixa offline no mesmo insumo — ambos aplicados, saldo correto.

## Revisitar quando

- O volume de movimentos exigir estratégia de snapshot periódico (saldo fechado por mês, movimentos posteriores somados sobre ele)
