# US-066 · Alerta de atraso de sincronizacao

|  |  |
|---|---|
| **Épico** | [E-06 · Sincronizacao Local-Nuvem](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 6 |
| **Requisitos funcionais** | RF-OFF-06 |
| **Regras de negócio** | RN-003 |
| **ADRs** | ADR-022 |
| **Eventos** | EVT-081 |
| **Aplicações** | api-edge, api-cloud, web-admin, web-platform |
| **Autoridade do dado** | Local e nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8) e administrador da plataforma (P9),
> **quero** ser avisado quando a sincronização parar de funcionar,
> **para** que o problema seja resolvido antes de virar perda de dado ou decisão errada.

## 2. Contexto e motivação

Queda de sincronização é falha silenciosa: nada para de funcionar na loja, e por isso ninguém percebe. O gestor continua olhando o painel achando que está vendo o dia de hoje.

O limiar definido no documento 02, seção 6.5, é de **5 minutos** — acima disso, gestor e plataforma são avisados. O alerta é duplo por desenho: o gestor precisa saber para não confiar no painel; a plataforma precisa saber para agir tecnicamente.

## 3. Escopo

### 3.1 Dentro desta história

- Detecção de atraso acima do limiar configurável
- Alerta ao gestor pelos canais da US-081
- Alerta à plataforma no painel de saúde
- Escalonamento por duração do atraso
- Resolução automática do alerta quando a sincronização normalizar
- Distinção entre atraso por queda de internet e por falha do worker

### 3.2 Fora desta história

- Indicador passivo (US-065)
- Painel de saúde da plataforma (US-140)
- Diagnóstico remoto automatizado

## 4. Critérios de aceite

```gherkin
Funcionalidade: Alerta de atraso de sincronização

  Cenário: Atraso acima do limiar
    Dado o limiar configurado em 5 minutos
    Quando o atraso de sincronização ultrapassar 5 minutos
    Então o gestor deve ser alertado
    E a plataforma deve ser alertada
    E o evento sync.delayed deve ser emitido

  Cenário: Escalonamento por duração
    Dado um atraso de 5 minutos já alertado
    Quando o atraso ultrapassar 30 minutos
    Então o alerta deve escalar de severidade
    E a plataforma deve receber notificação prioritária

  Cenário: Resolução automática
    Dado um alerta de atraso ativo
    Quando a sincronização normalizar
    Então o alerta deve ser resolvido automaticamente
    E a duração total do incidente deve ficar registrada

  Cenário: Alerta não repetido
    Dado um alerta de atraso já ativo
    Quando o atraso continuar
    Então não devem ser criados alertas duplicados
    E apenas a severidade deve mudar conforme o escalonamento

  Cenário: Falha do worker com internet disponível
    Dado a internet funcionando mas o worker de sync parado
    Quando o atraso ultrapassar o limiar
    Então o alerta deve indicar falha técnica, não queda de internet
    E a plataforma deve ser priorizada no direcionamento
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-003 | Cada transição de estado gera alerta aos perfis envolvidos | Gestor e plataforma são os perfis desta situação |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-081 | `sync.delayed` | Atraso acima do limiar | delaySeconds, pendingEvents, cause | ↑ |
| EVT-083 | `edge.offline_detected` | Perda de conexão | lastSyncAt, pendingEvents | ↑ |

> Reação normativa: `sync.delayed` → notifica gestor e plataforma, registra atraso como métrica.

## 7. Contrato de API

```http
# Alerta pelos canais da US-081:
{ "type": "alert.raised",
  "data": { "alertType": "SYNC_DELAYED", "severity": "HIGH",
            "entityId": "<installationId>",
            "message": "Os dados da loja estão 42 minutos atrasados.",
            "meta": { "delaySeconds": 2520, "pendingEvents": 380,
                      "cause": "NO_INTERNET" } } }

PATCH /v1/tenant/thresholds
{ "syncDelayWarnMinutes": 5, "syncDelayCriticalMinutes": 30 }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `alert` | Alerta ativo | `type=SYNC_DELAYED`, `severity`, `raised_at`, `resolved_at`, `entity_id` |
| `edge_installation` | Origem da detecção | `sync_lag_seconds`, `offline_since` |
| `tenant_config` | Limiares | `thresholds.syncDelayWarnMinutes` |

## 9. Comportamento offline

Situação particular: se a queda for de internet, o edge não consegue avisar a nuvem. Por isso a detecção é **dupla**.

- **Do lado do edge:** detecta que não consegue enviar e alerta localmente quem estiver na loja.
- **Do lado da nuvem:** detecta a ausência de contato da instalação (heartbeat perdido) e alerta o gestor por push e a plataforma no painel de saúde.

É a segunda que garante que o gestor fora da loja seja avisado.

## 10. Interface e experiência

- Mensagem em linguagem de negócio: "os dados da loja estão 42 minutos atrasados", não "sync lag 2520s"
- No painel do gestor, o alerta acompanha a faixa de defasagem da US-065, sem duplicar informação
- Para a plataforma, informação técnica completa: causa provável, eventos pendentes, última versão vista

## 11. Métricas, alertas e observabilidade

- Contagem e duração de incidentes de sincronização por instalação
- Tempo médio de resolução
- Instalações com incidentes recorrentes — insumo de conversa sobre infraestrutura com o cliente

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Alerta disparado no limiar, para gestor e plataforma |
| Integração | Escalonamento por duração sem duplicar alerta |
| Integração | Resolução automática ao normalizar |
| Integração | Detecção pela nuvem quando o edge está sem internet |
| Integração | Distinção entre queda de internet e falha do worker |

## 13. Dependências

**Depende de:** US-065, US-080  
**Habilita:** US-140

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

- Limiar de 5 minutos pode ser ruidoso em loja com internet ruim. Configurável por tenant, com calibração no piloto.

---

*US-066 · Épico E-06 · Pacote 004_DonaBetinha · Replay Studio.*