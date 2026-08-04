# US-103 · Baixa automatica na conclusao do item

|  |  |
|---|---|
| **Épico** | [E-10 · Estoque e Ficha Tecnica](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | M — Must have |
| **Estimativa** | 13 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-EST-04 |
| **Regras de negócio** | RN-007, RN-008 |
| **ADRs** | ADR-008, ADR-007 |
| **Eventos** | EVT-040 |
| **Aplicações** | api-edge, packages/domain |
| **Autoridade do dado** | Local (a baixa nasce na loja) → consolidada na nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** que o insumo saia do estoque automaticamente quando o item fica pronto,
> **para** que eu nunca mais precise dar baixa manual e o saldo reflita a realidade.

## 2. Contexto e motivação

É a história que fecha o ciclo entre operação e retaguarda: o mesmo evento que marca a pizza como pronta baixa a mussarela do estoque e registra o custo do item.

Duas decisões, ambas marcadas como hipótese e ambas com consequência forte:

- **RN-007** — a baixa ocorre na **conclusão da produção**, não no lançamento do pedido. Baixar no lançamento inventariaria consumo de itens que podem ser cancelados antes de começar.
- **ADR-008** — o saldo é **derivado da soma dos movimentos**, nunca armazenado como número editável. É isso que elimina o único conflito real de sincronização.

E a baixa de meio a meio é **proporcional ao peso da fração**: meia mussarela baixa metade da ficha.

## 3. Escopo

### 3.1 Dentro desta história

- Baixa disparada por `order.item.ready`
- Baixa proporcional ao peso das frações
- Baixa dos insumos de modificadores escolhidos
- Registro do custo unitário no item, congelado
- Movimento de estoque com referência ao item de origem
- Saldo sempre derivado; `current_stock` apenas materializado
- Tratamento de insumo sem saldo suficiente
- Funcionamento integral offline

### 3.2 Fora desta história

- Registro de perda por cancelamento (US-105)
- Bloqueio de venda por falta de insumo (RF-EST-12)
- Apuração de CMV (US-107)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Baixa automática de estoque

  Cenário: Baixa por ficha técnica
    Dado uma pizza com 180 g de mussarela na ficha
    Quando o item for marcado como pronto
    Então deve ser criado movimento de saída de 0,180 kg
    E o custo do item deve ser registrado pelo custo médio vigente

  Cenário: Baixa proporcional em meio a meio
    Dado um item com duas frações de peso 0,5
    Quando concluído
    Então cada ficha deve baixar metade das quantidades

  Cenário: Insumos de modificadores
    Dado um item com o adicional "Borda Catupiry" que consome 60 g
    Quando o item for concluído
    Então o catupiry também deve ser baixado

  Cenário: Custo congelado no item
    Dado um item concluído com custo de R$ 8,42
    Quando o custo do insumo mudar depois
    Então o custo registrado naquele item não deve mudar

  Cenário: Saldo derivado dos movimentos
    Dado entradas e saídas registradas
    Quando o saldo for consultado
    Então deve ser a soma algébrica dos movimentos
    E não deve existir campo de saldo editável manualmente

  Cenário: Saldo insuficiente
    Dado um insumo com saldo abaixo do necessário
    Quando o item for concluído
    Então a baixa deve ocorrer, gerando saldo negativo
    E um alerta deve ser disparado ao gestor
    E a operação não deve ser bloqueada

  Cenário: Baixa offline
    Dado que a loja está sem internet
    Quando um item for concluído
    Então a baixa deve ocorrer localmente
    E o movimento deve sincronizar depois, preservando occurredAt

  Cenário: Item sem ficha técnica
    Dado um produto sem ficha cadastrada
    Quando o item for concluído
    Então nenhuma baixa deve ocorrer
    E o produto deve constar no indicador de cobertura
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-006 | Cada produto possui ficha técnica que determina a baixa de insumo | A ficha é a fonte da baixa |
| RN-007 | A baixa de estoque ocorre na conclusão da produção do item, não no lançamento do pedido | **[HIPÓTESE]** — disparada por `order.item.ready` |
| RN-008 | Item cancelado após início da produção não estorna insumo; gera registro de perda | **[HIPÓTESE]** — tratada na US-105 |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-040 | `stock.deducted` | Baixa por produção | ingredientId, qty, orderItemId, cost | ↑ |
| EVT-008 | `order.item.ready` | Evento que dispara a baixa | prepSeconds | ↑ |

> Reação normativa (doc. 04, seção 5): `order.item.ready` → item em READY, notifica garçom e mesa, calcula tempo de produção e executa **baixa por ficha técnica**.

## 7. Contrato de API

```http
# Não há endpoint próprio — a baixa é efeito do avanço de estado no KDS.
# POST /v1/kds/items/{id}/advance com destino READY dispara:

# 1. resolve a ficha de cada fração, ponderada pelo peso
# 2. resolve os insumos dos modificadores
# 3. cria stock_movement (PRODUCTION, negativo) por insumo
# 4. grava unit_cost no order_item

GET /v1/ingredients/{id}/balance
→ { "balance": 12.4, "uom": "KG",
    "derivedFrom": { "movements": 1842 },
    "asOf": "..." }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `stock_movement` | Verdade do estoque, append-only | `ingredient_id`, `type=PRODUCTION`, `quantity` (negativa), `unit_cost`, `order_item_id`, `occurred_at` |
| `order_item` | Custo congelado | `unit_cost`, `recipe_version` |
| `ingredient` | Saldo materializado, nunca editado à mão | `current_stock`, `avg_cost` |
| `recipe` / `recipe_item` | Fonte das quantidades | `quantity`, `waste_percent` |

> Decisão 2 do ERD: `stock_movement` é a verdade, `current_stock` é materializado. Isso elimina o único conflito real de sincronização — não se sincroniza saldo, sincronizam-se movimentos (ADR-008).

## 9. Comportamento offline

**Crítico.** A baixa acontece no edge, no momento em que o item fica pronto, sem qualquer consulta à nuvem. Se dependesse de conexão, uma noite offline deixaria o estoque completamente divergente.

É também a razão pela qual a arquitetura sincroniza movimentos e não saldos: a loja dá baixa offline enquanto a nuvem registra uma entrada de compra. Como o saldo é derivado da soma, não há conflito — há apenas ordem de aplicação (doc. 02, seção 6.4).

A ficha técnica precisa estar replicada no edge, o que é feito pelo pull (US-063).

## 10. Interface e experiência

- Sem interface — a baixa é invisível ao operador, que é exatamente o objetivo
- Efeito visível: saldo de estoque atualizado em tempo real no painel
- Alerta ao gestor quando um insumo entra em saldo negativo — indica ficha errada ou entrada não registrada

## 11. Métricas, alertas e observabilidade

- Consumo teórico por insumo e por período — base do CMV teórico (US-107)
- Custo unitário por item produzido
- Insumos com saldo negativo — sintoma de ficha incorreta ou entrada faltando
- Percentual de itens concluídos sem ficha técnica

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Baixa proporcional com 2, 3 e 4 frações |
| Unitário | Inclusão dos insumos de modificadores |
| Unitário | Aplicação do percentual de perda e conversão de unidade |
| Integração | Movimento criado na mesma transação do avanço de estado |
| Integração | Custo congelado no item não muda com reajuste posterior |
| Integração | Saldo derivado bate com a soma dos movimentos |
| Caos offline | Baixa funcionando com internet caída; movimentos sincronizando depois |
| Propriedade | Para qualquer sequência de movimentos, o saldo derivado é consistente |

## 13. Dependências

**Depende de:** US-101, US-041, US-104  
**Habilita:** US-107, US-108, US-109

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
- [ ] Conferência manual do saldo derivado contra contagem física em uma amostra de insumos

## 15. Riscos, premissas e pendências

- **RN-007 é hipótese.** Confirmar com o cliente que a baixa na conclusão (e não no lançamento) é o comportamento desejado.
- Saldo negativo é sintoma, não erro do sistema — pode indicar ficha errada, entrada não registrada ou consumo não previsto. O alerta é a resposta correta, não o bloqueio.

---

*US-103 · Épico E-10 · Pacote 004_DonaBetinha · Replay Studio.*