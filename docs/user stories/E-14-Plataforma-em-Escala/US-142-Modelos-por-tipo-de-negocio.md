# US-142 · Modelos por tipo de negocio

|  |  |
|---|---|
| **Épico** | [E-14 · Plataforma em Escala](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | S — Should have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 5 |
| **Requisitos funcionais** | RF-PLT-06 |
| **Regras de negócio** | RN-016 |
| **ADRs** | ADR-013, ADR-032 |
| **Eventos** | — |
| **Aplicações** | web-platform, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** partir de um modelo pronto conforme o tipo de negócio do cliente,
> **para** que hamburgueria e restaurante não precisem ser configurados do zero.

## 2. Contexto e motivação

A diretriz de produto replicável cita explicitamente pizzaria, hamburgueria, restaurante e lanchonete. Cada um tem praças, categorias, limiares e regras típicas diferentes.

O modelo é **conjunto de configuração e seeds**, nunca variação de código — é a aplicação direta do ADR-013.

## 3. Escopo

### 3.1 Dentro desta história

- Modelos por tipo de negócio: pizzaria, hamburgueria, restaurante, lanchonete
- Seeds de praças, categorias, limiares e papéis
- Aplicação do modelo na criação do tenant
- Edição do modelo aplicado, sem afetar outros tenants
- Manutenção dos modelos pela Replay

### 3.2 Fora desta história

- Marketplace de modelos
- Modelo criado pelo próprio cliente

## 4. Critérios de aceite

```gherkin
Funcionalidade: Modelos por tipo de negócio

  Cenário: Aplicação do modelo
    Dado um tenant criado com modelo HAMBURGUERIA
    Quando o provisionamento concluir
    Então praças, categorias e limiares devem refletir o modelo
    E devem ser diferentes dos de uma pizzaria

  Cenário: Edição sem afetar outros
    Dado dois tenants criados com o mesmo modelo
    Quando um deles alterar sua configuração
    Então o outro não deve ser afetado

  Cenário: Modelo sem código específico
    Dado qualquer modelo aplicado
    Quando o código for inspecionado
    Então não deve haver condicional por tipo de negócio

  Cenário: Atualização de modelo
    Dado um modelo atualizado pela Replay
    Quando um novo tenant for criado
    Então deve receber a versão nova
    E tenants existentes não devem ser alterados
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-016 | Regra específica é configuração, nunca código | Modelo é conjunto de dados, jamais branch |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET  /v1/platform/templates
→ [ { "code": "PIZZERIA", "name": "Pizzaria", "version": 3 },
    { "code": "BURGER",   "name": "Hamburgueria", "version": 2 } ]

POST /v1/platform/tenants
{ "template": "BURGER", ... }

GET  /v1/platform/templates/{code}
PUT  /v1/platform/templates/{code}
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `business_template` | Modelo | `code`, `name`, `version`, `config` (JSONB), `seeds` (JSONB) |
| `tenant_config` | Configuração aplicada | `template_code`, `template_version` |

> Seeds detalhados em `Domain/12-Seeds-e-Dados-Iniciais.md`.

## 9. Comportamento offline

Operação de nuvem.

## 10. Interface e experiência

- Escolha do modelo na criação do tenant, com pré-visualização do que será criado
- Modelo aplicado visível na configuração, com opção de ver o que foi customizado depois

## 11. Métricas, alertas e observabilidade

- Modelos mais usados
- Grau de customização após a aplicação — alto indica modelo mal calibrado
- Tempo de implantação por modelo

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Cada modelo cria a configuração esperada |
| Integração | Edição de um tenant não afeta outro do mesmo modelo |
| Governança | Nenhuma condicional por tipo de negócio no código (trava do CI) |

## 13. Dependências

**Depende de:** US-002, US-141  
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

- Modelo mal desenhado gera customização pesada em todo cliente, anulando o ganho. Calibrar a partir do que for aprendido nas primeiras implantações reais.

---

*US-142 · Épico E-14 · Pacote 004_DonaBetinha · Replay Studio.*