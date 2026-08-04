# US-123 · Folha de pagamento

|  |  |
|---|---|
| **Épico** | [E-12 · Financeiro de Gestao](./README.md) |
| **Fase** | 3 — Financeiro de gestão |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 3 |
| **Requisitos funcionais** | RF-FIN-04 |
| **Regras de negócio** | — |
| **ADRs** | ADR-023, ADR-031 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** registrar salários e encargos da equipe,
> **para** que o custo de pessoal entre no meu resultado e no prime cost.

## 2. Contexto e motivação

Citado explicitamente pelo cliente: *salários de funcionários*. Custo de pessoal costuma ser a segunda maior linha do restaurante e é metade do **prime cost** (US-124), o indicador mais usado do setor.

Escopo importante de delimitar: isto é **registro de custo para gestão**, não processamento de folha. Cálculo de encargos, guias e obrigações continuam com o contador. Confundir os dois gera expectativa que o produto não vai cumprir.

Dado de folha é informação sensível: exige controle de acesso mais restrito que o resto do financeiro.

## 3. Escopo

### 3.1 Dentro desta história

- Cadastro de funcionário com função, tipo de contrato e vínculo com o usuário do sistema
- Registro de folha por competência, com salário, encargos e benefícios
- Total de custo de pessoal por período
- Custo por função e por área
- Permissão específica para acesso a dados de folha
- Vínculo entre funcionário e operador, para o custo por produtividade

### 3.2 Fora desta história

- Cálculo de encargos, férias e décimo terceiro
- Geração de guias e obrigações acessórias
- Controle de ponto

## 4. Critérios de aceite

```gherkin
Funcionalidade: Folha de pagamento

  Cenário: Registro de folha da competência
    Dado 8 funcionários cadastrados
    Quando a folha de julho for registrada
    Então o custo total de pessoal do mês deve ser calculado
    E deve compor o resultado do período

  Cenário: Custo por função
    Dado funcionários em funções distintas
    Quando o custo por função for exibido
    Então deve agrupar salários e encargos por função

  Cenário: Acesso restrito
    Dado um usuário sem permissão de folha
    Quando tentar acessar os dados
    Então deve receber 403
    E a tentativa deve ser registrada em auditoria

  Cenário: Vínculo com operador
    Dado um garçom com usuário no sistema e cadastro de funcionário
    Quando o custo por produtividade for calculado
    Então deve relacionar o custo dele com os pedidos que atendeu

  Cenário: Funcionário desligado
    Dado um funcionário desligado em julho
    Quando a folha de agosto for registrada
    Então ele não deve compor o custo de agosto
    E o histórico de julho deve permanecer

  Cenário: Repetição da competência anterior
    Dado a folha de julho registrada
    Quando o gestor gerar a de agosto
    Então os valores de julho devem ser sugeridos como base editável
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | Acesso e alteração de folha registrados |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
POST /v1/finance/employees
{ "name": "...", "role": "WAITER", "contractType": "CLT",
  "userId": "...", "hiredAt": "2026-03-01" }

POST /v1/finance/payroll
{ "period": "2026-07",
  "items": [ { "employeeId": "...", "salary": 200000,
               "charges": 76000, "benefits": 25000 } ] }
→ 201 { "payroll": { "period": "2026-07", "totalCost": 2408000 } }

GET /v1/finance/payroll?period=2026-07
GET /v1/finance/labor-cost?period=2026-07&groupBy=role
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `employee` | Funcionário | `name`, `role`, `contract_type`, `user_id`, `hired_at`, `terminated_at` |
| `payroll` | Folha da competência | `period`, `total_cost`, `closed_at` |
| `payroll_item` | Item por funcionário | `employee_id`, `salary`, `charges`, `benefits` |
| `financial_entry` | Despesa gerada | `type=EXPENSE`, `source_type=payroll` |
| `metric_operator_daily` | Produtividade por operador | `orders`, `revenue` |

## 9. Comportamento offline

Operação de nuvem, com acesso restrito.

## 10. Interface e experiência

- Acesso a folha atrás de permissão específica, distinta do financeiro geral
- Repetição da competência anterior como base editável — reduz digitação repetida
- Custo por função e por área, não só total
- Aviso explícito de que isto é registro de custo, não processamento de folha

## 11. Métricas, alertas e observabilidade

- Custo de pessoal por período e por função
- Custo de pessoal como percentual da receita
- Metade do prime cost (US-124)
- Custo por operador relacionado à produtividade

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo do custo total com salário, encargos e benefícios |
| Integração | Permissão específica exigida; acesso registrado em auditoria |
| Integração | Funcionário desligado sai das competências seguintes |
| Segurança | Dados de folha não acessíveis por perfil operacional |

## 13. Dependências

**Depende de:** US-121  
**Habilita:** US-124, US-127

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

- **Expectativa mal calibrada** — o cliente pode esperar processamento de folha. Delimitar o escopo por escrito na proposta.
- Dado de folha é sensível. Permissão específica e auditoria de acesso são obrigatórias.

---

*US-123 · Épico E-12 · Pacote 004_DonaBetinha · Replay Studio.*