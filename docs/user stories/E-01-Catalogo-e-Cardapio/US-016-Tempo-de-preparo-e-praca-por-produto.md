# US-016 · Tempo de preparo e praca por produto

|  |  |
|---|---|
| **Épico** | [E-01 · Catalogo e Cardapio](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 2 |
| **Requisitos funcionais** | RF-CAT-08, RF-CAT-09 |
| **Regras de negócio** | RN-013 |
| **ADRs** | ADR-012 |
| **Eventos** | EVT-050 |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** definir quanto tempo cada produto leva e em qual praça é feito,
> **para** que o sistema roteie corretamente e calcule prazos que se cumprem.

## 2. Contexto e motivação

Dois campos aparentemente banais que sustentam três funcionalidades inteiras: o roteamento de itens para a praça certa (US-031), o escalonamento de cor do cronômetro do KDS (US-040) e o cálculo de prazo dinâmico e fire time (E-11).

O tempo de preparo cadastrado é a estimativa inicial. A partir do momento em que houver histórico real, o sistema passa a poder comparar estimado versus realizado — e essa comparação é uma das primeiras coisas úteis que o painel do dono entrega.

## 3. Escopo

### 3.1 Dentro desta história

- Campo de tempo de preparo em minutos por variação
- Vínculo de produto a praça de produção
- Limiares de atenção e crítico por produto, com herança do padrão do tenant
- Comparativo entre tempo cadastrado e tempo real médio, exibido na tela de cadastro

### 3.2 Fora desta história

- Fire time e sequenciamento reverso (US-115, Fase 2)
- Prazo dinâmico por fila (US-118, Fase 2)
- Ajuste automático do tempo cadastrado a partir do histórico

## 4. Critérios de aceite

```gherkin
Funcionalidade: Tempo de preparo e praça

  Cenário: Roteamento pela praça
    Dado que "Pizza Mussarela" está vinculada à praça "Forno"
    E "Refrigerante" está vinculado à praça "Bebidas"
    Quando um pedido com os dois itens for confirmado
    Então cada item deve aparecer na fila da sua praça

  Cenário: Limiar herdado do tenant
    Dado o limiar padrão de atenção configurado em 12 minutos
    E um produto sem limiar próprio
    Quando o item ultrapassar 12 minutos
    Então o cartão deve entrar em estado de atenção

  Cenário: Limiar específico do produto
    Dado uma pizza com limiar próprio de 18 minutos
    Quando o item ultrapassar 12 minutos
    Então ainda deve estar em estado normal
    E deve entrar em atenção apenas aos 18 minutos

  Cenário: Comparativo estimado versus real
    Dado um produto com tempo cadastrado de 12 minutos
    E tempo real médio de 16 minutos nos últimos 30 dias
    Quando o gestor abrir o cadastro
    Então a divergência deve ser exibida com sugestão de ajuste
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-013 | O prazo informado ao cliente é calculado pela fila atual, nunca fixo | **[HIPÓTESE]** — o tempo cadastrado é insumo do cálculo, não o prazo final |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-050 | `product.updated` | Tempo ou praça alterados | variantId, prepMinutes, stationId | ↓ |

## 7. Contrato de API

```http
PATCH /v1/catalog/variants/{id}
{ "prepMinutes": 12, "warnMinutes": 15, "criticalMinutes": 20 }

PATCH /v1/catalog/products/{id}
{ "stationId": "<forno>" }

GET /v1/catalog/variants/{id}/prep-time-analysis
→ { "configuredMinutes": 12, "actualAvgMinutes": 16.4,
    "actualP90Minutes": 21.2, "sampleSize": 340,
    "suggestion": 16 }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `product_variant` | Tempo e limiares | `prep_minutes`, `warn_minutes`, `critical_minutes` |
| `product` | Praça de destino | `station_id` |
| `station` | Praça de produção | `code`, `name`, `capacity_slots`, `is_bottleneck` |
| `metric_product_daily` | Tempo real observado | `avg_prep_seconds`, `p90_prep_seconds` |

## 9. Comportamento offline

Replicado ao edge e usado localmente pelo KDS para escalonamento de cor e por qualquer cálculo de prazo. Nenhuma dependência de nuvem em tempo de operação.

## 10. Interface e experiência

- Tempo e praça editáveis na mesma linha da variação, sem tela separada
- Divergência entre cadastrado e real destacada com cor, e sugestão de valor — o gestor não precisa consultar relatório para descobrir que a estimativa está errada
- Praça exibida como etiqueta colorida na lista de produtos

## 11. Métricas, alertas e observabilidade

- Divergência entre tempo cadastrado e p90 real, por produto — relatório de calibração
- Distribuição de itens por praça, indicando desbalanceamento de carga

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Herança de limiar do tenant quando o produto não define o próprio |
| Integração | Roteamento correto por praça em pedido com itens de praças distintas |
| Integração | Cálculo do comparativo estimado versus real com amostra suficiente |

## 13. Dependências

**Depende de:** US-011, US-017  
**Habilita:** US-031, US-040, US-115, US-118

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

- Tempo cadastrado otimista demais gera cronômetro sempre vermelho e a cozinha aprende a ignorar a cor — o alerta perde valor. Calibrar no piloto com dados reais das duas primeiras semanas.

---

*US-016 · Épico E-01 · Pacote 004_DonaBetinha · Replay Studio.*