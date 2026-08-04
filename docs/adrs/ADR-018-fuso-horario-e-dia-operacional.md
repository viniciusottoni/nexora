# ADR-018 · Fuso horário e conceito de dia operacional

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO |
| **Relacionados** | ADR-012, ADR-016, ADR-034 |
| **Requisitos afetados** | RF-BI-05 a 08, RF-CXA-06, RN-020 |

---

## Contexto

Uma pizzaria abre às 18h e fecha às 2h da manhã. Um pedido feito à 1h30 de sábado pertence, do ponto de vista do negócio, **à sexta-feira**. Se o sistema fechar o dia à meia-noite:

- O faturamento de sexta aparece partido em dois dias
- O caixa aberto às 18h de sexta fecha "no sábado"
- O comparativo "mesma sexta do mês passado" fica errado
- O relatório da madrugada mostra dois dias com movimento pela metade

Esse é um erro clássico de sistemas de restaurante e é difícil de corrigir depois, porque contamina todo o histórico.

Há ainda a questão do fuso: o Brasil tem múltiplos fusos, e o produto é replicável para qualquer estabelecimento.

## Decisão

**Todo timestamp é armazenado em UTC (`TIMESTAMPTZ`). Toda agregação de negócio usa o conceito de `business_day`, calculado a partir do horário de virada configurado por estabelecimento.**

## Detalhamento

### Armazenamento

| Camada | Formato |
|---|---|
| Banco | `TIMESTAMPTZ`, sempre UTC |
| API | ISO 8601 com offset — `2026-07-31T23:47:12.334Z` |
| Aplicação | `Date` em UTC; conversão apenas na apresentação |
| Exibição | Fuso do tenant (`tenant.timezone`, ex.: `America/Sao_Paulo`) |

**Nunca** armazenar horário local sem fuso. É a origem de erro mais comum e a mais difícil de reverter.

### Dia operacional

```ts
// tenant_config.operation.businessDayStartHour = 5   (padrão: 5h da manhã)

export function businessDay(occurredAt: Date, tz: string, startHour: number): string {
  const local = utcToZonedTime(occurredAt, tz);
  const shifted = subHours(local, startHour);
  return format(shifted, 'yyyy-MM-dd');
}

// pedido às 01h30 de sábado, virada às 5h → business_day = sexta-feira
// pedido às 19h00 de sexta,  virada às 5h → business_day = sexta-feira ✔
```

### Onde `business_day` é obrigatório

| Contexto | Uso |
|---|---|
| Agregados de métrica | `metric_daily.business_day` |
| Sequência de código curto | Reinicia por dia operacional (ADR-016) |
| Fechamento de caixa | Vinculado ao dia operacional |
| Relatórios e comparativos | Sempre por dia operacional |
| Metas diárias | Idem |

### Onde o horário real (UTC) é obrigatório

| Contexto | Uso |
|---|---|
| Cálculo de duração | `served_at − placed_at` |
| Ordenação de eventos | Sempre por `occurred_at` |
| Agregação por hora | Hora local do tenant, derivada do UTC |
| Auditoria | Momento exato do fato |

### Horário de verão

`TIMESTAMPTZ` com fuso IANA trata automaticamente. Em transição, um dia operacional pode ter 23 ou 25 horas — comportamento correto e esperado. Agregados por hora precisam lidar com hora repetida ou inexistente; os testes cobrem os dois casos.

### Configuração por tenant

```json
{
  "timezone": "America/Sao_Paulo",
  "businessDayStartHour": 5,
  "operatingHours": { "fri": { "open": "18:00", "close": "02:00" } }
}
```

Uma padaria configuraria virada às 3h; um restaurante de almoço, às 4h. É parâmetro do produto, não constante de código (ADR-013).

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Dia civil (00h–24h) | Trivial | Parte o movimento da noite em dois dias | Erro de negócio grave e permanente no histórico |
| Vincular o dia à sessão de caixa | Reflete a operação real | Caixa pode ser reaberto ou ter mais de um turno | Ambíguo; dificulta comparativo |
| Armazenar em horário local | Leitura direta | Ambíguo no horário de verão; impede multi-fuso | Erro clássico e irreversível |
| Virada fixa às 5h para todos | Simples | Não serve a padaria, food truck, restaurante de almoço | Viola a diretriz de produto configurável |

## Consequências

**Positivas**

- Faturamento da noite fica íntegro em um único dia
- Comparativos "mesmo dia da semana" ficam corretos
- Fechamento de caixa alinhado ao turno real
- Multi-fuso funciona sem mudança de código

**Negativas**

- `business_day` precisa ser calculado e persistido em toda entidade agregável
- Consulta por "hoje" exige a função, não `CURRENT_DATE`
- Casos de horário de verão exigem teste específico

**Mitigações**

- `business_day` é coluna materializada, calculada na escrita — nunca em tempo de consulta
- Helper único em `packages/domain/time`; uso direto de `CURRENT_DATE` bloqueado por lint
- Testes cobrindo virada de dia, horário de verão e fuso diferente do servidor

## Como validar

- Teste: pedido às 01h30 de sábado é contabilizado na sexta
- Teste: caixa aberto às 18h e fechado às 03h pertence a um único dia operacional
- Teste: transição de horário de verão não duplica nem perde hora nos agregados
- Teste: tenant em fuso diferente do servidor agrega corretamente

## Revisitar quando

- O produto atender estabelecimento que opere 24 horas sem virada natural
