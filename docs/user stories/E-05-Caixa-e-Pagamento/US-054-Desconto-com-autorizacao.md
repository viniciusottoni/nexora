# US-054 · Desconto com autorizacao

|  |  |
|---|---|
| **Épico** | [E-05 · Caixa e Pagamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 5 |
| **Requisitos funcionais** | RF-CXA-05 |
| **Regras de negócio** | RN-011 |
| **ADRs** | ADR-023 |
| **Eventos** | EVT-034, EVT-071 |
| **Aplicações** | web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** caixa (P4) e gestor (P8),
> **quero** que descontos acima do limite exijam autorização de quem pode dar,
> **para** que cortesia não vire prejuízo invisível.

## 2. Contexto e motivação

Desconto é a exceção mais fácil de abusar e a mais difícil de rastrear em operação sem sistema. A RN-011 estabelece o limite configurável e a autorização acima dele.

O desenho segue o padrão de ação sensível do documento 05: o gerente digita o PIN no próprio dispositivo do operador, sem trocar de sessão. Isso mantém o fluxo rápido e mantém o registro completo — quem executou e quem autorizou são pessoas distintas no log.

## 3. Escopo

### 3.1 Dentro desta história

- Desconto em percentual e em valor absoluto
- Limite configurável sem autorização
- Autorização de perfil superior acima do limite
- Motivo obrigatório
- Registro de executor, autorizador, valor e motivo
- Desconto por item ou sobre o total
- Alerta ao gestor em padrão anômalo

### 3.2 Fora desta história

- Cupons e promoções automáticas (Fase 4)
- Programa de fidelidade (fora do escopo)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Desconto com autorização

  Cenário: Desconto acima do limite
    Dado limite de 5% sem autorização
    Quando o operador aplicar 15%
    Então deve ser exigido PIN de perfil superior
    E o registro deve conter valor, motivo e autorizador

  Cenário: Desconto dentro do limite
    Dado limite de 5% sem autorização
    Quando o operador aplicar 3%
    Então o desconto deve ser aplicado sem autorização
    E ainda assim deve ser registrado com autor e motivo

  Cenário: Autorização negada
    Dado um desconto de 15% solicitado
    Quando o PIN informado não tiver permissão de autorizar desconto
    Então a operação deve ser recusada com 403
    E a tentativa deve ser registrada

  Cenário: Desconto em valor absoluto
    Dado uma conta de R$ 198,00
    Quando for aplicado desconto de R$ 20,00
    Então o percentual equivalente deve ser calculado e registrado
    E o limite deve ser avaliado sobre o percentual

  Cenário: Desconto por item
    Dado um item com problema de qualidade
    Quando o desconto for aplicado apenas àquele item
    Então apenas o valor daquele item deve ser reduzido
    E o motivo deve ficar vinculado ao item

  Cenário: Padrão anômalo
    Dado um operador com volume de desconto muito acima da média do turno
    Quando o limiar for ultrapassado
    Então o gestor deve ser alertado

  Cenário: Desconto com internet caída
    Dado que a loja está sem internet
    Quando um desconto acima do limite for autorizado
    Então o PIN do autorizador deve ser validado localmente
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-011 | Desconto acima do limite configurado exige autorização de perfil superior | **[HIPÓTESE]** — limite em `tenant_config`; autorização por `X-Authorization-Token` |
| RN-004 | Toda ação registra autor, horário e dispositivo | Executor e autorizador registrados separadamente |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-034 | `discount.applied` | Desconto aplicado | amount, percent, reason, authorizedBy, scope | ↑ |
| EVT-071 | `authorization.granted` | Autorização concedida | action=APPLY_DISCOUNT, authorizedBy | ↑ |

## 7. Contrato de API

```http
POST /v1/sessions/{id}/discount
X-Authorization-Token: <se acima do limite>
{ "percent": 10, "reason": "cortesia", "scope": "SESSION" }
→ 200 { "session": { "discount": 1980, "total": 17820 },
        "authorizedBy": {...} }

POST /v1/sessions/{id}/discount
{ "amount": 2000, "reason": "QUALITY_ISSUE",
  "scope": "ITEM", "orderItemId": "..." }

→ 403 { "code": "AUTHORIZATION_REQUIRED",
        "meta": { "action": "APPLY_DISCOUNT",
                  "limitPercent": 5, "requestedPercent": 15 } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `table_session` | Desconto aplicado | `discount`, `discount_percent`, `discount_reason`, `discount_by`, `authorized_by` |
| `order_item` | Desconto por item | `discount` |
| `tenant_config` | Limite sem autorização | `operation.maxDiscountWithoutAuthPercent` |
| `audit_log` | Trilha imutável | `action=DISCOUNT_APPLIED`, `before`, `after`, `authorized_by` |

## 9. Comportamento offline

Integralmente local, incluindo a validação do PIN do autorizador contra a réplica local de usuários.

## 10. Interface e experiência

- Desconto em percentual ou valor, com conversão automática entre os dois
- Aviso claro quando o valor digitado cruza o limite e exigirá autorização — antes de o operador confirmar
- Modal de autorização sobre o contexto, sem perder a conta em edição
- Motivo escolhido de lista, com campo livre opcional

## 11. Métricas, alertas e observabilidade

- Volume e valor de descontos por operador, motivo e período
- Percentual de descontos que exigiram autorização
- Impacto do desconto sobre a margem (Fase 2, quando houver custo)
- Alerta ao gestor em padrão anômalo (RF-ALT-01)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Conversão percentual↔valor e avaliação do limite |
| Integração | Desconto acima do limite sem token é recusado |
| Integração | Registro contém executor e autorizador distintos |
| Integração | Desconto por item afeta apenas aquele item |
| Caos offline | Autorização de desconto funciona com internet caída |

## 13. Dependências

**Depende de:** US-004, US-051  
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

- Quem pode autorizar desconto é regra pendente (Visão Geral 10.3). Definir antes do piloto.
- Limite muito baixo faz o gerente ser chamado o tempo todo e a autorização vira carimbo. Calibrar no piloto.

---

*US-054 · Épico E-05 · Pacote 004_DonaBetinha · Replay Studio.*