# US-027 · Dividir a conta

|  |  |
|---|---|
| **Épico** | [E-02 · Mesa e Comanda](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 3 |
| **Requisitos funcionais** | RF-SAL-10 |
| **Regras de negócio** | RN-010 |
| **ADRs** | ADR-017 |
| **Eventos** | — |
| **Aplicações** | web-pos, web-menu, api-edge, packages/domain |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** caixa (P4) e cliente do salão (P1),
> **quero** dividir a conta por pessoa, por item ou por valor,
> **para** que o fechamento não vire uma conta de padaria feita na calculadora do celular.

## 2. Contexto e motivação

É a operação que mais consome tempo do caixa no fim da noite e a que mais gera erro manual. Três modos cobrem praticamente todos os casos reais:

- **Por pessoa** — total dividido igualmente por N
- **Por item** — cada pessoa assume os itens que consumiu
- **Por valor** — alguém paga um valor arbitrário e o resto fica em aberto

O ponto delicado é o arredondamento: a soma das partes precisa ser exatamente igual ao total, sempre. Centavo perdido em divisão vira divergência de caixa no fechamento.

## 3. Escopo

### 3.1 Dentro desta história

- Os três modos de divisão
- Distribuição do resíduo de arredondamento, garantindo soma exata
- Taxa de serviço proporcional à parte de cada um
- Pagamento parcial: parte paga, restante em aberto
- Pré-visualização da divisão pelo cliente antes de chamar o caixa

### 3.2 Fora desta história

- Registro do pagamento propriamente dito (US-052)
- Transferência de itens entre mesas (Fase 2)
- Cobrança individual por link de pagamento

## 4. Critérios de aceite

```gherkin
Funcionalidade: Divisão de conta

  Cenário: Divisão por pessoa com resíduo
    Dado uma conta de R$ 100,00 para dividir entre 3 pessoas
    Quando a divisão for calculada
    Então devem ser gerados valores de R$ 33,34, R$ 33,33 e R$ 33,33
    E a soma deve ser exatamente R$ 100,00

  Cenário: Divisão por item
    Dado uma mesa com 6 itens e 3 pessoas
    Quando cada pessoa selecionar os itens que consumiu
    Então cada parte deve conter apenas os itens atribuídos
    E nenhum item pode ficar sem atribuição antes de fechar

  Cenário: Divisão por valor
    Dado uma conta de R$ 180,00
    Quando uma pessoa pagar R$ 50,00
    Então devem restar R$ 130,00 em aberto
    E a sessão deve permanecer em BILL_REQUESTED

  Cenário: Taxa de serviço proporcional
    Dado uma conta de R$ 100,00 com taxa de 10% dividida entre 4
    Quando a divisão for calculada
    Então cada parte deve ser R$ 27,50, com a taxa distribuída proporcionalmente
    E a taxa deve continuar identificada separadamente em cada parte

  Cenário: Retirada da taxa por uma das partes
    Dado uma divisão por pessoa
    Quando uma das pessoas optar por não pagar a taxa de serviço
    Então apenas a parte dela deve ser recalculada
    E a retirada deve ser registrada com autor

  Cenário: Item pendente durante a divisão
    Dado um item ainda em produção
    Quando a divisão for calculada
    Então o item deve ser incluído com marcação de pendência
    E o caixa deve ser avisado antes de concluir o recebimento
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-010 | Taxa de serviço é opcional ao cliente; a retirada é registrada e auditada | **[HIPÓTESE]** — a retirada é possível por parte, sempre registrada |
| RN-017 | Conta não pode ser fechada com item pendente, salvo autorização | Aviso na divisão, bloqueio no fechamento |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> A divisão é cálculo, não fato de negócio. Os eventos nascem no pagamento (EVT-032) e na retirada de taxa (EVT-035).

## 7. Contrato de API

```http
GET /v1/sessions/{id}/bill?split=BY_PERSON&people=4
→ { "items": [...], "subtotal": 18000, "serviceFee": 1800, "total": 19800,
    "split": [ { "person": 1, "amount": 4950, "serviceFee": 450 },
               { "person": 2, "amount": 4950, "serviceFee": 450 },
               { "person": 3, "amount": 4950, "serviceFee": 450 },
               { "person": 4, "amount": 4950, "serviceFee": 450 } ],
    "pendingItems": [ { "name": "...", "status": "IN_PRODUCTION" } ] }

GET /v1/sessions/{id}/bill?split=BY_ITEM
POST /v1/sessions/{id}/bill/assign-items
{ "assignments": [ { "person": 1, "itemIds": ["...","..."] } ] }

GET /v1/sessions/{id}/bill?split=BY_AMOUNT&amount=5000
```

> Valores em centavos. A soma de `split[].amount` é sempre exatamente igual a `total` — invariante testada.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `table_session` | Preferência e estado | `split_mode`, `split_people`, `total`, `service_fee` |
| `order_item` | Itens a atribuir no modo por item | `id`, `total`, `status` |
| `payment` / `payment_allocation` | Pagamentos parciais registrados | `amount`, `allocated_to` |

## 9. Comportamento offline

Cálculo integralmente local, em função pura de `packages/domain`. A divisão precisa funcionar com internet caída porque é etapa obrigatória do fechamento — e o fechamento é operação crítica de tempo real.

## 10. Interface e experiência

- Divisão por pessoa como padrão, por ser o caso mais comum — os outros dois ficam a um toque
- No modo por item, atribuição por toque no item e depois na pessoa, sem arrastar
- Itens não atribuídos destacados; não é possível concluir com item órfão
- Cliente pode pré-visualizar a divisão no celular antes de o caixa começar
- Valor de cada parte em fonte grande — é o número que a pessoa vai olhar

## 11. Métricas, alertas e observabilidade

- Tempo de fechamento por sessão, comparado entre os modos de divisão
- Frequência de retirada da taxa de serviço — informação sensível para o gestor
- Divergência de caixa correlacionada a divisões — deve ser zero se o arredondamento estiver correto

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Distribuição de resíduo garante soma exata em todos os casos, incluindo valores primos |
| Unitário | Taxa proporcional por parte, com e sem retirada |
| Propriedade | Para qualquer total e qualquer N, a soma das partes é igual ao total |
| Integração | Divisão por item recusa conclusão com item não atribuído |
| Integração | Pagamento parcial mantém a sessão em aberto com o saldo correto |
| Caos offline | Divisão e fechamento com internet caída |

## 13. Dependências

**Depende de:** US-024, US-026  
**Habilita:** US-052

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

- Arredondamento incorreto é a causa mais comum de divergência de caixa em sistemas de PDV. A invariante "soma das partes igual ao total" precisa ser teste de propriedade, não teste de exemplo.
- RN-010 (taxa opcional) é hipótese; confirmar com o cliente a política de taxa de serviço antes do piloto.

---

*US-027 · Épico E-02 · Pacote 004_DonaBetinha · Replay Studio.*