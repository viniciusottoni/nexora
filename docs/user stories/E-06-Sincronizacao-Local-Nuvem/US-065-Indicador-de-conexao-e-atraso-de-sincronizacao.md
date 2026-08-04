# US-065 · Indicador de conexao e atraso de sincronizacao

|  |  |
|---|---|
| **Épico** | [E-06 · Sincronizacao Local-Nuvem](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 6 |
| **Requisitos funcionais** | RF-OFF-05, RF-BI-14 |
| **Regras de negócio** | RN-005 |
| **ADRs** | ADR-011 |
| **Eventos** | — |
| **Aplicações** | web-pos, web-kds, web-admin, api-edge, api-cloud |
| **Autoridade do dado** | Local e nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8) e operação,
> **quero** saber se o sistema está conectado e o quanto os dados estão defasados,
> **para** que eu nunca tome decisão achando que estou vendo o agora quando não estou.

## 2. Contexto e motivação

Princípio 6 da Visão Geral, seção 14.2: *o painel do dono reflete o atraso de sincronização de forma explícita — nunca apresentar dado defasado como se fosse tempo real*.

São dois públicos com necessidades distintas. A **operação** precisa saber que está offline para não estranhar a ausência do delivery, mas sem alarme que interrompa o trabalho. O **gestor** precisa saber, de forma inequívoca, que os números que está lendo têm 40 minutos de atraso.

## 3. Escopo

### 3.1 Dentro desta história

- Indicador de estado de conexão nos dispositivos da loja
- Contador de eventos pendentes
- Indicador de atraso de sincronização no painel do dono
- Campo `syncDelaySeconds` e `asOf` em toda resposta de métrica
- Distinção visual entre online, degradado e offline
- Linguagem sem jargão técnico

### 3.2 Fora desta história

- Alerta ativo de atraso (US-066)
- Painel de saúde da plataforma (US-140, Fase 5)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Indicador de conexão e atraso

  Cenário: Indicador na operação
    Dado que a loja está sem internet
    Quando o garçom, a cozinha ou o caixa usarem o sistema
    Então deve haver indicação discreta e permanente do estado offline
    E deve informar quantos registros aguardam envio
    E não deve haver modal nem interrupção do trabalho

  Cenário: Atraso explícito no painel do dono
    Dado o painel acessado com 40 minutos de atraso de sincronização
    Quando os números forem exibidos
    Então deve estar visível que os dados têm 40 minutos de defasagem
    E o horário de referência dos dados deve ser informado

  Cenário: Estado normal
    Dado a sincronização em dia, com atraso abaixo de 30 segundos
    Quando o painel for exibido
    Então o indicador deve ser discreto e não competir com os números

  Cenário: Modo degradado do WebSocket
    Dado que o WebSocket caiu mas a internet está funcionando
    Quando o dispositivo entrar em polling
    Então deve indicar modo degradado, distinto do estado offline

  Cenário: Linguagem sem jargão
    Dado o indicador exibido a um operador
    Quando a mensagem for lida
    Então deve dizer algo como "trabalhando sem internet · 380 registros aguardando envio"
    E não deve mencionar outbox, cursor ou sincronização

  Cenário: Retorno ao normal
    Dado o edge reconectado e a fila sincronizada
    Quando a sincronização concluir
    Então o indicador deve voltar ao estado normal sem exigir ação
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-005 | A operação local não depende de internet | O indicador informa sem bloquear |
| RN-020 | Métrica usa `ocorrido_em` | O `asOf` informa até quando os dados são confiáveis |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> Consome `edge.offline_detected`, `edge.reconnected` e `sync.delayed`.

## 7. Contrato de API

```http
# WebSocket, para todos os dispositivos da loja:
{ "type": "sync.status",
  "data": { "online": false, "pendingEvents": 380,
            "lastSyncAt": "2026-07-31T20:35:00Z",
            "degraded": false } }

GET https://edge.local/v1/health
→ { "cloud": "OFFLINE", "offlineSince": "...", "pendingEvents": 380 }

# Toda resposta de métrica carrega a defasagem:
GET /v1/dashboard/pulse
→ { "revenueToday": 482000, ...,
    "syncDelaySeconds": 2400,
    "asOf": "2026-07-31T21:02:00Z" }
```

> `syncDelaySeconds` e `asOf` são obrigatórios em toda resposta de métrica — é contrato, não opcional.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `edge_installation` | Estado da instalação | `last_sync_at`, `offline_since`, `pending_events`, `sync_lag_seconds` |
| `outbox` | Contagem de pendentes | `synced_at IS NULL` |

## 9. Comportamento offline

É a história que **comunica** o estado offline. Precisa funcionar em ambos os cenários: com o edge offline em relação à nuvem, e com um dispositivo offline em relação ao edge.

O tom da comunicação importa: para a operação, informação discreta e permanente; para a gestão, alerta inequívoco. A mesma informação técnica exige tratamentos opostos.

## 10. Interface e experiência

- Nunca modal, nunca pop-up, nunca som — a operação não pode ser interrompida por um estado que não a impede de trabalhar
- Discreto porém permanente: uma faixa fina ou ícone com contagem, sempre no mesmo lugar
- No painel do gestor, o oposto: faixa clara indicando que os dados estão defasados e desde quando
- Três estados visualmente distintos: normal, degradado (WebSocket caído) e offline (sem internet)
- Linguagem de negócio, nunca de infraestrutura

## 11. Métricas, alertas e observabilidade

- Tempo em cada estado por dispositivo e por instalação
- Atraso médio e máximo de sincronização por dia
- Correlação entre estado offline e volume de operação — mede o impacto real das quedas

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Indicador reflete o estado real em menos de 30 s |
| Integração | `syncDelaySeconds` e `asOf` presentes em toda resposta de métrica |
| Integração | Distinção correta entre degradado e offline |
| Usabilidade | Mensagem compreendida por operador sem conhecimento técnico |
| Caos offline | Indicador correto durante e após uma queda longa |

## 13. Dependências

**Depende de:** US-034, US-061  
**Habilita:** US-066, US-070

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

- Indicador intrusivo demais é ignorado ou desativado; discreto demais não é percebido. Calibrar no piloto com usuários reais.

---

*US-065 · Épico E-06 · Pacote 004_DonaBetinha · Replay Studio.*