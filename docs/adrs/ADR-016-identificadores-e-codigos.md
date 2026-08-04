# ADR-016 · UUIDv7 como identificador e código curto de pedido

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead |
| **Relacionados** | ADR-007, ADR-020, ADR-034 |
| **Requisitos afetados** | RF-OFF-03, RF-KDS-04, RNF-OFF-03 |

---

## Contexto

Duas necessidades distintas e frequentemente confundidas:

1. **Identidade técnica** — precisa ser gerada **offline, na origem**, para que o evento possa ser idempotente (ADR-007). Um `SERIAL` do banco não serve: o edge não pode consultar a nuvem para obter um número.
2. **Identidade humana** — a cozinha precisa digitar o número do pedido no teclado numérico (RF-KDS-04). Ninguém digita um UUID.

## Decisão

**UUIDv7 como chave primária de todas as entidades**, gerado na origem.
**Código curto sequencial por loja e por dia operacional** para uso humano.

## Detalhamento

### UUIDv7

```
018f2c4a-7b3e-7000-8000-1a2b3c4d5e6f
└─── timestamp ms ───┘ └─ versão ─┘ └─ aleatório ─┘
```

Vantagens sobre UUIDv4:

| Propriedade | Efeito |
|---|---|
| Ordenável por tempo | Índice B-tree não fragmenta; inserções vão para o fim |
| Timestamp embutido | Permite ordenação e depuração sem coluna extra |
| Gerado na origem | Essencial para idempotência offline |
| Sem coordenação | Sem risco prático de colisão |

```ts
import { uuidv7 } from 'uuidv7';
const id = uuidv7();      // gerado no edge, sem consultar nada
```

### Código curto do pedido

```
Formato: <letra do dia><sequência>     ex.: A47, B12
```

| Regra | Valor |
|---|---|
| Escopo | Por loja e por **dia operacional** (ADR-018) |
| Reinício | A cada abertura de dia operacional |
| Sequência | 1 a 999, com prefixo de letra rotativa por dia |
| Geração | No edge, com sequência local — não precisa da nuvem |
| Unicidade | Garantida por `UNIQUE (tenant_id, store_id, business_day, short_code)` |

O código curto é **apresentação**, nunca chave estrangeira. Nenhuma tabela referencia um pedido por `short_code`.

### Por que sequência local não é problema

Cada loja tem seu próprio contador, e o código é único apenas dentro do dia operacional daquela loja. Como a cozinha só enxerga a própria fila, não há ambiguidade. Na nuvem, a identidade continua sendo o UUID.

### Digitação no KDS

```
Operador digita: 4 7 [Enter]
Sistema resolve: short_code='A47' AND business_day=hoje AND store=esta
                 → avança o item para o próximo estado
```

Se o código não existir na fila atual, retorno visual de erro sem travar a tela.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| `SERIAL` / `BIGSERIAL` | Compacto; legível | Exige o banco central para gerar | Impossível offline |
| UUIDv4 | Universal | Aleatório: fragmenta índice e não ordena | Pior desempenho de escrita sem ganho |
| ULID | Ordenável; compacto em texto | Menos suporte nativo em Postgres que UUID | UUIDv7 entrega o mesmo com tipo nativo |
| Snowflake ID | Compacto; ordenável | Exige coordenação de `worker_id` por instalação | Complexidade de operação em parque distribuído |
| Prefixo de loja + sequência como PK | Legível | Acopla identidade a organização física | Migração de loja quebraria referências |

## Consequências

**Positivas**

- Identidade gerada offline — pré-requisito da idempotência (ADR-020)
- Índices não fragmentam com o tempo
- Ordenação cronológica natural sem coluna adicional
- Cozinha digita 2 ou 3 dígitos, cumprindo o requisito de um toque

**Negativas**

- UUID ocupa 16 bytes contra 8 de bigint (irrelevante nesta volumetria)
- Menos legível em depuração manual
- Necessidade de manter duas identidades (técnica e humana) coerentes

**Mitigações**

- Ferramentas de suporte sempre exibem `short_code` junto do UUID
- `short_code` indexado e único por loja e dia operacional
- Timestamp extraível do próprio UUIDv7 facilita depuração

## Como validar

- Teste: dois edges offline geram IDs sem colisão
- Teste: reinício do dia operacional zera a sequência sem violar unicidade
- Teste E2E: digitar código curto no KDS avança o item correto
- Verificação de índice: sem fragmentação relevante após 1 milhão de linhas

## Revisitar quando

- Uma loja ultrapassar 999 pedidos em um dia operacional (aí o formato ganha um dígito)
