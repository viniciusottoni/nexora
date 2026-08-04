# US-135 · Endereco salvo e repetir pedido

|  |  |
|---|---|
| **Épico** | [E-13 · Delivery Proprio](./README.md) |
| **Fase** | 4 — Delivery próprio |
| **Prioridade** | S — Should have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 4 |
| **Requisitos funcionais** | RF-DEL-05 |
| **Regras de negócio** | — |
| **ADRs** | — |
| **Eventos** | — |
| **Aplicações** | web-menu, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** cliente de delivery (P6),
> **quero** ter meu endereço salvo e repetir o pedido anterior com um toque,
> **para** que pedir de novo seja mais rápido que abrir o aplicativo do concorrente.

## 2. Contexto e motivação

Recompra é o que sustenta um canal próprio. Cada etapa entre a vontade de pedir e a confirmação é uma chance de o cliente desistir e abrir o marketplace.

Endereço salvo e repetição de pedido reduzem o fluxo de quatro etapas para uma.

## 3. Escopo

### 3.1 Dentro desta história

- Múltiplos endereços por cliente, com apelido
- Endereço padrão
- Histórico de pedidos do cliente
- Repetição de pedido anterior com um toque
- Verificação de disponibilidade dos itens na repetição
- Atualização de preço na repetição

### 3.2 Fora desta história

- Programa de fidelidade (Fase 6)
- Recomendação personalizada

## 4. Critérios de aceite

```gherkin
Funcionalidade: Endereço salvo e repetição

  Cenário: Endereço padrão
    Dado um cliente com endereço padrão salvo
    Quando iniciar um novo pedido
    Então o endereço deve vir preenchido
    E deve ser possível trocar em um toque

  Cenário: Repetição de pedido
    Dado um pedido anterior do cliente
    Quando ele tocar em "pedir de novo"
    Então o carrinho deve ser montado com os mesmos itens e opções
    E o preço atual deve ser aplicado

  Cenário: Item indisponível na repetição
    Dado um pedido anterior com item hoje indisponível
    Quando o cliente repetir
    Então o item indisponível deve ser sinalizado
    E os demais devem ser mantidos no carrinho

  Cenário: Mudança de preço
    Dado um item que subiu de preço desde o pedido anterior
    Quando a repetição for montada
    Então o novo preço deve ser aplicado
    E a diferença deve ficar visível antes de confirmar

  Cenário: Múltiplos endereços
    Dado um cliente com casa e trabalho salvos
    Quando escolher o endereço
    Então deve ver os dois com apelido, e a taxa de cada zona
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET  /v1/public/customers/me/addresses
POST /v1/public/customers/me/addresses
{ "label": "Casa", "street": "...", "number": "...",
  "isDefault": true }

GET  /v1/public/customers/me/orders?limit=10
POST /v1/public/orders/{id}/repeat
→ 200 { "cart": { "items": [...], "unavailable": [...],
                  "priceChanges": [ { "name": "...", "oldPrice": 5200,
                                      "newPrice": 5500 } ] } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `customer_address` | Endereços do cliente | `label`, `is_default`, `zone_id` |
| `order` | Histórico do cliente | `customer_id`, `placed_at` |

## 9. Comportamento offline

Funcionalidade de nuvem.

## 10. Interface e experiência

- Endereço padrão preenchido, sem exigir confirmação a cada pedido
- Repetição acessível na tela inicial do cliente recorrente, não escondida no histórico
- Diferenças de preço e disponibilidade exibidas antes da confirmação, nunca depois
- Apelido do endereço, não o endereço completo, na escolha

## 11. Métricas, alertas e observabilidade

- Taxa de recompra
- Proporção de pedidos criados por repetição
- Tempo do acesso à confirmação, com e sem repetição

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Repetição monta o carrinho com opções idênticas |
| Integração | Item indisponível sinalizado sem quebrar a repetição |
| Integração | Preço atual aplicado, com diferença visível |

## 13. Dependências

**Depende de:** US-130  
**Habilita:** —

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

- Dados de endereço são pessoais e sujeitos à LGPD. Aplicar a política de retenção definida no doc. 02, seção 8.1: 24 meses sem novo pedido, seguidos de anonimização.

---

*US-135 · Épico E-13 · Pacote 004_DonaBetinha · Replay Studio.*