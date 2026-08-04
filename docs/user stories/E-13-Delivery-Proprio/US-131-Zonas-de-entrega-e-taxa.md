# US-131 · Zonas de entrega e taxa

|  |  |
|---|---|
| **Épico** | [E-13 · Delivery Proprio](./README.md) |
| **Fase** | 4 — Delivery próprio |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 4 |
| **Requisitos funcionais** | RF-DEL-02 |
| **Regras de negócio** | — |
| **ADRs** | ADR-016 |
| **Eventos** | — |
| **Aplicações** | web-admin, web-menu, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** definir até onde entrego e quanto cobro por região,
> **para** que eu não perca dinheiro entregando longe por preço de perto.

## 2. Contexto e motivação

A taxa de entrega precisa refletir o custo real do deslocamento. Zona única com taxa única subsidia a entrega distante com a margem da entrega próxima — e ninguém percebe.

A definição de zonas também delimita a área de atendimento, evitando pedidos que a operação não consegue cumprir no prazo prometido.

## 3. Escopo

### 3.1 Dentro desta história

- Cadastro de zonas por CEP, bairro ou raio
- Taxa e prazo adicional por zona
- Pedido mínimo por zona
- Resolução automática do endereço para a zona
- Desativação temporária de zona
- Visualização das zonas no painel

### 3.2 Fora desta história

- Roteirização e otimização de rota
- Taxa dinâmica por demanda
- Cálculo por distância real percorrida

## 4. Critérios de aceite

```gherkin
Funcionalidade: Zonas de entrega

  Cenário: Taxa por zona
    Dado a zona Centro com taxa de R$ 5,00 e a zona Norte com R$ 12,00
    Quando o cliente informar um endereço no Norte
    Então a taxa aplicada deve ser R$ 12,00

  Cenário: Prazo adicional por zona
    Dado a zona Norte com 10 minutos adicionais
    Quando o prazo for calculado
    Então deve incluir os 10 minutos

  Cenário: Endereço fora de zona
    Dado um endereço que não corresponde a nenhuma zona
    Quando for informado
    Então o pedido deve ser recusado com explicação

  Cenário: Pedido mínimo por zona
    Dado a zona Norte com mínimo de R$ 60,00
    Quando o carrinho somar R$ 45,00
    Então deve haver aviso do valor faltante antes da confirmação

  Cenário: Zona desativada temporariamente
    Dado a zona Norte desativada por falta de entregador
    Quando um cliente daquela zona acessar
    Então deve ser informado da indisponibilidade temporária

  Cenário: Sobreposição de zonas
    Dado um endereço que se enquadra em duas zonas
    Quando a resolução ocorrer
    Então deve prevalecer a zona de maior prioridade configurada
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-016 | Configuração, não código | Zonas e taxas são dados por tenant |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
POST /v1/delivery/zones
{ "name": "Centro", "matchType": "NEIGHBORHOOD",
  "values": ["Centro","Vila Nova"],
  "fee": 500, "additionalMinutes": 0,
  "minimumOrder": 3000, "priority": 1, "isActive": true }

GET /v1/delivery/zones/resolve?zip=...&neighborhood=...
→ { "zone": { "id": "...", "name": "Centro", "fee": 500,
              "additionalMinutes": 0, "minimumOrder": 3000 } }
→ 404 { "code": "OUT_OF_DELIVERY_AREA" }

PATCH /v1/delivery/zones/{id}   { "isActive": false }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `delivery_zone` | Zona de entrega | `name`, `match_type`, `values`, `fee`, `additional_minutes`, `minimum_order`, `priority`, `is_active` |
| `customer_address` | Endereço com zona resolvida | `zone_id` |
| `order` | Taxa aplicada | `delivery_fee` |

## 9. Comportamento offline

Configuração de nuvem, usada pelo canal público, que também é de nuvem.

## 10. Interface e experiência

- Cadastro por bairro como padrão — CEP exige base de dados e raio exige geolocalização precisa
- Taxa e prazo lado a lado, para que a relação fique evidente
- Desativação temporária em um clique, para quando falta entregador
- Aviso de pedido mínimo antes da confirmação, com o valor faltante

## 11. Métricas, alertas e observabilidade

- Pedidos e faturamento por zona
- Taxa média de entrega recebida
- Pedidos recusados por estar fora de área — insumo de decisão de expansão
- Tempo real de entrega por zona contra o prazo cadastrado

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Resolução de zona por bairro, CEP e prioridade |
| Integração | Taxa e prazo aplicados corretamente ao pedido |
| Integração | Endereço fora de área recusado |
| Integração | Pedido mínimo por zona validado |

## 13. Dependências

**Depende de:** US-002  
**Habilita:** US-130, US-132, US-136

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

- **Pendência da Visão Geral 6.2** — taxa por região não foi definida com o cliente. Levantar antes da Fase 4.

---

*US-131 · Épico E-13 · Pacote 004_DonaBetinha · Replay Studio.*