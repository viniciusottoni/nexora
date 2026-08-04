# US-116 · Prioridade dinamica explicavel

|  |  |
|---|---|
| **Épico** | [E-11 · Inteligencia de Fluxo](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | S — Should have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-KDS-12 |
| **Regras de negócio** | — |
| **ADRs** | ADR-012, ADR-032 |
| **Eventos** | — |
| **Aplicações** | web-kds, api-edge, packages/domain |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** pizzaiolo (P3),
> **quero** que a fila me sugira a ordem, explicando o porquê,
> **para** que eu confie na sugestão em vez de ignorá-la.

## 2. Contexto e motivação

Ordem por chegada é simples e previsível, mas ignora prazo, sincronização e canal. Um pedido de delivery esfria na rota; um item cuja mesa já tem outro pronto deveria acompanhar.

A fórmula de pontuação está no documento 04, seção 7.2, com pesos configuráveis por tenant. E a restrição de produto é explícita no mesmo documento: *a ordem é exibida com o motivo e pode ser sobreposta pelo operador — sistema que reordena sem explicar perde a confiança da cozinha na primeira semana*.

## 3. Escopo

### 3.1 Dentro desta história

- Cálculo do score de prioridade por item
- Pesos configuráveis por tenant
- Motivo textual da posição, exibido no cartão
- Sobreposição manual, com fixação de ordem
- Modo por chegada como alternativa sempre disponível
- Registro de sobreposições para calibração

### 3.2 Fora desta história

- Fire time (US-115, que alimenta um dos fatores)
- Agrupamento de itens idênticos (RF-KDS-15, Fase 3)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Prioridade dinâmica

  Cenário: Ordem com motivo
    Dado a fila ordenada por prioridade calculada
    Quando o cartão for exibido
    Então deve indicar o motivo da posição, como "prazo em 3 min"
    E o motivo deve ser compreensível sem treinamento

  Cenário: Fatores da pontuação
    Dado itens com urgência, espera, sincronização e canal distintos
    Quando o score for calculado
    Então deve considerar todos os fatores com os pesos configurados
    E itens aguardando fire time devem ser despriorizados

  Cenário: Sobreposição pelo operador
    Dado a ordem sugerida pelo sistema
    Quando o operador fixar um item no topo
    Então a fixação deve ser respeitada
    E deve ser registrada para análise

  Cenário: Modo por chegada
    Dado um tenant que prefere ordem por chegada
    Quando o modo for configurado
    Então a fila deve seguir estritamente a ordem de confirmação

  Cenário: Estabilidade da ordem
    Dado a fila em exibição
    Quando os scores forem recalculados
    Então a ordem não deve mudar a cada segundo
    E deve haver histerese para evitar reordenação constante

  Cenário: Sincronização de mesa
    Dado um item cuja mesa já tem outro item pronto
    Quando o score for calculado
    Então ele deve ganhar prioridade
    E o motivo deve indicar a sincronização
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-014 | Itens do mesmo pedido devem sair sincronizados | É um dos fatores do score |
| RN-016 | Configuração, não código | Pesos configuráveis por tenant |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/kds/queue?stationId=...&order=PRIORITY
→ { "items": [ { "orderItemId": "...", "priorityScore": 87,
                 "priorityReason": "prazo em 3 min",
                 "pinned": false } ] }

POST /v1/kds/items/{id}/pin
POST /v1/kds/items/{id}/unpin

PATCH /v1/tenant/config
{ "kitchen": { "queueOrder": "PRIORITY",
               "priorityWeights": { "urgency": 0.4, "waiting": 0.25,
                                    "sync": 0.15, "channel": 0.1,
                                    "fireTime": 0.1 } } }
```

> Fórmula de referência (doc. 04, 7.2): score = w1·urgência + w2·espera + w3·sincronização + w4·canal − w5·fire_time.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Score e fixação | `priority_score`, `priority_reason`, `pinned`, `pinned_by` |
| `tenant_config` | Pesos e modo | `kitchen.priorityWeights`, `kitchen.queueOrder` |

## 9. Comportamento offline

Cálculo integralmente local, recalculado a cada mudança da fila.

## 10. Interface e experiência

- Motivo em linguagem direta, no cartão, sempre — não em tooltip nem em tela de detalhe
- Histerese obrigatória: fila que reordena a cada segundo é inutilizável
- Fixação de item acessível pelo teclado numérico
- Modo por chegada a um toque de distância, sempre
- Nenhuma reordenação de item já iniciado

## 11. Métricas, alertas e observabilidade

- Taxa de sobreposição pelo operador — o indicador de confiança no algoritmo
- OTD com ordem por prioridade contra ordem por chegada
- Frequência de reordenação da fila

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo do score com pesos variados |
| Unitário | Histerese impede reordenação constante |
| Integração | Motivo coerente com o fator dominante do score |
| Integração | Fixação respeitada e registrada |
| Usabilidade | Cozinha compreende os motivos sem treinamento |

## 13. Dependências

**Depende de:** US-040, US-115  
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

- **Restrição de produto explícita do doc. 04**: sistema que reordena sem explicar perde a confiança da cozinha. O motivo não é adorno — é requisito.
- Pesos mal calibrados produzem ordem contraintuitiva. Iniciar com ordem por chegada como padrão e migrar após validação.

---

*US-116 · Épico E-11 · Pacote 004_DonaBetinha · Replay Studio.*