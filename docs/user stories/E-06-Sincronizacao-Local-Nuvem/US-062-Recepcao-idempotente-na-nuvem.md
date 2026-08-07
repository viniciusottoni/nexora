# US-062 · Recepcao idempotente na nuvem

|  |  |
|---|---|
| **Épico** | [E-06 · Sincronizacao Local-Nuvem](./README.md) — ❌ **CANCELADA** |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 6 |
| **Requisitos funcionais** | RF-OFF-03 |
| **Regras de negócio** | RN-015 |
| **ADRs** | ADR-020, ADR-035 |
| **Eventos** | — |
| **Aplicações** | api-cloud, packages/sync |
| **Autoridade do dado** | Nuvem |

---

> ❌ **Cancelada em 06/08/2026.** Mudança de foco de negócio: o produto passa a operar 100% online, sem edge nem sincronização (ver [ADR-040](../../adrs/ADR-040-arquitetura-100-online-api-unica.md) e [E-16](../E-16-iMenu-Online/README.md)). Conteúdo mantido como registro histórico.

## 1. História

> **Como** time de desenvolvimento,
> **quero** que a nuvem aceite qualquer reenvio sem duplicar registro,
> **para** que uma falha de rede nunca gere pedido, pagamento ou receita em duplicidade.

## 2. Contexto e motivação

A idempotência é a garantia mais importante do sync. O mecanismo é simples e por isso confiável: `event_id` é UUID v7 **gerado na origem** e é chave primária na nuvem. Upsert por essa chave torna o reenvio inofensivo por construção.

A nuvem também valida assinatura e tenant, aplica os eventos em ordem de sequência e responde exatamente até onde aceitou — o que permite ao edge saber precisamente o que já está seguro.

## 3. Escopo

### 3.1 Dentro desta história

- Validação de assinatura HMAC e de instalação
- Deduplicação por `event_id` com upsert
- Aplicação em ordem de `device_seq`
- Validação de schema por versão, com FluentValidation
- Registro de eventos rejeitados, sem travar o lote
- Atribuição de `recorded_at` na nuvem
- Resposta com `acceptedUntilSeq`, duplicados, rejeitados e conflitos
- Materialização do estado a partir dos eventos recebidos

### 3.2 Fora desta história

- Resolução de conflitos (US-067)
- Agregação de métricas (E-07)
- Pull de configuração (US-063)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Recepção idempotente

  Cenário: Reenvio de lote
    Dado um lote já processado
    Quando o mesmo lote for reenviado
    Então nenhum registro deve ser duplicado
    E a resposta deve informar a quantidade de duplicados ignorados

  Cenário: Aplicação em ordem
    Dado um lote com eventos de device_seq 100 a 150
    Quando forem aplicados
    Então devem ser processados na ordem da sequência
    E um evento fora de ordem deve ser rejeitado com motivo explícito

  Cenário: Schema inválido
    Dado um evento com payload que não corresponde à sua versão de schema
    Quando o lote for processado
    Então esse evento deve ser rejeitado e registrado
    E os demais do lote devem ser aplicados normalmente

  Cenário: Assinatura inválida
    Dado uma requisição com HMAC incorreto
    Quando chegar à nuvem
    Então deve ser recusada com 401
    E a tentativa deve ser registrada para investigação

  Cenário: Tenant divergente
    Dado um evento cujo tenantId não corresponde à instalação que assinou
    Quando for processado
    Então deve ser rejeitado
    E deve gerar alerta de segurança na plataforma

  Cenário: Atribuição de recordedAt
    Dado um evento com occurredAt de 20h03
    Quando for recebido às 21h15
    Então occurredAt deve permanecer 20h03
    E recordedAt deve ser 21h15

  Cenário: Materialização de estado
    Dado eventos de criação e avanço de um pedido
    Quando forem aplicados na nuvem
    Então o estado do pedido na nuvem deve corresponder ao da loja
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-015 | Isolamento entre estabelecimentos | Tenant validado contra a instalação assinante; divergência é incidente de segurança |
| RN-020 | Métrica usa `ocorrido_em` | `occurred_at` preservado; `recorded_at` atribuído na chegada |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
POST /v1/sync/push
→ 200 { "acceptedUntilSeq": 148600,
        "duplicates": 3,
        "rejected": [ { "eventId": "...", "reason": "SCHEMA_INVALID" },
                      { "eventId": "...", "reason": "OUT_OF_ORDER" } ],
        "conflicts": [ { "eventId": "...", "resolution": "KEPT_REMOTE" } ] }
→ 401 { "code": "INVALID_SIGNATURE" }
→ 403 { "code": "TENANT_MISMATCH" }

GET /v1/sync/health
→ { "serverTime": "...", "expectedVersion": "1.4.2", "configVersion": 88 }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `domain_event` | Log append-only, particionado por `occurred_at` | `id` (PK, do edge), `occurred_at`, `recorded_at`, `device_seq`, `origin` |
| `sync_cursor` | Cursor de recepção por instalação | `installation_id`, `last_accepted_seq` |
| Tabelas de negócio | Estado materializado a partir dos eventos | `order`, `order_item`, `payment`, `table_session`… |
| `sync_conflict` | Conflitos registrados | `event_id`, `resolution`, `reviewed_at` |

> `domain_event` é particionada por `occurred_at`, não por `recorded_at` — evento sincronizado com atraso pertence ao mês em que ocorreu (ADR-035, decisão 6 do ERD).

## 9. Comportamento offline

Componente exclusivo de nuvem. O que ele precisa garantir, do ponto de vista do offline, é que **um edge que passou horas desconectado seja atendido igual a um que nunca desconectou** — sem tratamento especial, sem janela de aceitação, sem descarte por antiguidade.

## 10. Interface e experiência

- Sem interface — infraestrutura
- Rejeições visíveis no painel de plataforma, para diagnóstico (US-140)

## 11. Métricas, alertas e observabilidade

- Eventos recebidos, duplicados e rejeitados por instalação
- Distribuição de atraso entre `occurred_at` e `recorded_at`
- Alerta de plataforma para taxa de rejeição acima do limiar — indica versão incompatível no parque
- Alerta de segurança para divergência de tenant ou assinatura inválida

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Validação de schema por versão; tratamento de versões antigas |
| Integração | Reenvio do mesmo lote não duplica nenhum registro |
| Integração | Evento fora de ordem é rejeitado com motivo |
| Integração | Schema inválido não trava o restante do lote |
| Integração | `occurredAt` preservado e `recordedAt` atribuído |
| Segurança | HMAC inválido e tenant divergente são recusados e registrados |
| Carga | 4.000 eventos processados em menos de 5 minutos |

## 13. Dependências

**Depende de:** US-061  
**Habilita:** US-067, US-068, US-070

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

- Materializar estado a partir de eventos exige que a lógica da nuvem seja idêntica à do edge — daí `packages/domain` compartilhado ser inegociável (ADR-015).
- Versão de schema divergente entre parque e nuvem é o risco T7 (deriva de versão). O monitoramento de versão por instalação é a mitigação.

---

*US-062 · Épico E-06 · Pacote 004_DonaBetinha · Replay Studio.*