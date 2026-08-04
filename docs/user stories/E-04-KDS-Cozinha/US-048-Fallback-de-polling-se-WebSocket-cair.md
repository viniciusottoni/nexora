# US-048 · Fallback de polling se WebSocket cair

|  |  |
|---|---|
| **Épico** | [E-04 · KDS Cozinha](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 4 |
| **Requisitos funcionais** | RF-KDS-01 |
| **Regras de negócio** | RN-005 |
| **ADRs** | ADR-011 |
| **Eventos** | — |
| **Aplicações** | web-kds, web-pos, web-menu, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** pizzaiolo (P3),
> **quero** que a fila continue atualizando mesmo se a conexão em tempo real falhar,
> **para** que nenhum pedido fique invisível por causa de um problema técnico.

## 2. Contexto e motivação

Está escrito no documento 05, seção 7.4, com todas as letras: *a cozinha nunca pode depender de uma única via de comunicação. O fallback de polling é requisito, não otimização*.

A razão é simples: a falha do WebSocket é silenciosa. A tela não mostra erro, ela apenas para de atualizar — e a cozinha descobre quando o garçom vem reclamar. O polling garante um piso de 5 segundos, e o indicador visual garante que a equipe saiba que está no modo degradado.

## 3. Escopo

### 3.1 Dentro desta história

- Detecção de queda do WebSocket por heartbeat
- Ativação automática de polling a cada 5 s
- Indicação visual do modo degradado
- Reconexão com backoff (1 s, 2 s, 4 s… teto 30 s)
- Recuperação dos eventos perdidos via `lastEventId`
- Retorno automático ao tempo real quando a conexão voltar
- Aplicável também a `web-pos` e `web-menu`

### 3.2 Fora desta história

- Fila de ações do cliente para escrita offline (US-034)
- Sincronização com a nuvem (E-06)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Fallback de polling

  Cenário: Falha do canal em tempo real
    Dado que a conexão WebSocket do KDS caiu
    Quando um novo pedido for confirmado
    Então o KDS deve exibi-lo em no máximo 5 segundos via polling
    E deve indicar visualmente o modo degradado

  Cenário: Detecção por heartbeat
    Dado o heartbeat configurado a cada 20 segundos
    Quando não houver resposta por 60 segundos
    Então o cliente deve considerar a conexão perdida e reconectar

  Cenário: Reconexão com backoff
    Dado a conexão perdida
    Quando as tentativas de reconexão ocorrerem
    Então devem seguir 1 s, 2 s, 4 s, 8 s, 16 s e 30 s como teto
    E não devem sobrecarregar o servidor

  Cenário: Recuperação de eventos perdidos
    Dado um KDS que ficou 40 segundos desconectado
    Quando reconectar informando o lastEventId
    Então o servidor deve reenviar os eventos do intervalo
    E nenhum evento deve ser duplicado na fila

  Cenário: Retorno ao tempo real
    Dado o KDS em modo polling
    Quando o WebSocket reconectar
    Então o polling deve cessar
    E o indicador de modo degradado deve sumir

  Cenário: Escrita durante o modo degradado
    Dado o KDS em modo polling
    Quando o operador avançar um item
    Então a operação deve funcionar normalmente por REST
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-005 | A operação local não depende de internet | O fallback garante o piso de atualização mesmo com falha do canal |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
# Heartbeat (cliente → servidor), a cada 20 s:
{ "type": "heartbeat" }

# Reconexão com recuperação:
wss://edge.local/rt?token=<jwt>&lastEventId=<uuid>

# Polling de fallback, a cada 5 s:
GET /v1/kds/queue?stationId=...&since=<lastEventId>
→ { "items": [...], "lastEventId": "...", "degraded": true }
```

> A escrita (avanço de estado) nunca passa por WebSocket — é sempre REST. Por isso a operação continua íntegra no modo degradado.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `domain_event` | Fonte da recuperação por `lastEventId` | `id`, `device_seq`, `occurred_at` |

## 9. Comportamento offline

Esta história trata da falha do canal em tempo real **dentro** da rede local, que é problema distinto da queda de internet.

As duas se combinam: com a internet caída e o WebSocket funcionando, tudo opera normalmente; com o WebSocket caído, o polling garante o piso de 5 segundos; com a LAN caída, a fila de ações do cliente (US-034) preserva a operação até a reconexão.

## 10. Interface e experiência

- Indicador de modo degradado discreto e inequívoco — nunca modal, nunca alarme
- Nenhuma funcionalidade bloqueada no modo degradado
- Retorno ao tempo real silencioso, sem notificação que interrompa

## 11. Métricas, alertas e observabilidade

- Contagem e duração de quedas de WebSocket por dispositivo — diagnóstico direto da rede da loja
- Tempo total em modo degradado por dia
- Eventos recuperados na reconexão

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Queda do WebSocket ativa o polling em até 5 s |
| Integração | Backoff segue a progressão especificada |
| Integração | Recuperação por `lastEventId` sem duplicar nem perder evento |
| Caos | Derrubar o WebSocket repetidamente durante o pico sem perder pedido |

## 13. Dependências

**Depende de:** US-031, US-040  
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
- [ ] Teste de caos de rede executado com o WebSocket derrubado durante operação real

## 15. Riscos, premissas e pendências

- Polling mal implementado (sem `since`) recarrega a fila inteira a cada 5 s e degrada o desempenho. A recuperação incremental por `lastEventId` é obrigatória.

---

*US-048 · Épico E-04 · Pacote 004_DonaBetinha · Replay Studio.*