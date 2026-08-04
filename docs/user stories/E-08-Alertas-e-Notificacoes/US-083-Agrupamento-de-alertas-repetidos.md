# US-083 · Agrupamento de alertas repetidos

|  |  |
|---|---|
| **Épico** | [E-08 · Alertas e Notificacoes](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | S — Should have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-ALT-04 |
| **Regras de negócio** | — |
| **ADRs** | — |
| **Eventos** | — |
| **Aplicações** | api-edge, api-cloud |
| **Autoridade do dado** | Local e nuvem |

---

## 1. História

> **Como** todos os perfis,
> **quero** que alertas parecidos venham agrupados,
> **para** que uma rajada de problemas não vire uma rajada de notificações.

## 2. Contexto e motivação

No pico, cinco pedidos atrasam ao mesmo tempo. Cinco notificações separadas em dez segundos fazem o usuário silenciar o sistema — e perder também as próximas.

O agrupamento resolve: uma notificação dizendo "5 pedidos atrasados", com o detalhamento disponível ao toque.

## 3. Escopo

### 3.1 Dentro desta história

- Janela de agrupamento configurável por tipo de alerta
- Mensagem consolidada com contagem
- Detalhamento acessível ao abrir
- Novo alerta do mesmo tipo dentro da janela atualiza o grupo, sem notificar de novo
- Severidade do grupo igual à maior individual

### 3.2 Fora desta história

- Silenciamento por usuário (RF-ALT-05, Fase 2)
- Medição de alertas ignorados (RF-ALT-06, Fase 3)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Agrupamento de alertas

  Cenário: Rajada agrupada
    Dado cinco pedidos atrasando em 30 segundos
    Quando os alertas forem entregues
    Então deve haver uma notificação informando "5 pedidos atrasados"
    E o detalhamento deve estar acessível ao toque

  Cenário: Atualização do grupo
    Dado um grupo ativo com 5 pedidos atrasados
    Quando um sexto atrasar dentro da janela
    Então o grupo deve passar a 6
    E não deve haver nova notificação sonora

  Cenário: Severidade do grupo
    Dado um grupo com alertas de severidade média e alta
    Quando o grupo for exibido
    Então a severidade do grupo deve ser alta

  Cenário: Tipos distintos não agrupam
    Dado um alerta de pedido atrasado e um de produto indisponível
    Quando forem entregues
    Então devem permanecer separados

  Cenário: Fim da janela
    Dado um grupo cuja janela expirou
    Quando um novo alerta do mesmo tipo ocorrer
    Então deve iniciar um grupo novo, com nova notificação
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/alerts?grouped=true
→ { "groups": [ { "type": "ORDER_LATE", "count": 5, "severity": "HIGH",
                  "message": "5 pedidos atrasados",
                  "firstRaisedAt": "...", "lastRaisedAt": "...",
                  "alerts": [ {...}, {...} ] } ] }

PATCH /v1/tenant/alert-routing
{ "ORDER_LATE": { "groupWindowSeconds": 60 } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `alert` | Vínculo ao grupo | `group_key`, `group_window_start` |
| `tenant_config` | Janela por tipo | `alertRouting.<type>.groupWindowSeconds` |

## 9. Comportamento offline

Agrupamento local para alertas operacionais.

## 10. Interface e experiência

- Mensagem do grupo direta: "5 pedidos atrasados", não "múltiplos alertas"
- Um toque abre a lista completa do grupo
- Som toca apenas na criação do grupo, nunca nas atualizações

## 11. Métricas, alertas e observabilidade

- Taxa de agrupamento por tipo — alta indica limiar mal calibrado, não só rajada
- Tamanho médio dos grupos

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Janela de agrupamento e cálculo de severidade do grupo |
| Integração | Rajada gera uma notificação, não N |
| Integração | Tipos distintos não agrupam |

## 13. Dependências

**Depende de:** US-081  
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

- Agrupamento pode esconder um problema individual grave dentro de um grupo. Alertas de severidade crítica devem ser exceção ao agrupamento.

---

*US-083 · Épico E-08 · Pacote 004_DonaBetinha · Replay Studio.*