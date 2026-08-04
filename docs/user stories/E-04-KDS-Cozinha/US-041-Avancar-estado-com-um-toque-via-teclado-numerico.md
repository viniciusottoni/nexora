# US-041 · Avancar estado com um toque via teclado numerico

|  |  |
|---|---|
| **Épico** | [E-04 · KDS Cozinha](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 4 |
| **Requisitos funcionais** | RF-KDS-04, RF-KDS-05 |
| **Regras de negócio** | RN-002, RN-004 |
| **ADRs** | ADR-020, ADR-021 |
| **Eventos** | EVT-005, EVT-006, EVT-007, EVT-008 |
| **Aplicações** | web-kds, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** pizzaiolo (P3),
> **quero** avançar o pedido digitando o número dele e apertando Enter,
> **para** que eu não precise largar o que estou fazendo para usar um mouse.

## 2. Contexto e motivação

O briefing menciona explicitamente **teclado numérico para a cozinha** — operação por código, sem digitação livre. É a restrição de interface mais dura do produto e a que mais determina se a cozinha vai adotar ou abandonar a ferramenta.

O desenho da API acompanha: `advance` sem informar o estado destino avança para o próximo estado natural (doc. 05, 5.3). Digitar `47` e Enter avança o pedido 47 — é essa simplicidade que torna a operação viável com as mãos ocupadas.

A resposta visual precisa vir em **menos de 300 ms** (doc. 02, seção 11). Acima disso, o operador digita de novo achando que não funcionou.

## 3. Escopo

### 3.1 Dentro desta história

- Entrada por código curto seguida de Enter
- Avanço para o próximo estado natural, sem escolha explícita
- Atualização otimista da interface, com confirmação silenciosa
- Retorno visual de erro para código inexistente, sem travar a tela
- Avanço em lote de todos os itens de um pedido
- Desfazer o último avanço dentro de uma janela curta
- Emissão dos carimbos T1 a T4 com autor e dispositivo

### 3.2 Fora desta história

- Refazimento de item — `re-fire` (RF-KDS-11, Fase 2)
- Prioridade dinâmica (US-116, Fase 2)
- Marcação de indisponibilidade (US-044)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Avanço de estado por teclado numérico

  Cenário: Operação sem mouse
    Dado um pedido com código curto 47 em estado QUEUED
    Quando o operador digitar 47 e pressionar Enter
    Então o item deve avançar para FIRED
    E a resposta visual deve ocorrer em menos de 300 ms
    E nenhuma digitação de texto deve ser necessária

  Cenário: Código inexistente
    Quando o operador digitar um código sem correspondência
    Então deve haver retorno visual de erro sem travar a tela
    E o campo deve limpar automaticamente para a próxima entrada

  Cenário: Sequência completa do ciclo
    Dado um item de praça gargalo em QUEUED
    Quando o operador digitar o código quatro vezes seguidas
    Então o item deve percorrer FIRED, IN_OVEN, OUT_OF_OVEN e READY
    E os carimbos T1, T2, T3 e T4 devem ser gravados
    E cada um deve registrar autor e dispositivo

  Cenário: Item que não passa pelo gargalo
    Dado um refrigerante na praça Bebidas em QUEUED
    Quando o operador digitar o código duas vezes
    Então o item deve ir de QUEUED para FIRED e depois para READY
    E os carimbos de forno devem permanecer nulos

  Cenário: Avanço em lote do pedido
    Dado um pedido com três itens da mesma praça
    Quando o operador digitar o código do pedido e confirmar o avanço em lote
    Então os três itens devem avançar juntos
    E cada um deve gerar seu próprio evento

  Cenário: Desfazer avanço acidental
    Dado um item que acabou de ser avançado por engano
    Quando o operador acionar o desfazer dentro de 10 segundos
    Então o item deve voltar ao estado anterior
    E a correção deve ser registrada, sem apagar o evento original

  Cenário: Duplo Enter acidental
    Dado que o operador pressionou Enter duas vezes rapidamente
    Quando a segunda requisição chegar com a mesma Idempotency-Key
    Então o item não deve avançar dois estados

  Cenário: Avanço com internet caída
    Dado que a loja está sem internet
    Quando o operador avançar um item
    Então a operação deve concluir normalmente em menos de 300 ms
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-002 | A cozinha registra obrigatoriamente início e conclusão de cada item | T1 e T4 gravados aqui |
| RN-004 | Toda ação registra autor, horário e dispositivo | Cada avanço grava operador e terminal |
| RN-020 | Métrica usa `ocorrido_em` | `X-Occurred-At` enviado pelo KDS |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-005 | `order.item.fired` | **T1** — produção iniciada | stationId, operatorId | ↑ |
| EVT-006 | `order.item.oven_in` | **T2** — entrou no gargalo | slotIndex | ↑ |
| EVT-007 | `order.item.oven_out` | **T3** — saiu do gargalo | cookSeconds | ↑ |
| EVT-008 | `order.item.ready` | **T4** — pronto | prepSeconds | ↑ |
| EVT-013 | `order.ready` | Todos os itens do pedido prontos | totalPrepSeconds | ↑ |

> Reação normativa: `order.item.ready` notifica garçom e mesa, calcula tempo de produção e **dispara a baixa de estoque por ficha técnica** (Fase 2, US-103).

## 7. Contrato de API

```http
POST /v1/kds/items/{id}/advance
Idempotency-Key: <uuid>
X-Occurred-At: 2026-07-31T20:51:33.102Z
{ }                                  # sem "to": avança ao próximo estado natural
→ 200 { "item": { "status": "FIRED", "firedAt": "..." },
        "nextAction": "IN_OVEN" }

POST /v1/kds/items/{id}/advance
{ "to": "READY" }                    # avanço explícito, quando necessário

POST /v1/kds/orders/{code}/advance   # avanço em lote do pedido
POST /v1/kds/items/{id}/undo         # desfazer, janela de 10 s

→ 404 { "code": "SHORT_CODE_NOT_FOUND" }
→ 409 { "code": "INVALID_STATE_TRANSITION",
        "meta": { "current": "READY", "attempted": "FIRED" } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Estado e carimbos | `status`, `fired_at`, `oven_in_at`, `oven_out_at`, `ready_at`, `fired_by`, `ready_by` |
| `order` | Estado agregado | `status`, `ready_at` |
| `station` | Slots ocupados no gargalo | `capacity_slots` |
| `outbox` | Eventos na mesma transação | `event_id`, `device_seq` |

> A constraint `ck_item_sequence` garante que os carimbos fiquem em ordem cronológica — é o que impede duração negativa corromper o indicador.

## 9. Comportamento offline

Integralmente local, e é onde a latência de 300 ms se torna possível: a requisição não sai da LAN.

A interface atualiza de forma otimista antes mesmo da confirmação do servidor, com reversão silenciosa em caso de erro. A idempotência protege contra o duplo Enter, que é frequente quando o operador está com pressa.

## 10. Interface e experiência

- Campo de entrada sempre focado — o operador nunca precisa clicar antes de digitar
- Confirmação visual imediata: o cartão muda de estado e de cor na hora
- Erro de código sem modal, sem som de alarme — apenas realce breve e campo limpo
- Desfazer acessível por tecla dedicada, não por menu
- Nenhum elemento da tela exige mouse, incluindo o desfazer e o avanço em lote

## 11. Métricas, alertas e observabilidade

- Latência do avanço (p95) — meta abaixo de 300 ms
- MET-002 a MET-007, todas derivadas dos carimbos gravados aqui
- Produção por operador de cozinha — indicador de pessoas do painel do dono
- Contagem de desfazer — alta indica atrito ou código pouco visível no cartão
- Contagem de códigos inexistentes digitados

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Máquina de estados do item: próximo estado natural com e sem gargalo |
| Unitário | Transições proibidas recusadas (READY → FIRED) |
| Integração | Idempotência protege contra duplo Enter |
| Integração | Todos os carimbos gravados com autor e dispositivo |
| Integração | Desfazer registra correção sem apagar o evento original |
| Desempenho | Latência abaixo de 300 ms no hardware de referência |
| Usabilidade | Ciclo completo de 12 pedidos operado apenas com teclado numérico |
| Caos offline | Avanço funciona com internet da loja derrubada |

## 13. Dependências

**Depende de:** US-040, US-032  
**Habilita:** US-071, US-103

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
- [ ] Operação validada com teclado numérico físico, pela equipe de cozinha

## 15. Riscos, premissas e pendências

- Se o código curto não estiver legível no cartão, a operação por teclado falha na origem. Tamanho e posição do código são decisões de desenho críticas.
- Avanço acidental em lote pode marcar pedidos como prontos indevidamente — daí a janela de desfazer e a confirmação explícita no modo lote.

---

*US-041 · Épico E-04 · Pacote 004_DonaBetinha · Replay Studio.*