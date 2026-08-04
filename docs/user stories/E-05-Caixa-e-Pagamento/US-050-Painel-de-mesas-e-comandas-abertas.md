# US-050 · Painel de mesas e comandas abertas

|  |  |
|---|---|
| **Épico** | [E-05 · Caixa e Pagamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 5 |
| **Requisitos funcionais** | RF-CXA-01 |
| **Regras de negócio** | — |
| **ADRs** | ADR-011 |
| **Eventos** | — |
| **Aplicações** | web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** caixa (P4),
> **quero** ver todas as mesas e comandas abertas em uma tela, com valor e tempo,
> **para** que eu saiba a situação do salão sem perguntar a ninguém.

## 2. Contexto e motivação

A tela do caixa exige alta densidade de informação: todas as mesas visíveis de uma vez, sem rolagem, com valor e tempo. É o oposto do KDS, que privilegia legibilidade a distância.

As mesas com conta solicitada precisam saltar aos olhos — é onde o caixa deve agir primeiro, e é onde o tempo de espera do cliente começa a contar contra a experiência.

## 3. Escopo

### 3.1 Dentro desta história

- Lista de todas as sessões abertas com valor, tempo, pessoas e garçom
- Destaque para mesas com conta solicitada
- Ordenação por urgência, com alternativa por número de mesa
- Atualização em tempo real
- Busca por mesa e por comanda
- Totalizador do salão aberto

### 3.2 Fora desta história

- Montagem da conta (US-051)
- Recebimento (US-052)
- Mapa visual do salão para o garçom (US-023)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Painel do caixa

  Cenário: Visão de todas as comandas
    Dado 14 mesas abertas
    Quando o caixa abrir o painel
    Então deve ver as 14 com valor consumido, tempo aberto e garçom responsável
    E deve ver o total consolidado do salão aberto

  Cenário: Prioridade de conta solicitada
    Dado três mesas com conta solicitada
    Quando o painel for exibido na ordenação padrão
    Então as três devem aparecer no topo
    E devem exibir há quanto tempo a conta foi pedida

  Cenário: Atualização em tempo real
    Dado o painel aberto
    Quando um pedido for confirmado em qualquer mesa
    Então o valor daquela mesa deve atualizar em até 2 segundos

  Cenário: Densidade de informação
    Dado 30 mesas abertas
    Quando o painel for exibido em monitor de desktop
    Então todas devem caber na tela sem rolagem

  Cenário: Operação offline
    Dado que a loja está sem internet
    Quando o caixa abrir o painel
    Então todas as informações devem estar corretas e atualizadas
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-005 | A operação local não depende de internet | Painel servido pelo edge |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> Consome `order.placed`, `table.bill_requested`, `payment.registered` e `table.session.closed`.

## 7. Contrato de API

```http
GET /v1/cash/open-sessions
→ { "sessions": [ { "sessionId": "...", "table": "12", "area": "Salão",
                    "openedAt": "...", "minutesOpen": 47,
                    "guestCount": 4, "waiter": { "name": "Ana" },
                    "total": 18700,
                    "status": "BILL_REQUESTED",
                    "billRequestedAt": "...", "waitingSeconds": 180,
                    "pendingItems": 0 } ],
    "summary": { "openSessions": 14, "totalOpen": 218400 } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `table_session` | Sessões abertas | `status`, `opened_at`, `total`, `guest_count`, `waiter_id` |
| `dining_table` | Identificação e ambiente | `label`, `area_id` |
| `order_item` | Itens pendentes de entrega | `status` |

## 9. Comportamento offline

Integralmente local, com WebSocket do edge e fallback de polling.

## 10. Interface e experiência

- Densidade alta: todas as mesas em uma tela, sem rolagem — é a diretriz de experiência do caixa
- Conta solicitada em destaque, com o tempo de espera correndo
- Cores consistentes com o mapa do garçom e com o KDS
- Busca com foco automático, para operação por teclado

## 11. Métricas, alertas e observabilidade

- Tempo entre solicitação de conta e recebimento — gargalo clássico do fim da noite
- Valor médio em aberto no salão por faixa horária
- Sessões abertas há mais tempo que a média

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Totalizador bate com a soma das sessões |
| Integração | Atualização em tempo real em menos de 2 s |
| Desempenho | 30 sessões renderizadas sem rolagem em monitor de 1080p |
| Caos offline | Painel correto com internet caída |

## 13. Dependências

**Depende de:** US-022, US-031  
**Habilita:** US-051

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

*US-050 · Épico E-05 · Pacote 004_DonaBetinha · Replay Studio.*