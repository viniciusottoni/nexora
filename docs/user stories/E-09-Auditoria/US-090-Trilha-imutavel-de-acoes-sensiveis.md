# US-090 · Trilha imutavel de acoes sensiveis

|  |  |
|---|---|
| **Épico** | [E-09 · Auditoria](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-AUD-01, RF-AUD-02, RF-AUD-04 |
| **Regras de negócio** | RN-004 |
| **ADRs** | ADR-022, ADR-023, ADR-035 |
| **Eventos** | EVT-071, EVT-072, EVT-074 |
| **Aplicações** | api-edge, api-cloud, packages/db |
| **Autoridade do dado** | Local e nuvem — cada lado registra o que acontece nele |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** que toda ação sensível fique registrada de forma que ninguém possa apagar,
> **para** que eu tenha como investigar qualquer divergência com evidência, não com suposição.

## 2. Contexto e motivação

Exigência confirmada de forma direta na descoberta. A dor por trás dela é conhecida: sem registro, cancelamento, desconto e ajuste de estoque viram buraco sem responsável.

O ponto técnico central é a **imutabilidade real**. Uma tabela chamada `audit_log` em que a aplicação pode dar `UPDATE` não é trilha de auditoria — é uma tabela de log. A garantia vem de revogar `UPDATE` e `DELETE` no nível do banco para o papel da aplicação.

Escopo mínimo do RF-AUD-02: cancelamento, desconto, alteração de preço, movimentação de estoque, ajuste financeiro, abertura e fechamento de caixa, e alteração de permissão.

## 3. Escopo

### 3.1 Dentro desta história

- Tabela `audit_log` append-only com `UPDATE` e `DELETE` revogados
- Registro de autor, autorizador, horário, dispositivo, tenant, valores antes e depois
- Cobertura de todas as ações sensíveis do RF-AUD-02
- Correlação com o evento de domínio e com o traço de observabilidade
- Particionamento por período (ADR-035)
- Teste de cobertura garantindo que nenhuma ação sensível fique sem registro

### 3.2 Fora desta história

- Consulta e filtro pelo gestor (US-091)
- Exportação da trilha (Fase 2)
- Assinatura criptográfica encadeada (avaliar em fase posterior)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Trilha imutável de auditoria

  Cenário: Registro completo
    Dado um desconto aplicado com autorização
    Quando a ação for concluída
    Então o log deve conter autor, autorizador, horário, dispositivo,
         valores antes e depois

  Cenário: Imutabilidade
    Dado um registro de auditoria existente
    Quando qualquer usuário da aplicação tentar alterá-lo ou apagá-lo
    Então o banco deve recusar a operação

  Cenário: Cobertura das ações sensíveis
    Dado a lista de ações sensíveis do RF-AUD-02
    Quando cada uma for executada
    Então todas devem produzir registro na trilha

  Cenário: Correlação com o evento
    Dado uma ação registrada na trilha
    Quando o registro for consultado
    Então deve haver referência ao evento de domínio correspondente
    E ao identificador de correlação do traço

  Cenário: Registro de tentativa negada
    Dado uma tentativa de acesso a recurso de outro tenant
    Quando for recusada
    Então a tentativa deve ser registrada na trilha

  Cenário: Acesso de suporte da plataforma
    Dado que a Replay acessou dados do tenant com token de suporte
    Quando o acesso ocorrer
    Então deve ser registrado na trilha
    E deve ser visível ao cliente

  Cenário: Registro offline
    Dado que a loja está sem internet
    Quando uma ação sensível for executada
    Então deve ser registrada localmente
    E deve subir para a nuvem na sincronização
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | É o objeto desta história |
| RN-015 | Isolamento entre estabelecimentos | Tentativa de acesso cruzado é registrada |
| RN-011 | Ação sensível exige autorização de perfil superior | Executor e autorizador registrados separadamente |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-071 | `authorization.granted` | Ação sensível autorizada | action, authorizedBy, context | ↑ |
| EVT-072 | `permission.changed` | Permissão alterada | roleId, added[], removed[] | ↑ |
| EVT-074 | `support.access.granted` | Replay acessou dados do tenant | reason, durationMinutes | ↑ |

## 7. Contrato de API

```http
# Sem endpoint de escrita — a trilha é gravada pelos casos de uso.
# Contrato interno:

await audit.record(tx, {
  action:      'DISCOUNT_APPLIED',
  targetType:  'table_session',
  targetId:    sessionId,
  actorId, authorizedBy, deviceId,
  before:      { discount: 0,    total: 19800 },
  after:       { discount: 1980, total: 17820 },
  reason:      'cortesia',
  correlationId
});

# Permissões de banco (migration):
REVOKE UPDATE, DELETE ON audit_log FROM app_role;
```

> A revogação de permissão no banco é o que torna a imutabilidade real. Sem ela, a tabela é apenas um log.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `audit_log` | Trilha append-only, particionada | `id`, `tenant_id`, `action`, `target_type`, `target_id`, `actor_id`, `authorized_by`, `device_id`, `before` (JSONB), `after` (JSONB), `reason`, `correlation_id`, `occurred_at` |
| `domain_event` | Correlação com o evento | `id`, `correlation_id` |

> Sem `UPDATE` nem `DELETE` — a permissão é revogada para o papel da aplicação. Correção se faz com novo registro, nunca alterando o anterior.

## 9. Comportamento offline

Registrada integralmente no edge e sincronizada como qualquer outro dado. Auditoria que dependesse da nuvem teria um buraco exatamente nas horas de operação offline — que é quando o controle importa mais.

A imutabilidade vale nos dois lados: a revogação de permissão está na migration, que é a mesma no edge e na nuvem (ADR-019).

## 10. Interface e experiência

- Sem interface própria — a consulta é a US-091
- Efeito visível: toda tela de ação sensível informa que a ação será registrada

## 11. Métricas, alertas e observabilidade

- Contagem de ações sensíveis por tipo, autor e período
- Cancelamentos, descontos e ajustes por operador — insumo direto do painel de gestão
- Acessos de suporte por tenant
- Tentativas negadas — indicador de segurança

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | `UPDATE` e `DELETE` recusados pelo banco na `audit_log` |
| Integração | Todas as ações do RF-AUD-02 produzem registro |
| Regressão | Teste de cobertura falha se uma ação sensível não registrar |
| Integração | Registro correlacionado ao evento de domínio e ao traço |
| Caos offline | Registro local com internet caída, sincronizado depois |

## 13. Dependências

**Depende de:** US-001, US-004  
**Habilita:** US-091, US-033, US-054, US-055

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
- [ ] Teste de cobertura de auditoria rodando no CI e bloqueando merge

## 15. Riscos, premissas e pendências

- Ação sensível esquecida cria um ponto cego permanente. A lista do RF-AUD-02 é o mínimo; revisar a cada novo caso de uso.
- Volume da trilha cresce indefinidamente; particionamento e política de retenção do ADR-035 são obrigatórios.

---

*US-090 · Épico E-09 · Pacote 004_DonaBetinha · Replay Studio.*