# US-022 · Abrir mesa por garcom ou por cliente

|  |  |
|---|---|
| **Épico** | [E-02 · Mesa e Comanda](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 3 |
| **Requisitos funcionais** | RF-SAL-04 |
| **Regras de negócio** | RN-004 |
| **ADRs** | ADR-020 |
| **Eventos** | EVT-020 |
| **Aplicações** | web-pos, web-menu, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** garçom (P2),
> **quero** abrir a mesa pelo meu celular, informando quantas pessoas sentaram,
> **para** que o consumo comece a ser registrado desde o primeiro momento.

## 2. Contexto e motivação

A abertura da sessão é o marco zero da medição do salão: dela saem tempo de permanência, giro de mesa e ticket médio por pessoa.

Dois caminhos levam ao mesmo lugar — o garçom abre pelo celular ou o cliente abre lendo o QR Code. O sistema registra a origem (`source`), porque a diferença entre os dois é um indicador de adoção relevante.

## 3. Escopo

### 3.1 Dentro desta história

- Abertura de sessão com contagem de pessoas e garçom responsável
- Registro da origem: `QR` ou `WAITER`
- Bloqueio de abertura em mesa que já tem sessão ativa
- Alteração da contagem de pessoas durante a sessão
- Atribuição e troca de garçom responsável
- Idempotência: duplo toque não abre duas sessões

### 3.2 Fora desta história

- Fechamento e pagamento (E-05)
- Transferência de itens entre mesas (RF-SAL-09, Fase 2)
- Reserva de mesa

## 4. Critérios de aceite

```gherkin
Funcionalidade: Abertura de mesa

  Cenário: Abertura pelo garçom
    Dado a mesa 12 livre
    Quando o garçom abrir a mesa informando 4 pessoas
    Então a sessão deve ser criada com status OPEN e origem WAITER
    E o garçom deve ficar registrado como responsável
    E a mesa deve aparecer como ocupada no mapa

  Cenário: Abertura pelo cliente via QR
    Dado a mesa 12 livre
    Quando o cliente ler o QR Code
    Então a sessão deve ser criada com origem QR
    E a contagem de pessoas deve ficar pendente de confirmação pelo garçom

  Cenário: Mesa já ocupada
    Dado uma sessão já aberta na mesa 12
    Quando alguém tentar abrir outra
    Então deve receber 409
    E deve ser direcionado à sessão existente

  Cenário: Duplo toque do garçom
    Dado um garçom que tocou "abrir mesa" duas vezes por instabilidade de rede
    Quando a segunda requisição chegar com a mesma Idempotency-Key
    Então deve retornar a mesma sessão, sem duplicar

  Cenário: Troca de garçom responsável
    Dado uma sessão aberta pelo garçom João
    Quando a mesa for repassada à garçonete Ana
    Então o responsável deve mudar
    E ambos devem constar no histórico da sessão

  Cenário: Abertura com internet caída
    Dado que a loja está sem internet
    Quando o garçom abrir a mesa
    Então a sessão deve ser criada normalmente no servidor local
    E o evento deve ficar enfileirado para sincronização
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | A sessão grava `opened_by`, `device_id` e `occurred_at` |
| RN-005 | A operação local não depende de internet | Abertura 100% local |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-020 | `table.session.opened` | Mesa aberta | tableId, guestCount, waiterId, source | ↑ |

## 7. Contrato de API

```http
POST /v1/tables/{id}/sessions
Idempotency-Key: <uuid>
X-Occurred-At: 2026-07-31T20:12:04.221Z
{ "guestCount": 4 }
→ 201 { "session": { "id": "...", "status": "OPEN", "openedAt": "...",
                     "waiter": {...}, "source": "WAITER" } }
→ 409 { "code": "TABLE_ALREADY_OPEN", "meta": { "sessionId": "..." } }

PATCH /v1/sessions/{id}    { "guestCount": 5, "waiterId": "..." }
GET   /v1/sessions/{id}
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `table_session` | Sessão de consumo | `table_id`, `opened_at`, `opened_by`, `waiter_id`, `guest_count`, `source`, `status` |
| `dining_table` | Estado da mesa | `status` passa a OCCUPIED |
| `idempotency_key` | Proteção contra duplo envio | `key`, `response`, `expires_at` |

## 9. Comportamento offline

Integralmente local. A abertura de mesa é operação crítica de tempo real e nunca pode depender da nuvem (RF-OFF-01).

O `X-Occurred-At` enviado pelo dispositivo preserva o horário real da abertura mesmo que o evento só sincronize horas depois — sem isso, a métrica de permanência e de giro de mesa nasceria errada (RN-020).

## 10. Interface e experiência

- Abrir mesa em dois toques no celular do garçom: escolher a mesa, informar quantas pessoas
- Contagem de pessoas com botões grandes de incremento, sem teclado
- Mapa de mesas como tela inicial do garçom, não menu
- Feedback otimista: a mesa aparece ocupada imediatamente, com confirmação silenciosa depois

## 11. Métricas, alertas e observabilidade

- Tempo de permanência por mesa (`closed_at − opened_at`)
- Giro de mesa por dia e por ambiente
- Ticket médio por pessoa (requer `guest_count` preenchido)
- Proporção de sessões abertas por QR versus garçom

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Transição de estado da mesa e validação de sessão única |
| Integração | Idempotência: duas requisições com a mesma chave retornam a mesma sessão |
| Integração | Abertura funciona com o edge desconectado da nuvem |
| Integração | `occurredAt` preservado após sincronização atrasada |
| E2E | Abertura pelo garçom e entrada do cliente na mesma sessão pelo QR |

## 13. Dependências

**Depende de:** US-020, US-004  
**Habilita:** US-023, US-024, US-030, US-051

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

- `guest_count` não preenchido invalida o ticket médio por pessoa. Mitigação: campo obrigatório na abertura pelo garçom e confirmação pendente quando a origem é QR.

---

*US-022 · Épico E-02 · Pacote 004_DonaBetinha · Replay Studio.*