# US-011 · Variacoes de produto com preco proprio

|  |  |
|---|---|
| **Épico** | [E-01 · Catalogo e Cardapio](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 1 |
| **Requisitos funcionais** | RF-CAT-02 |
| **Regras de negócio** | — |
| **ADRs** | ADR-016, ADR-017 |
| **Eventos** | EVT-050, EVT-052 |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** cadastrar tamanhos de um mesmo produto com preço próprio,
> **para** que o cliente escolha pequena, média ou grande sem que eu cadastre três produtos separados.

## 2. Contexto e motivação

A `product_variant` é a unidade real de venda do sistema. Preço, ficha técnica, tempo de preparo e movimentação de estoque referenciam a variação, não o produto. Essa decisão evita a duplicação de cadastro e faz com que a curva ABC e a margem sejam calculadas no nível certo.

O `size_code` da variação também é o que impede, estruturalmente, combinar meia pizza grande com meia pizza média em um meio a meio (decisão 9 do ERD consolidado).

## 3. Escopo

### 3.1 Dentro desta história

- CRUD de variação vinculada ao produto
- Campos: nome, `size_code`, `fraction_group`, número de frações permitidas, posição
- Preço base por variação (o preço por canal vem na US-014)
- Regra de que produto sem variação recebe uma variação única implícita
- Bloqueio de exclusão de variação com histórico de venda — apenas desativação

### 3.2 Fora desta história

- Preço por canal (US-014)
- Ficha técnica por variação (US-101, Fase 2)
- Regra de precificação de fração (US-013)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Variações de produto

  Cenário: Produto com três tamanhos
    Dado o produto "Pizza Mussarela"
    Quando o gestor cadastrar as variações Pequena, Média e Grande com preços distintos
    Então o cardápio deve exibir as três opções
    E o preço exibido inicialmente deve ser o da menor variação, com indicação "a partir de"

  Cenário: Produto sem variação
    Dado um produto "Refrigerante Lata" sem tamanhos
    Quando for cadastrado com preço único
    Então o sistema deve criar uma variação padrão implícita
    E o cliente não deve ver nenhuma escolha de tamanho

  Cenário: Compatibilidade de fração
    Dado uma variação "Pizza Grande" com size_code = G
    E uma variação "Pizza Média" com size_code = M
    Quando o cliente tentar montar um meio a meio combinando as duas
    Então o sistema deve impedir e explicar que os tamanhos devem ser iguais

  Cenário: Exclusão com histórico
    Dado uma variação com pedidos já registrados
    Quando o gestor tentar excluí-la
    Então a exclusão deve ser recusada
    E deve ser oferecida a desativação, preservando o histórico

  Cenário: Alteração de preço registrada
    Dado uma variação com preço de R$ 45,00
    Quando o gestor alterar para R$ 48,00
    Então o evento price.changed deve ser emitido
    E o preço anterior deve permanecer historizado com valid_from e valid_to
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-016 | Configuração, não código | Tamanhos e grupos de fração são dados por tenant |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-050 | `product.updated` | Variação criada ou alterada | productId, variantId | ↓ |
| EVT-052 | `price.changed` | Preço da variação alterado | variantId, oldAmount, newAmount, validFrom | ↓ |

## 7. Contrato de API

```http
POST /v1/catalog/products/{id}/variants
{ "name": "Grande", "sizeCode": "G", "fractionGroup": "PIZZA",
  "maxFractions": 2, "position": 3, "basePrice": 5200 }
→ 201 { "variant": {...} }

PATCH /v1/catalog/variants/{id}
GET   /v1/catalog/variants/{id}
```

> Valores monetários trafegam em centavos, como inteiro (ADR-017). `5200` é R$ 52,00.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `product_variant` | Unidade real de venda | `product_id`, `name`, `size_code`, `fraction_group`, `max_fractions`, `prep_minutes`, `is_active` |
| `price` | Preço historizado por canal | `variant_id`, `channel`, `amount`, `valid_from`, `valid_to` |

> `price` é historizado justamente para permitir recalcular a margem de um pedido antigo com o preço da época (decisão 3 do ERD).

## 9. Comportamento offline

Leitura no edge, replicada pelo pull. A operação sempre tem a última versão sincronizada de preço e variação.

Alteração de preço feita na nuvem durante uma queda de internet só chega quando a conexão voltar — o pedido criado nesse intervalo usa o preço vigente no edge, que é o comportamento correto: o cliente pagou o preço que estava no cardápio no momento do pedido.

## 10. Interface e experiência

- Variações editadas na mesma tela do produto, em linha, sem navegação separada
- Preço digitado com máscara de moeda; armazenado em centavos
- Aviso quando duas variações do mesmo produto têm `size_code` repetido

## 11. Métricas, alertas e observabilidade

- Venda e margem por variação (não por produto) — base da curva ABC correta
- Contagem de alterações de preço por período, com autor — insumo da auditoria

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Criação de variação implícita para produto sem tamanho |
| Unitário | Validação de compatibilidade por `size_code` e `fraction_group` |
| Integração | Alteração de preço historiza o valor anterior e emite `price.changed` |
| Integração | Exclusão de variação com histórico é recusada |

## 13. Dependências

**Depende de:** US-010  
**Habilita:** US-013, US-014, US-101

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

- Modelar preço direto na variação (sem tabela historizada) inviabilizaria o recálculo de margem histórica na Fase 2 — decisão já tomada, não reabrir.

---

*US-011 · Épico E-01 · Pacote 004_DonaBetinha · Replay Studio.*