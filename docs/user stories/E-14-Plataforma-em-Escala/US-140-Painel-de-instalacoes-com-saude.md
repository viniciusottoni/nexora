# US-140 · Painel de instalacoes com saude

> ⚠️ **Marcada para redesenho em 06/08/2026 — não cancelada.** Sem edge, não existe mais hardware físico por loja para monitorar (ver [ADR-040](../../adrs/ADR-040-arquitetura-100-online-api-unica.md) e [E-16 · iMenu Online](../E-16-iMenu-Online/README.md)). A tabela `edge_installation` que sustenta esta história é removida (E-16/US-169). O valor original — "descobrir o problema antes de o cliente ligar" — provavelmente continua válido para a Fase 5, mas precisa ser redesenhado em torno de saúde de tenant/API (taxa de erro, latência, jobs de background por tenant), não de hardware de loja. Esta história **não deve ser implementada como está** — precisa de refinamento próprio quando a Fase 5 for planejada.

|  |  |
|---|---|
| **Épico** | [E-14 · Plataforma em Escala](./README.md) — ⚠️ **PENDENTE DE REDESENHO** |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 5 |
| **Requisitos funcionais** | RF-PLT-07 |
| **Regras de negócio** | — |
| **ADRs** | ADR-022 |
| **Eventos** | — |
| **Aplicações** | web-platform, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** ver a saúde de todas as instalações em uma tela,
> **para** que eu descubra o problema antes de o cliente ligar.

## 2. Contexto e motivação

Suporte reativo não escala. Com dez lojas, cada incidente descoberto por ligação do cliente consome tempo e desgasta a relação.

O painel consolida o que já é medido: atraso de sincronização, versão, último contato, eventos pendentes e alertas abertos. É a ferramenta que torna o modelo de receita recorrente sustentável.

## 3. Escopo

### 3.1 Dentro desta história

- Lista de instalações com estado de saúde
- Atraso de sincronização, versão, último contato e eventos pendentes
- Classificação: saudável, degradada, fora do ar
- Alerta automático de mudança de estado
- Histórico de incidentes por instalação
- Diagnóstico remoto: logs e health check

### 3.2 Fora desta história

- Acesso a dados de negócio do cliente (US-145)
- Atualização do parque (US-146)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Painel de saúde das instalações

  Cenário: Visão consolidada
    Dado 12 instalações ativas
    Quando o painel for aberto
    Então deve mostrar todas com versão, último contato,
         atraso de sincronização e estado de saúde

  Cenário: Instalação fora do ar
    Dado uma instalação sem contato há mais que o limiar
    Quando o painel for atualizado
    Então deve aparecer como fora do ar
    E a plataforma deve ser alertada automaticamente

  Cenário: Instalação degradada
    Dado uma instalação com sincronização atrasada mas ativa
    Quando o painel for exibido
    Então deve aparecer como degradada, distinta de fora do ar

  Cenário: Deriva de versão
    Dado instalações em versões distintas
    Quando o painel for exibido
    Então as desatualizadas devem ser identificadas
    E a diferença de versão deve estar visível

  Cenário: Histórico de incidentes
    Dado uma instalação com incidentes anteriores
    Quando o histórico for consultado
    Então deve mostrar duração e causa de cada um

  Cenário: Diagnóstico remoto
    Dado uma instalação degradada
    Quando o diagnóstico for solicitado
    Então devem ser exibidos health check e logs recentes
    E nenhum dado de negócio do cliente deve ser exposto
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-015 | Isolamento entre estabelecimentos | O painel expõe saúde técnica, nunca dado de negócio |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> Consome `edge.offline_detected`, `edge.reconnected` e `sync.delayed`.

## 7. Contrato de API

```http
GET /v1/platform/installations
→ [ { "tenantName": "Pizzaria Dona Betinha", "storeName": "Matriz",
      "version": "1.4.2", "expectedVersion": "1.4.2",
      "lastSeenAt": "...", "syncLagSeconds": 4,
      "pendingEvents": 0, "openAlerts": 0,
      "health": "OK" } ]

GET /v1/platform/installations/{id}/diagnostics
→ { "healthCheck": { "postgres": "OK", "redis": "OK", "sync": "OK" },
    "recentLogs": [...], "diskUsagePercent": 42,
    "lastBackupAt": "..." }

GET /v1/platform/installations/{id}/incidents
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `edge_installation` | Estado de cada instalação | `version`, `last_seen_at`, `sync_lag_seconds`, `pending_events`, `health` |
| `installation_incident` | Histórico de incidentes | `type`, `started_at`, `resolved_at`, `cause` |

## 9. Comportamento offline

O painel é de nuvem. Uma instalação offline é justamente o que ele precisa detectar — a ausência de contato é o sinal, e a detecção é da nuvem, não do edge.

## 10. Interface e experiência

- Estados distintos por cor e por rótulo: saudável, degradada, fora do ar
- Ordenação por criticidade como padrão
- Diagnóstico acessível sem sair do painel
- Nenhum dado de negócio do cliente visível — só saúde técnica

## 11. Métricas, alertas e observabilidade

- Disponibilidade por instalação
- Tempo médio de detecção e de resolução de incidentes
- Distribuição de versões no parque
- Instalações com incidentes recorrentes

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Estado de saúde reflete a realidade de cada instalação |
| Integração | Alerta automático em mudança de estado |
| Isolamento | Painel não expõe dado de negócio de nenhum tenant |

## 13. Dependências

**Depende de:** US-066, US-068  
**Habilita:** US-145, US-146

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

- **Risco 14 da Visão Geral** — suporte contínuo a várias instalações locais é custo estrutural do modelo. Este painel é a mitigação principal.

---

*US-140 · Épico E-14 · Pacote 004_DonaBetinha · Replay Studio.*