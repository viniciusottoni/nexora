# US-082 · Direcionamento por perfil e por acao

|  |  |
|---|---|
| **Épico** | [E-08 · Alertas e Notificacoes](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-ALT-01 |
| **Regras de negócio** | RN-003 |
| **ADRs** | ADR-023 |
| **Eventos** | — |
| **Aplicações** | api-edge, api-cloud |
| **Autoridade do dado** | Local e nuvem |

---

## 1. História

> **Como** todos os perfis,
> **quero** receber só os alertas que dependem de mim,
> **para** que eu não aprenda a ignorar as notificações do sistema.

## 2. Contexto e motivação

É o que separa um sistema de alertas útil de uma fonte de ruído. O RF-ALT-01 é preciso: *notificar cada perfil apenas sobre eventos que exigem ação dele*.

A matriz da Visão Geral (seção 15) define quem recebe o quê. O direcionamento vai além do perfil: um item pronto na mesa 12 alerta **o garçom responsável por aquela mesa**, não todos os garçons.

## 3. Escopo

### 3.1 Dentro desta história

- Matriz de direcionamento por tipo de alerta e perfil
- Direcionamento por responsabilidade, quando aplicável (mesa, praça, entrega)
- Escalonamento quando não há resposta no prazo
- Configuração da matriz por tenant
- Prevenção de alerta ao próprio autor da ação

### 3.2 Fora desta história

- Motor de avaliação (US-080)
- Canais de entrega (US-081)
- Silenciamento por usuário (Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Direcionamento de alertas

  Cenário: Alerta só para quem age
    Dado um item pronto na janela de expedição
    Quando o alerta for disparado
    Então apenas o garçom responsável pela mesa deve ser notificado
    E cozinha e caixa não devem receber esse alerta

  Cenário: Direcionamento por praça
    Dado um alerta de item atrasado na praça Forno
    Quando for disparado
    Então deve alcançar quem está no KDS do forno
    E não deve alcançar quem está na praça de bebidas

  Cenário: Escalonamento por falta de resposta
    Dado um alerta direcionado ao garçom, sem reconhecimento no prazo
    Quando o prazo for ultrapassado
    Então o alerta deve escalar para os demais garçons e para o gestor

  Cenário: Autor não é alertado da própria ação
    Dado que o garçom cancelou um item
    Quando o alerta de cancelamento for disparado
    Então ele não deve receber alerta da própria ação
    E caixa e gestor devem receber

  Cenário: Alerta exclusivo de gestão
    Dado uma divergência de caixa acima do limiar
    Quando o alerta for disparado
    Então apenas o gestor deve ser notificado
    E nenhum perfil operacional deve receber

  Cenário: Matriz configurável
    Dado um tenant que alterou o direcionamento de um tipo de alerta
    Quando o alerta ocorrer
    Então deve seguir a configuração daquele tenant
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-003 | Cada transição gera alerta aos perfis envolvidos | "Envolvidos" significa quem age, não todo mundo |
| RN-016 | Configuração, não código | A matriz de direcionamento é dado por tenant |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET   /v1/tenant/alert-routing
PATCH /v1/tenant/alert-routing
{ "ORDER_LATE":        { "roles": ["WAITER","KITCHEN","MANAGER"],
                         "scope": "RESPONSIBLE",
                         "escalateAfterSeconds": 120 },
  "ITEM_READY":        { "roles": ["WAITER"], "scope": "TABLE_OWNER" },
  "CASH_DIVERGENCE":   { "roles": ["MANAGER"], "scope": "TENANT" },
  "PRODUCT_UNAVAILABLE": { "roles": ["WAITER","CASHIER","MANAGER"],
                           "scope": "TENANT" } }
```

> `scope` define o recorte: TENANT (todos do papel), RESPONSIBLE (quem responde pela entidade), TABLE_OWNER, STATION.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `alert` | Destinatários resolvidos | `target_roles`, `target_user_ids` |
| `tenant_config` | Matriz de direcionamento | `alertRouting` (JSONB) |
| `table_session` | Responsável pela mesa | `waiter_id` |

## 9. Comportamento offline

Resolução de destinatários integralmente local para alertas operacionais, usando a réplica de usuários e papéis.

## 10. Interface e experiência

- Tela de configuração da matriz em linguagem de negócio: "quem deve ser avisado quando um pedido atrasa?"
- Padrões sensatos vindos da matriz da Visão Geral, para funcionar sem configuração
- Pré-visualização de quem receberia cada tipo de alerta

## 11. Métricas, alertas e observabilidade

- Alertas recebidos por perfil e por usuário — desbalanceamento indica direcionamento errado
- Taxa de escalonamento por tipo
- Correlação entre volume de alertas por usuário e taxa de reconhecimento — queda indica saturação

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Resolução de destinatários para cada escopo |
| Integração | Alerta de item pronto chega só ao garçom responsável |
| Integração | Autor da ação não recebe alerta da própria ação |
| Integração | Escalonamento por falta de resposta |

## 13. Dependências

**Depende de:** US-080, US-081  
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

- **A matriz de alertas da Visão Geral (seção 15) é proposta inicial e está marcada como "a validar".** Confirmar com o cliente antes da implementação.

---

*US-082 · Épico E-08 · Pacote 004_DonaBetinha · Replay Studio.*