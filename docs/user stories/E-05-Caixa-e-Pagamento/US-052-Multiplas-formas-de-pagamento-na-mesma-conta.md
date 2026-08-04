# US-052 · Multiplas formas de pagamento na mesma conta

|  |  |
|---|---|
| **Épico** | [E-05 · Caixa e Pagamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 5 |
| **Requisitos funcionais** | RF-CXA-03 |
| **Regras de negócio** | — |
| **ADRs** | ADR-020, ADR-024, ADR-017 |
| **Eventos** | EVT-032 |
| **Aplicações** | web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** caixa (P4),
> **quero** receber uma conta com várias formas de pagamento ao mesmo tempo,
> **para** que a mesa que divide em cartão, PIX e dinheiro seja fechada em uma operação só.

## 2. Contexto e motivação

Cenário rotineiro: mesa de quatro pessoas, duas pagam no cartão, uma no PIX e uma em dinheiro. Sem suporte a pagamento múltiplo, o caixa fecha quatro vezes ou faz conta na mão.

A abstração de provedor (ADR-024) é o que permite que o MVP registre a forma manualmente e a Fase 4 acrescente a integração real sem reescrever o fluxo.

A invariante crítica: a soma dos pagamentos precisa ser exatamente igual ao total. Divergência aqui vira divergência de caixa no fechamento.

## 3. Escopo

### 3.1 Dentro desta história

- Registro de múltiplos pagamentos na mesma conta
- Formas: dinheiro, débito, crédito, PIX, voucher
- Cálculo de troco para dinheiro
- Pagamento parcial mantendo a sessão em aberto
- Validação de que a soma bate com o total
- Idempotência no registro do pagamento
- Transição da sessão para `PAID` e depois `CLOSED`
- Geração automática de `financial_entry` de receita

### 3.2 Fora desta história

- Integração real com maquininha ou gateway (Fase 4, US-134)
- Estorno (RF-CXA-13, Fase 2)
- Conciliação de recebimentos eletrônicos (RF-CXA-11, Fase 3)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Pagamento com múltiplas formas

  Cenário: Três formas na mesma conta
    Dado uma conta de R$ 198,00
    Quando o caixa registrar R$ 100,00 em crédito, R$ 50,00 em PIX e R$ 48,00 em dinheiro
    Então a sessão deve ir para PAID
    E os três pagamentos devem ficar registrados separadamente
    E o evento payment.registered deve ser emitido para cada um

  Cenário: Soma divergente do total
    Dado uma conta de R$ 198,00
    Quando o caixa tentar registrar pagamentos somando R$ 190,00
    Então deve receber 422
    E deve ser informada a diferença de R$ 8,00

  Cenário: Troco em dinheiro
    Dado uma conta de R$ 198,00 e recebimento de R$ 200,00 em dinheiro
    Quando o pagamento for registrado
    Então o troco de R$ 2,00 deve ser calculado e exibido
    E o valor registrado como receita deve ser R$ 198,00

  Cenário: Pagamento parcial
    Dado uma conta de R$ 198,00
    Quando uma pessoa pagar R$ 50,00
    Então devem restar R$ 148,00 em aberto
    E a sessão deve permanecer em BILL_REQUESTED

  Cenário: Duplo registro por instabilidade
    Dado que o caixa confirmou o pagamento duas vezes
    Quando a segunda requisição chegar com a mesma Idempotency-Key
    Então não deve haver pagamento duplicado

  Cenário: Receita registrada automaticamente
    Dado um pagamento confirmado
    Quando a sessão for encerrada
    Então deve ser criado um lançamento de receita automaticamente
    E deve estar vinculado ao canal e à forma de pagamento

  Cenário: Fechamento com internet caída
    Dado que a loja está sem internet
    Quando o caixa registrar o pagamento
    Então a operação deve concluir normalmente
    E os eventos devem ficar enfileirados para sincronização
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-005 | A operação local não depende de internet | Recebimento 100% local |
| RN-017 | Conta não pode ser fechada com item pendente, salvo autorização | Verificado na US-035 |
| RN-004 | Toda ação registra autor, horário e dispositivo | Cada pagamento registra o operador e o terminal |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-032 | `payment.registered` | Pagamento recebido | method, amount, provider, providerRef | ↑ |
| EVT-023 | `table.session.closed` | Comanda encerrada | total, serviceFee, durationSeconds | ↑ |
| EVT-026 | `table.released` | Mesa liberada | turnaroundSeconds | ↑ |

> Reação normativa: `payment.registered` → notifica caixa e mesa, atualiza receita e ticket, **cria lançamento de receita** no financeiro.

## 7. Contrato de API

```http
POST /v1/sessions/{id}/payments
Idempotency-Key: <uuid>
X-Occurred-At: 2026-07-31T22:14:08.552Z
{ "payments": [
    { "method": "CREDIT", "amount": 10000,
      "provider": "CIELO", "providerRef": "..." },
    { "method": "PIX",    "amount": 5000 },
    { "method": "CASH",   "amount": 5000, "receivedAmount": 5000 } ] }
→ 201 { "session": { "status": "PAID" },
        "payments": [...],
        "change": 200,
        "receipt": { "url": "..." } }

→ 422 { "code": "PAYMENT_SUM_MISMATCH",
        "detail": "A soma dos pagamentos não corresponde ao total.",
        "meta": { "total": 19800, "provided": 19000, "difference": 800 } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `payment` | Pagamento registrado | `method`, `amount`, `net_amount`, `provider`, `provider_ref`, `cash_session_id` |
| `payment_allocation` | Vínculo pagamento↔conta | `payment_id`, `session_id`, `amount` |
| `table_session` | Estado da sessão | `status`, `paid_at`, `closed_at` |
| `financial_entry` | Receita gerada automaticamente | `type=REVENUE`, `amount`, `channel`, `competence_date` |
| `cash_session` | Caixa ao qual o pagamento pertence | `id`, `operator_id` |

> `net_amount` é coluna gerada, descontando a taxa de cartão — é a despesa que costuma ser invisível ao dono (decisão 10 do ERD).

## 9. Comportamento offline

Integralmente local. O fechamento de conta é operação crítica de tempo real: se dependesse da nuvem, uma queda de internet impediria o cliente de ir embora.

Pagamentos por maquininha externa são registrados manualmente (US-058), o que funciona igualmente bem offline. Pagamento online integrado (Fase 4) é a única forma que exige internet — e sua indisponibilidade é degradação esperada e comunicada.

## 10. Interface e experiência

- Formas de pagamento como botões grandes, com atalho de teclado numérico
- Valor restante sempre visível e atualizado a cada forma acrescentada
- Troco calculado e exibido em fonte grande — é o número que o caixa precisa ler rápido
- Confirmação única do conjunto de pagamentos, não uma por forma
- Erro de soma apontando exatamente a diferença, nunca mensagem genérica

## 11. Métricas, alertas e observabilidade

- Faturamento por forma de pagamento e por canal
- Ticket médio por forma
- Custo de taxa de cartão por transação (RF-FIN-10, base na Fase 3)
- Tempo médio de fechamento por sessão

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Validação de soma, cálculo de troco e pagamento parcial |
| Propriedade | Soma dos pagamentos sempre igual ao total, para qualquer combinação |
| Integração | Idempotência impede pagamento duplicado |
| Integração | Lançamento de receita criado automaticamente |
| Integração | Sessão vai para PAID e depois CLOSED, liberando a mesa |
| Caos offline | Fechamento completo com internet caída |

## 13. Dependências

**Depende de:** US-051, US-027, US-055  
**Habilita:** US-057, US-073, US-120

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

- **Pendência 4 do índice** — a modalidade de integração de pagamento (TEF versus gateway) não foi definida. O MVP registra a forma manualmente; a integração real muda a arquitetura de pagamento e precisa de reunião técnica específica.
- A abstração de provedor (ADR-024) precisa ser respeitada desde o MVP, senão a Fase 4 vira reescrita.

---

*US-052 · Épico E-05 · Pacote 004_DonaBetinha · Replay Studio.*