# US-051 · Conta montada automaticamente

|  |  |
|---|---|
| **Épico** | [E-05 · Caixa e Pagamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 5 |
| **Requisitos funcionais** | RF-CXA-02 |
| **Regras de negócio** | RN-010 |
| **ADRs** | ADR-017 |
| **Eventos** | — |
| **Aplicações** | web-pos, api-edge, packages/domain |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** caixa (P4),
> **quero** que a conta já venha pronta a partir do que foi lançado,
> **para** que eu não precise somar nada à mão nem errar.

## 2. Contexto e motivação

Responde diretamente à dor da persona P4. A conta não é digitada: ela é a consequência aritmética dos itens lançados, dos modificadores escolhidos, das frações e da regra de taxa de serviço.

O rigor exigido é o de contabilidade: valores em centavos inteiros (ADR-017), nenhuma operação em ponto flutuante, e a invariante de que a soma das partes é sempre exatamente o total.

## 3. Escopo

### 3.1 Dentro desta história

- Montagem da conta a partir dos itens da sessão
- Discriminação de modificadores e frações no detalhamento
- Subtotal, taxa de serviço, desconto e total
- Exclusão de itens cancelados
- Aviso de itens pendentes de entrega
- Integração com os três modos de divisão (US-027)
- Pré-visualização antes do recebimento

### 3.2 Fora desta história

- Recebimento do pagamento (US-052)
- Cálculo da divisão em si (US-027)
- Comprovante impresso (US-057)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Conta montada automaticamente

  Cenário: Conta completa
    Dado uma sessão com 6 itens, sendo um meio a meio com adicional
    Quando o caixa abrir a conta
    Então cada item deve aparecer com quantidade, preço unitário e total
    E o meio a meio deve exibir os dois sabores
    E o adicional deve aparecer discriminado
    E subtotal, taxa de serviço e total devem estar corretos

  Cenário: Item cancelado excluído
    Dado uma sessão com um item cancelado
    Quando a conta for montada
    Então o item cancelado não deve compor o total
    E deve aparecer riscado no detalhamento, para conferência

  Cenário: Taxa de serviço calculada
    Dado a taxa configurada em 10% e subtotal de R$ 180,00
    Quando a conta for montada
    Então a taxa deve ser R$ 18,00 e o total R$ 198,00
    E a taxa deve estar identificada como opcional

  Cenário: Aviso de item pendente
    Dado uma sessão com um item ainda em produção
    Quando a conta for montada
    Então deve haver aviso destacado do item pendente

  Cenário: Precisão monetária
    Dado uma conta com valores que produzem fração de centavo
    Quando o total for calculado
    Então nenhum centavo deve ser perdido ou criado
    E todos os valores devem ser inteiros em centavos

  Cenário: Preço da época preservado
    Dado um item lançado antes de um reajuste de preço
    Quando a conta for montada
    Então deve usar o preço registrado no item, não o preço atual
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-010 | Taxa de serviço é opcional ao cliente; a retirada é registrada e auditada | **[HIPÓTESE]** — taxa calculada e identificada como opcional |
| RN-017 | Conta não pode ser fechada com item pendente, salvo autorização | Aviso aqui; bloqueio na US-035 |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> A montagem é cálculo, não fato de negócio. Os eventos nascem no pagamento (EVT-032), no desconto (EVT-034) e na retirada de taxa (EVT-035).

## 7. Contrato de API

```http
GET /v1/sessions/{id}/bill
→ { "items": [ { "name": "Pizza G · Mussarela / Calabresa",
                 "quantity": 1, "unitPrice": 5200, "total": 5200,
                 "modifiers": [ { "name": "Borda Catupiry", "priceDelta": 800 } ],
                 "fractions": [ { "name": "Mussarela", "weight": 0.5 },
                                { "name": "Calabresa", "weight": 0.5 } ],
                 "status": "SERVED" },
               { "name": "Refrigerante Lata", "quantity": 2,
                 "unitPrice": 800, "total": 1600, "status": "CANCELLED" } ],
    "subtotal": 18000,
    "serviceFee": 1800, "serviceFeePercent": 10, "serviceFeeOptional": true,
    "discount": 0,
    "total": 19800,
    "pendingItems": [],
    "session": { "openedAt": "...", "minutesOpen": 47, "guestCount": 4 } }

GET /v1/sessions/{id}/bill?split=BY_PERSON&people=4
```

> Todos os valores em centavos inteiros. Nenhuma operação monetária usa ponto flutuante (ADR-017).

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `table_session` | Totais da sessão | `subtotal`, `service_fee`, `discount`, `total` |
| `order_item` | Itens com preço da época | `unit_price`, `quantity`, `status` |
| `order_item_modifier` | Adicionais com snapshot | `name_snapshot`, `price_delta_snapshot` |
| `order_item_fraction` | Frações do meio a meio | `name_snapshot`, `weight` |
| `tenant_config` | Percentual da taxa | `operation.serviceFeePercent` |

## 9. Comportamento offline

Cálculo integralmente local, em função pura de `packages/domain`. A mesma função roda na nuvem, o que garante que a conciliação financeira posterior não divirja do que foi cobrado na loja.

## 10. Interface e experiência

- Conta em tela única, com o total em fonte grande no rodapé fixo
- Itens cancelados visíveis e riscados — conferência, não ocultação
- Taxa de serviço sempre destacada como opcional
- Aviso de item pendente acima do total, não escondido no meio da lista
- Impressão e pré-visualização acessíveis por atalho de teclado

## 11. Métricas, alertas e observabilidade

- Ticket médio por sessão e por pessoa
- Percentual de contas com taxa de serviço retirada
- Valor médio de itens cancelados por conta

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Montagem com modificadores, frações, cancelados e taxa |
| Unitário | Precisão monetária: nenhum centavo criado ou perdido |
| Propriedade | Para qualquer combinação de itens, a soma dos totais é igual ao total da conta |
| Integração | Preço da época preservado após reajuste |
| Caos offline | Montagem correta com internet caída |

## 13. Dependências

**Depende de:** US-030, US-050  
**Habilita:** US-027, US-052, US-053, US-057

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

- Erro de arredondamento é a causa mais comum de divergência de caixa em PDV. Toda operação em centavos inteiros e invariantes testadas por propriedade, não por exemplo.

---

*US-051 · Épico E-05 · Pacote 004_DonaBetinha · Replay Studio.*