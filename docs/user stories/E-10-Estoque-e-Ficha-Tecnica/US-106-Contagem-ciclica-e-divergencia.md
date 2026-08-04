# US-106 · Contagem ciclica e divergencia

|  |  |
|---|---|
| **Épico** | [E-10 · Estoque e Ficha Tecnica](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-EST-07 |
| **Regras de negócio** | — |
| **ADRs** | ADR-008, ADR-023 |
| **Eventos** | EVT-044, EVT-043 |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** estoquista (P7) e gestor (P8),
> **quero** contar o estoque fisicamente e comparar com o saldo do sistema,
> **para** que eu descubra divergência antes que ela vire prejuízo grande.

## 2. Contexto e motivação

A contagem é o que valida todo o resto. O saldo do sistema é teórico — derivado de entradas, baixas por ficha e perdas registradas. A contagem física é a realidade. A diferença entre os dois é **perda não registrada, desvio ou ficha técnica errada**, e cada uma dessas hipóteses leva a uma ação diferente.

Contagem cíclica (parcial, rotativa) é preferível a inventário geral: menos disruptiva e mais frequente, portanto mais útil.

## 3. Escopo

### 3.1 Dentro desta história

- Criação de contagem com seleção de insumos
- Registro da quantidade contada por insumo
- Cálculo da divergência em quantidade e em valor
- Ajuste de saldo por movimento de ajuste, com motivo
- Autorização de perfil superior para ajuste acima do limite
- Ciclos de contagem configuráveis por categoria
- Histórico de divergências por insumo

### 3.2 Fora desta história

- Inventário geral com bloqueio de operação
- Apuração de CMV real (US-107, que consome esta contagem)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Contagem cíclica

  Cenário: Contagem com divergência
    Dado saldo de sistema de 15,0 kg
    E contagem física de 14,2 kg
    Quando a contagem for registrada
    Então a divergência de 0,8 kg deve ser calculada
    E o impacto em custo deve ser exibido

  Cenário: Ajuste de saldo
    Dado uma divergência confirmada
    Quando o ajuste for aplicado com motivo
    Então deve ser criado movimento de ajuste
    E o saldo derivado deve passar a corresponder ao contado

  Cenário: Ajuste acima do limite
    Dado o limite de ajuste sem autorização configurado
    Quando um ajuste acima do limite for aplicado
    Então deve ser exigida autorização de perfil superior
    E deve ser registrado em auditoria

  Cenário: Contagem parcial
    Dado uma contagem cíclica de apenas 12 insumos
    Quando for concluída
    Então apenas esses insumos devem ser ajustados
    E os demais devem permanecer inalterados

  Cenário: Divergência recorrente
    Dado um insumo com divergência em três contagens seguidas
    Quando o histórico for consultado
    Então o padrão deve ser destacado
    E deve ser sugerida revisão da ficha técnica

  Cenário: Contagem sem divergência
    Dado contagem igual ao saldo de sistema
    Quando for registrada
    Então nenhum ajuste deve ser criado
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-011 | Ação sensível exige autorização de perfil superior | Ajuste de estoque acima do limite |
| RN-004 | Toda ação registra autor, horário e dispositivo | Contagem e ajuste registram quem contou |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-044 | `stock.counted` | Contagem cíclica | countId, items[], totalDivergenceCost | ↑ |
| EVT-043 | `stock.adjusted` | Ajuste manual | ingredientId, qty, reason, authorizedBy | ↑ |

> Reação normativa: `stock.counted` → saldo ajustado, notifica o gestor se divergente, alimenta **CMV real e divergência**.

## 7. Contrato de API

```http
POST /v1/inventory-counts
{ "name": "Contagem semanal — laticínios",
  "items": [ { "ingredientId": "...", "countedQty": 14.2 } ] }
→ 201 { "count": {...},
        "divergences": [ { "ingredientId": "...", "name": "Mussarela",
                           "expected": 15.0, "counted": 14.2,
                           "divergence": -0.8, "costImpact": -2720 } ],
        "totalDivergenceCost": -2720 }

POST /v1/inventory-counts/{id}/apply
X-Authorization-Token: <se acima do limite>
{ "reason": "..." }

GET /v1/ingredients/{id}/count-history
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `inventory_count` | Contagem | `name`, `counted_at`, `counted_by`, `total_divergence_cost`, `applied_at` |
| `inventory_count_item` | Item contado | `ingredient_id`, `expected_qty`, `counted_qty`, `divergence`, `cost_impact` |
| `stock_movement` | Ajuste gerado | `type=ADJUSTMENT`, `quantity`, `reason`, `authorized_by` |
| `audit_log` | Registro do ajuste | `action=STOCK_ADJUSTED`, `before`, `after` |

## 9. Comportamento offline

Contagem registrada na nuvem. Não é operação de tempo real.

O ajuste resultante é um **movimento** como qualquer outro — desce ao edge pelo pull e compõe o saldo derivado, sem conflito com as baixas feitas offline (ADR-008).

## 10. Interface e experiência

- Lista de contagem otimizada para uso no depósito, pelo celular
- Saldo esperado oculto durante a contagem, para não induzir o contador
- Divergência e impacto em custo exibidos após o registro de todos os itens
- Insumos com divergência recorrente destacados, com sugestão de revisar a ficha

## 11. Métricas, alertas e observabilidade

- Divergência por insumo e por contagem, em quantidade e valor
- Acurácia de estoque (percentual de itens sem divergência relevante)
- Divergência recorrente por insumo — o sinal mais forte de ficha técnica errada ou desvio
- Insumo da divergência que compõe o CMV real (US-107)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo de divergência em quantidade e custo |
| Integração | Ajuste cria movimento e o saldo derivado passa a bater com o contado |
| Integração | Autorização exigida acima do limite |
| Integração | Contagem parcial não afeta insumos não contados |

## 13. Dependências

**Depende de:** US-103, US-104  
**Habilita:** US-107

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

- Contagem feita com o saldo do sistema à vista induz o contador a confirmar o número esperado. Ocultar o esperado é decisão de desenho deliberada.

---

*US-106 · Épico E-10 · Pacote 004_DonaBetinha · Replay Studio.*