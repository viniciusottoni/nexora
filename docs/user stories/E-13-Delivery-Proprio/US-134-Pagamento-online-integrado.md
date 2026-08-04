# US-134 · Pagamento online integrado

|  |  |
|---|---|
| **Épico** | [E-13 · Delivery Proprio](./README.md) |
| **Fase** | 4 — Delivery próprio |
| **Prioridade** | M — Must have |
| **Estimativa** | 13 pontos |
| **Sprint sugerida** | Fase 4 |
| **Requisitos funcionais** | RF-CXA-09 |
| **Regras de negócio** | RN-022 |
| **ADRs** | ADR-024, ADR-031 |
| **Eventos** | EVT-032 |
| **Aplicações** | web-menu, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** cliente de delivery (P6),
> **quero** pagar online no momento do pedido,
> **para** que eu não precise ter dinheiro na hora nem esperar a maquininha chegar.

## 2. Contexto e motivação

O cliente citou o **Mercado Pago** como forma de pagamento pelo aplicativo. A abstração de provedor (ADR-024) é o que permite trocar ou acrescentar provedores sem reescrever o fluxo.

É a história com maior dependência externa do épico: exige credenciais, definição de modalidade e homologação com o provedor. Sem isso, fica bloqueada.

Segurança é inegociável: **chaves de pagamento nunca no cliente** (doc. 02, seção 8).

## 3. Escopo

### 3.1 Dentro desta história

- Integração com o provedor de pagamento definido
- PIX e cartão de crédito online
- Webhook de confirmação de pagamento
- Tratamento de pagamento pendente, aprovado e recusado
- Pagamento na entrega como alternativa
- Registro do pagamento no financeiro
- Idempotência e reconciliação com o provedor

### 3.2 Fora desta história

- Estorno online (RF-CXA-13, fase posterior)
- Assinatura ou pagamento recorrente
- Carteira ou saldo do cliente

## 4. Critérios de aceite

```gherkin
Funcionalidade: Pagamento online

  Cenário: Pagamento por PIX aprovado
    Dado um pedido pendente de pagamento
    Quando o cliente pagar por PIX e o webhook confirmar
    Então o pedido deve ser liberado para produção
    E o pagamento deve ser registrado no financeiro

  Cenário: Pedido não produzido antes da confirmação
    Dado um pedido com pagamento online pendente
    Quando ainda não houver confirmação
    Então o pedido não deve entrar na fila da cozinha
    E o cliente deve ver que aguarda pagamento

  Cenário: Pagamento recusado
    Dado um cartão recusado pelo provedor
    Quando a resposta chegar
    Então o cliente deve ser informado
    E deve poder tentar outra forma

  Cenário: Webhook duplicado
    Dado um webhook de confirmação recebido duas vezes
    Quando o segundo chegar
    Então não deve haver pagamento duplicado

  Cenário: Pagamento na entrega
    Dado o cliente que escolhe pagar na entrega
    Quando confirmar o pedido
    Então o pedido deve ir direto para produção
    E a forma deve ficar registrada para o entregador

  Cenário: Timeout de pagamento
    Dado um pedido aguardando pagamento além do limite configurado
    Quando o limite for atingido
    Então o pedido deve ser cancelado automaticamente
    E o cliente deve ser informado

  Cenário: Chaves protegidas
    Dado o fluxo de pagamento
    Quando o código do cliente for inspecionado
    Então nenhuma chave secreta do provedor deve estar presente
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-022 | Política de estorno e cancelamento de pagamento | **[PENDÊNCIA]** — fora desta história |
| RN-004 | Toda ação registra autor, horário e dispositivo | Pagamento online registra origem e referência do provedor |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-032 | `payment.registered` | Pagamento online confirmado | method, amount, provider, providerRef | ↓ |

## 7. Contrato de API

```http
POST /v1/public/orders/{id}/payment
{ "method": "PIX" }
→ 201 { "paymentId": "...", "qrCode": "...", "expiresAt": "...",
        "status": "PENDING" }

POST /v1/webhooks/payments/{provider}
X-Signature: <assinatura do provedor>
{ "paymentId": "...", "status": "APPROVED", "providerRef": "..." }
→ 200

GET /v1/public/orders/{id}/payment/status
→ { "status": "APPROVED", "approvedAt": "..." }
```

> O webhook é validado por assinatura do provedor e tratado de forma idempotente — reenvio não duplica pagamento.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `payment` | Pagamento online | `method`, `provider`, `provider_ref`, `status`, `net_amount`, `fee_amount` |
| `order` | Estado do pagamento | `payment_status`, `paid_at` |
| `tenant_secret` | Credenciais do provedor, nunca no cliente | `provider`, `encrypted_credentials` |
| `financial_entry` | Receita gerada | `type=REVENUE`, `channel=DELIVERY` |

## 9. Comportamento offline

Depende integralmente de internet, dos dois lados. Sem conexão, o pagamento online não acontece — degradação esperada.

A alternativa de pagamento na entrega é o caminho degradado que mantém o canal funcionando quando o provedor está indisponível (resposta 503 na API de pagamento).

## 10. Interface e experiência

- PIX como opção destacada — menor taxa e confirmação imediata
- QR Code e código copiável na mesma tela
- Estado do pagamento atualizado sem exigir recarregar
- Pagamento na entrega sempre disponível como alternativa
- Mensagem de recusa orientando a próxima ação, sem expor detalhe técnico do provedor

## 11. Métricas, alertas e observabilidade

- Taxa de aprovação por forma de pagamento
- Tempo médio até a confirmação
- Pedidos cancelados por timeout de pagamento
- Custo de taxa por transação online

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Fluxo completo com o ambiente de homologação do provedor |
| Integração | Webhook duplicado não duplica pagamento |
| Integração | Timeout cancela o pedido e informa o cliente |
| Segurança | Nenhuma chave secreta exposta no cliente |
| Segurança | Webhook sem assinatura válida é recusado |

## 13. Dependências

**Depende de:** US-130, US-058  
**Habilita:** US-120

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
- [ ] Homologação concluída com o provedor de pagamento

## 15. Riscos, premissas e pendências

- **Dependência externa bloqueante** — credenciais do Mercado Pago são pré-requisito (PRD, seção 8). Sem elas, a história não avança.
- **Pendência 4 do índice** — a modalidade de integração precisa ser definida. Gateway online e TEF são coisas distintas.
- RN-022 (política de estorno) é pendência aberta e afeta o desenho do pós-venda.

---

*US-134 · Épico E-13 · Pacote 004_DonaBetinha · Replay Studio.*