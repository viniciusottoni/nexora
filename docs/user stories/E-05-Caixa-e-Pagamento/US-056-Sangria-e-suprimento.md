# US-056 · Sangria e suprimento

|  |  |
|---|---|
| **Épico** | [E-05 · Caixa e Pagamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | S — Should have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 5 |
| **Requisitos funcionais** | RF-CXA-07 |
| **Regras de negócio** | RN-004 |
| **ADRs** | ADR-023 |
| **Eventos** | EVT-031 |
| **Aplicações** | web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** caixa (P4),
> **quero** registrar retiradas e entradas de dinheiro no caixa durante o turno,
> **para** que o valor esperado no fechamento continue correto.

## 2. Contexto e motivação

Sangria (retirada por segurança) e suprimento (entrada de troco) são movimentos rotineiros que, se não registrados, aparecem como divergência no fechamento — e a divergência perde o significado de indicador.

Está registrado como hipótese a validar na Visão Geral (6.2, M3).

## 3. Escopo

### 3.1 Dentro desta história

- Registro de sangria com valor, motivo e destino
- Registro de suprimento com valor e origem
- Autorização de perfil superior acima do limite configurável
- Impacto imediato no valor esperado do caixa
- Histórico dos movimentos do turno

### 3.2 Fora desta história

- Integração com cofre ou malote
- Fluxo de tesouraria (Fase 3)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Sangria e suprimento

  Cenário: Sangria registrada
    Dado uma sessão de caixa aberta com R$ 1.500,00 em dinheiro
    Quando o operador registrar sangria de R$ 500,00 com motivo
    Então o valor esperado deve cair para R$ 1.000,00
    E o evento cash.movement.registered deve ser emitido

  Cenário: Suprimento de troco
    Dado uma sessão de caixa aberta
    Quando o operador registrar suprimento de R$ 200,00
    Então o valor esperado deve subir em R$ 200,00

  Cenário: Sangria acima do limite
    Dado o limite de sangria sem autorização em R$ 300,00
    Quando o operador registrar sangria de R$ 800,00
    Então deve ser exigida autorização de perfil superior

  Cenário: Movimento sem caixa aberto
    Dado nenhuma sessão de caixa aberta
    Quando alguém tentar registrar um movimento
    Então deve receber 409
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | Movimento registra operador e autorizador |
| RN-011 | Ação sensível exige autorização de perfil superior | Sangria acima do limite |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-031 | `cash.movement.registered` | Sangria ou suprimento | type, amount, reason, authorizedBy | ↑ |

## 7. Contrato de API

```http
POST /v1/cash-sessions/movements
X-Authorization-Token: <se acima do limite>
{ "type": "WITHDRAWAL", "amount": 50000, "reason": "sangria de segurança" }
→ 201 { "movement": {...}, "newExpected": 100000 }

{ "type": "SUPPLY", "amount": 20000, "reason": "troco" }

GET /v1/cash-sessions/current/movements
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `cash_movement` | Movimento de caixa | `cash_session_id`, `type`, `amount`, `reason`, `actor_id`, `authorized_by` |
| `tenant_config` | Limite sem autorização | `operation.maxWithdrawalWithoutAuth` |

## 9. Comportamento offline

Integralmente local.

## 10. Interface e experiência

- Dois botões distintos e inequívocos: retirar e suprir
- Valor esperado atualizado imediatamente após o registro
- Motivo obrigatório, com sugestões de lista
- Histórico do turno acessível na mesma tela

## 11. Métricas, alertas e observabilidade

- Volume de sangrias por turno — insumo de política de segurança
- Suprimentos por turno, indicando dimensionamento do fundo de caixa

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Impacto correto no valor esperado, nos dois sentidos |
| Integração | Autorização exigida acima do limite |
| Integração | Movimento recusado sem caixa aberto |

## 13. Dependências

**Depende de:** US-055  
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

- Funcionalidade registrada como hipótese a validar (Visão Geral 6.2). Confirmar com o cliente se a prática existe.

---

*US-056 · Épico E-05 · Pacote 004_DonaBetinha · Replay Studio.*