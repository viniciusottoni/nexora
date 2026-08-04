# US-058 · Registrar pagamento de maquininha externa

|  |  |
|---|---|
| **Épico** | [E-05 · Caixa e Pagamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 5 |
| **Requisitos funcionais** | RF-CXA-10 |
| **Regras de negócio** | RN-022 |
| **ADRs** | ADR-024 |
| **Eventos** | EVT-032 |
| **Aplicações** | web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** caixa (P4),
> **quero** registrar no sistema o pagamento feito na maquininha da Cielo ou do Mercado Pago,
> **para** que o caixa feche certo mesmo sem integração automática.

## 2. Contexto e motivação

Situação atual do cliente, registrada na descoberta: *maquininha Cielo com conta do Banco do Brasil e maquininha do Mercado Pago*, sem qualquer integração.

Como a modalidade de integração (TEF versus gateway) é **pendência aberta**, o MVP registra a forma manualmente. A abstração de provedor do ADR-024 garante que, quando a integração chegar, o fluxo do caixa não precise ser reescrito — só o adaptador.

## 3. Escopo

### 3.1 Dentro desta história

- Registro de pagamento com provedor e referência da transação (NSU/autorização)
- Formas: débito, crédito e PIX pela maquininha
- Campo opcional de bandeira e de parcelas
- Cálculo do valor líquido a partir da taxa configurada por provedor e forma
- Preparação da estrutura para conciliação futura

### 3.2 Fora desta história

- Integração TEF ou gateway (Fase 4, US-134)
- Conciliação automática com o extrato (RF-CXA-11, Fase 3)
- Estorno

## 4. Critérios de aceite

```gherkin
Funcionalidade: Pagamento em maquininha externa

  Cenário: Registro com referência
    Dado um pagamento de R$ 100,00 feito na maquininha Cielo
    Quando o caixa registrar com provedor CIELO e o NSU da transação
    Então o pagamento deve constar com provedor e referência
    E deve compor o total recebido normalmente

  Cenário: Valor líquido calculado
    Dado a taxa de crédito da Cielo configurada em 2,8%
    E um pagamento de R$ 100,00 em crédito
    Quando o pagamento for registrado
    Então o valor líquido deve ser R$ 97,20
    E a diferença deve ficar registrada como custo de taxa

  Cenário: Referência duplicada
    Dado um NSU já registrado no mesmo turno
    Quando o caixa tentar registrar novamente
    Então deve haver aviso de possível duplicidade
    E o registro deve exigir confirmação explícita

  Cenário: Registro sem referência
    Quando o caixa registrar sem informar o NSU
    Então o pagamento deve ser aceito
    E deve ser sinalizado como pendente de conciliação
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-022 | Política de estorno e cancelamento de pagamento | **[PENDÊNCIA]** — fora desta história |
| RN-004 | Toda ação registra autor, horário e dispositivo | Registro com operador identificado |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-032 | `payment.registered` | Pagamento recebido | method, amount, provider, providerRef, netAmount | ↑ |

## 7. Contrato de API

```http
POST /v1/sessions/{id}/payments
{ "payments": [ { "method": "CREDIT", "amount": 10000,
                  "provider": "CIELO", "providerRef": "NSU123456",
                  "brand": "VISA", "installments": 1 } ] }
→ 201 { "payments": [ { "amount": 10000, "netAmount": 9720,
                        "feeAmount": 280,
                        "reconciliationStatus": "PENDING" } ] }

PATCH /v1/tenant/config
{ "payment": { "providers": [ { "code": "CIELO",
                                "fees": { "CREDIT": 2.8, "DEBIT": 1.5 } } ] } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `payment` | Pagamento com provedor | `provider`, `provider_ref`, `brand`, `installments`, `fee_amount`, `net_amount`, `reconciliation_status` |
| `tenant_config` | Taxas por provedor e forma | `payment.providers` |

> `net_amount` é coluna gerada — a taxa de cartão é despesa que costuma ser invisível ao dono (decisão 10 do ERD).

## 9. Comportamento offline

Integralmente local. Registro manual funciona igualmente bem com ou sem internet.

## 10. Interface e experiência

- Campo de referência opcional, com teclado numérico — o caixa nem sempre tem o comprovante em mãos
- Bandeira e parcelas como campos opcionais, não obrigatórios
- Aviso de possível duplicidade sem bloquear — o caixa é quem sabe se é duplicata real
- Valor líquido exibido no registro, para que o operador veja o efeito da taxa

## 11. Métricas, alertas e observabilidade

- Faturamento bruto versus líquido por provedor e forma
- Custo total de taxa de cartão por período — insumo direto do financeiro (RF-FIN-10)
- Percentual de pagamentos sem referência, medindo a qualidade do registro

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo de valor líquido por provedor, forma e parcelas |
| Integração | Detecção de referência duplicada no mesmo turno |
| Integração | Pagamento sem referência marcado como pendente de conciliação |

## 13. Dependências

**Depende de:** US-052  
**Habilita:** US-124, US-134

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

- **Pendência 4 do índice** — a modalidade de integração de pagamento não foi definida. Integração TEF e gateway online são tecnicamente distintas, com custos diferentes; o cliente citou ambas. Exige reunião técnica específica.
- Registro manual depende de disciplina do operador. A conciliação da Fase 3 é o que fecha o controle.

---

*US-058 · Épico E-05 · Pacote 004_DonaBetinha · Replay Studio.*