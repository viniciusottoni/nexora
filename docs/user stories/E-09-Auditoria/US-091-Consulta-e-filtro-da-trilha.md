# US-091 · Consulta e filtro da trilha

|  |  |
|---|---|
| **Épico** | [E-09 · Auditoria](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-AUD-03 |
| **Regras de negócio** | — |
| **ADRs** | ADR-023 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** consultar a trilha filtrando por pessoa, período, tipo de ação e valor,
> **para** que eu consiga investigar uma suspeita sem depender de suporte técnico.

## 2. Contexto e motivação

Trilha que só o desenvolvedor consegue ler não cumpre a função. O RF-AUD-03 exige que seja *consultável e filtrável pelo gestor*.

O caso de uso típico é investigativo: "quem cancelou pedidos na terça à noite?", "quais descontos acima de 10% foram dados este mês?", "quem alterou o preço da pizza grande?". Os filtros precisam servir a essas perguntas, não a uma consulta genérica.

## 3. Escopo

### 3.1 Dentro desta história

- Consulta com filtros por período, autor, autorizador, tipo de ação e entidade
- Filtro por valor (descontos acima de X, cancelamentos acima de Y)
- Exibição do antes e depois de forma legível
- Paginação por cursor
- Acesso restrito a perfis autorizados, com o próprio acesso registrado
- Ligação da trilha ao pedido ou entidade de origem

### 3.2 Fora desta história

- Exportação em planilha e PDF (Fase 2)
- Alertas sobre padrões anômalos (US-080)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Consulta da trilha de auditoria

  Cenário: Filtro por autor e período
    Dado a trilha com registros de vários operadores
    Quando o gestor filtrar por um operador e por uma semana
    Então deve ver apenas as ações daquele operador no período

  Cenário: Filtro por tipo de ação
    Dado registros de vários tipos
    Quando o gestor filtrar por DISCOUNT_APPLIED
    Então deve ver apenas os descontos

  Cenário: Filtro por valor
    Dado descontos de valores variados
    Quando o gestor filtrar por descontos acima de R$ 50,00
    Então deve ver apenas os que ultrapassam esse valor

  Cenário: Antes e depois legíveis
    Dado um registro de alteração de preço
    Quando for exibido
    Então deve mostrar o valor anterior e o novo em formato legível
    E não deve exibir JSON bruto ao gestor

  Cenário: Navegação até a origem
    Dado um registro de cancelamento de item
    Quando o gestor tocar no registro
    Então deve chegar ao pedido de origem

  Cenário: Acesso restrito e registrado
    Dado um usuário sem permissão de auditoria
    Quando tentar acessar a trilha
    Então deve receber 403
    E, quando um autorizado acessar, o acesso deve ser registrado

  Cenário: Desempenho com volume
    Dado uma trilha com 500.000 registros
    Quando uma consulta filtrada for executada
    Então deve responder em menos de 3 segundos
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | Inclusive o acesso à própria trilha |
| RN-015 | Isolamento entre estabelecimentos | A consulta respeita RLS |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/audit?from=...&to=...&actorId=...&action=DISCOUNT_APPLIED&minAmount=5000&limit=50&cursor=...
→ { "data": [ { "id": "...", "action": "DISCOUNT_APPLIED",
                "actor": { "name": "Carlos" },
                "authorizedBy": { "name": "Ana" },
                "device": { "label": "Caixa 1" },
                "occurredAt": "...",
                "target": { "type": "table_session", "id": "...", "label": "Mesa 12" },
                "summary": "Desconto de 10% (R$ 19,80) aplicado",
                "before": { "discount": 0 }, "after": { "discount": 1980 },
                "reason": "cortesia" } ],
    "meta": { "nextCursor": "...", "hasMore": true } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `audit_log` | Origem da consulta | Índices por `tenant_id`, `occurred_at`, `actor_id`, `action` |
| `app_user` | Nomes de autor e autorizador | `name` |
| `device` | Identificação do terminal | `label` |

## 9. Comportamento offline

Consulta de nuvem. A trilha local existe e é consultável pelo painel do edge em caso de necessidade operacional, mas a investigação gerencial acontece na nuvem, onde está o histórico consolidado.

## 10. Interface e experiência

- Filtros pensados nas perguntas reais do gestor, não em campos de banco
- Antes e depois traduzidos para linguagem de negócio, nunca JSON bruto
- Resumo de uma linha por registro, com detalhe ao expandir
- Ligação direta ao pedido, mesa ou produto de origem
- Aviso explícito de que o acesso à trilha também é registrado

## 11. Métricas, alertas e observabilidade

- Frequência de consulta à trilha — uso alto pode indicar desconfiança operacional
- Filtros mais usados, revelando as preocupações reais do gestor

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Todos os filtros isolados e combinados |
| Integração | Acesso negado sem permissão; acesso autorizado é registrado |
| Integração | Navegação do registro até a entidade de origem |
| Desempenho | Consulta filtrada em base com 500.000 registros em menos de 3 s |
| Isolamento | Trilha de um tenant não é visível a outro |

## 13. Dependências

**Depende de:** US-090  
**Habilita:** US-076

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

- Consulta sem índice adequado degrada rapidamente com o volume. Índices por tenant, período, autor e ação são obrigatórios desde a primeira migration.

---

*US-091 · Épico E-09 · Pacote 004_DonaBetinha · Replay Studio.*