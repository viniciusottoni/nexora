# US-105 · Registro de perda com motivo classificado

|  |  |
|---|---|
| **Épico** | [E-10 · Estoque e Ficha Tecnica](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-EST-06 |
| **Regras de negócio** | RN-008 |
| **ADRs** | ADR-023 |
| **Eventos** | EVT-042 |
| **Aplicações** | web-admin, web-pos, api-edge |
| **Autoridade do dado** | Local e nuvem |

---

## 1. História

> **Como** gestor (P8) e estoquista (P7),
> **quero** registrar perdas com o motivo classificado,
> **para** que eu saiba quanto estou jogando fora e por quê.

## 2. Contexto e motivação

Perda registrada com motivo transforma prejuízo invisível em informação acionável. A diferença entre perder por validade, por queima e por erro de produção leva a ações completamente distintas.

A RN-008 conecta este registro ao cancelamento: item cancelado depois de iniciado **não estorna insumo — gera perda**. Estornar inventariaria mercadoria que já foi consumida.

## 3. Escopo

### 3.1 Dentro desta história

- Registro manual de perda com insumo, quantidade e motivo
- Motivos classificados: validade, queima, quebra, erro de produção, cancelamento após início
- Perda automática por cancelamento de item já iniciado
- Custo da perda calculado pelo custo médio vigente
- Autorização de perfil superior acima do limite configurável
- Registro em auditoria

### 3.2 Fora desta história

- Contagem cíclica (US-106)
- Apuração de CMV (US-107)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Registro de perda

  Cenário: Perda por validade
    Dado 2,5 kg de um insumo vencido
    Quando a perda for registrada com motivo EXPIRATION
    Então deve ser criado movimento de saída
    E o custo da perda deve ser calculado pelo custo médio vigente

  Cenário: Perda por cancelamento após início
    Dado um item cancelado em estado FIRED
    Quando o cancelamento for confirmado
    Então os insumos da ficha devem gerar perda automática
    E o motivo deve ser CANCELLED_AFTER_START
    E nenhum estorno deve ocorrer

  Cenário: Cancelamento antes do início
    Dado um item cancelado em estado QUEUED
    Quando o cancelamento for confirmado
    Então nenhuma perda deve ser registrada
    E nenhuma baixa deve ter ocorrido

  Cenário: Autorização acima do limite
    Dado o limite de perda sem autorização configurado
    Quando uma perda acima do limite for registrada
    Então deve ser exigida autorização de perfil superior

  Cenário: Relatório por motivo
    Dado perdas de motivos variados no mês
    Quando o relatório for gerado
    Então deve mostrar valor e quantidade por motivo
    E o motivo de maior impacto deve estar destacado
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-008 | Item cancelado após início da produção não estorna insumo; gera registro de perda | **[HIPÓTESE]** — perda automática com motivo CANCELLED_AFTER_START |
| RN-011 | Ação sensível exige autorização de perfil superior | Perda acima do limite |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-042 | `stock.wasted` | Perda registrada | ingredientId, qty, reason, cost | ↑ |
| EVT-010 | `order.item.cancelled` | Origem da perda automática | reason, wasStarted | ↑ |

## 7. Contrato de API

```http
POST /v1/stock/waste
X-Authorization-Token: <se acima do limite>
{ "ingredientId": "...", "quantity": 2.5, "uom": "KG",
  "reason": "EXPIRATION", "notes": "lote L-2026-06" }
→ 201 { "movement": {...}, "costImpact": 8500 }

GET /v1/stock/waste?from=...&to=...&groupBy=reason
→ { "byReason": [ { "reason": "EXPIRATION", "quantity": 12.4,
                    "costImpact": 42100, "sharePercent": 46.2 } ] }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `stock_movement` | Perda como movimento | `type=WASTE`, `waste_reason`, `quantity` (negativa), `unit_cost`, `authorized_by` |
| `order_item` | Origem, quando por cancelamento | `order_item_id` |
| `audit_log` | Registro da ação | `action=STOCK_WASTED` |

> Perda por motivo é derivada de `stock_movement WHERE type='WASTE'` agrupado por `waste_reason` (ERD, seção 4).

## 9. Comportamento offline

A perda automática por cancelamento acontece no edge, junto com o cancelamento (US-033). O registro manual pode ser feito tanto no edge quanto na nuvem — ambos geram movimento e sincronizam.

## 10. Interface e experiência

- Registro em tela simples, acessível também do celular — a perda é constatada no depósito, não no escritório
- Motivos como botões grandes, não lista suspensa
- Custo da perda exibido no momento do registro — torna o prejuízo concreto
- Relatório por motivo com o de maior impacto destacado

## 11. Métricas, alertas e observabilidade

- Valor e quantidade de perda por motivo, insumo e período
- Perda como percentual do CMV
- Perda por cancelamento após início — mede o custo real dos cancelamentos
- Evolução da perda ao longo do tempo

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo do custo da perda pelo custo médio vigente |
| Integração | Perda automática apenas quando o cancelamento ocorre após o início |
| Integração | Autorização exigida acima do limite |
| Integração | Relatório por motivo bate com os movimentos |

## 13. Dependências

**Depende de:** US-033, US-103  
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

- **RN-008 é hipótese.** Confirmar com o cliente o tratamento do insumo em item cancelado após o início (pendência registrada em Visão Geral 10.3).

---

*US-105 · Épico E-10 · Pacote 004_DonaBetinha · Replay Studio.*