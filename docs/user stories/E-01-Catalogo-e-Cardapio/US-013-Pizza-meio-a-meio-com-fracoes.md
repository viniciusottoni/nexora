# US-013 · Pizza meio a meio com fracoes

|  |  |
|---|---|
| **Épico** | [E-01 · Catalogo e Cardapio](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 2 |
| **Requisitos funcionais** | RF-CAT-04, RF-CAT-05 |
| **Regras de negócio** | RN-009 |
| **ADRs** | ADR-016, ADR-017 |
| **Eventos** | EVT-003 |
| **Aplicações** | web-menu, web-pos, api-edge, packages/domain |
| **Autoridade do dado** | Nuvem (regra) → aplicada no local (cálculo) |

---

## 1. História

> **Como** cliente do salão (P1) e garçom (P2),
> **quero** montar uma pizza com dois ou mais sabores,
> **para** que o pedido reflita o que realmente se consome numa pizzaria.

## 2. Contexto e motivação

É a funcionalidade mais específica do domínio e a que mais gente modela errado. A decisão registrada (ERD, decisão 1) é usar uma tabela `order_item_fraction` em vez de dois campos de sabor.

O ganho é duplo: suporta 2, 3 ou 4 sabores sem mudar o schema, e a baixa de estoque fica **proporcional ao peso da fração** — meia pizza de mussarela baixa metade da ficha técnica da mussarela. Sem isso, o CMV da Fase 2 nasce errado.

A regra de precificação está marcada como **[HIPÓTESE] RN-009**: o padrão sugerido é maior valor entre as frações, mas as três regras devem ser implementadas e a escolha deve ser configuração do tenant.

## 3. Escopo

### 3.1 Dentro desta história

- Montagem de item com N frações, respeitando `max_fractions` da variação
- Validação de compatibilidade por `size_code` e `fraction_group`
- Peso da fração calculado automaticamente (1/N) e gravado
- Três regras de precificação configuráveis: `HIGHEST`, `AVERAGE`, `PROPORTIONAL`
- Exibição do preço final antes da confirmação
- Descrição composta no KDS e no comprovante
- Preparo do terreno para a baixa proporcional de estoque (executada na US-103)

### 3.2 Fora desta história

- Baixa de estoque propriamente dita (US-103, Fase 2)
- Modificadores por fração — nesta versão o adicional vale para o item inteiro
- Fração com preço promocional específico

## 4. Critérios de aceite

```gherkin
Funcionalidade: Pizza meio a meio

  Cenário: Montagem de meio a meio
    Dado um produto que permite frações e limite de 2 sabores
    Quando o cliente escolher dois sabores de mesmo tamanho
    Então o item deve conter duas frações com peso 0,5 cada
    E a soma dos pesos deve ser exatamente 1,0

  Cenário: Precificação por maior valor
    Dado a regra "HIGHEST" configurada
    E sabores de R$ 45,00 e R$ 52,00
    Quando o item for calculado
    Então o preço deve ser R$ 52,00

  Cenário: Precificação por média
    Dado a regra "AVERAGE" configurada
    E sabores de R$ 45,00 e R$ 52,00
    Quando o item for calculado
    Então o preço deve ser R$ 48,50

  Cenário: Precificação proporcional
    Dado a regra "PROPORTIONAL" configurada
    E sabores de R$ 45,00 e R$ 52,00 com peso 0,5 cada
    Quando o item for calculado
    Então o preço deve ser R$ 48,50
    E, com três sabores de pesos iguais, deve ser a soma ponderada dos três

  Cenário: Tamanhos incompatíveis
    Dado um sabor em tamanho G e outro em tamanho M
    Quando o cliente tentar combiná-los
    Então o sistema deve impedir e explicar que os tamanhos devem ser iguais

  Cenário: Grupos de fração distintos
    Dado uma variação de pizza e uma variação de hambúrguer
    Quando alguém tentar combiná-las em frações
    Então o sistema deve impedir pela divergência de fraction_group

  Cenário: Baixa proporcional de estoque
    Dado um meio a meio de Mussarela e Calabresa
    Quando o item for concluído
    Então deve ser baixada metade dos insumos de cada ficha técnica

  Cenário: Exibição no KDS
    Dado um item com duas frações
    Quando aparecer no cartão do KDS
    Então deve exibir "Pizza G · Mussarela / Calabresa"
    E as duas metades devem ser legíveis sem abrir detalhe
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-009 | O preço do meio a meio segue regra configurável; padrão sugerido é maior valor entre as frações | **[HIPÓTESE]** — as três regras são implementadas; a escolha vive em `tenant_config` |
| RN-016 | Configuração, não código | A regra de precificação é parâmetro, não branch |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-003 | `order.item.added` | Item com frações acrescentado | variantId, qty, fractions[], modifiers[] | ↑ |

## 7. Contrato de API

```http
POST /v1/public/orders
{
  "channel": "DINE_IN",
  "sessionToken": "...",
  "items": [
    {
      "variantId": "<pizza-grande>",
      "quantity": 1,
      "fractions": [
        { "variantId": "<mussarela-g>", "weight": 0.5 },
        { "variantId": "<calabresa-g>", "weight": 0.5 }
      ],
      "modifiers": [ { "modifierId": "<borda-catupiry>" } ],
      "notes": "bem assada"
    }
  ]
}
→ 201 { "order": { "items": [ { "unitPrice": 5200,
                                "priceRule": "HIGHEST",
                                "description": "Pizza G · Mussarela / Calabresa" } ] } }

→ 422 { "code": "FRACTION_SIZE_MISMATCH",
        "detail": "As frações devem ter o mesmo tamanho.",
        "meta": { "sizes": ["G","M"] } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Item pai que agrega as frações | `variant_id`, `quantity`, `unit_price`, `price_rule` |
| `order_item_fraction` | Uma linha por sabor | `order_item_id`, `variant_id`, `weight`, `name_snapshot` |
| `product_variant` | Define compatibilidade e limite | `size_code`, `fraction_group`, `max_fractions` |
| `tenant_config` | Regra de precificação vigente | `operation.fractionPriceRule` |

> A soma de `weight` das frações de um item deve ser exatamente 1,0 — garantido por constraint de banco, não por validação de aplicação.

## 9. Comportamento offline

Cálculo integralmente local, em `packages/domain`. A mesma função pura roda no edge e na nuvem, o que garante que o preço calculado offline seja idêntico ao que a nuvem calcularia — requisito para que a conciliação financeira não divirja depois da sincronização.

## 10. Interface e experiência

- Montagem em duas etapas visuais: escolher o tamanho, depois escolher os sabores
- Preço final atualizado a cada escolha, sempre visível — o cliente nunca descobre o valor só no fim
- Sabores indisponíveis exibidos como bloqueados, com o motivo
- No cartão do KDS, os sabores aparecem lado a lado, com separador claro — a cozinha precisa ler a 1,5 m de distância
- No comprovante, as duas frações discriminadas

## 11. Métricas, alertas e observabilidade

- Percentual de pizzas vendidas como meio a meio — insumo direto da curva ABC
- `fraction_quantity` em `metric_product_daily`, para não contar meia pizza como unidade inteira (decisão 8 do ERD)
- Combinações de sabores mais frequentes

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | As três regras de precificação, com 2, 3 e 4 frações |
| Unitário | Soma de pesos sempre igual a 1,0; arredondamento em centavos sem perda |
| Unitário | Rejeição por `size_code` e por `fraction_group` divergentes |
| Integração | Constraint de banco recusa item cuja soma de pesos não seja 1,0 |
| Integração | Preço calculado no edge é idêntico ao calculado na nuvem |
| E2E | Cliente monta meio a meio pela mesa e o KDS exibe a descrição composta |

## 13. Dependências

**Depende de:** US-011, US-012  
**Habilita:** US-030, US-103

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Persona, ação e resultado estão claros
- [ ] Critérios de aceite escritos em Gherkin
- [ ] Requisito funcional (RF) e evento (EVT) referenciados
- [ ] Dependências identificadas e resolvidas
- [ ] Desenho de tela existe (quando há interface)
- [ ] Estimada pelo time
- [ ] Comportamento offline definido
- [ ] Impacto em métrica e alerta identificado

**DoD — a história só é concluída quando:**

- [ ] Código revisado e aprovado por outro desenvolvedor
- [ ] Testes unitários dos casos de negócio passando
- [ ] Teste de integração do fluxo principal passando
- [ ] Teste de isolamento multi-tenant (quando a história toca tabela com `tenant_id`)
- [ ] Eventos emitidos conforme o catálogo do documento 04
- [ ] Comportamento offline verificado (quando aplicável)
- [ ] Critérios de aceite validados em ambiente de teste pelo PO
- [ ] Sem violação do ADR-013 (proibição de código por cliente)
- [ ] Documentação atualizada (OpenAPI, catálogo de eventos, modelo de dados)
- [ ] Observabilidade instrumentada (log estruturado + traço OpenTelemetry)
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- **RN-009 é hipótese não validada.** A regra padrão precisa ser confirmada com o cliente antes do piloto — cobrar pelo maior valor é prática comum, mas não universal.
- Arredondamento na regra `AVERAGE` com valores ímpares em centavos precisa de política explícita (arredondar para cima, para baixo ou bancário). Definir na implementação e documentar.

---

*US-013 · Épico E-01 · Pacote 004_DonaBetinha · Replay Studio.*