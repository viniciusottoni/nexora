# US-130 · Canal publico de pedido com marca propria

|  |  |
|---|---|
| **Épico** | [E-13 · Delivery Proprio](./README.md) |
| **Fase** | 4 — Delivery próprio |
| **Prioridade** | M — Must have |
| **Estimativa** | 13 pontos |
| **Sprint sugerida** | Fase 4 |
| **Requisitos funcionais** | RF-DEL-01 |
| **Regras de negócio** | — |
| **ADRs** | ADR-009, ADR-010, ADR-020 |
| **Eventos** | EVT-002 |
| **Aplicações** | web-menu, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** cliente de delivery (P6),
> **quero** pedir pelo site do estabelecimento, com a marca dele,
> **para** que eu compre direto de quem produz, sem passar por marketplace.

## 2. Contexto e motivação

É o canal que reduz a dependência do iFood. A referência mencionada pelo cliente é a **Yon San**, como modelo de pedido e delivery — o que exatamente agrada nessa referência ainda é pendência de qualificação.

O produto reaproveita integralmente o cardápio, os modificadores e as frações já construídos, mudando apenas canal, preço e fluxo de entrega. É a vantagem concreta de ter modelado catálogo e pedido corretamente desde o MVP.

## 3. Escopo

### 3.1 Dentro desta história

- Cardápio público no canal `DELIVERY`, com preço próprio
- Carrinho com modificadores e frações
- Identificação do cliente por telefone com código OTP
- Cadastro e escolha de endereço
- Cálculo de taxa por zona
- Confirmação com prazo estimado
- Marca do estabelecimento em toda a experiência
- PWA instalável

### 3.2 Fora desta história

- Pagamento online (US-134)
- Rastreio (US-133)
- Endereço salvo e repetição (US-135)
- Integração com iFood (fora do escopo)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Canal público de delivery

  Cenário: Pedido completo
    Dado um cliente no site do estabelecimento
    Quando montar o carrinho, informar o endereço e confirmar
    Então o pedido deve ser criado no canal DELIVERY
    E deve chegar ao KDS como qualquer outro pedido
    E o prazo estimado deve ser informado

  Cenário: Marca do estabelecimento
    Dado o acesso pelo domínio do cliente
    Quando a página carregar
    Então a marca exibida deve ser a do estabelecimento
    E nenhuma marca da Replay deve aparecer em primeiro plano

  Cenário: Preço do canal delivery
    Dado uma pizza a R$ 45,00 no salão e R$ 52,00 no delivery
    Quando o cliente montar o pedido pelo canal público
    Então o preço aplicado deve ser R$ 52,00

  Cenário: Identificação por telefone
    Dado um cliente novo
    Quando informar o telefone e o código recebido
    Então deve ser autenticado sem senha
    E o acesso deve durar 30 dias

  Cenário: Endereço fora da área de entrega
    Dado um endereço fora de todas as zonas cadastradas
    Quando o cliente informar
    Então deve ser avisado de que a entrega não é possível
    E deve ser oferecida a retirada no local, se configurada

  Cenário: Loja fechada
    Dado o estabelecimento fora do horário de funcionamento
    Quando o cliente acessar
    Então deve ver o horário de abertura
    E não deve conseguir finalizar o pedido

  Cenário: Produto indisponível
    Dado um produto marcado como indisponível pela cozinha
    Quando o cardápio de delivery for carregado
    Então o produto não deve estar disponível para pedido
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-001 | Todo pedido confirmado é roteado simultaneamente para cozinha e caixa | Vale igualmente para delivery |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-002 | `order.placed` | Pedido de delivery confirmado | channel=DELIVERY, items[], deliveryAddress | ↓ |

> Pedido de delivery nasce na nuvem e desce ao edge para produção — direção inversa à do pedido de salão.

## 7. Contrato de API

```http
GET  /v1/public/menu?channel=DELIVERY
POST /v1/public/auth/otp        { "phone": "..." }
POST /v1/public/auth/verify     { "phone": "...", "code": "123456" }

POST /v1/public/orders
Idempotency-Key: <uuid>
{ "channel": "DELIVERY",
  "customerId": "...",
  "addressId": "...",
  "items": [...],
  "paymentMethod": "ON_DELIVERY" }
→ 201 { "order": {...}, "promisedAt": "...", "estimatedMinutes": 32,
        "deliveryFee": 800 }

→ 422 { "code": "OUT_OF_DELIVERY_AREA" }
→ 422 { "code": "STORE_CLOSED", "meta": { "opensAt": "..." } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `customer` | Cliente de delivery | `phone`, `name`, `created_at` |
| `customer_address` | Endereço | `street`, `number`, `complement`, `zone_id`, `lat`, `lng` |
| `order` | Pedido de delivery | `channel=DELIVERY`, `customer_id`, `address_id`, `delivery_fee` |
| `delivery_zone` | Zona e taxa | `name`, `fee`, `estimated_minutes` |

## 9. Comportamento offline

**Canal que depende de internet por natureza.** Com a loja offline, o pedido de delivery não chega ao edge — a degradação é esperada e precisa ser comunicada.

Comportamento correto: quando o edge está offline por mais que o limiar, o canal público exibe aviso de indisponibilidade temporária em vez de aceitar pedidos que não serão produzidos.

Pedidos já confirmados antes da queda continuam sendo produzidos normalmente na loja.

## 10. Interface e experiência

- Fluxo de pedido em no máximo quatro etapas: cardápio, carrinho, endereço, confirmação
- Marca do estabelecimento em primeiro plano, do favicon ao comprovante
- Taxa de entrega visível antes da confirmação, nunca como surpresa
- Loja fechada com horário de abertura, não apenas mensagem de erro
- PWA instalável, oferecido sem insistência

## 11. Métricas, alertas e observabilidade

- Taxa de conversão do carrinho ao pedido
- Ticket médio do canal delivery contra salão
- Pedidos por canal, medindo a migração do iFood
- Abandono por endereço fora da área

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Pedido de delivery chegando ao KDS do edge |
| Integração | Preço do canal delivery aplicado corretamente |
| Integração | Endereço fora de zona recusado com mensagem clara |
| Integração | Loja fechada impede finalização |
| Desempenho | Cardápio público carregando em menos de 2 s em 4G |
| E2E | Fluxo completo do cardápio à confirmação |

## 13. Dependências

**Depende de:** US-003, US-014, US-131  
**Habilita:** US-132, US-133, US-134

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

- **Pendência de qualificação da referência** — o que exatamente agrada na Yon San (fluxo, velocidade, aparência) não foi levantado (Visão Geral 3.3). Sessão de referências recomendada antes do desenho de UX.
- Migrar cliente do iFood para o canal próprio é desafio de marketing, não de produto. O sistema entrega o canal; a adoção depende de comunicação.

---

*US-130 · Épico E-13 · Pacote 004_DonaBetinha · Replay Studio.*