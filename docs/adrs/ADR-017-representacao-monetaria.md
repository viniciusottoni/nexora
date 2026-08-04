# ADR-017 · Representação monetária e regra de arredondamento

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO |
| **Relacionados** | ADR-005, ADR-008 |
| **Requisitos afetados** | RF-SAL-10, RF-CXA-02 a 05, RF-EST-13, RF-FIN-* |

---

## Contexto

O sistema calcula dinheiro em vários pontos sensíveis: preço de pizza meio a meio, divisão de conta entre pessoas, taxa de serviço, desconto percentual, custo por ficha técnica, margem por produto, CMV.

Erro de centavo em qualquer um deles é grave por dois motivos: quebra a conciliação de caixa (RF-CXA-08) e destrói a confiança do dono no número — que é o produto que estamos vendendo.

O problema clássico do ponto flutuante:

```js
0.1 + 0.2                    // 0.30000000000000004
45.90 * 3                    // 137.70000000000002
(180 / 3).toFixed(2)         // arredondamentos que não somam de volta
```

O mesmo problema existe em C# com `double`/`float` — o motivo de proibir ponto flutuante para dinheiro não muda com a troca de stack, só a ferramenta que resolve.

## Decisão

**Dinheiro é armazenado como `NUMERIC(12,2)` no PostgreSQL e manipulado como `decimal` nativo do C# na aplicação. `double`/`float` são proibidos para valores monetários.**

Arredondamento: **half-up** (0,005 → 0,01), com **conciliação obrigatória** em toda divisão.

## Detalhamento

### Tipos

| Contexto | Tipo |
|---|---|
| PostgreSQL — dinheiro | `NUMERIC(12,2)` |
| PostgreSQL — quantidade de insumo | `NUMERIC(14,4)` (precisão de grama e mililitro) |
| PostgreSQL — percentual | `NUMERIC(6,3)` |
| Aplicação (C#) | `decimal` nativo |
| API (JSON) | **string** — ex.: `"45.90"` |
| Exibição | Formatado por `Intl.NumberFormat('pt-BR')` no frontend |

> Em JSON, dinheiro trafega como string. Serializar como número reintroduz o problema do ponto flutuante no cliente TypeScript — motivo pelo qual `Nexora.Contracts` usa um `JsonConverter<decimal>` customizado (`System.Text.Json`) que serializa e desserializa `decimal` sempre como string, nunca como `JsonNumber`.

### Regra de conciliação em divisão

Toda operação que divide um valor precisa **somar de volta exatamente ao original**. A sobra de centavos vai para a primeira parcela.

```csharp
public static IReadOnlyList<decimal> SplitAmount(decimal total, int parts)
{
    var baseAmount = Math.Floor(total / parts * 100) / 100;   // ROUND_DOWN em 2 casas
    var result = Enumerable.Repeat(baseAmount, parts).ToArray();
    var remainder = total - baseAmount * parts;               // ex.: 0,01
    result[0] += remainder;
    return result;
}

// 100,00 ÷ 3  →  [33.34, 33.33, 33.33]   soma = 100,00 ✔
```

### Ordem das operações no cálculo da conta

A ordem importa e é normativa:

```
1. preço unitário × quantidade          → subtotal do item
2. + modificadores                      → total do item
3. Σ itens                              → subtotal do pedido
4. − desconto                           → base
5. + taxa de serviço (sobre a base)     → total
6. divisão entre pessoas (com conciliação)
```

Arredondamento ocorre **apenas** ao final de cada etapa numerada, nunca em cálculos intermediários.

### Custo e margem

Custo de insumo usa 4 casas (`NUMERIC(14,4)`) porque frações de grama importam no CMV. O arredondamento para 2 casas ocorre somente na apresentação e no lançamento financeiro.

```
mussarela: 0,1800 kg × R$ 42,3500/kg = R$ 7,6230  → exibe R$ 7,62
```

Se arredondássemos a 2 casas por insumo, o custo de uma pizza com 8 insumos acumularia erro relevante e o CMV ficaria sistematicamente errado.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| `float` / `double` | Nativo, rápido | Erro de representação inevitável | Inaceitável para dinheiro |
| Inteiro em centavos | Exato; simples | Insuficiente para custo com 4 casas; converte em toda fronteira | Bom para dinheiro puro, ruim para custo de insumo |
| `Money` do PostgreSQL | Tipo dedicado | Depende de locale; suporte fraco em ORMs | Frágil e pouco portável |
| `NUMERIC` no banco e `double` na aplicação | Menos cerimônia | Perde precisão exatamente onde o cálculo acontece | Move o problema, não resolve |

## Consequências

**Positivas**

- Zero erro de arredondamento em conta, divisão, taxa e desconto
- Divisão sempre soma de volta ao total — a conciliação de caixa fecha
- CMV e margem confiáveis, sem erro acumulado
- Comportamento idêntico no edge, na nuvem e no navegador

**Negativas**

- Conversões nas fronteiras (banco, API, exibição) ainda exigem atenção, mesmo com `decimal` nativo do C# suportando operadores aritméticos diretos (`+`, `-`, `*`, `/`)
- No frontend TypeScript, dinheiro continua exigindo uma lib de precisão decimal (decimal.js) para não reintroduzir o problema do ponto flutuante ao consumir a string vinda da API

**Mitigações**

- Helpers centralizados em `Nexora.Domain/Money` (arredondamento half-up, conciliação de divisão)
- Analyzer Roslyn/regra de `Nexora.ArchitectureTests` proibindo `double`/`float` em propriedades de domínio marcadas como monetárias
- `JsonConverter<decimal>` customizado em `Nexora.Contracts` cobre serialização/desserialização automática nos DTOs

## Como validar

- Teste D-07: divisão de conta soma exatamente o total, em 1.000 combinações aleatórias
- Teste de propriedade: `splitAmount(t, n).reduce(sum) === t` para qualquer `t` e `n`
- Teste: pizza com 8 insumos — custo calculado com 4 casas difere do arredondado por insumo, e o correto é o primeiro
- Conciliação diária D-02: faturamento igual à soma dos pagamentos, ao centavo

## Revisitar quando

- O produto precisar operar em moeda com número diferente de casas decimais
