# US-026 · Solicitar a conta

|  |  |
|---|---|
| **Épico** | [E-02 · Mesa e Comanda](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 3 |
| **Requisitos funcionais** | RF-SAL-08 |
| **Regras de negócio** | RN-003 |
| **ADRs** | — |
| **Eventos** | EVT-022 |
| **Aplicações** | web-menu, web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** cliente do salão (P1) e garçom (P2),
> **quero** pedir a conta pelo celular, já escolhendo como quero dividir,
> **para** que o caixa comece a preparar antes de eu chamar alguém.

## 2. Contexto e motivação

O pedido de conta é o gatilho que muda o estado da sessão para `BILL_REQUESTED` e alerta o caixa. Antecipar esse momento reduz o tempo entre a decisão de ir embora e a liberação da mesa — que é exatamente o que o giro de mesa mede.

Escolher o modo de divisão já na solicitação evita o vaivém clássico entre mesa e caixa.

## 3. Escopo

### 3.1 Dentro desta história

- Solicitação de conta pelo cliente ou pelo garçom
- Escolha do modo de divisão na solicitação (por pessoa, por item, valor único)
- Transição da sessão para `BILL_REQUESTED`
- Alerta ao caixa e ao garçom responsável
- Possibilidade de voltar a pedir, retornando a sessão para `OPEN`

### 3.2 Fora desta história

- Cálculo e apresentação da divisão (US-027)
- Recebimento do pagamento (US-052)
- Avaliação do atendimento (RF-SAL-12, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Solicitar a conta

  Cenário: Solicitação pelo cliente
    Dado uma sessão aberta com consumo
    Quando o cliente solicitar a conta escolhendo divisão por 4 pessoas
    Então a sessão deve ir para BILL_REQUESTED
    E o caixa e o garçom responsável devem ser alertados
    E a preferência de divisão deve ficar registrada

  Cenário: Novo pedido após solicitar a conta
    Dado uma sessão em BILL_REQUESTED
    Quando o cliente adicionar um novo item
    Então a sessão deve voltar para OPEN
    E o caixa deve ser informado da mudança

  Cenário: Item pendente de entrega
    Dado uma sessão com um item ainda não entregue
    Quando a conta for solicitada
    Então a solicitação deve ser aceita
    E o caixa deve ver o aviso de item pendente antes de receber o pagamento

  Cenário: Solicitação pelo garçom
    Dado o garçom no mapa de mesas
    Quando marcar a mesa 12 como "conta solicitada"
    Então o efeito deve ser idêntico ao da solicitação pelo cliente
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-003 | Cada transição gera alerta aos perfis envolvidos | Caixa e garçom alertados |
| RN-017 | Conta não pode ser fechada com item pendente de entrega, salvo autorização registrada | **[HIPÓTESE]** — a solicitação é aceita; o bloqueio acontece no fechamento (US-035) |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-022 | `table.bill_requested` | Conta solicitada | tableId, splitMode, people | ↑ |

## 7. Contrato de API

```http
POST /v1/public/table/{qrToken}/request-bill
{ "splitMode": "BY_PERSON", "people": 4 }
→ 202 { "session": { "status": "BILL_REQUESTED" } }

POST /v1/sessions/{id}/request-bill        # pelo garçom
{ "splitMode": "SINGLE" }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `table_session` | Estado e preferência de divisão | `status`, `split_mode`, `split_people`, `bill_requested_at` |
| `alert` | Alerta ao caixa | `type=BILL_REQUESTED` |

## 9. Comportamento offline

Integralmente local, com alerta pelo WebSocket do edge.

## 10. Interface e experiência

- Escolha do modo de divisão em uma tela simples, com o valor por pessoa já calculado na pré-visualização
- No caixa, mesas com conta solicitada aparecem no topo da lista
- Se o cliente pedir mais alguma coisa depois, a mudança é silenciosa para ele e explícita para o caixa

## 11. Métricas, alertas e observabilidade

- Tempo entre solicitação de conta e pagamento — gargalo frequente do fim da refeição
- Distribuição dos modos de divisão escolhidos
- Tempo total de permanência, do qual esta etapa é a parte final

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Transição de estado e alerta ao caixa |
| Integração | Novo pedido retorna a sessão para OPEN |
| Integração | Preferência de divisão chega ao caixa |

## 13. Dependências

**Depende de:** US-024, US-023  
**Habilita:** US-027, US-051

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

—

---

*US-026 · Épico E-02 · Pacote 004_DonaBetinha · Replay Studio.*