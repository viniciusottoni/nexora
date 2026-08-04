# US-061 · Worker de envio com retry e cursor

|  |  |
|---|---|
| **Épico** | [E-06 · Sincronizacao Local-Nuvem](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 6 |
| **Requisitos funcionais** | RF-OFF-02 |
| **Regras de negócio** | RN-005 |
| **ADRs** | ADR-007, ADR-031 |
| **Eventos** | EVT-080 |
| **Aplicações** | api-edge, packages/sync |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** time de desenvolvimento,
> **quero** um worker que envie os eventos pendentes à nuvem de forma resiliente,
> **para** que a sincronização se recupere sozinha de qualquer falha de rede.

## 2. Contexto e motivação

O worker lê o outbox por sequência, monta lotes comprimidos e envia. O que o torna resiliente são três coisas: **cursor persistido** (retoma de onde parou), **backoff exponencial** (não martela a nuvem indisponível) e **confirmação antes de marcar** (o lote só é dado como sincronizado depois que a nuvem confirma a persistência).

Os parâmetros operacionais estão fixados no documento 02, seção 6.5: push a cada 2 s, lote de 500 eventos ou 1 MB, backoff de 2 s dobrando até o teto de 5 minutos.

## 3. Escopo

### 3.1 Dentro desta história

- Worker `BackgroundService` (.NET) com intervalo de 2 s, disparado imediatamente quando há evento novo
- Leitura do outbox em ordem de `device_seq`
- Lote de até 500 eventos ou 1 MB, comprimido em gzip
- Assinatura HMAC por requisição, com o par de chaves da instalação
- Backoff exponencial com teto de 5 minutos
- Cursor persistido, com retomada automática
- Marcação de sincronizado apenas após confirmação da nuvem

### 3.2 Fora desta história

- Recepção na nuvem (US-062)
- Pull de configuração (US-063)
- Recuperação após reconexão longa (US-068)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Worker de envio

  Cenário: Envio imediato de evento novo
    Dado o outbox vazio e a conexão estável
    Quando um evento novo for gravado
    Então o envio deve ocorrer em no máximo 2 segundos

  Cenário: Lote respeitando os limites
    Dado 1.200 eventos pendentes
    Quando o worker montar os lotes
    Então cada lote deve ter no máximo 500 eventos ou 1 MB
    E os lotes devem seguir a ordem de device_seq

  Cenário: Nuvem indisponível
    Dado a nuvem retornando 503
    Quando o worker tentar enviar
    Então deve aplicar backoff exponencial de 2 s, 4 s, 8 s, até o teto de 5 min
    E os eventos devem permanecer pendentes no outbox

  Cenário: Confirmação antes de marcar
    Dado um lote enviado
    Quando a resposta confirmar acceptedUntilSeq
    Então apenas os eventos até essa sequência devem ser marcados como sincronizados

  Cenário: Falha parcial do lote
    Dado um lote com um evento rejeitado por schema inválido
    Quando a resposta chegar
    Então os aceitos devem ser marcados
    E o rejeitado deve ser registrado para revisão, sem travar a fila

  Cenário: Assinatura HMAC
    Dado uma requisição de sync
    Quando for enviada
    Então deve conter X-Signature calculada com a chave da instalação
    E a nuvem deve recusar requisição com assinatura inválida

  Cenário: Retomada após reinício do worker
    Dado o worker reiniciado no meio de um envio
    Quando voltar a executar
    Então deve retomar do cursor persistido
    E nenhum evento deve ser pulado ou reenviado desnecessariamente
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-005 | A operação local não depende de internet | O worker falha silenciosamente sem afetar a operação |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-080 | `sync.batch.sent` | Lote enviado | fromSeq, toSeq, count, bytes | — |

> `sync.batch.sent` é evento local de observabilidade; não trafega para a nuvem.

## 7. Contrato de API

```http
POST /v1/sync/push
X-Installation-Id: <uuid>
X-Signature: <hmac-sha256>
Content-Encoding: gzip
{
  "installationId": "...",
  "fromSeq": 148100,
  "toSeq": 148600,
  "events": [ { "id", "type", "version", "aggregateType", "aggregateId",
                "payload", "actorId", "deviceId", "deviceSeq",
                "occurredAt" } ]
}
→ 200 {
    "acceptedUntilSeq": 148600,
    "duplicates": 3,
    "rejected": [ { "eventId": "...", "reason": "SCHEMA_INVALID" } ],
    "conflicts": [ { "eventId": "...", "resolution": "KEPT_REMOTE" } ]
  }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `outbox` | Origem dos eventos | `device_seq`, `synced_at` |
| `sync_cursor` | Cursor persistido | `direction=PUSH`, `cursor`, `updated_at` |
| `edge_installation` | Chave e estado | `private_key`, `last_sync_at`, `pending_events` |

## 9. Comportamento offline

O worker é justamente o componente que lida com a ausência de conexão. Com a nuvem inacessível, ele aplica backoff e aguarda — sem consumir recurso de forma agressiva e sem afetar em nada a operação da loja.

O teto de 5 minutos evita que uma queda longa gere milhares de tentativas inúteis, mas garante que a retomada aconteça rápido quando a conexão voltar.

## 10. Interface e experiência

- Sem interface própria; alimenta o indicador da US-065
- Log estruturado detalhado o suficiente para diagnóstico remoto de uma instalação problemática

## 11. Métricas, alertas e observabilidade

- Latência de sincronização (evento gravado → confirmado na nuvem)
- Taxa de sucesso de lote e distribuição de tentativas até o sucesso
- Volume sincronizado por hora, em eventos e em bytes
- Tempo em backoff por instalação — insumo de diagnóstico de qualidade de internet do cliente

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Montagem de lote respeitando limites de contagem e de bytes |
| Unitário | Progressão do backoff exponencial com teto |
| Integração | Cursor persistido permite retomada exata após reinício |
| Integração | Lote só é marcado após confirmação; falha parcial não trava a fila |
| Integração | Requisição sem HMAC válida é recusada |
| Caos | Nuvem indisponível por 2 horas, com retomada automática ao voltar |

## 13. Dependências

**Depende de:** US-060, US-006  
**Habilita:** US-062, US-065, US-068

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

- Evento rejeitado por schema inválido pode travar a fila se o worker parar no primeiro erro. O comportamento correto é registrar e seguir, alertando a plataforma.
- Chave privada da instalação precisa de proteção adequada em repouso (ADR-031).

---

*US-061 · Épico E-06 · Pacote 004_DonaBetinha · Replay Studio.*