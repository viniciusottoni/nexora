# US-141 · Provisionamento autoatendido

|  |  |
|---|---|
| **Épico** | [E-14 · Plataforma em Escala](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 5 |
| **Requisitos funcionais** | RF-PLT-05 |
| **Regras de negócio** | RN-016 |
| **ADRs** | ADR-013 |
| **Eventos** | — |
| **Aplicações** | web-platform, web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9) e gestor do novo cliente (P8),
> **quero** que a implantação de um novo cliente siga um roteiro guiado,
> **para** que a Replay implante em escala sem depender de desenvolvimento.

## 2. Contexto e motivação

A US-002 criou o tenant. Esta história transforma os nove passos da Visão Geral (8.5) em um **roteiro guiado e rastreável**, com o máximo possível de autoatendimento.

Quanto mais dessa lista for autoatendido, mais barata e replicável fica a operação da Replay — e mais próximo se chega da meta de 5 dias úteis.

## 3. Escopo

### 3.1 Dentro desta história

- Checklist de implantação com os nove passos
- Progresso visível para a Replay e para o cliente
- Assistente de configuração inicial no painel do cliente
- Carga de marca, cardápio, mesas e usuários pelo próprio cliente
- Validação de completude antes da ativação
- Medição do tempo de implantação

### 3.2 Fora desta história

- Importação de cardápio por planilha (US-144)
- Modelos por tipo de negócio (US-142)
- Cobrança e contrato

## 4. Critérios de aceite

```gherkin
Funcionalidade: Provisionamento autoatendido

  Cenário: Checklist de implantação
    Dado um tenant recém-criado
    Quando o painel de implantação for aberto
    Então deve mostrar os nove passos com o estado de cada um

  Cenário: Autoatendimento pelo cliente
    Dado o gestor do novo cliente com acesso
    Quando ele carregar marca, cardápio e mesas
    Então os passos correspondentes devem ser marcados
    E a Replay deve ver o progresso

  Cenário: Validação antes da ativação
    Dado uma implantação com cardápio incompleto
    Quando a ativação for solicitada
    Então deve ser bloqueada
    E os itens pendentes devem ser listados

  Cenário: Medição do tempo
    Dado uma implantação concluída
    Quando o tempo for apurado
    Então deve ser medido da criação do tenant à ativação
    E deve alimentar a métrica de tempo médio de implantação

  Cenário: Nenhum desenvolvimento necessário
    Dado uma implantação completa
    Quando revisada
    Então nenhuma etapa deve ter exigido alteração de código
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-016 | Regra específica é configuração, nunca código | Implantação sem desenvolvimento é o teste dessa regra |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/platform/tenants/{id}/onboarding
→ { "steps": [ { "key": "TENANT_CREATED",  "status": "DONE" },
               { "key": "BRANDING",        "status": "DONE" },
               { "key": "MENU",            "status": "IN_PROGRESS",
                 "progress": { "products": 44, "expected": 60 } },
               { "key": "TABLES",          "status": "PENDING" },
               { "key": "EDGE_INSTALL",    "status": "PENDING" },
               { "key": "PAYMENT_CONFIG",  "status": "PENDING" },
               { "key": "TRAINING",        "status": "PENDING" },
               { "key": "PILOT",           "status": "PENDING" },
               { "key": "ACTIVATION",      "status": "PENDING" } ],
    "startedAt": "...", "elapsedBusinessDays": 2 }

POST /v1/platform/tenants/{id}/activate
→ 422 { "code": "ONBOARDING_INCOMPLETE", "meta": { "pending": [...] } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `tenant` | Estado da implantação | `status`, `onboarding_started_at`, `activated_at` |
| `onboarding_step` | Progresso por passo | `key`, `status`, `completed_at`, `completed_by` |

## 9. Comportamento offline

Operação de nuvem.

## 10. Interface e experiência

- Checklist visível para os dois lados, evitando o vaivém de "o que falta?"
- Assistente no painel do cliente, com linguagem de negócio
- Progresso quantificado onde possível (44 de 60 produtos)
- Bloqueio de ativação com lista clara do que falta

## 11. Métricas, alertas e observabilidade

- **Tempo médio de implantação — meta ≤ 5 dias úteis** (PRD, seção 7)
- Passo que mais atrasa a implantação
- Percentual de passos concluídos por autoatendimento

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Progresso atualizado conforme os passos são concluídos |
| Integração | Ativação bloqueada com pendências |
| E2E | Implantação completa sem nenhuma alteração de código |

## 13. Dependências

**Depende de:** US-002, US-003, US-006  
**Habilita:** US-142, US-144

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

- A carga de cardápio e fichas técnicas continua sendo o passo mais demorado e o mais dependente do cliente. A US-144 é a mitigação.

---

*US-141 · Épico E-14 · Pacote 004_DonaBetinha · Replay Studio.*