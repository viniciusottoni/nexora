# US-068 · Recuperacao apos reconexao longa

|  |  |
|---|---|
| **Épico** | [E-06 · Sincronizacao Local-Nuvem](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 6 |
| **Requisitos funcionais** | RF-OFF-02, RF-OFF-03 |
| **Regras de negócio** | RN-020 |
| **ADRs** | ADR-007, ADR-035 |
| **Eventos** | EVT-084 |
| **Aplicações** | api-edge, api-cloud, packages/sync |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** que uma noite inteira operada sem internet suba corretamente quando a conexão voltar,
> **para** que eu não perca o histórico nem tenha número errado no painel.

## 2. Contexto e motivação

É o teste real da arquitetura. Uma queda de internet de seis horas em uma sexta-feira de pico gera milhares de eventos acumulados. Recuperar isso sem perder nada, sem duplicar nada, sem travar a operação em curso e com os horários corretos é o que separa uma arquitetura offline-first de uma promessa de marketing.

A meta é explícita: 4.000 eventos sincronizando em **menos de 5 minutos**, e o painel refletindo todos os dados no horário correto de ocorrência.

## 3. Escopo

### 3.1 Dentro desta história

- Sincronização em lotes ordenados após reconexão longa
- Priorização sem bloquear a operação corrente
- Controle de vazão para não sobrecarregar a nuvem nem a internet da loja
- Recálculo de agregados afetados pelos eventos atrasados
- Verificação de integridade após a recuperação
- Registro do incidente com duração e volume

### 3.2 Fora desta história

- Recuperação de falha do próprio servidor local (pendência aberta)
- Restauração de backup (US-006)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Recuperação após reconexão longa

  Cenário: Retomada após 6 horas offline
    Dado 4.000 eventos acumulados no outbox
    Quando a conexão for restabelecida
    Então a sincronização deve ocorrer em lotes ordenados
    E concluir em menos de 5 minutos
    E o painel deve refletir todos os dados no horário correto de ocorrência

  Cenário: Operação não bloqueada durante a recuperação
    Dado a sincronização de recuperação em andamento
    Quando novos pedidos forem criados na loja
    Então a operação deve seguir normalmente
    E os eventos novos devem entrar na fila sem furar a ordem

  Cenário: Controle de vazão
    Dado uma internet de baixa capacidade na loja
    Quando a recuperação estiver em curso
    Então a taxa de envio deve se ajustar
    E não deve saturar a conexão a ponto de prejudicar outros usos

  Cenário: Recálculo de agregados
    Dado eventos das 20h chegando às 02h
    Quando forem aplicados
    Então os agregados horários das 20h devem ser recalculados
    E o mapa de calor deve refletir o pico real

  Cenário: Verificação de integridade
    Dado a recuperação concluída
    Quando a verificação for executada
    Então a contagem de pedidos, itens e pagamentos deve bater entre loja e nuvem
    E qualquer divergência deve gerar alerta

  Cenário: Interrupção durante a recuperação
    Dado a conexão caindo novamente no meio da recuperação
    Quando voltar
    Então deve retomar do cursor, sem reenviar o já confirmado

  Cenário: Registro do incidente
    Dado a recuperação concluída
    Quando o incidente for registrado
    Então deve conter duração do offline, volume de eventos e tempo de recuperação
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-020 | Métrica usa `ocorrido_em` | É o que garante o painel correto após a recuperação |
| RN-005 | A operação local não depende de internet | A recuperação nunca pode bloquear a operação |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-084 | `edge.reconnected` | Conexão restabelecida | offlineSeconds, pendingEvents | ↑ |

## 7. Contrato de API

```http
POST /v1/sync/push          # em lotes de até 500 eventos ou 1 MB

GET /v1/sync/integrity-check?installationId=...&businessDay=2026-07-31
→ { "edge":  { "orders": 184, "items": 512, "payments": 176 },
    "cloud": { "orders": 184, "items": 512, "payments": 176 },
    "match": true }

# Job de recálculo de agregados afetados:
POST /v1/internal/metrics/recompute
{ "businessDay": "2026-07-31", "reason": "LATE_EVENTS" }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `outbox` | Fila acumulada | `device_seq`, `synced_at` |
| `sync_cursor` | Retomada exata | `cursor` |
| `metric_hourly` / `metric_daily` | Agregados recalculados | `bucket_hour`, `recomputed_at` |
| `edge_installation` | Registro do incidente | `offline_since`, `recovered_at`, `recovered_events` |

## 9. Comportamento offline

Esta história é a prova final do requisito estruturante. Todo o resto da arquitetura offline existe para que este cenário funcione.

O recálculo noturno de agregados (definido no doc. 04, seção 6.1) é o complemento: um job recalcula o dia anterior por completo, corrigindo agregados afetados por eventos que chegaram atrasados. Sem ele, o relatório do dia seguinte mostraria números incompletos.

## 10. Interface e experiência

- Progresso da recuperação visível no painel do gestor e no de plataforma
- Mensagem clara ao concluir: "todos os dados da loja foram sincronizados"
- Nenhuma interrupção da operação durante o processo

## 11. Métricas, alertas e observabilidade

- Tempo de recuperação por volume de eventos
- Vazão de sincronização em eventos por segundo
- Resultado da verificação de integridade — divergência é incidente, não estatística
- Histórico de incidentes de offline prolongado por instalação

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Caos offline | 6 horas offline com 4.000 eventos, recuperando em menos de 5 minutos |
| Caos | Conexão caindo no meio da recuperação, com retomada correta |
| Integração | Operação corrente não bloqueada durante a recuperação |
| Integração | Agregados recalculados refletem o horário real de ocorrência |
| Integração | Verificação de integridade bate entre loja e nuvem |
| Carga | Recuperação sob rede limitada, com controle de vazão |

## 13. Dependências

**Depende de:** US-061, US-062, US-064  
**Habilita:** US-070, US-140

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
- [ ] Cenário de 6 horas offline executado com dados reais de volume equivalente ao pico
- [ ] Verificação de integridade automatizada rodando diariamente

## 15. Riscos, premissas e pendências

- **Risco T2** — divergência após sincronização longa. A verificação de integridade diária é a detecção; a conciliação assistida é a correção.
- Recuperação que satura a internet da loja atrapalha o delivery e o pagamento online no momento em que a conexão acabou de voltar. Controle de vazão é obrigatório.

---

*US-068 · Épico E-06 · Pacote 004_DonaBetinha · Replay Studio.*