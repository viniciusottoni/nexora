# US-060 · Outbox transacional

|  |  |
|---|---|
| **Épico** | [E-06 · Sincronizacao Local-Nuvem](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 6 |
| **Requisitos funcionais** | RF-OFF-02 |
| **Regras de negócio** | RN-005 |
| **ADRs** | ADR-007, ADR-006 |
| **Eventos** | — |
| **Aplicações** | api-edge, packages/sync |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** time de desenvolvimento,
> **quero** que todo evento seja gravado na mesma transação do estado que o originou,
> **para** que nenhum fato operacional se perca, em nenhuma circunstância.

## 2. Contexto e motivação

É o padrão *transactional outbox* do ADR-007, e a razão de existir é específica: se o evento fosse publicado **depois** do commit da transação de negócio, uma queda de processo entre os dois momentos perderia o evento silenciosamente — e com ele a métrica, o alerta e a sincronização (doc. 04, seção 1).

Gravar estado e evento juntos elimina essa janela. O worker de envio (US-061) lê o outbox depois, de forma assíncrona, e o pior caso passa a ser atraso, nunca perda.

## 3. Escopo

### 3.1 Dentro desta história

- Tabela `outbox` com o evento completo e `device_seq` monotônico
- Gravação obrigatória na mesma transação do estado
- Sequência monotônica por instalação, independente de relógio
- Marcação de sincronizado, sem apagar imediatamente
- Retenção de 30 dias para eventos já sincronizados
- Abstração no `packages/sync` para uso uniforme por todos os módulos
- Teste que falha se alguma transição de estado não emitir seu evento

### 3.2 Fora desta história

- Envio à nuvem (US-061)
- Recepção na nuvem (US-062)
- Pull de configuração (US-063)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Outbox transacional

  Cenário: Evento nunca se perde
    Dado que um pedido foi criado
    Quando a transação for confirmada
    Então o estado e o evento no outbox devem ter sido gravados juntos
    E, se o processo cair logo após, o evento deve continuar pendente

  Cenário: Rollback também descarta o evento
    Dado uma transação de criação de pedido que falha na validação final
    Quando a transação for revertida
    Então nem o estado nem o evento devem existir

  Cenário: Sequência monotônica
    Dado três eventos criados em sequência na mesma instalação
    Quando forem gravados no outbox
    Então os device_seq devem ser estritamente crescentes
    E não devem depender do relógio do sistema

  Cenário: Retenção após sincronização
    Dado um evento sincronizado há 10 dias
    Quando a rotina de limpeza executar
    Então o evento deve permanecer no outbox
    E deve ser removido apenas após 30 dias

  Cenário: Toda transição emite evento
    Dado qualquer transição de estado do sistema
    Quando o teste de cobertura de eventos executar
    Então nenhuma transição pode existir sem evento correspondente no catálogo

  Cenário: Payload contém o delta
    Dado um evento de mudança de estado de item
    Quando o payload for inspecionado
    Então deve conter apenas o que mudou, não o objeto inteiro
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-005 | A operação local não depende de internet | O outbox é o que torna o desacoplamento possível |
| RN-020 | Métrica usa `ocorrido_em` | `occurred_at` gravado no evento no momento do fato |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> Esta história não emite eventos de negócio — ela é o mecanismo pelo qual **todos** os eventos são emitidos. Regra R6 do documento 04: evento é emitido na mesma transação do estado.

## 7. Contrato de API

```http
# Sem endpoint. Abstração interna de packages/sync:

await prisma.$transaction(async (tx) => {
  const order = await tx.order.create({ data: {...} });
  await outbox.emit(tx, {
    type: 'order.placed',
    version: 1,
    aggregateType: 'Order',
    aggregateId: order.id,
    actorId, deviceId,
    occurredAt,                    // horário do FATO
    payload: { items, total, promisedAt }
  });
  return order;
});
```

> A API do outbox recebe a transação como parâmetro — é assim que a gravação conjunta fica garantida por construção, não por disciplina.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `outbox` | Fila de eventos pendentes | `event_id` (UUID v7), `type`, `version`, `aggregate_type`, `aggregate_id`, `payload` (JSONB), `actor_id`, `device_id`, `device_seq`, `occurred_at`, `synced_at` |
| `domain_event` | Log append-only local, particionado por `occurred_at` | Mesmos campos, mais `recorded_at` |
| Sequência `device_seq` | Monotônica por instalação | Sequence do PostgreSQL, nunca reiniciada |

> `event_id` é UUID v7 gerado na origem — é o que torna o reenvio idempotente (regra R1 do doc. 04). O evento é append-only: nunca alterado, nunca apagado; correção se faz com evento compensatório (R3).

## 9. Comportamento offline

O outbox **é** o mecanismo offline. Com a internet caída, os eventos simplesmente se acumulam — a operação não percebe diferença alguma.

A retenção de 30 dias após a sincronização serve a dois propósitos: permitir reprocessamento em caso de problema na nuvem e servir de evidência em auditoria de divergência.

## 10. Interface e experiência

- Sem interface — infraestrutura
- Efeito visível apenas no contador de eventos pendentes do indicador de sincronização (US-065)

## 11. Métricas, alertas e observabilidade

- Contagem de eventos pendentes no outbox — número central do diagnóstico de sincronização
- Idade do evento pendente mais antigo
- Taxa de emissão de eventos por tipo e por hora
- Alerta interno se algum evento permanecer pendente acima do limiar

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Emissão dentro de transação; rollback descarta estado e evento juntos |
| Integração | Queda de processo após o commit mantém o evento pendente |
| Integração | `device_seq` estritamente crescente sob concorrência |
| Regressão | Teste de cobertura: toda transição de estado emite seu evento do catálogo |
| Carga | 10.000 eventos gravados sem degradação da transação de negócio |

## 13. Dependências

**Depende de:** US-032  
**Habilita:** US-061, US-062, US-068

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
- [ ] Teste de cobertura de eventos rodando no CI e bloqueando merge

## 15. Riscos, premissas e pendências

- Se o outbox crescer sem limpeza, degrada o desempenho do edge. A retenção de 30 dias e o particionamento são obrigatórios (ADR-035).
- Emitir evento fora da transação, ainda que por engano em um único módulo, quebra a garantia. A abstração que exige a transação como parâmetro é a proteção estrutural.

---

*US-060 · Épico E-06 · Pacote 004_DonaBetinha · Replay Studio.*