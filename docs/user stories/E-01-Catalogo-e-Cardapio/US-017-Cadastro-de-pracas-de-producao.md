# US-017 · Cadastro de pracas de producao

|  |  |
|---|---|
| **Épico** | [E-01 · Catalogo e Cardapio](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 1 |
| **Requisitos funcionais** | RF-CAT-09, RF-KDS-06 |
| **Regras de negócio** | — |
| **ADRs** | ADR-012 |
| **Eventos** | EVT-054 |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** cadastrar as praças de produção da minha cozinha,
> **para** que cada item vá para quem realmente vai prepará-lo.

## 2. Contexto e motivação

Praça de produção é a unidade de organização da cozinha: forno, montagem, bebidas, sobremesas. Sem ela, o KDS vira uma fila única em que o operador do forno vê pedido de refrigerante — ruído que faz a cozinha parar de olhar a tela.

A praça também carrega o conceito de **gargalo**: a praça marcada como `is_bottleneck` (tipicamente o forno) tem capacidade em slots e é a que determina o ritmo real da produção. É desse campo que sai o indicador de ocupação do gargalo da Fase 2 (US-117).

A separação por praças está registrada como *hipótese a validar* na Visão Geral (6.2, M2) — precisa de confirmação com a equipe real antes do piloto.

## 3. Escopo

### 3.1 Dentro desta história

- CRUD de praça com código, nome e cor de identificação
- Campo `capacity_slots` (posições simultâneas do recurso)
- Marcação de praça como gargalo
- Ordem de exibição das praças no KDS
- Praças padrão semeadas pelo modelo de negócio (US-002)

### 3.2 Fora desta história

- Indicador de ocupação do gargalo (US-117, Fase 2)
- Balanceamento automático de carga entre praças
- Vínculo de operador a praça

## 4. Critérios de aceite

```gherkin
Funcionalidade: Praças de produção

  Cenário: Praças padrão do modelo pizzaria
    Dado um tenant criado com o modelo PIZZERIA
    Quando o provisionamento concluir
    Então devem existir as praças Forno, Montagem e Bebidas
    E Forno deve estar marcada como gargalo

  Cenário: Capacidade do gargalo
    Dado a praça Forno com capacity_slots igual a 5
    Quando 5 itens estiverem simultaneamente no estado IN_OVEN
    Então o KDS deve indicar que o gargalo está cheio

  Cenário: Exclusão de praça com produtos vinculados
    Dado uma praça com 30 produtos vinculados
    Quando o gestor tentar excluí-la
    Então a exclusão deve ser recusada
    E deve ser exigido reatribuir os produtos antes

  Cenário: Filtro do KDS por praça
    Dado três praças cadastradas
    Quando o operador do forno abrir o KDS filtrado por Forno
    Então deve ver apenas os itens roteados para o forno
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-016 | Configuração, não código | A estrutura da cozinha é dado por tenant — pizzaria e hamburgueria têm praças diferentes |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-054 | `tenant.config_updated` | Praça criada ou alterada | stationId, changedKeys[] | ↓ |

## 7. Contrato de API

```http
POST /v1/catalog/stations
{ "code": "OVEN", "name": "Forno", "color": "#C1121F",
  "capacitySlots": 5, "isBottleneck": true, "position": 1 }

GET   /v1/catalog/stations
PATCH /v1/catalog/stations/{id}
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `station` | Praça de produção | `code`, `name`, `color`, `capacity_slots`, `is_bottleneck`, `position` |
| `product` | Vínculo com a praça | `station_id` |
| `order_item` | Praça herdada no momento do pedido | `station_id` |

> `order_item.station_id` é copiado do produto no momento da criação — se o produto for reatribuído depois, os itens em produção não mudam de fila.

## 9. Comportamento offline

Replicado ao edge. O roteamento por praça acontece integralmente na rede local, sem qualquer consulta à nuvem — é o que sustenta o requisito de pedido chegar ao KDS em menos de 2 segundos.

## 10. Interface e experiência

- Cor por praça usada consistentemente no KDS, no cadastro de produto e nos relatórios
- Aviso ao tentar salvar mais de uma praça marcada como gargalo — o gargalo é, por definição, um só
- Contagem de produtos vinculados exibida na lista de praças

## 11. Métricas, alertas e observabilidade

- Volume de itens por praça e por faixa horária — mapa de carga da cozinha
- Ocupação do gargalo (base do MET-030, ociosidade com fila)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Validação de gargalo único e de capacidade positiva |
| Integração | Exclusão bloqueada com produtos vinculados |
| Integração | Seeds do modelo PIZZERIA criam as praças esperadas |

## 13. Dependências

**Depende de:** US-002  
**Habilita:** US-010, US-016, US-031, US-042, US-117

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

- **Hipótese não validada** — a separação da cozinha por praças precisa ser confirmada com a equipe da Dona Betinha (Visão Geral, 6.2). Se a cozinha for pequena e sem separação real, forçar praças adiciona atrito sem ganho. O modelo suporta praça única.

---

*US-017 · Épico E-01 · Pacote 004_DonaBetinha · Replay Studio.*