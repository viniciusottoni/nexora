# US-035 · Bloquear fechamento com item pendente

|  |  |
|---|---|
| **Épico** | [E-03 · Pedido e Roteamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | S — Should have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 3 |
| **Requisitos funcionais** | RF-PED-06 |
| **Regras de negócio** | RN-017 |
| **ADRs** | ADR-023 |
| **Eventos** | EVT-071 |
| **Aplicações** | web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** caixa (P4),
> **quero** ser avisado quando tentar fechar uma conta com item ainda não entregue,
> **para** que o cliente não pague por algo que não recebeu, nem vá embora sem receber.

## 2. Contexto e motivação

Situação corriqueira no fim da noite: o cliente pede a conta, o caixa fecha, e cinco minutos depois a cozinha entrega uma sobremesa para uma mesa que já foi embora. Prejuízo dos dois lados.

A regra RN-017 é hipótese: *conta não pode ser fechada com item pendente de entrega, salvo autorização registrada*. O ponto de desenho é que o bloqueio seja **contornável com registro**, não absoluto — há casos legítimos, como cliente que desistiu do item e não quer esperar.

## 3. Escopo

### 3.1 Dentro desta história

- Verificação de itens não entregues no momento do fechamento
- Bloqueio com aviso claro, listando os itens pendentes
- Autorização de perfil superior para prosseguir mesmo assim
- Registro da autorização com motivo
- Configurabilidade do comportamento por tenant (bloqueia, avisa ou ignora)

### 3.2 Fora desta história

- Cancelamento dos itens pendentes (US-033)
- Fechamento de caixa com mesa aberta (RN-018, tratada na US-055)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Bloqueio de fechamento com item pendente

  Cenário: Fechamento bloqueado
    Dado uma mesa com um item em estado READY, ainda não entregue
    Quando o caixa tentar concluir o pagamento
    Então o fechamento deve ser bloqueado
    E os itens pendentes devem ser listados na tela

  Cenário: Fechamento autorizado mesmo com pendência
    Dado o fechamento bloqueado por item pendente
    Quando um perfil superior autorizar com motivo
    Então o fechamento deve prosseguir
    E a autorização deve ser registrada em audit_log

  Cenário: Item cancelado resolve a pendência
    Dado uma mesa com item pendente
    Quando o item for cancelado com autorização
    Então o fechamento deve prosseguir sem novo bloqueio

  Cenário: Comportamento configurável
    Dado um tenant configurado com o modo "apenas avisar"
    Quando houver item pendente no fechamento
    Então deve ser exibido o aviso
    E o caixa deve poder prosseguir sem autorização
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-017 | Conta não pode ser fechada com item pendente de entrega, salvo autorização registrada | **[HIPÓTESE]** — comportamento configurável entre bloquear, avisar e ignorar |
| RN-011 | Ação sensível exige autorização de perfil superior | Prosseguir com pendência é ação sensível |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-071 | `authorization.granted` | Fechamento com pendência autorizado | action=CLOSE_WITH_PENDING, authorizedBy, reason | ↑ |

## 7. Contrato de API

```http
POST /v1/sessions/{id}/payments
X-Authorization-Token: <se houver item pendente e o modo for BLOCK>
{ "payments": [...] }
→ 422 { "code": "PENDING_ITEMS",
        "detail": "Há itens que ainda não foram entregues.",
        "meta": { "pendingItems": [ { "name": "Petit Gateau",
                                      "status": "READY" } ] } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Itens não entregues | `status` diferente de SERVED e CANCELLED |
| `tenant_config` | Modo de comportamento | `operation.pendingItemsOnClose` = BLOCK | WARN | IGNORE |
| `audit_log` | Autorização registrada | `action=CLOSE_WITH_PENDING`, `reason`, `authorized_by` |

## 9. Comportamento offline

Verificação e autorização integralmente locais, como todo o fluxo de caixa.

## 10. Interface e experiência

- Aviso listando exatamente quais itens estão pendentes e em que estado — informação acionável, não alarme genérico
- Dois caminhos oferecidos na mesma tela: cancelar os itens ou autorizar o fechamento
- Autorização no mesmo dispositivo, sem trocar de sessão

## 11. Métricas, alertas e observabilidade

- Contagem de fechamentos com pendência autorizada, por motivo e autorizador
- Itens que ficaram prontos e nunca foram entregues — indicador de falha do salão

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Bloqueio nos três modos de configuração |
| Integração | Autorização registra motivo e autorizador |
| Integração | Cancelamento do item pendente libera o fechamento |

## 13. Dependências

**Depende de:** US-033, US-052  
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

- RN-017 é hipótese. Se o bloqueio for rígido demais, o caixa aprende a autorizar automaticamente e o controle perde valor. Iniciar no modo WARN durante o piloto e endurecer só se o dado justificar.

---

*US-035 · Épico E-03 · Pacote 004_DonaBetinha · Replay Studio.*