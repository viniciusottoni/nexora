# US-070 · Pulso em tempo real no celular

|  |  |
|---|---|
| **Épico** | [E-07 · Painel do Dono v1](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-BI-01, RF-BI-14 |
| **Regras de negócio** | RN-020 |
| **ADRs** | ADR-012, ADR-009 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** abrir o celular e ver, em cinco números, como está a operação agora,
> **para** que eu acompanhe o negócio sem precisar estar dentro da loja.

## 2. Contexto e motivação

É a tela mais usada do painel e a que define a percepção de valor do produto para o dono. A diretriz de desenho é explícita: **acessível do celular**, porque o dono precisa acompanhar de fora da loja.

O rigor está na seleção. Cinco números que cabem numa tela de celular e respondem "preciso agir agora?". Colocar quinze indicadores transforma sala de controle em ruído.

E o requisito de honestidade: o painel indica explicitamente o atraso de sincronização. Dado defasado apresentado como tempo real é pior que ausência de dado.

## 3. Escopo

### 3.1 Dentro desta história

- Faturamento do dia com comparativo contra a média do mesmo dia da semana
- Pedidos atrasados no momento
- Tempo médio da última hora contra a meta
- Ocupação de mesas
- Alertas abertos
- Indicador de atraso de sincronização e horário de referência
- Carregamento em menos de 3 segundos
- Atualização automática enquanto a tela estiver aberta

### 3.2 Fora desta história

- Detalhamento por etapa (US-071)
- Drill-down (US-076)
- Indicadores de custo e margem (Fase 2)
- Configuração de quais indicadores exibir (RF-BI-10, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Pulso em tempo real

  Cenário: Cinco números essenciais
    Dado o gestor fora da loja no celular
    Quando abrir o painel
    Então deve ver faturamento do dia com comparativo, pedidos atrasados,
         tempo médio da última hora, ocupação de mesas e alertas abertos
    E a tela deve carregar em menos de 3 segundos
    E deve indicar o atraso de sincronização dos dados

  Cenário: Comparativo sempre presente
    Dado o faturamento do dia de R$ 4.820,00
    Quando for exibido
    Então deve haver comparação com a média das últimas quatro sextas-feiras
    E a variação percentual deve estar visível

  Cenário: Dado defasado sinalizado
    Dado a loja com 40 minutos de atraso de sincronização
    Quando o painel for exibido
    Então deve estar claro que os dados têm 40 minutos de defasagem
    E o horário de referência deve ser informado

  Cenário: Atualização automática
    Dado o painel aberto no celular
    Quando novos dados chegarem
    Então os números devem atualizar sem recarregar a página

  Cenário: Loja fechada
    Dado o estabelecimento fora do horário de operação
    Quando o gestor abrir o painel
    Então deve ver o resumo do último dia operacional
    E deve ficar claro que a operação está encerrada

  Cenário: Desempenho em 4G
    Dado uma conexão 4G comum
    Quando o painel for carregado
    Então deve renderizar em menos de 3 segundos
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-020 | Métrica de horário usa sempre `ocorrido_em` | Todos os números respeitam o horário de ocorrência |
| RN-015 | Isolamento entre estabelecimentos | O painel mostra exclusivamente o tenant do token |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> Consome agregados de `metric_hourly` e `metric_daily`, nunca eventos brutos (ADR-012).

## 7. Contrato de API

```http
GET /v1/dashboard/pulse
→ {
    "revenueToday": 482000, "revenueVsAvgPercent": 12.4,
    "ordersLate": 2,
    "avgMinutesLastHour": 11.3, "targetMinutes": 10,
    "tablesOccupied": 14, "tablesTotal": 20,
    "openAlerts": 3,
    "syncDelaySeconds": 4,
    "asOf": "2026-07-31T21:02:00Z"
  }
```

> `syncDelaySeconds` e `asOf` são obrigatórios — é o contrato de honestidade do dado (RF-BI-14).

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `metric_hourly` | Agregados da hora corrente | `bucket_hour`, `revenue`, `orders`, `avg_total_seconds` |
| `metric_daily` | Base do comparativo histórico | `business_day`, `revenue`, `orders` |
| `order` | Pedidos atrasados em tempo real | `promised_at`, `served_at`, `status` |
| `alert` | Alertas abertos | `resolved_at IS NULL` |
| `edge_installation` | Atraso de sincronização | `sync_lag_seconds` |

> Estratégia de BI (doc. 02, seção 11): eventos brutos nunca são consultados diretamente pelo painel. Um worker mantém as tabelas de agregação; o painel lê agregado.

## 9. Comportamento offline

O painel roda na nuvem e depende de internet **do lado do gestor**. Com a loja offline, os números são os da última sincronização — e é exatamente por isso que o `syncDelaySeconds` é obrigatório.

O comportamento correto quando a defasagem é grande: manter os números visíveis, mas com sinalização inequívoca. Esconder o dado é pior que mostrá-lo com a ressalva.

## 10. Interface e experiência

- Desenhado para celular primeiro — é onde o dono vai usar
- Cinco números, não quinze. Cada indicador adicional reduz o valor dos demais
- Comparativo sempre presente: número solto não gera decisão; número contra período anterior ou meta, sim
- Sinalização de defasagem visível sem ser alarmista
- Cada número é tocável, levando ao detalhamento (US-076)

## 11. Métricas, alertas e observabilidade

- Tempo de carregamento (p90) — meta abaixo de 3 s
- Frequência de acesso ao painel pelo gestor — indicador de adoção e de valor percebido
- Horários de acesso, revelando quando o dono realmente olha o negócio

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo do comparativo contra a média do mesmo dia da semana |
| Integração | Todos os números batem com a soma dos eventos de origem |
| Integração | `syncDelaySeconds` reflete o atraso real da instalação |
| Desempenho | Carregamento em menos de 3 s em 4G |
| Integração | Números corretos após operação offline prolongada |

## 13. Dependências

**Depende de:** US-064, US-071, US-073, US-075  
**Habilita:** US-076, US-077

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

- **Pendência 9 do índice** — quais indicadores são prioritários para o dono na v1 ainda não foi definido. Workshop de indicadores recomendado antes da Sprint 7.
- Painel bonito com dado errado destrói a confiança de forma irreversível. Validar cada número com o gestor no piloto, contra a realidade observada.

---

*US-070 · Épico E-07 · Pacote 004_DonaBetinha · Replay Studio.*