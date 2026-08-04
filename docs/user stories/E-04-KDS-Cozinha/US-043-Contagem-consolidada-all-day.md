# US-043 · Contagem consolidada all-day

|  |  |
|---|---|
| **Épico** | [E-04 · KDS Cozinha](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | S — Should have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 4 |
| **Requisitos funcionais** | RF-KDS-07 |
| **Regras de negócio** | — |
| **ADRs** | — |
| **Eventos** | — |
| **Aplicações** | web-kds, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** pizzaiolo (P3),
> **quero** ver quantas unidades de cada produto estão pendentes no total,
> **para** que eu possa preparar em lote em vez de um a um.

## 2. Contexto e motivação

Prática consagrada de cozinha profissional: o "all-day" mostra a soma de itens iguais em toda a fila. Saber que há 12 pizzas de mussarela pendentes muda a forma de trabalhar — abre-se espaço para preparar massa e cobertura em lote.

O cuidado é não prejudicar o pedido mais antigo em nome do agrupamento. Nesta fase a contagem é apenas informativa; a sugestão ativa de agrupamento fica para a Fase 3 (RF-KDS-15).

## 3. Escopo

### 3.1 Dentro desta história

- Painel lateral com a contagem consolidada por produto e variação
- Contagem por praça, respeitando o filtro ativo
- Ordenação por quantidade pendente
- Atualização em tempo real
- Frações contadas proporcionalmente (meia pizza conta 0,5)

### 3.2 Fora desta história

- Sugestão ativa de agrupamento (RF-KDS-15, Fase 3)
- Agrupamento automático de itens

## 4. Critérios de aceite

```gherkin
Funcionalidade: Contagem all-day

  Cenário: Consolidação por produto
    Dado 12 itens de Pizza G Mussarela pendentes em pedidos distintos
    Quando o painel all-day for exibido
    Então deve mostrar "Pizza G Mussarela · 12"

  Cenário: Contagem proporcional de frações
    Dado quatro pedidos de meio a meio, todos com metade de Mussarela
    Quando o all-day for calculado
    Então Mussarela deve contar 2 unidades, não 4

  Cenário: Respeito ao filtro de praça
    Dado o KDS filtrado pela praça Forno
    Quando o all-day for exibido
    Então deve considerar apenas itens do forno

  Cenário: Atualização em tempo real
    Dado o all-day exibindo 12 unidades de um produto
    Quando um desses itens for marcado como pronto
    Então a contagem deve cair para 11 imediatamente

  Cenário: Fila vazia
    Dado nenhum item pendente
    Quando o painel for exibido
    Então deve indicar fila vazia, sem lista
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/kds/queue?stationId=...
→ { "items": [...],
    "allDay": [ { "variantId": "...", "productName": "Pizza G Mussarela",
                  "pending": 12, "fractionQuantity": 10.5 } ] }
```

> `pending` é a contagem de itens; `fractionQuantity` é a soma ponderada pelas frações — os dois números são úteis e diferentes.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Itens pendentes agregados | `variant_id`, `status` em QUEUED/FIRED/IN_OVEN |
| `order_item_fraction` | Peso das frações | `variant_id`, `weight` |

## 9. Comportamento offline

Cálculo local, derivado da fila. Nenhuma dependência externa.

## 10. Interface e experiência

- Painel lateral estreito, sem competir com a fila principal pela atenção
- Ordenado por quantidade decrescente — o que mais tem pendente aparece primeiro
- Números grandes, texto curto
- Ocultável, para telas menores

## 11. Métricas, alertas e observabilidade

- Correlação entre uso do all-day e tempo médio de produção — valida a hipótese de ganho por lote
- Produtos com maior fila recorrente, insumo de decisão de cardápio e de capacidade

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Contagem proporcional com frações de pesos variados |
| Integração | Contagem respeita o filtro de praça |
| Integração | Atualização em tempo real ao avançar item |

## 13. Dependências

**Depende de:** US-040, US-042  
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

- Preparar em lote pode atrasar o pedido mais antigo. O indicador é informativo; a política de agrupamento é decisão humana nesta fase.

---

*US-043 · Épico E-04 · Pacote 004_DonaBetinha · Replay Studio.*