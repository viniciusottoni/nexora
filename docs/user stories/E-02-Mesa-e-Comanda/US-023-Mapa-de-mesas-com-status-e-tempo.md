# US-023 · Mapa de mesas com status e tempo

|  |  |
|---|---|
| **Épico** | [E-02 · Mesa e Comanda](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 3 |
| **Requisitos funcionais** | RF-SAL-05 |
| **Regras de negócio** | RN-003 |
| **ADRs** | ADR-011, ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** garçom (P2) e caixa (P4),
> **quero** ver todas as mesas em uma tela, com status, tempo e valor,
> **para** que eu saiba onde preciso agir sem percorrer o salão.

## 2. Contexto e motivação

A dor da persona P2 é andar. O mapa de mesas existe para transformar percurso em informação: quem está esperando, quem já pediu a conta, quem está há muito tempo sem consumir.

O destaque por tempo acima da média é o que transforma a tela de passiva em ativa — o garçom não precisa comparar números, o sistema aponta.

## 3. Escopo

### 3.1 Dentro desta história

- Grade de mesas agrupadas por ambiente
- Status visual: livre, ocupada, conta solicitada, em limpeza
- Tempo decorrido e valor consumido por mesa
- Destaque de mesas acima do tempo médio de permanência
- Indicadores de ação pendente: garçom chamado, item pronto para levar
- Atualização em tempo real por WebSocket, com fallback de polling
- Filtro por ambiente e por mesas do próprio garçom

### 3.2 Fora desta história

- Planta baixa com posicionamento espacial real
- Unir e separar mesas (Fase 2)
- Painel de tempo real do gestor (US-070)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Mapa de mesas

  Cenário: Visão do salão
    Dado mesas em estados diferentes
    Quando o garçom abrir o mapa
    Então cada mesa deve exibir status, tempo aberto e valor consumido
    E mesas acima do tempo médio devem ser destacadas

  Cenário: Ação pendente destacada
    Dado que a mesa 7 chamou o garçom
    E a mesa 12 tem um item pronto para ser levado
    Quando o garçom abrir o mapa
    Então as duas mesas devem exibir o indicador de ação correspondente
    E devem aparecer no topo da ordenação por urgência

  Cenário: Atualização em tempo real
    Dado o mapa aberto no celular do garçom
    Quando um pedido for confirmado em outra mesa
    Então o valor daquela mesa deve atualizar em até 2 segundos, sem recarregar

  Cenário: Fallback de polling
    Dado que a conexão WebSocket do dispositivo caiu
    Quando houver mudança em qualquer mesa
    Então o mapa deve refletir em no máximo 5 segundos
    E deve indicar visualmente o modo degradado

  Cenário: Filtro por responsabilidade
    Dado um garçom responsável por 6 das 20 mesas
    Quando ativar o filtro "minhas mesas"
    Então deve ver apenas as 6

  Cenário: Operação offline
    Dado que a loja está sem internet
    Quando o garçom abrir o mapa
    Então todas as informações devem estar corretas e atualizadas
    E deve haver indicação discreta do estado offline
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-003 | Cada transição de estado gera alerta aos perfis envolvidos | Ação pendente aparece como indicador na mesa |
| RN-005 | A operação local não depende de internet | Mapa servido integralmente pelo edge |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> Esta história consome eventos; não emite. Reage a `order.placed`, `order.item.ready`, `table.waiter_called`, `table.bill_requested` e `table.released`.

## 7. Contrato de API

```http
GET /v1/tables
→ [ { "id": "...", "label": "12", "area": "Salão", "status": "OCCUPIED",
      "session": { "openedAt": "...", "minutesOpen": 47, "total": 18700,
                   "guestCount": 4, "waiter": { "id": "...", "name": "Ana" } },
      "flags": { "waiterCalled": false, "billRequested": false,
                 "itemsReadyToServe": 2, "aboveAvgDuration": true } } ]

# WebSocket:
{ "type": "table.waiter_called", "data": { "tableId": "...", "label": "12" } }
{ "type": "order.item.ready",   "data": { "table": "12", "productName": "..." } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `dining_table` | Estado e agrupamento | `status`, `area_id`, `label` |
| `table_session` | Sessão vigente e valor | `opened_at`, `total`, `guest_count`, `waiter_id` |
| `order_item` | Itens prontos aguardando expedição | `status=READY`, `ready_at` |
| `metric_daily` | Tempo médio de permanência para o destaque | `avg_session_seconds` |

## 9. Comportamento offline

Integralmente local. O mapa lê do PostgreSQL do edge e reage ao WebSocket local — nenhuma dependência de nuvem.

O fallback de polling a cada 5 segundos é requisito (ADR-011): o salão não pode depender de uma única via de comunicação, exatamente como a cozinha.

## 10. Interface e experiência

- Tela inicial do garçom, não um item de menu
- Cartões grandes, legíveis com o celular na mão e em movimento
- Cor de status consistente com o KDS e o caixa — o mesmo vermelho significa a mesma coisa em todo o produto
- Ordenação por urgência como padrão, com opção de ordenar por número de mesa
- Indicador de conexão discreto, sem alarmar o cliente que estiver olhando a tela

## 11. Métricas, alertas e observabilidade

- Ocupação do salão por faixa horária (mesas ocupadas ÷ total)
- Tempo médio de permanência, por ambiente
- Tempo entre item pronto e item entregue — mede a eficiência do salão (MET-005)
- Frequência de abertura do mapa por garçom, indicador de adoção

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Atualização em tempo real em menos de 2 s por WebSocket |
| Integração | Fallback de polling entrega em no máximo 5 s |
| Integração | Cálculo de valor consumido bate com a soma dos itens da sessão |
| Desempenho | Mapa com 60 mesas renderiza em menos de 1 s em celular de entrada |
| Caos offline | Mapa correto e reativo com a internet da loja derrubada |

## 13. Dependências

**Depende de:** US-022  
**Habilita:** US-025, US-026, US-050

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

- Excesso de indicadores no cartão da mesa torna a tela ilegível em movimento. Limitar a três sinais simultâneos e validar no piloto com garçons reais.

---

*US-023 · Épico E-02 · Pacote 004_DonaBetinha · Replay Studio.*