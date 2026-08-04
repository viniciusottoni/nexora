# US-080 · Motor de alertas com limiares configuraveis

|  |  |
|---|---|
| **Épico** | [E-08 · Alertas e Notificacoes](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-ALT-01, RF-ALT-02 |
| **Regras de negócio** | RN-003 |
| **ADRs** | ADR-032 |
| **Eventos** | — |
| **Aplicações** | api-edge, api-cloud, packages/domain |
| **Autoridade do dado** | Local (avaliação) · Nuvem (configuração dos limiares) |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** que o sistema me avise quando algo sai do padrão que eu defini,
> **para** que eu não precise procurar problema — o sistema aponta.

## 2. Contexto e motivação

Painel 4 da Visão Geral (7.6): *o dono não precisa procurar problema: o sistema aponta*.

O motor avalia condições sobre eventos e estado, dispara alertas e os resolve automaticamente quando a condição deixa de valer. Os limiares são **configuráveis por estabelecimento** (RF-ALT-02), porque o que é atraso numa pizzaria não é atraso num restaurante de fogão lento.

Alertas do MVP: pedido atrasado, tempo médio acima da meta, produto indisponível, divergência de caixa, falha de sincronização, cancelamento ou desconto acima do padrão.

## 3. Escopo

### 3.1 Dentro desta história

- Motor de avaliação de condições sobre eventos e estado
- Catálogo de tipos de alerta do MVP
- Limiares configuráveis por tenant, com valores padrão por modelo de negócio
- Severidade e escalonamento por duração
- Resolução automática quando a condição cessa
- Registro de alertas para consulta e métrica
- Deduplicação: uma condição ativa gera um alerta, não N

### 3.2 Fora desta história

- Entrega ao usuário (US-081)
- Direcionamento por perfil (US-082)
- Silenciamento por usuário (RF-ALT-05, Fase 2)
- Alertas de estoque e financeiro (Fases 2 e 3)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Motor de alertas

  Cenário: Pedido atrasado
    Dado o limiar de atraso configurado em 18 minutos
    Quando um pedido ultrapassar 18 minutos sem ser entregue
    Então um alerta ORDER_LATE deve ser criado com severidade alta

  Cenário: Resolução automática
    Dado um alerta de pedido atrasado ativo
    Quando o pedido for entregue
    Então o alerta deve ser resolvido automaticamente
    E a duração até a resolução deve ficar registrada

  Cenário: Deduplicação
    Dado um alerta ativo para um pedido atrasado
    Quando a condição continuar verdadeira nas avaliações seguintes
    Então nenhum alerta novo deve ser criado para o mesmo pedido

  Cenário: Escalonamento por duração
    Dado um alerta de severidade média ativo há mais que o limiar de escalonamento
    Quando o limiar for ultrapassado
    Então a severidade deve subir
    E o alerta deve ser redirecionado conforme a nova severidade

  Cenário: Limiar configurável por tenant
    Dado dois estabelecimentos com limiares diferentes
    Quando a mesma situação ocorrer nos dois
    Então cada um deve disparar conforme o próprio limiar

  Cenário: Limiar padrão do modelo de negócio
    Dado um tenant recém-criado com modelo PIZZERIA
    Quando o motor avaliar
    Então devem valer os limiares padrão do modelo

  Cenário: Avaliação offline
    Dado que a loja está sem internet
    Quando uma condição de alerta for atingida
    Então o alerta deve ser criado e entregue localmente
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-003 | Cada transição de estado gera alerta aos perfis envolvidos | É o objeto deste épico |
| RN-016 | Configuração, não código | Limiares são dados por tenant, nunca constantes no código |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

> O motor consome praticamente todo o catálogo de eventos e produz registros em `alert`, que são entregues pela US-081.

## 7. Contrato de API

```http
GET /v1/alerts?status=open
→ { "alerts": [ { "id": "...", "type": "ORDER_LATE", "severity": "HIGH",
                  "entityType": "order", "entityId": "...",
                  "message": "Pedido A47 da mesa 12 está há 21 minutos na fila.",
                  "raisedAt": "...", "targetRoles": ["WAITER","MANAGER"] } ] }

POST /v1/alerts/{id}/acknowledge
POST /v1/alerts/{id}/resolve

GET   /v1/tenant/thresholds
PATCH /v1/tenant/thresholds
{ "orderWarnMinutes": 12, "orderCriticalMinutes": 18,
  "avgTimeAboveTargetPercent": 20,
  "cashDivergenceAmount": 500,
  "syncDelayWarnMinutes": 5 }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `alert` | Alerta registrado | `type`, `severity`, `entity_type`, `entity_id`, `raised_at`, `acknowledged_at`, `resolved_at`, `target_roles` |
| `tenant_config` | Limiares | `thresholds` (JSONB) |

## 9. Comportamento offline

O motor roda **no edge** para os alertas operacionais (pedido atrasado, produto indisponível, tempo médio acima da meta) — se dependesse da nuvem, a cozinha ficaria sem alerta justamente quando a internet cai.

Alertas de gestão que dependem de consolidação (divergência de padrão, atraso de sincronização visto de fora) rodam na nuvem.

A configuração de limiares desce pelo pull (US-063).

## 10. Interface e experiência

- Sem interface própria — o motor alimenta a entrega da US-081
- Tela de configuração de limiares com explicação em linguagem de negócio de cada um
- Valores padrão sensatos, para que o estabelecimento funcione sem configurar nada

## 11. Métricas, alertas e observabilidade

- Contagem de alertas por tipo, severidade e período
- Tempo médio até reconhecimento e até resolução, por tipo
- Alertas resolvidos automaticamente versus por ação humana
- Taxa de alertas ignorados por tipo (base do RF-ALT-06, Fase 3)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Avaliação de cada tipo de condição com limiares variados |
| Unitário | Deduplicação e escalonamento por duração |
| Integração | Resolução automática quando a condição cessa |
| Integração | Limiares distintos por tenant produzem resultados distintos |
| Caos offline | Alertas operacionais funcionando com internet caída |

## 13. Dependências

**Depende de:** US-031, US-032  
**Habilita:** US-081, US-082, US-066, US-108

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

- Limiares mal calibrados geram excesso de alerta e a equipe desliga a atenção. Calibrar com dados reais nas duas primeiras semanas do piloto.
- Motor rodando em dois lugares (edge e nuvem) exige cuidado para não duplicar alerta da mesma condição.

---

*US-080 · Épico E-08 · Pacote 004_DonaBetinha · Replay Studio.*