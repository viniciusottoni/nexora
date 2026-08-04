# US-025 · Chamar garcom pela mesa

|  |  |
|---|---|
| **Épico** | [E-02 · Mesa e Comanda](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 3 |
| **Requisitos funcionais** | RF-SAL-07 |
| **Regras de negócio** | RN-003 |
| **ADRs** | ADR-011 |
| **Eventos** | EVT-021 |
| **Aplicações** | web-menu, web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** cliente do salão (P1),
> **quero** chamar o garçom pelo celular,
> **para** que eu não precise levantar a mão e esperar ser notado.

## 2. Contexto e motivação

Funcionalidade simples com efeito desproporcional: elimina o momento mais frustrante da experiência de salão, que é tentar chamar atenção sem sucesso.

O direcionamento importa: o alerta vai para o garçom responsável pela mesa, não para todos (RF-ALT-01). Alerta para todo mundo é alerta que ninguém atende.

## 3. Escopo

### 3.1 Dentro desta história

- Botão de chamar garçom no cardápio da mesa
- Alerta direcionado ao garçom responsável, com escalonamento se não atendido
- Indicador no mapa de mesas
- Confirmação de atendimento pelo garçom, encerrando a chamada
- Proteção contra chamadas repetidas (agrupamento)

### 3.2 Fora desta história

- Motivo da chamada em texto livre
- Chamada por categoria (água, talher, conta)
- Escalonamento ao gestor (Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Chamar o garçom

  Cenário: Chamada direcionada
    Dado a mesa 12 com a garçonete Ana como responsável
    Quando o cliente tocar em "chamar garçom"
    Então Ana deve receber o alerta
    E os demais garçons não devem ser notificados
    E a mesa deve exibir o indicador no mapa

  Cenário: Confirmação de atendimento
    Dado uma chamada pendente
    Quando a garçonete confirmar o atendimento
    Então o indicador deve desaparecer do mapa
    E o cliente deve ver que a chamada foi atendida

  Cenário: Chamada repetida
    Dado uma chamada feita há 30 segundos e ainda pendente
    Quando o cliente tocar novamente
    Então não deve ser criado um novo alerta
    E deve ser exibida a informação de que o garçom já foi avisado

  Cenário: Escalonamento por falta de atendimento
    Dado uma chamada pendente há mais do que o limiar configurado
    Quando o limiar for ultrapassado
    Então o alerta deve escalar para os demais garçons do ambiente

  Cenário: Chamada com internet caída
    Dado que a loja está sem internet
    Quando o cliente chamar o garçom
    Então o alerta deve chegar normalmente pela rede local
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-003 | Cada transição gera alerta aos perfis envolvidos | Alerta direcionado ao responsável pela mesa |
| RN-005 | Operação local independente de internet | Alerta trafega pelo WebSocket do edge |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-021 | `table.waiter_called` | Cliente chamou o garçom | tableId, sessionId | ↑ |

## 7. Contrato de API

```http
POST /v1/public/table/{qrToken}/call-waiter
Idempotency-Key: <uuid>
→ 202 { "acknowledged": true, "alreadyPending": false }

POST /v1/tables/{id}/acknowledge-call     # garçom confirma atendimento
→ 200 { "resolved": true, "responseSeconds": 42 }

# WebSocket, sala role:waiter e user:{waiterId}:
{ "type": "table.waiter_called", "data": { "tableId": "...", "label": "12" } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `alert` | Chamada como alerta rastreável | `type=WAITER_CALLED`, `entity_id`, `raised_at`, `resolved_at`, `resolved_by` |
| `table_session` | Contexto da chamada | `waiter_id` |

## 9. Comportamento offline

Integralmente local. WebSocket do edge com fallback de polling — a chamada nunca depende de internet.

## 10. Interface e experiência

- Botão sempre acessível, fixo, sem precisar navegar
- Confirmação visual imediata ao cliente de que a chamada foi registrada
- No celular do garçom, vibração e som curto — o alerta precisa vencer o ruído do salão
- Indicador no mapa com o tempo desde a chamada, não só a existência dela

## 11. Métricas, alertas e observabilidade

- Tempo de resposta à chamada (chamada → confirmação) — indicador direto de qualidade do serviço
- Chamadas por mesa e por faixa horária, revelando sobrecarga de garçom
- Taxa de escalonamento — alta indica dimensionamento insuficiente de equipe

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Alerta chega apenas ao garçom responsável |
| Integração | Chamada repetida não duplica o alerta |
| Integração | Escalonamento dispara no limiar configurado |
| Caos offline | Chamada funciona com internet da loja caída |

## 13. Dependências

**Depende de:** US-021, US-023  
**Habilita:** US-080

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

- Chamada sem motivo obriga o garçom a ir descobrir o que o cliente quer. Avaliar no piloto se vale acrescentar categorias — o risco oposto é adicionar atrito a uma ação que precisa ser de um toque.

---

*US-025 · Épico E-02 · Pacote 004_DonaBetinha · Replay Studio.*