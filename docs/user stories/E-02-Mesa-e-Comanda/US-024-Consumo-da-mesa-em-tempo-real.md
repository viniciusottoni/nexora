# US-024 · Consumo da mesa em tempo real

|  |  |
|---|---|
| **Épico** | [E-02 · Mesa e Comanda](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 3 |
| **Requisitos funcionais** | RF-SAL-06 |
| **Regras de negócio** | — |
| **ADRs** | ADR-011 |
| **Eventos** | — |
| **Aplicações** | web-menu, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** cliente do salão (P1),
> **quero** ver o que já foi consumido na mesa e quanto deu até agora,
> **para** que eu não precise perguntar ao garçom nem ter surpresa na conta.

## 2. Contexto e motivação

Registrado literalmente na descoberta: *"saber como está o consumo das mesas"*. Para o cliente, é transparência; para o estabelecimento, é redução de uma das interrupções mais frequentes do garçom.

A informação precisa incluir o **status de cada item** — o cliente que vê "em produção" tem paciência; o cliente que não vê nada chama o garçom em cinco minutos.

## 3. Escopo

### 3.1 Dentro desta história

- Lista dos itens da sessão com quantidade, valor e status
- Subtotal, taxa de serviço estimada e total
- Atualização em tempo real por WebSocket
- Status por item traduzido para linguagem do cliente ("na fila", "sendo preparado", "a caminho")
- Indicação de qual pedido é de quem, quando informado

### 3.2 Fora desta história

- Fechamento e pagamento (E-05)
- Divisão de conta (US-027)
- Identificação obrigatória do cliente por item (Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Consumo da mesa em tempo real

  Cenário: Visualização do consumo
    Dado uma sessão com quatro itens lançados
    Quando o cliente abrir a aba de consumo
    Então deve ver os quatro itens com quantidade, valor e status
    E deve ver subtotal, taxa de serviço estimada e total

  Cenário: Atualização automática
    Dado a tela de consumo aberta
    Quando a cozinha marcar um item como pronto
    Então o status deve mudar na tela em até 2 segundos, sem recarregar

  Cenário: Item cancelado
    Dado um item que foi cancelado pelo garçom
    Quando o cliente olhar o consumo
    Então o item deve aparecer riscado e não deve compor o total

  Cenário: Taxa de serviço como estimativa
    Dado a taxa de serviço configurada em 10%
    Quando o total for exibido
    Então a taxa deve ser mostrada separadamente
    E deve ficar claro que é opcional

  Cenário: Privacidade entre mesas
    Dado o token de sessão da mesa 12
    Quando alguém tentar consultar o consumo da mesa 13
    Então deve receber 404
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-010 | Taxa de serviço é opcional ao cliente; a retirada é registrada e auditada | **[HIPÓTESE]** — a taxa é exibida separada e identificada como opcional |
| RN-015 | Isolamento entre estabelecimentos e entre mesas | Token de sessão dá acesso a uma única mesa |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> Consome `order.placed`, `order.item.fired`, `order.item.ready`, `order.item.served` e `order.item.cancelled`.

## 7. Contrato de API

```http
GET /v1/public/sessions/current
Authorization: Bearer <sessionToken da mesa>
→ {
    "items": [ { "name": "Pizza G Mussarela / Calabresa", "quantity": 1,
                 "unitPrice": 5200, "total": 5200,
                 "status": "IN_PRODUCTION", "statusLabel": "Sendo preparada",
                 "etaMinutes": 6 } ],
    "subtotal": 8700, "serviceFee": 870, "serviceFeeOptional": true,
    "total": 9570,
    "openedAt": "...", "minutesOpen": 47
  }

# WebSocket, sala table:{id}:
{ "type": "order.item.ready", "data": { "orderItemId": "...", "productName": "..." } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `table_session` | Sessão e totais | `total`, `service_fee`, `status` |
| `order` / `order_item` | Itens e status | `status`, `unit_price`, `quantity` |
| `tenant_config` | Percentual da taxa de serviço | `operation.serviceFeePercent` |

## 9. Comportamento offline

Integralmente local, com WebSocket do edge. O cliente conectado ao Wi-Fi da loja vê o consumo atualizado mesmo com a internet caída.

Fallback de polling a cada 5 s se o WebSocket cair.

## 10. Interface e experiência

- Status em linguagem do cliente, nunca o nome técnico do estado da máquina
- Estimativa de tempo restante por item quando disponível — reduz a ansiedade e a chamada ao garçom
- Taxa de serviço sempre destacada como opcional, sem letras miúdas
- Total sempre visível, fixo no rodapé

## 11. Métricas, alertas e observabilidade

- Frequência de consulta ao consumo por sessão — indicador de valor percebido
- Correlação entre consulta ao consumo e chamada de garçom: se a consulta sobe e a chamada cai, a história cumpriu o objetivo

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Total calculado bate com a soma de itens, modificadores e taxa |
| Integração | Item cancelado não compõe o total |
| Integração | Atualização em tempo real em menos de 2 s |
| Segurança | Token de uma mesa não acessa outra |
| Caos offline | Consumo correto e reativo com internet da loja caída |

## 13. Dependências

**Depende de:** US-021, US-022  
**Habilita:** US-026, US-027

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

- Exibir taxa de serviço como se fosse obrigatória gera reclamação e é problema de consumidor, não de sistema. A regra RN-010 é hipótese e precisa de validação com o cliente.

---

*US-024 · Épico E-02 · Pacote 004_DonaBetinha · Replay Studio.*