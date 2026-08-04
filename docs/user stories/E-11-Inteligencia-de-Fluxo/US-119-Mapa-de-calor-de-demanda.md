# US-119 · Mapa de calor de demanda

|  |  |
|---|---|
| **Épico** | [E-11 · Inteligencia de Fluxo](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | S — Should have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-BI-08 |
| **Regras de negócio** | RN-020 |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** ver um mapa de quando meu movimento acontece, por dia e hora,
> **para** que eu dimensione equipe e compras pelo padrão real, não pela impressão.

## 2. Contexto e motivação

Responde a *"não sei quais etapas hoje são mais rápidas e mais lentas"* na dimensão temporal: onde está o pico, quanto tempo ele dura, quais dias são fracos.

É a informação que sustenta decisões de escala de pessoal, horário de funcionamento e planejamento de compra. E é completamente invisível hoje.

Depende diretamente da preservação de `occurred_at` (US-064): sem ela, um dia operado offline apareceria no mapa no horário errado.

## 3. Escopo

### 3.1 Dentro desta história

- Mapa de calor por dia da semana e faixa horária
- Métricas alternáveis: pedidos, faturamento, itens produzidos
- Recorte por canal
- Período configurável, com padrão de 8 semanas
- Identificação automática de picos e vales
- Comparação entre períodos

### 3.2 Fora desta história

- Previsão de demanda
- Sugestão automática de escala de pessoal

## 4. Critérios de aceite

```gherkin
Funcionalidade: Mapa de calor de demanda

  Cenário: Mapa por dia e hora
    Dado 8 semanas de operação
    Quando o mapa for gerado
    Então deve mostrar a intensidade por dia da semana e faixa horária
    E os picos devem ser visualmente evidentes

  Cenário: Métrica alternável
    Dado o mapa exibindo pedidos
    Quando o gestor alternar para faturamento
    Então a intensidade deve refletir o valor, não a contagem
    E o padrão pode ser diferente

  Cenário: Recorte por canal
    Dado vendas em salão e delivery
    Quando o filtro de canal for aplicado
    Então o mapa deve refletir apenas o canal escolhido

  Cenário: Horário correto após operação offline
    Dado um sábado operado integralmente offline
    Quando o mapa for gerado após a sincronização
    Então o movimento deve aparecer nos horários reais de ocorrência

  Cenário: Identificação de pico
    Dado um padrão claro de concentração
    Quando o mapa for exibido
    Então o pico deve ser identificado com dia, faixa e intensidade

  Cenário: Período insuficiente
    Dado menos de 4 semanas de operação
    Quando o mapa for gerado
    Então deve indicar que a base é curta para conclusões
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-020 | Métrica de horário usa sempre `ocorrido_em` | Sem isso o mapa fica errado após operação offline |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/metrics/heatmap?weeks=8&metric=orders&channel=DINE_IN
→ { "matrix": [ { "weekday": 5, "hour": 20, "value": 42,
                  "intensity": 0.95 } ],
    "peak": { "weekday": 6, "hour": 20, "value": 48 },
    "low":  { "weekday": 1, "hour": 15, "value": 2 },
    "weeksAnalyzed": 8, "sufficientData": true }

GET /v1/metrics/heatmap?metric=revenue
GET /v1/metrics/heatmap?metric=items
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `metric_hourly` | Base do mapa | `bucket_hour`, `business_day`, `orders`, `revenue`, `items` |
| `order` | Fonte, via `occurred_at` | `placed_at`, `channel` |

## 9. Comportamento offline

Consulta de nuvem, sobre agregados. A correção do mapa depende inteiramente da US-064.

## 10. Interface e experiência

- Mapa de calor clássico, dias na vertical e horas na horizontal
- Escala de cor acessível, com valores numéricos disponíveis ao passar ou tocar
- Pico e vale identificados textualmente, não só por cor
- Aviso claro quando a base de dados é curta demais para conclusão

## 11. Métricas, alertas e observabilidade

- Distribuição de demanda por dia e hora
- Concentração do faturamento nas faixas de pico
- Comparação entre períodos, revelando mudança de padrão

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo de intensidade normalizada |
| Integração | Mapa correto após período operado offline |
| Integração | Recorte por canal e por métrica |
| Desempenho | Mapa de 8 semanas gerado em menos de 3 s |

## 13. Dependências

**Depende de:** US-064, US-074  
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

- Mapa com base curta induz conclusão errada sobre padrão sazonal. O aviso de dados insuficientes é obrigatório, não opcional.

---

*US-119 · Épico E-11 · Pacote 004_DonaBetinha · Replay Studio.*