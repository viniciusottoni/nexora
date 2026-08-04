# US-055 · Abertura e fechamento de caixa

|  |  |
|---|---|
| **Épico** | [E-05 · Caixa e Pagamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 5 |
| **Requisitos funcionais** | RF-CXA-06, RF-CXA-08 |
| **Regras de negócio** | RN-018 |
| **ADRs** | ADR-018, ADR-023 |
| **Eventos** | EVT-030, EVT-036 |
| **Aplicações** | web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** caixa (P4) e gestor (P8),
> **quero** abrir e fechar o caixa com conferência de valores,
> **para** que eu saiba se sobrou ou faltou dinheiro, e por quê.

## 2. Contexto e motivação

O fechamento de caixa é o momento de verdade do dia: o valor esperado, calculado a partir dos pagamentos registrados, contra o valor efetivamente contado. A diferença entre os dois é o indicador mais direto de qualidade do registro operacional.

A RN-018 estabelece que *caixa não pode ser fechado com mesa aberta, salvo autorização registrada* — evitar que uma conta esquecida vire divergência atribuída ao operador errado.

## 3. Escopo

### 3.1 Dentro desta história

- Abertura com valor inicial (fundo de caixa)
- Cálculo do valor esperado a partir dos pagamentos em dinheiro e movimentos
- Fechamento com contagem informada pelo operador
- Cálculo e registro da divergência
- Justificativa obrigatória acima do limiar configurado
- Alerta ao gestor em caso de divergência relevante
- Bloqueio de fechamento com mesa aberta, contornável com autorização
- Relatório de fechamento do turno

### 3.2 Fora desta história

- Sangria e suprimento (US-056, história irmã)
- Conciliação com extratos de maquininha (RF-CXA-11, Fase 3)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Abertura e fechamento de caixa

  Cenário: Abertura com fundo
    Dado o início do turno
    Quando o operador abrir o caixa informando R$ 200,00 de fundo
    Então a sessão de caixa deve ser criada com status OPEN
    E o evento cash.session.opened deve ser emitido

  Cenário: Divergência no fechamento
    Dado esperado R$ 1.850,00 e contado R$ 1.843,50
    Quando o caixa for fechado
    Então a divergência de R$ 6,50 deve ser registrada
    E, acima do limiar, deve ser exigida justificativa
    E o gestor deve ser alertado

  Cenário: Fechamento sem divergência
    Dado esperado e contado iguais
    Quando o caixa for fechado
    Então a sessão deve ir para CLOSED sem exigir justificativa

  Cenário: Mesa aberta no fechamento
    Dado duas mesas ainda abertas
    Quando o operador tentar fechar o caixa
    Então o fechamento deve ser bloqueado
    E as mesas abertas devem ser listadas
    E deve ser possível prosseguir com autorização de perfil superior

  Cenário: Composição do valor esperado
    Dado fundo de R$ 200,00, R$ 1.500,00 recebidos em dinheiro,
         R$ 300,00 de suprimento e R$ 150,00 de sangria
    Quando o valor esperado for calculado
    Então deve ser R$ 1.850,00
    E a composição deve estar detalhada na tela

  Cenário: Um caixa por operador e turno
    Dado uma sessão de caixa já aberta pelo operador
    Quando ele tentar abrir outra
    Então deve receber 409 apontando a sessão existente

  Cenário: Fechamento com internet caída
    Dado que a loja está sem internet
    Quando o caixa for fechado
    Então a operação deve concluir normalmente
    E o alerta ao gestor será entregue após a sincronização
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-018 | Caixa não pode ser fechado com mesa aberta, salvo autorização registrada | **[HIPÓTESE]** — bloqueio contornável com autorização registrada |
| RN-004 | Toda ação registra autor, horário e dispositivo | Abertura e fechamento com operador identificado |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-030 | `cash.session.opened` | Caixa aberto | operatorId, openingAmount | ↑ |
| EVT-036 | `cash.session.closed` | Caixa fechado | expected, counted, divergence | ↑ |

> Reação normativa: `cash.session.closed` → sessão em CLOSED, notifica o gestor se houver divergência, registra a divergência como métrica e alimenta a conciliação financeira.

## 7. Contrato de API

```http
POST /v1/cash-sessions/open
{ "openingAmount": 20000 }
→ 201 { "session": { "id": "...", "status": "OPEN", "openedAt": "..." } }

GET /v1/cash-sessions/current
→ { "session": {...},
    "expected": { "opening": 20000, "cashPayments": 150000,
                  "supplies": 30000, "withdrawals": -15000,
                  "total": 185000 } }

POST /v1/cash-sessions/{id}/close
X-Authorization-Token: <se houver mesa aberta>
{ "countedAmount": 184350, "justification": "..." }
→ 200 { "expected": 185000, "counted": 184350, "divergence": -650,
        "requiresJustification": true }

→ 422 { "code": "OPEN_TABLES",
        "meta": { "openSessions": [ { "table": "12", "total": 8700 } ] } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `cash_session` | Sessão de caixa | `operator_id`, `opened_at`, `opening_amount`, `closed_at`, `expected_amount`, `counted_amount`, `divergence`, `justification`, `status` |
| `cash_movement` | Sangrias e suprimentos | `type`, `amount`, `reason` |
| `payment` | Pagamentos em dinheiro do turno | `method=CASH`, `amount` |
| `audit_log` | Autorização de fechamento com mesa aberta | `action=CLOSE_CASH_WITH_OPEN_TABLES` |

## 9. Comportamento offline

Integralmente local. O fechamento de caixa acontece no fim do turno, momento em que uma dependência de internet seria especialmente inconveniente.

O alerta ao gestor sobre divergência é entregue localmente se ele estiver na loja, e pela nuvem após a sincronização se estiver fora.

## 10. Interface e experiência

- Valor esperado com a composição detalhada — o operador precisa entender de onde vem o número
- Contagem informada em campo grande, com conferência antes de confirmar
- Divergência exibida em destaque, com sinal e valor absoluto
- Justificativa com motivos sugeridos, mais campo livre
- Relatório de fechamento imprimível e exportável

## 11. Métricas, alertas e observabilidade

- Divergência de caixa por turno e por operador — indicador de qualidade do registro
- Frequência e magnitude de divergências ao longo do tempo
- Fechamentos com mesa aberta autorizados
- Alerta ao gestor em divergência acima do limiar (RF-ALT-01)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo do valor esperado com todas as parcelas |
| Unitário | Avaliação do limiar de justificativa obrigatória |
| Integração | Bloqueio com mesa aberta e contorno por autorização |
| Integração | Uma sessão de caixa por operador e turno |
| Integração | Alerta ao gestor em divergência |
| Caos offline | Fechamento completo com internet caída |

## 13. Dependências

**Depende de:** US-004, US-052  
**Habilita:** US-056, US-073, US-127

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

- Divergência recorrente pode indicar erro de processo, treinamento ou desvio. O sistema registra e alerta; a interpretação é gerencial.
- RN-018 é hipótese; confirmar com o cliente se o bloqueio deve ser rígido ou apenas avisar.

---

*US-055 · Épico E-05 · Pacote 004_DonaBetinha · Replay Studio.*