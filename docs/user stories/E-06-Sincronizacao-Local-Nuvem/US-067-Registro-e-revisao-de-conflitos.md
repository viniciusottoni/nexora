# US-067 · Registro e revisao de conflitos

|  |  |
|---|---|
| **Épico** | [E-06 · Sincronizacao Local-Nuvem](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 6 |
| **Requisitos funcionais** | RF-OFF-07 |
| **Regras de negócio** | RN-019 |
| **ADRs** | ADR-008 |
| **Eventos** | EVT-082 |
| **Aplicações** | api-cloud, web-admin |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** saber quando uma alteração minha foi descartada por conflito,
> **para** que eu não descubra a divergência semanas depois, no fechamento.

## 2. Contexto e motivação

A arquitetura elimina o conflito na origem sempre que possível: cada dado tem um dono único (doc. 02, seção 2.1), e o único conflito real — saldo de estoque — é resolvido **sincronizando movimentos em vez de saldos** (ADR-008). Não há conflito, apenas ordem de aplicação.

Para o que sobra (edição concorrente do mesmo registro), vale a RN-019: prevalece o menor `ocorrido_em`; empate resolve pela origem local; **o descarte fica registrado para revisão do gestor**.

Essa última parte é o objeto desta história. Descarte silencioso é o que faz o gestor perder a confiança no sistema.

## 3. Escopo

### 3.1 Dentro desta história

- Detecção de conflito na aplicação do evento
- Resolução automática pela RN-019
- Registro do conflito com o valor descartado e o mantido
- Tela de revisão de conflitos para o gestor
- Marcação de conflito como revisado
- Alerta quando o volume de conflitos ultrapassar o limiar

### 3.2 Fora desta história

- Resolução manual com reversão (fora do escopo do MVP)
- Conflito de estoque — eliminado por desenho pelo ADR-008

## 4. Critérios de aceite

```gherkin
Funcionalidade: Registro e revisão de conflitos

  Cenário: Resolução pela regra
    Dado dois eventos alterando o mesmo registro
    Quando forem aplicados
    Então deve prevalecer o de menor occurredAt
    E o descartado deve ficar registrado em sync_conflict

  Cenário: Empate resolvido pela origem
    Dado dois eventos com occurredAt idêntico
    Quando forem aplicados
    Então deve prevalecer o de origem local
    E o descarte deve ser registrado

  Cenário: Revisão pelo gestor
    Dado três conflitos registrados
    Quando o gestor abrir a tela de conflitos
    Então deve ver o que foi mantido e o que foi descartado, com horário e autor
    E deve poder marcar cada um como revisado

  Cenário: Estoque não gera conflito
    Dado uma baixa de produção offline e uma entrada de compra na nuvem
    Quando ambas forem aplicadas
    Então nenhum conflito deve ser registrado
    E o saldo deve ser a soma dos movimentos

  Cenário: Volume anômalo de conflitos
    Dado o limiar de conflitos por dia configurado
    Quando o volume ultrapassar o limiar
    Então o gestor e a plataforma devem ser alertados
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-019 | Em conflito de sincronização, prevalece o evento com menor `ocorrido_em`; empate resolve por origem local | **[HIPÓTESE]** — resolução automática, com registro obrigatório do descarte |
| RN-020 | Métrica usa `ocorrido_em` | É também o critério de desempate do conflito |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-082 | `sync.conflict.detected` | Conflito registrado | eventId, resolution, discardedValue, keptValue | ↑ |

## 7. Contrato de API

```http
GET /v1/sync/conflicts?reviewed=false
→ { "conflicts": [ { "id": "...", "entityType": "product_variant",
                     "entityId": "...", "field": "price",
                     "kept":      { "value": 5200, "occurredAt": "...", "origin": "CLOUD" },
                     "discarded": { "value": 5000, "occurredAt": "...", "origin": "EDGE" },
                     "resolution": "KEPT_REMOTE",
                     "detectedAt": "..." } ] }

POST /v1/sync/conflicts/{id}/review
{ "notes": "..." }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `sync_conflict` | Conflito registrado | `event_id`, `entity_type`, `entity_id`, `field`, `kept_value`, `discarded_value`, `resolution`, `reviewed_at`, `reviewed_by` |
| `domain_event` | Ambos os eventos preservados | Nada é apagado — apenas um não é aplicado ao estado |

> O evento descartado continua existindo no log append-only. O que foi descartado foi a **aplicação ao estado**, nunca o registro do fato.

## 9. Comportamento offline

Conflito só existe porque houve operação offline com edição concorrente. A arquitetura minimiza a superfície: autoridade única por domínio elimina a maioria dos casos, e movimentos em vez de saldos eliminam o caso do estoque (ADR-008).

O que resta é raro — mas precisa ser visível, porque descarte silencioso destrói a confiança no dado.

## 10. Interface e experiência

- Tela de conflitos acessível pelo painel, com indicação de quantos aguardam revisão
- Comparação lado a lado do valor mantido e do descartado, com horário e autor de cada
- Linguagem de negócio: "o preço que você alterou às 14h02 foi mantido; a alteração da loja às 14h05 foi descartada"
- Marcação de revisado, para que a lista não cresça indefinidamente

## 11. Métricas, alertas e observabilidade

- Conflitos por dia, por tipo de entidade e por instalação
- Percentual revisado pelo gestor
- Alerta em volume anômalo — indica problema de processo ou de desenho de autoridade

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Resolução pela RN-019, incluindo empate por origem |
| Integração | Conflito registrado com os dois valores e ambos os eventos preservados |
| Integração | Movimentos de estoque não geram conflito |
| Integração | Alerta em volume anômalo |

## 13. Dependências

**Depende de:** US-062  
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

## 15. Riscos, premissas e pendências

- **Risco T2 (doc. 02)** — divergência de dados após sincronização longa. A mitigação principal é o desenho (movimentos, não saldos); esta história é a rede de segurança visível.
- RN-019 é hipótese. Se o volume de conflitos for relevante no piloto, a regra de autoridade por domínio precisa ser revista.

---

*US-067 · Épico E-06 · Pacote 004_DonaBetinha · Replay Studio.*