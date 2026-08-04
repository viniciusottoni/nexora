# US-076 · Drill-down do numero ate o pedido

|  |  |
|---|---|
| **Épico** | [E-07 · Painel do Dono v1](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-BI-11 |
| **Regras de negócio** | RN-009 |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** tocar em qualquer número e chegar ao pedido que o originou,
> **para** que eu possa investigar o que aconteceu em vez de só ver que aconteceu.

## 2. Contexto e motivação

Diretriz de desenho do painel (Visão Geral, 7.8): *do resumo ao detalhe — todo número permite abrir e chegar ao pedido individual*. E a regra R9: *todo indicador do painel deve permitir navegação até o evento de origem*.

O limite é explícito: no máximo **três toques** entre o número agregado e o pedido individual com todos os seus carimbos.

É o que separa um painel de vitrine de um instrumento de gestão. Um gráfico que mostra que o tempo médio das 20h subiu não resolve nada; ver **quais** pedidos das 20h atrasaram e por qual etapa, resolve.

## 3. Escopo

### 3.1 Dentro desta história

- Drill-down a partir de qualquer indicador agregado
- Lista de pedidos do recorte selecionado
- Linha do tempo completa do pedido, com os seis carimbos, autor e dispositivo
- Caminho de no máximo três toques
- Navegação de volta preservando o contexto do filtro
- Exportação do recorte

### 3.2 Fora desta história

- Edição de pedido histórico (não existe por desenho)
- Exportação completa em planilha e PDF (RF-BI-13, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Drill-down até o pedido

  Cenário: Do gráfico ao pedido
    Dado o gráfico de tempo médio por hora
    Quando o gestor tocar na barra das 20h
    Então deve ver a lista de pedidos daquela hora
    E ao tocar em um pedido deve ver todos os seus carimbos de tempo
    E o caminho deve ter no máximo 3 toques

  Cenário: Linha do tempo completa
    Dado um pedido aberto no detalhamento
    Quando a linha do tempo for exibida
    Então deve mostrar cada etapa com horário, autor e dispositivo
    E deve mostrar a duração de cada intervalo

  Cenário: Drill-down do faturamento
    Dado o faturamento de um dia
    Quando o gestor tocar no número
    Então deve ver os pedidos que o compõem, ordenáveis por valor

  Cenário: Drill-down de um produto
    Dado a venda de um produto no período
    Quando o gestor tocar na linha do produto
    Então deve ver os pedidos que contêm aquele produto

  Cenário: Contexto preservado ao voltar
    Dado o gestor navegando do gráfico até um pedido
    Quando voltar
    Então o filtro e a posição do gráfico devem estar preservados

  Cenário: Recorte vazio
    Dado um recorte sem pedidos
    Quando o drill-down for acionado
    Então deve indicar ausência de dados, sem erro
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-009 | Todo indicador do painel deve permitir navegação até o evento de origem | Regra derivada da diretriz de métrica total (Visão Geral, 10.2) |
| RN-015 | Isolamento entre estabelecimentos | O drill-down respeita RLS como qualquer consulta |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/metrics/{code}/drill-down?bucket=2026-07-31T20:00:00Z&groupBy=hour
→ { "orders": [ { "id": "...", "code": "A47", "placedAt": "...",
                  "totalSeconds": 848, "table": "12",
                  "items": [ { "name": "...", "prepSeconds": 546 } ] } ],
    "context": { "metric": "MET-006", "bucket": "...", "count": 24 } }

GET /v1/orders/{id}/timeline
→ { "timestamps": { "placedAt": { "at": "...", "actor": {...}, "device": {...} },
                    "firedAt":  { ... }, "readyAt": { ... }, "servedAt": { ... } },
    "durations": { "queueSeconds": 214, "cookSeconds": 420, "totalSeconds": 848 } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order` / `order_item` | Pedidos e carimbos | Os seis carimbos, autor e dispositivo por transição |
| `domain_event` | Origem completa, quando o detalhe exigir | `type`, `occurred_at`, `payload` |
| `metric_hourly` | Ponto de partida do drill-down | `bucket_hour` |

> Estratégia (doc. 04, 6.1): o painel lê agregado; o detalhamento consulta o evento apenas quando o usuário abre o número.

## 9. Comportamento offline

Consulta de nuvem. Depende dos dados sincronizados; a defasagem é sinalizada como em toda visão.

## 10. Interface e experiência

- **Três toques é limite medido, não aspiração** — testar o caminho de cada indicador
- Todo número clicável, sem exceção; número que não abre quebra a expectativa criada pelos que abrem
- Linha do tempo do pedido em visualização horizontal, com as durações entre as etapas
- Voltar preserva filtro, posição e rolagem
- Autor e dispositivo visíveis em cada etapa — responde "quem fez" sem ir à auditoria

## 11. Métricas, alertas e observabilidade

- Frequência de uso do drill-down — mede se o painel virou instrumento de investigação
- Indicadores mais investigados, revelando as dúvidas reais do gestor
- Profundidade média da navegação

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Drill-down disponível a partir de todos os indicadores da v1 |
| Integração | Linha do tempo com autor e dispositivo em cada etapa |
| Usabilidade | Caminho de no máximo três toques verificado indicador a indicador |
| Desempenho | Drill-down de um recorte com 200 pedidos em menos de 3 s |

## 13. Dependências

**Depende de:** US-032, US-071  
**Habilita:** US-091

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
- [ ] Caminho de três toques verificado em todos os indicadores da v1

## 15. Riscos, premissas e pendências

- Um único indicador sem drill-down quebra a confiança no conjunto. A cobertura precisa ser total, não parcial.

---

*US-076 · Épico E-07 · Pacote 004_DonaBetinha · Replay Studio.*