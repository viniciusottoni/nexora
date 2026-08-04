# US-053 · Taxa de servico configuravel com retirada registrada

|  |  |
|---|---|
| **Épico** | [E-05 · Caixa e Pagamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 5 |
| **Requisitos funcionais** | RF-CXA-04 |
| **Regras de negócio** | RN-010 |
| **ADRs** | ADR-023 |
| **Eventos** | EVT-035 |
| **Aplicações** | web-pos, api-edge |
| **Autoridade do dado** | Nuvem (configuração) → aplicada no local |

---

## 1. História

> **Como** caixa (P4) e gestor (P8),
> **quero** aplicar a taxa de serviço automaticamente e registrar quando ela for retirada,
> **para** que a equipe receba o que é dela e eu saiba quando e por que a taxa foi dispensada.

## 2. Contexto e motivação

A taxa de serviço é opcional ao cliente por lei, mas sua retirada precisa ser rastreável — é dinheiro da equipe. A RN-010 estabelece que *a retirada é registrada e auditada*.

O ponto de desenho é o equilíbrio: exigir autorização para toda retirada trava o caixa em uma situação corriqueira; não registrar nada abre espaço para desvio. A solução é registrar sempre, com motivo, e alertar o gestor quando o padrão fugir do normal.

## 3. Escopo

### 3.1 Dentro desta história

- Percentual de taxa configurável por tenant
- Aplicação automática na montagem da conta
- Retirada total ou parcial, com motivo
- Registro do autor da retirada
- Retirada por parte, no caso de conta dividida
- Relatório de taxa arrecadada e retirada por período

### 3.2 Fora desta história

- Rateio da taxa entre a equipe (fora do escopo)
- Taxa variável por ambiente ou por horário

## 4. Critérios de aceite

```gherkin
Funcionalidade: Taxa de serviço

  Cenário: Aplicação automática
    Dado a taxa configurada em 10%
    Quando a conta for montada com subtotal de R$ 180,00
    Então a taxa de R$ 18,00 deve ser aplicada automaticamente
    E deve aparecer identificada como opcional

  Cenário: Retirada registrada
    Dado uma conta com taxa aplicada
    Quando o cliente optar por não pagar a taxa
    Então a taxa deve ser removida do total
    E o evento service_fee.waived deve ser emitido com motivo e autor

  Cenário: Retirada parcial em conta dividida
    Dado uma conta dividida entre 4 pessoas
    Quando uma delas retirar a taxa
    Então apenas a parte dela deve ser recalculada
    E as outras três devem manter a taxa

  Cenário: Taxa desativada no tenant
    Dado um estabelecimento com taxa configurada em 0%
    Quando a conta for montada
    Então nenhuma taxa deve aparecer na conta

  Cenário: Padrão anômalo de retirada
    Dado um operador que retirou a taxa em 80% das contas do turno
    Quando o limiar de anomalia for ultrapassado
    Então o gestor deve ser alertado
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-010 | Taxa de serviço é opcional ao cliente; a retirada é registrada e auditada | **[HIPÓTESE]** — registro obrigatório com motivo e autor; alerta em padrão anômalo |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-035 | `service_fee.waived` | Taxa de serviço retirada | amount, reason, waivedBy, scope | ↑ |

## 7. Contrato de API

```http
POST /v1/sessions/{id}/service-fee/waive
{ "reason": "CUSTOMER_REQUEST", "scope": "FULL" }
→ 200 { "session": { "serviceFee": 0, "total": 18000 } }

POST /v1/sessions/{id}/service-fee/waive
{ "reason": "SERVICE_ISSUE", "scope": "PARTIAL", "person": 2 }

PATCH /v1/tenant/config
{ "operation": { "serviceFeePercent": 12 } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `table_session` | Taxa aplicada e retirada | `service_fee`, `service_fee_waived`, `waived_by`, `waive_reason` |
| `tenant_config` | Percentual configurado | `operation.serviceFeePercent` |
| `audit_log` | Registro da retirada | `action=SERVICE_FEE_WAIVED`, `before`, `after` |

## 9. Comportamento offline

Integralmente local. O percentual vem da última sincronização de configuração.

## 10. Interface e experiência

- Taxa sempre visível e identificada como opcional — nunca embutida no total sem discriminação
- Retirada em dois toques, com motivo escolhido de lista curta
- Sem exigência de autorização superior para a retirada simples: travar isso atrapalha o caixa em situação corriqueira
- No caso de conta dividida, deixar claro qual parte foi afetada

## 11. Métricas, alertas e observabilidade

- Taxa arrecadada versus taxa retirada, por período e por operador
- Percentual de contas com taxa retirada — o número que revela se a política está sendo respeitada
- Alerta ao gestor em padrão anômalo por operador

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo da taxa e recálculo após retirada total e parcial |
| Integração | Retirada emite evento com motivo e autor |
| Integração | Retirada parcial em conta dividida afeta apenas a parte correta |
| Integração | Alerta de padrão anômalo dispara no limiar |

## 13. Dependências

**Depende de:** US-051  
**Habilita:** US-090

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

- RN-010 é hipótese; a política de taxa de serviço precisa ser confirmada com o cliente antes do piloto.

---

*US-053 · Épico E-05 · Pacote 004_DonaBetinha · Replay Studio.*