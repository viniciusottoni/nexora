# US-077 · Resumo diario automatico

|  |  |
|---|---|
| **Épico** | [E-07 · Painel do Dono v1](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | S — Should have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-BI-12 |
| **Regras de negócio** | — |
| **ADRs** | ADR-018 |
| **Eventos** | — |
| **Aplicações** | api-cloud, web-admin |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** receber um resumo do dia sem precisar abrir o painel,
> **para** que eu acompanhe o negócio mesmo nos dias em que não paro para olhar.

## 2. Contexto e motivação

O painel só entrega valor se for aberto. O resumo diário inverte a lógica: em vez de esperar o gestor procurar a informação, o sistema entrega o essencial no fechamento do dia operacional.

O conteúdo precisa ser curto o suficiente para ser lido no celular em vinte segundos, e conter pelo menos uma informação acionável.

## 3. Escopo

### 3.1 Dentro desta história

- Resumo enviado no fechamento do dia operacional
- Faturamento com comparativo, pedidos, ticket médio e tempo médio
- Destaques positivos e pontos de atenção
- Alertas do dia que não foram resolvidos
- Entrega por push de navegador e in-app
- Horário de envio configurável

### 3.2 Fora desta história

- Envio por e-mail, WhatsApp ou SMS (Fase 6)
- Relatório completo exportável (RF-BI-13, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Resumo diário automático

  Cenário: Envio no fechamento do dia
    Dado o dia operacional encerrado
    Quando o horário de envio configurado for atingido
    Então o gestor deve receber o resumo
    E deve conter faturamento comparado, pedidos, ticket médio e tempo médio

  Cenário: Ponto de atenção destacado
    Dado um dia com tempo médio acima da meta
    Quando o resumo for gerado
    Então o desvio deve aparecer como ponto de atenção
    E deve indicar em qual faixa horária ocorreu

  Cenário: Alertas não resolvidos
    Dado três alertas do dia ainda abertos
    Quando o resumo for gerado
    Então devem constar na seção de pendências

  Cenário: Dia sem operação
    Dado um dia em que a loja não abriu
    Quando o horário de envio chegar
    Então nenhum resumo deve ser enviado

  Cenário: Dados incompletos por atraso de sincronização
    Dado uma loja com sincronização atrasada no momento do envio
    Quando o resumo for gerado
    Então deve indicar que os dados podem estar incompletos
    E deve informar o atraso
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-020 | Métrica usa `ocorrido_em` | O resumo cobre o dia operacional de ocorrência |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
# Job agendado no fechamento do dia operacional:
POST /v1/internal/daily-summary
{ "tenantId": "...", "businessDay": "2026-07-31" }

GET /v1/dashboard/daily-summary?date=2026-07-31
→ { "revenue": { "gross": 482000, "variancePercent": 12.4 },
    "orders": 184, "avgTicket": 2620,
    "avgTotalMinutes": 11.3, "targetMinutes": 10,
    "highlights": [ "Faturamento 12% acima da média das sextas" ],
    "attention": [ "Tempo médio acima da meta entre 20h e 21h" ],
    "openAlerts": 3,
    "dataComplete": true }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `metric_daily` | Base do resumo | `business_day`, todos os agregados do dia |
| `alert` | Alertas não resolvidos | `resolved_at IS NULL` |
| `tenant_config` | Horário de envio | `notifications.dailySummaryHour` |

## 9. Comportamento offline

Gerado na nuvem. Se a loja estiver com sincronização atrasada no momento do envio, o resumo indica que os dados podem estar incompletos.

## 10. Interface e experiência

- Curto o suficiente para ser lido em vinte segundos no celular
- Sempre com pelo menos um ponto acionável — resumo puramente descritivo é ignorado após uma semana
- Toque no resumo leva ao painel com o dia já filtrado
- Nunca enviado em dia sem operação

## 11. Métricas, alertas e observabilidade

- Taxa de abertura do resumo — mede se o formato está funcionando
- Percentual de aberturas que levam ao painel

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Envio no fechamento do dia operacional configurado |
| Integração | Nenhum envio em dia sem operação |
| Integração | Sinalização de dados incompletos com sincronização atrasada |

## 13. Dependências

**Depende de:** US-073, US-081  
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

- Resumo genérico vira notificação ignorada. A seção de pontos de atenção precisa ser calibrada para trazer informação real, não ruído.

---

*US-077 · Épico E-07 · Pacote 004_DonaBetinha · Replay Studio.*