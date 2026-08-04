# US-120 · Receita automatica a partir de pagamentos

|  |  |
|---|---|
| **Épico** | [E-12 · Financeiro de Gestao](./README.md) |
| **Fase** | 3 — Financeiro de gestão |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 3 |
| **Requisitos funcionais** | RF-FIN-01 |
| **Regras de negócio** | RN-020 |
| **ADRs** | ADR-018, ADR-017 |
| **Eventos** | EVT-032 |
| **Aplicações** | api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** que a receita entre no financeiro sozinha, a partir das vendas,
> **para** que eu não precise lançar nada e o número seja sempre confiável.

## 2. Contexto e motivação

Aplicação direta da regra de ouro da métrica: *nenhuma métrica é digitada; toda métrica é derivada*. A receita já existe como pagamento registrado — lançá-la de novo no financeiro seria digitação redundante e fonte de divergência.

A reação já está prevista como normativa no documento 04, seção 5: `payment.registered` → **lançamento de receita**.

## 3. Escopo

### 3.1 Dentro desta história

- Lançamento de receita gerado automaticamente por pagamento confirmado
- Classificação por canal e forma de pagamento
- Receita bruta e líquida (descontando taxa de cartão)
- Competência pelo dia operacional
- Estorno gerando lançamento compensatório
- Conferência entre receita financeira e faturamento operacional

### 3.2 Fora desta história

- Categorias de despesa (US-121)
- Conciliação bancária (fora do escopo)
- Emissão fiscal (pendência)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Receita automática

  Cenário: Receita gerada por pagamento
    Dado um pagamento de R$ 198,00 confirmado
    Quando a sessão for encerrada
    Então deve ser criado um lançamento de receita de R$ 198,00
    E deve estar classificado por canal e forma de pagamento

  Cenário: Receita líquida
    Dado um pagamento em crédito com 2,8% de taxa
    Quando a receita for registrada
    Então bruta e líquida devem ser registradas separadamente
    E a taxa deve constar como despesa de meio de pagamento

  Cenário: Competência pelo dia operacional
    Dado um pagamento às 00h40 com virada do dia às 5h
    Quando a competência for atribuída
    Então deve pertencer ao dia operacional anterior

  Cenário: Estorno compensatório
    Dado um pagamento estornado
    Quando o estorno for registrado
    Então deve ser criado lançamento compensatório
    E o lançamento original não deve ser alterado

  Cenário: Conferência com o operacional
    Dado o faturamento operacional do mês
    Quando comparado com a receita financeira
    Então os valores devem ser idênticos
    E qualquer divergência deve ser sinalizada

  Cenário: Nenhum lançamento manual de receita
    Dado o módulo financeiro
    Quando o gestor tentar lançar receita de venda manualmente
    Então deve ser orientado de que a receita é automática
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-020 | Métrica de horário usa sempre `ocorrido_em` | Competência pelo dia operacional de ocorrência |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-032 | `payment.registered` | Pagamento que origina a receita | method, amount, provider | ↑ |
| EVT-033 | `payment.refunded` | Estorno que origina compensação | amount, reason | ↑ |

> Reação normativa (doc. 04, seção 5): `payment.registered` → notifica caixa e mesa, atualiza receita e ticket, **cria lançamento de receita**.

## 7. Contrato de API

```http
GET /v1/finance/entries?type=REVENUE&period=2026-07
→ { "entries": [ { "id": "...", "type": "REVENUE",
                   "grossAmount": 19800, "netAmount": 19236,
                   "feeAmount": 564,
                   "channel": "DINE_IN", "paymentMethod": "CREDIT",
                   "competenceDate": "2026-07-31",
                   "sourceType": "payment", "sourceId": "...",
                   "isAutomatic": true } ] }

GET /v1/finance/revenue-reconciliation?period=2026-07
→ { "operationalRevenue": 4820000, "financialRevenue": 4820000,
    "difference": 0, "match": true }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `financial_entry` | Lançamento de receita | `type=REVENUE`, `gross_amount`, `net_amount`, `fee_amount`, `channel`, `competence_date`, `source_type`, `source_id`, `is_automatic` |
| `payment` | Origem | `amount`, `net_amount`, `method`, `provider` |

> `is_automatic` distingue lançamento derivado de lançamento manual — receita de venda nunca deve ser manual.

## 9. Comportamento offline

Gerado na nuvem, a partir dos pagamentos sincronizados. Vendas feitas offline entram no financeiro quando sincronizam, com a **competência do dia em que ocorreram** — não do dia em que subiram.

## 10. Interface e experiência

- Receita exibida como derivada, com link ao pagamento de origem
- Bruto e líquido lado a lado; a taxa de cartão precisa ser visível
- Conferência entre operacional e financeiro em um clique
- Lançamento manual de receita de venda bloqueado, com explicação

## 11. Métricas, alertas e observabilidade

- Receita bruta e líquida por canal e forma de pagamento
- Custo total de meios de pagamento
- Divergência entre receita financeira e faturamento operacional — deve ser sempre zero

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Pagamento gera lançamento de receita automaticamente |
| Integração | Competência pelo dia operacional, incluindo virada após meia-noite |
| Integração | Estorno gera compensação sem alterar o original |
| Propriedade | Receita financeira sempre igual ao faturamento operacional |

## 13. Dependências

**Depende de:** US-052, US-058  
**Habilita:** US-124, US-126, US-127

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

—

---

*US-120 · Épico E-12 · Pacote 004_DonaBetinha · Replay Studio.*