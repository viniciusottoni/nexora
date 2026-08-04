# US-014 · Preco por canal de venda

|  |  |
|---|---|
| **Épico** | [E-01 · Catalogo e Cardapio](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 1 |
| **Requisitos funcionais** | RF-CAT-06 |
| **Regras de negócio** | RN-021 |
| **ADRs** | ADR-017 |
| **Eventos** | EVT-052 |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** cobrar preços diferentes no salão e no delivery,
> **para** que a taxa do canal não coma a minha margem.

## 2. Contexto e motivação

Delivery tem custo que o salão não tem: embalagem, entregador, comissão de marketplace. Preço único nos dois canais significa margem diferente sem que ninguém perceba — exatamente a cegueira que o produto existe para eliminar.

O modelo já prevê `price` por canal e historizado. Esta história expõe isso na interface de gestão.

## 3. Escopo

### 3.1 Dentro desta história

- Preço por canal: `DINE_IN`, `DELIVERY`, `COUNTER`, `MARKETPLACE`
- Herança do preço base quando o canal não tem preço próprio
- Historização com `valid_from` e `valid_to`
- Edição em massa por categoria (reajuste percentual)
- Exibição do preço correto conforme o canal do pedido

### 3.2 Fora desta história

- Precificação diferenciada por público (RN-021, pendência ligada ao app de frios)
- Promoções e combos (RF-CAT-11, Fase 4)
- Preço dinâmico por horário

## 4. Critérios de aceite

```gherkin
Funcionalidade: Preço por canal

  Cenário: Preço distinto no delivery
    Dado uma pizza com preço de R$ 45,00 no salão e R$ 52,00 no delivery
    Quando o cliente pedir pela mesa
    Então o preço aplicado deve ser R$ 45,00
    Quando o cliente pedir pelo delivery
    Então o preço aplicado deve ser R$ 52,00

  Cenário: Herança do preço base
    Dado uma variação sem preço específico para o canal de balcão
    Quando um pedido de balcão for criado
    Então deve ser aplicado o preço base da variação

  Cenário: Reajuste em massa
    Dado uma categoria com 20 produtos
    Quando o gestor aplicar reajuste de 8% no canal de delivery
    Então os 20 preços devem ser atualizados
    E os preços anteriores devem ficar historizados
    E a ação deve constar em audit_log

  Cenário: Preço da época preservado
    Dado um pedido fechado há 30 dias com pizza a R$ 45,00
    Quando o preço atual for R$ 52,00
    Então o relatório histórico deve continuar usando R$ 45,00
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-021 | Regra de precificação diferenciada por canal/público | **[PENDÊNCIA]** — o canal está implementado; a diferenciação por público depende da decisão sobre o app de frios |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-052 | `price.changed` | Preço alterado em um canal | variantId, channel, oldAmount, newAmount, validFrom | ↓ |

## 7. Contrato de API

```http
PUT /v1/catalog/variants/{id}/prices
{ "prices": [ { "channel": "DINE_IN",  "amount": 4500 },
              { "channel": "DELIVERY", "amount": 5200 } ] }

POST /v1/catalog/prices/bulk-adjust
{ "categoryId": "...", "channel": "DELIVERY", "percent": 8 }
→ 200 { "updated": 20, "effectiveFrom": "..." }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `price` | Preço historizado por canal | `variant_id`, `channel`, `amount`, `valid_from`, `valid_to` |
| `audit_log` | Registro do reajuste | `action=PRICE_CHANGED`, `before`, `after`, `actor_id` |

## 9. Comportamento offline

Replicado ao edge. O pedido criado offline usa o preço vigente na última sincronização — o correto, porque é o preço que o cliente viu no cardápio no momento do pedido.

Reajuste feito na nuvem durante uma queda de internet entra em vigor no edge apenas após o pull. O intervalo é registrado e visível no indicador de atraso de sincronização (US-065).

## 10. Interface e experiência

- Tabela de preços por canal editável em linha, sem navegação
- Reajuste em massa com pré-visualização do antes e depois antes de confirmar
- Aviso quando o preço de delivery for menor que o de salão — geralmente é engano

## 11. Métricas, alertas e observabilidade

- Margem por canal, por produto — o indicador que justifica esta história
- Histórico de reajustes por período e por autor

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Resolução de preço por canal com e sem herança do base |
| Unitário | Cálculo de reajuste percentual com arredondamento em centavos |
| Integração | Pedido histórico mantém o preço da época |
| Integração | Reajuste em massa é transacional — falha parcial não deixa preços inconsistentes |

## 13. Dependências

**Depende de:** US-011  
**Habilita:** US-030, US-109, US-130

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

- RN-021 (precificação por público, do tipo "preço de Ceasa") é pendência aberta ligada ao app de frios. Não implementar por antecipação — o modelo de canal já cobre o caso do delivery.

---

*US-014 · Épico E-01 · Pacote 004_DonaBetinha · Replay Studio.*