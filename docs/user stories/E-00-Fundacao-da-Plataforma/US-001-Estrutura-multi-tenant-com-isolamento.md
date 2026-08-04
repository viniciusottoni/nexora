# US-001 · Estrutura multi-tenant com isolamento

|  |  |
|---|---|
| **Épico** | [E-00 · Fundacao da Plataforma](./README.md) |
| **Fase** | 0 — Fundação da plataforma |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-PLT-01 |
| **Regras de negócio** | RN-015 |
| **ADRs** | ADR-004, ADR-015 |
| **Eventos** | — |
| **Aplicações** | api-cloud, api-edge, packages/db |
| **Autoridade do dado** | Nuvem — o cadastro de tenant nasce na plataforma |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** que os dados de cada estabelecimento fiquem isolados por construção,
> **para** que nenhum cliente veja, em nenhuma circunstância, dados de outro.

## 2. Contexto e motivação

Este é o requisito mais caro de adicionar depois e o mais barato de adicionar agora. A diretriz de produto replicável (Visão Geral, seção 8) só se sustenta se o isolamento for **inegociável e estrutural**.

A decisão registrada no ADR-004 é banco único, schema compartilhado, isolamento por **Row Level Security** do PostgreSQL. O motivo é direto: um `WHERE tenant_id` esquecido em uma única query vaza dados entre estabelecimentos. Com RLS, o banco recusa — o erro deixa de depender de disciplina e passa a ser impossível por construção.

Vale para os dois lados: o edge server é single-tenant por instalação, mas mantém `tenant_id` em todas as tabelas para que os eventos sincronizem sem transformação e o código seja idêntico no local e na nuvem.

## 3. Escopo

### 3.1 Dentro desta história

- Coluna `tenant_id` obrigatória em toda tabela de negócio
- `ALTER TABLE ... ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` em todas elas
- Política `tenant_isolation` usando `current_setting('app.tenant_id')`
- `TenantConnectionInterceptor` do EF Core que executa `SET LOCAL app.tenant_id` a cada requisição autenticada
- `AppDbContext` configurado para recusar query sem contexto de tenant definido (interceptor obrigatório, sem caminho de acesso ao banco fora dele)
- Resposta 404 (nunca 403) para recurso de outro tenant
- Teste automatizado de isolamento executado em todo PR

### 3.2 Fora desta história

- Banco por tenant ou schema por tenant (rejeitado no ADR-004; revisar só se houver exigência contratual de isolamento físico)
- Multi-loja dentro do mesmo tenant — o modelo já prevê `store`, a funcionalidade fica para fase posterior
- Criptografia por tenant em repouso

## 4. Critérios de aceite

```gherkin
Funcionalidade: Isolamento de dados entre estabelecimentos

  Contexto:
    Dado que existem os tenants "A" e "B" provisionados
    E que ambos possuem pedidos, mesas e usuários próprios

  Cenário: Isolamento imposto pelo banco
    Dado um usuário autenticado no tenant A
    Quando ele consultar qualquer tabela de negócio
    Então apenas registros com tenant_id = A devem retornar
    E nenhuma cláusula WHERE de aplicação deve ser necessária para isso

  Cenário: Tentativa de acesso cruzado por ID
    Dado um usuário do tenant A
    Quando tentar acessar um pedido do tenant B informando o ID exato
    Então deve receber 404, resposta idêntica à de recurso inexistente
    E a tentativa deve ser registrada em audit_log com autor, IP e recurso alvo

  Cenário: Query sem contexto de tenant
    Dado que a conexão não definiu app.tenant_id
    Quando executar uma query em tabela com RLS
    Então nenhum registro deve retornar
    E o erro deve ser logado como violação de contrato interno

  Cenário: Escrita com tenant divergente do token
    Dado um usuário do tenant A
    Quando tentar inserir um registro com tenant_id = B
    Então o banco deve recusar a operação pela política WITH CHECK

  Cenário: Isolamento no servidor local
    Dado um edge server instalado para o tenant A
    Quando qualquer query for executada
    Então tenant_id deve estar presente e igual a A em todas as linhas
    E o comportamento deve ser idêntico ao da nuvem
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-015 | Nenhum dado de um estabelecimento é acessível a outro, em nenhuma circunstância | Política RLS `USING` + `WITH CHECK` em todas as 53 tabelas com `tenant_id` |
| RN-016 | Regra específica de negócio deve existir como configuração, nunca como código de cliente | O isolamento é genérico: nenhuma política menciona um tenant específico |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-074 | `support.access.granted` | Replay acessa dados de um tenant | reason, durationMinutes, grantedBy | ↑ |

> O acesso de suporte é a única exceção autorizada ao isolamento — e ela é sempre registrada e visível ao cliente (RF-PLT-08).

## 7. Contrato de API

```http
# Nenhuma rota nova. O isolamento é infraestrutura transversal.
# Toda rota autenticada passa a executar, antes da primeira query:

SET LOCAL app.tenant_id = '<tid do JWT>';

# Regra do documento 05, princípio 6:
# "Tenant nunca vem do cliente em rota autenticada — sempre do token."

# Comportamento de erro esperado (RFC 7807):
GET /v1/orders/{id-de-outro-tenant}
→ 404 {
    "type": "https://docs.<plataforma>/errors/not-found",
    "title": "Recurso não encontrado",
    "status": 404,
    "code": "NOT_FOUND"
  }
```

> Responder 403 revelaria que o recurso existe em outro tenant. 404 é a resposta correta por segurança.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `tenant` | Tabela raiz — global, sem RLS | `id`, `slug`, `name`, `plan`, `status`, `created_at` |
| Todas as tabelas de negócio | Portadoras de `tenant_id` com RLS ativo | `tenant_id uuid NOT NULL REFERENCES tenant(id)` |
| `audit_log` | Registra tentativa de acesso cruzado | `actor_id`, `action`, `target_type`, `target_id`, `ip`, `occurred_at` |
| `unit_of_measure` | Tabela global sem RLS (catálogo compartilhado) | `code`, `name`, `dimension` |

> Referência: `Domain/10-RLS-Papeis-e-Indices.md` — 53 políticas RLS mapeadas.

## 9. Comportamento offline

O edge server é **single-tenant por instalação**: `app.tenant_id` é fixado por variável de ambiente definida no `install.sh` e não muda em tempo de execução. Mesmo assim, RLS permanece habilitado e o código é idêntico ao da nuvem — isso garante que uma regressão de isolamento apareça em desenvolvimento local, e não só em produção multi-tenant.

Não há comportamento degradado nesta história: o isolamento nunca é relaxado por falta de conectividade.

## 10. Interface e experiência

- Sem interface — história puramente estrutural
- Efeito visível apenas indireto: recurso de outro tenant se comporta como inexistente, sem mensagem que revele sua existência

## 11. Métricas, alertas e observabilidade

- Log estruturado (Serilog) para toda query recusada por falta de contexto de tenant — deve ser sempre zero em produção
- Métrica de contagem de respostas 404 por tentativa de acesso cruzado, por tenant de origem
- Atributo `tenant.id` propagado em todo span OpenTelemetry (ADR-022)
- Alerta de plataforma se a taxa de acessos cruzados de um usuário exceder o limiar — indica credencial comprometida

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | `TenantConnectionInterceptor` recusa/bloqueia query sem `app.tenant_id` definido |
| Integração | Usuário do tenant A não lê nem escreve registro do tenant B em nenhuma das tabelas |
| Integração | Política `WITH CHECK` recusa insert com `tenant_id` divergente do contexto |
| Contrato | Rota que recebe `tenantId` no corpo em rota autenticada falha no CI |
| Segurança | Varredura automatizada: toda tabela com `tenant_id` tem RLS habilitado e forçado |
| Regressão | Suíte de isolamento roda em todo PR e bloqueia merge se falhar |

## 13. Dependências

**Depende de:** nenhuma  
**Habilita:** US-002, US-004, e todas as histórias que tocam tabela de negócio

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
- [ ] Todas as 53 tabelas com `tenant_id` verificadas por script automatizado
- [ ] Revisão de segurança formal do desenho de RLS registrada

## 15. Riscos, premissas e pendências

- **Risco T4 do documento 02** — vazamento entre tenants tem probabilidade baixa e impacto crítico. A mitigação é teste automatizado de isolamento em cada PR, não revisão manual.
- Uso de conexão de pool sem `SET LOCAL` corretamente escopada por transação pode vazar contexto entre requisições — exige teste específico de concorrência.
- Jobs em background (worker de métrica, sync) rodam fora do ciclo de requisição e precisam de mecanismo próprio de definição de tenant — mapear na implementação.

---

*US-001 · Épico E-00 · Pacote 004_DonaBetinha · Replay Studio.*