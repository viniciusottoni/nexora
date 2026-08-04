# US-034 · Operar pedido integralmente offline

|  |  |
|---|---|
| **Épico** | [E-03 · Pedido e Roteamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 13 pontos |
| **Sprint sugerida** | Sprint 2 |
| **Requisitos funcionais** | RF-PED-09, RF-OFF-01, RF-OFF-02, RF-OFF-05 |
| **Regras de negócio** | RN-005, RN-020 |
| **ADRs** | ADR-001, ADR-007, ADR-027 |
| **Eventos** | EVT-083, EVT-084 |
| **Aplicações** | web-pos, web-kds, web-menu, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** garçom (P2), pizzaiolo (P3) e caixa (P4),
> **quero** continuar trabalhando normalmente quando a internet cair,
> **para** que a loja não pare de vender por causa de um problema de conexão.

## 2. Contexto e motivação

É o requisito estruturante do produto, registrado literalmente pelo cliente: *"Se internet cair, produção local continua funcionando"*.

É também o requisito que mais impacta arquitetura e custo (Visão Geral, 14), e o que justifica sozinho a decisão de arquitetura local-first do ADR-001.

Esta história não implementa a sincronização — isso é o E-06. Ela garante que a **operação** funcione, que os eventos sejam enfileirados corretamente e que o usuário saiba, sem susto, em que estado está.

## 3. Escopo

### 3.1 Dentro desta história

- Fluxo completo pedido→cozinha→caixa operando sem internet
- Enfileiramento de eventos no outbox local
- Fila de ações no cliente (Dexie/IndexedDB) para quedas momentâneas de LAN
- Indicador discreto de estado offline nos dispositivos
- Detecção de perda e de retorno de conexão
- Nenhuma funcionalidade operacional bloqueada no estado offline
- Degradação explícita e comunicada do que realmente depende de internet

### 3.2 Fora desta história

- Motor de sincronização (E-06)
- Indicador de atraso de sincronização no painel (US-065)
- Cache de cardápio para contingência de queda do próprio edge (RF-OFF-08, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Operação offline

  Cenário: Serviço com internet caída
    Dado que a internet da loja está indisponível
    Quando garçom, cozinha e caixa operarem normalmente
    Então todos os pedidos devem ser criados, produzidos e pagos
    E os eventos devem ficar enfileirados para sincronização
    E os dispositivos devem indicar o estado offline de forma discreta
    E nenhuma funcionalidade operacional deve ficar bloqueada

  Cenário: Queda no meio de um pedido
    Dado um cliente montando o pedido quando a conexão cai
    Quando ele confirmar o envio
    Então o pedido deve ser criado normalmente pelo servidor local
    E o cliente não deve perceber diferença no fluxo

  Cenário: Queda momentânea da rede local
    Dado um garçom cujo celular perdeu o Wi-Fi por 20 segundos
    Quando ele lançar um pedido nesse intervalo
    Então a ação deve ficar na fila local do dispositivo
    E deve ser enviada automaticamente ao reconectar
    E deve usar a mesma Idempotency-Key, sem duplicar

  Cenário: Detecção de perda de conexão
    Dado o edge conectado à nuvem
    Quando a conexão com a nuvem cair
    Então o evento edge.offline_detected deve ser registrado
    E os dispositivos devem exibir o indicador em até 30 segundos

  Cenário: Retorno da conexão
    Dado o edge offline há 40 minutos com 380 eventos acumulados
    Quando a conexão for restabelecida
    Então o evento edge.reconnected deve ser registrado
    E a sincronização deve iniciar automaticamente
    E a operação não deve ser interrompida durante o processo

  Cenário: Degradação comunicada
    Dado que a loja está sem internet
    Quando o gestor tentar acessar o painel pelo celular fora da loja
    Então deve ver que os dados estão defasados
    E deve saber há quanto tempo

  Cenário: Fechamento de conta offline
    Dado uma mesa pronta para fechar, com a internet caída
    Quando o caixa registrar o pagamento
    Então o fechamento deve concluir normalmente
    E o comprovante não fiscal deve ser gerado
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-005 | A operação local não depende de internet; a nuvem consolida | É o objeto desta história |
| RN-020 | Métrica de horário usa sempre `ocorrido_em` | Preserva a validade das métricas geradas offline |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-083 | `edge.offline_detected` | Perda de conexão com a nuvem | lastSyncAt, pendingEvents | ↑ |
| EVT-084 | `edge.reconnected` | Conexão restabelecida | offlineSeconds, pendingEvents | ↑ |

## 7. Contrato de API

```http
GET https://edge.local/v1/health
→ { "postgres": "OK", "redis": "OK",
    "cloud": "OFFLINE", "offlineSince": "...",
    "pendingEvents": 380, "lastSyncAt": "..." }

# WebSocket, para todos os dispositivos da loja:
{ "type": "sync.status",
  "data": { "online": false, "pendingEvents": 380, "lastSyncAt": "..." } }

# Fila no cliente (Dexie), reenvio ao reconectar:
# cada ação guardada com sua Idempotency-Key original
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `outbox` | Fila de eventos pendentes | `event_id`, `type`, `payload`, `device_seq`, `synced_at` |
| `edge_installation` | Estado de conexão | `last_sync_at`, `offline_since`, `pending_events` |
| IndexedDB (cliente) | Fila de ações do PWA | `action`, `payload`, `idempotencyKey`, `occurredAt` |

> A fila do cliente cobre queda de LAN entre dispositivo e edge; o outbox cobre queda de internet entre edge e nuvem. São dois problemas distintos com soluções distintas.

## 9. Comportamento offline

Esta história **é** o comportamento offline. Resumo do que funciona e do que degrada:

| Funcionalidade | Sem internet |
|---|---|
| Abrir mesa, lançar pedido, produzir, entregar | Funciona integralmente |
| KDS, cronômetro, avanço de estado | Funciona integralmente |
| Caixa, divisão de conta, pagamento, fechamento | Funciona integralmente |
| Cardápio da mesa pelo Wi-Fi local | Funciona integralmente |
| Autenticação por PIN e autorização de ação sensível | Funciona integralmente |
| Painel do dono acessado de fora da loja | Indisponível ou defasado, com indicação explícita |
| Delivery e pagamento online | Indisponível — depende de internet por natureza |
| Alteração de cardápio, preço e configuração | Indisponível até a reconexão |

O princípio 6 da Visão Geral (14.2) é obrigatório: *o painel do dono reflete o atraso de sincronização de forma explícita — nunca apresentar dado defasado como se fosse tempo real*.

## 10. Interface e experiência

- Indicador de offline discreto e permanente, nunca modal ou pop-up — o operador não pode ser interrompido
- Linguagem sem jargão: "trabalhando sem internet · 380 registros aguardando envio"
- Nenhum botão desabilitado no fluxo operacional — funcionalidade bloqueada offline é falha de desenho
- Ao reconectar, aviso breve de que a sincronização começou, sem exigir ação
- No painel do gestor, marca-d'água ou faixa indicando dado defasado e há quanto tempo

## 11. Métricas, alertas e observabilidade

- Tempo total em estado offline por dia e por instalação
- Volume de eventos acumulados no outbox durante a queda
- Contagem de quedas por instalação — insumo de diagnóstico de infraestrutura do cliente
- Alerta à plataforma quando a queda ultrapassar o limiar (US-066)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Caos offline | Derrubar a internet no meio do pico e operar 60 minutos sem interrupção |
| Caos offline | Derrubar o Wi-Fi de um dispositivo por 20 s e verificar reenvio idempotente |
| Integração | Nenhum evento se perde durante a queda; contagem no outbox bate com a operação |
| Integração | Detecção de queda e de retorno emitindo os eventos corretos |
| E2E | Ciclo completo salão→cozinha→caixa com a nuvem derrubada |
| Restauração | Após a reconexão, todos os dados aparecem na nuvem com o horário de ocorrência correto |

## 13. Dependências

**Depende de:** US-006, US-030, US-031  
**Habilita:** US-060, US-065, US-066

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
- [ ] Cenário de caos offline do documento 10 executado e aprovado
- [ ] Nenhuma funcionalidade operacional bloqueada verificada tela a tela

## 15. Riscos, premissas e pendências

- **Risco 3 da Visão Geral** — offline-first eleva custo e complexidade de forma relevante. Deve ser precificado separadamente na proposta.
- O comportamento se o **servidor local** cair (não só a internet) é pendência aberta (Visão Geral 14.3, pendência 5 do índice). Esta história cobre queda de internet, não queda do edge.
- Operadores precisam de treinamento sobre o que muda no estado offline — especialmente que nada muda na operação.

---

*US-034 · Épico E-03 · Pacote 004_DonaBetinha · Replay Studio.*