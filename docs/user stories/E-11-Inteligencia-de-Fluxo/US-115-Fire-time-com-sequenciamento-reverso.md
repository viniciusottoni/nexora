# US-115 · Fire time com sequenciamento reverso

|  |  |
|---|---|
| **Épico** | [E-11 · Inteligencia de Fluxo](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | S — Should have |
| **Estimativa** | 13 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-KDS-09 |
| **Regras de negócio** | RN-014 |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-kds, api-edge, packages/domain |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** pizzaiolo (P3) e cliente do salão (P1),
> **quero** saber o momento certo de começar cada item para que o pedido saia junto,
> **para** que a batata não esfrie esperando a pizza ficar pronta.

## 2. Contexto e motivação

Resolve uma perda de qualidade que nenhum cronômetro detecta: os itens de um mesmo pedido ficam prontos em momentos diferentes, e os primeiros esfriam esperando os últimos.

A solução é sequenciamento reverso: o item mais longo define a saída, e os demais começam depois, de forma que todos fiquem prontos juntos. A função de referência está no documento 04, seção 7.1.

A RN-014 formaliza: *itens do mesmo pedido devem sair sincronizados; o sistema calcula o início de cada um*. Está marcada como hipótese.

## 3. Escopo

### 3.1 Dentro desta história

- Cálculo do `fire_at` por item, a partir do tempo de preparo
- Exibição no KDS: item aguardando o momento de iniciar, com contagem regressiva
- Destaque quando o momento chega
- Sobreposição manual pelo operador
- Recalculo quando o pedido recebe novo item
- Configuração por tenant: ativado, apenas informativo ou desativado
- Tratamento de itens de praças diferentes

### 3.2 Fora desta história

- Prioridade dinâmica da fila (US-116)
- Prazo dinâmico ao cliente (US-118)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Fire time

  Cenário: Saída sincronizada
    Dado um pedido com pizza de 12 min e fritas de 5 min
    Quando o pedido for confirmado
    Então a pizza deve ser liberada para produção imediatamente
    E as fritas devem ser liberadas 7 minutos depois
    E o KDS deve indicar o momento de iniciar cada item

  Cenário: Contagem regressiva visível
    Dado um item aguardando o momento de iniciar
    Quando o cartão for exibido
    Então deve mostrar quanto falta para o momento de início
    E deve ser visualmente distinto dos itens já liberados

  Cenário: Sobreposição pelo operador
    Dado um item aguardando fire time
    Quando o operador decidir iniciar antes
    Então deve conseguir avançar normalmente
    E a decisão deve ser registrada para análise

  Cenário: Novo item acrescentado ao pedido
    Dado um pedido em produção
    Quando um item de 15 minutos for acrescentado
    Então os fire times devem ser recalculados
    E o KDS deve refletir a mudança

  Cenário: Itens de praças diferentes
    Dado um pedido com item do forno e item de bebidas
    Quando os fire times forem calculados
    Então cada praça deve ver apenas seus próprios itens, com o momento correto

  Cenário: Modo apenas informativo
    Dado a configuração em modo informativo
    Quando o fire time for calculado
    Então o item deve aparecer normalmente na fila
    E o momento sugerido deve ser exibido sem ocultar o item

  Cenário: Item único no pedido
    Dado um pedido com um só item
    Quando o fire time for calculado
    Então deve ser imediato
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-014 | Itens do mesmo pedido devem sair sincronizados; o sistema calcula o início de cada um | **[HIPÓTESE]** — sequenciamento reverso a partir do item mais longo |
| RN-016 | Configuração, não código | Modo de operação configurável por tenant |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> O `fire_at` é calculado e gravado no item; não gera evento próprio. A decisão de sobrepor é registrada para análise.

## 7. Contrato de API

```http
GET /v1/kds/queue?stationId=...
→ { "items": [ { "orderItemId": "...", "productName": "Batata Frita",
                 "status": "QUEUED",
                 "fireAt": "2026-07-31T20:54:00Z",
                 "secondsUntilFire": 420,
                 "fireState": "WAITING" } ] }

# Função de referência (doc. 04, 7.1):
function calculateFireTimes(items) {
  const longest = Math.max(...items.map(i => i.prepMinutes));
  return new Map(items.map(i => [
    i.id, addMinutes(now, longest - i.prepMinutes)
  ]));
}

PATCH /v1/tenant/config
{ "kitchen": { "fireTimeMode": "ACTIVE" } }   # ACTIVE | INFORMATIVE | OFF
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Momento calculado | `fire_at`, `fire_overridden`, `fire_overridden_by` |
| `product_variant` | Tempo de preparo, insumo do cálculo | `prep_minutes` |
| `tenant_config` | Modo de operação | `kitchen.fireTimeMode` |

## 9. Comportamento offline

Cálculo integralmente local, em função pura de `packages/domain`. Nenhuma dependência de nuvem.

## 10. Interface e experiência

- Item aguardando com aparência distinta, mas nunca escondido — a cozinha precisa ver o que vem
- Contagem regressiva grande e legível a 1,5 m
- Transição para "pode iniciar" com destaque visual e sinal sonoro opcional
- Sobreposição sempre disponível, sem confirmação — o operador conhece a cozinha melhor que o algoritmo
- Modo informativo como padrão inicial, ativo só depois de validado com a equipe

## 11. Métricas, alertas e observabilidade

- Dispersão entre o `ready_at` dos itens do mesmo pedido — o número que o fire time existe para reduzir
- Taxa de sobreposição pelo operador — alta indica cálculo mal calibrado
- Tempo de expedição (MET-005) antes e depois da ativação

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo de fire time com combinações variadas de tempos |
| Unitário | Recalculo ao acrescentar item |
| Integração | Exibição correta no KDS por praça |
| Integração | Sobreposição registrada |
| Usabilidade | Validação com a cozinha real antes da ativação |
| Validação | Dispersão de `ready_at` medida antes e depois |

## 13. Dependências

**Depende de:** US-016, US-040  
**Habilita:** US-116, US-118

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

- **RN-014 é hipótese.** O ganho depende de a cozinha aceitar segurar itens curtos. Se a cultura for "faz tudo assim que chega", o recurso vira ruído.
- Fire time depende de tempo de preparo calibrado. Ativar antes de ter 30 dias de dado real produz sincronização errada.

---

*US-115 · Épico E-11 · Pacote 004_DonaBetinha · Replay Studio.*