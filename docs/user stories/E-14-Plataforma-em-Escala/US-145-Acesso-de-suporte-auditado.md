# US-145 · Acesso de suporte auditado

|  |  |
|---|---|
| **Épico** | [E-14 · Plataforma em Escala](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 5 |
| **Requisitos funcionais** | RF-PLT-08 |
| **Regras de negócio** | RN-015 |
| **ADRs** | ADR-023, ADR-022 |
| **Eventos** | EVT-074 |
| **Aplicações** | web-platform, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9) e gestor do cliente (P8),
> **quero** que todo acesso da Replay aos dados do cliente seja autorizado, temporário e visível,
> **para** que o cliente confie que ninguém olha seus dados sem que ele saiba.

## 2. Contexto e motivação

É a única exceção autorizada ao isolamento da RN-015 — e por isso precisa ser a mais controlada de todo o produto.

O requisito é explícito no documento 02, seção 8: *token de escopo especial, expiração curta, registro obrigatório e visível ao cliente*. Visível ao cliente é a parte que costuma ser esquecida, e é justamente a que sustenta a confiança.

## 3. Escopo

### 3.1 Dentro desta história

- Solicitação de acesso com motivo e duração
- Token de escopo especial, com expiração curta
- Registro obrigatório em auditoria
- Visibilidade ao cliente: notificação e registro consultável
- Revogação imediata pelo cliente
- Relatório de acessos de suporte por tenant

### 3.2 Fora desta história

- Acesso de emergência sem registro (não existe, por desenho)
- Acesso permanente da Replay

## 4. Critérios de aceite

```gherkin
Funcionalidade: Acesso de suporte auditado

  Cenário: Acesso concedido com registro
    Dado um chamado aberto pelo cliente
    Quando a Replay solicitar acesso informando motivo e duração
    Então deve ser gerado token de escopo especial com expiração
    E o evento support.access.granted deve ser emitido
    E o cliente deve ser notificado

  Cenário: Visibilidade ao cliente
    Dado um acesso de suporte realizado
    Quando o gestor consultar a trilha
    Então deve ver quem acessou, quando, por quanto tempo e por quê

  Cenário: Expiração do token
    Dado um token de suporte expirado
    Quando for utilizado
    Então o acesso deve ser recusado
    E a tentativa deve ser registrada

  Cenário: Revogação pelo cliente
    Dado um acesso de suporte ativo
    Quando o gestor revogá-lo
    Então o acesso deve cessar imediatamente

  Cenário: Nenhum acesso sem registro
    Dado qualquer tentativa de acesso da Replay a dados de tenant
    Quando ocorrer
    Então deve exigir token de suporte válido
    E deve ser impossível acessar sem registro

  Cenário: Relatório de acessos
    Dado vários acessos ao longo do período
    Quando o relatório for gerado
    Então deve listar todos, com motivo, duração e responsável
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-015 | Nenhum dado de um estabelecimento é acessível a outro, em nenhuma circunstância | O acesso de suporte é a única exceção — sempre autorizada, temporária e visível |
| RN-004 | Toda ação registra autor, horário e dispositivo | Acesso e ações durante o acesso registrados |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-074 | `support.access.granted` | Replay acessou dados do tenant | reason, durationMinutes, grantedBy | ↑ |

## 7. Contrato de API

```http
POST /v1/platform/tenants/{id}/support-access
{ "reason": "Investigação de divergência de caixa — chamado #482",
  "durationMinutes": 60 }
→ 201 { "token": "...", "expiresAt": "...", "notifiedCustomer": true }

DELETE /v1/tenant/support-access/{id}     # revogação pelo cliente
GET    /v1/tenant/support-access-history  # visível ao cliente
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `support_access` | Acesso concedido | `tenant_id`, `granted_to`, `reason`, `granted_at`, `expires_at`, `revoked_at` |
| `audit_log` | Registro do acesso e das ações | `action=SUPPORT_ACCESS`, `actor_id` |

## 9. Comportamento offline

Operação de nuvem.

## 10. Interface e experiência

- Motivo obrigatório e substantivo, com referência ao chamado
- Notificação ao cliente no momento da concessão, não depois
- Histórico de acessos sempre disponível ao cliente, sem precisar pedir
- Revogação em um clique pelo cliente

## 11. Métricas, alertas e observabilidade

- Acessos de suporte por tenant e por período
- Duração média do acesso
- Motivos mais frequentes — insumo de melhoria do produto
- Revogações pelo cliente — sinal de desconforto que merece conversa

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Acesso sem token válido é recusado |
| Integração | Token expirado recusado e registrado |
| Integração | Revogação pelo cliente cessa o acesso imediatamente |
| Segurança | Nenhum caminho de acesso a dado de tenant sem registro |
| Isolamento | Token de suporte de um tenant não acessa outro |

## 13. Dependências

**Depende de:** US-001, US-090  
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

- É o ponto mais sensível do modelo multi-tenant. Qualquer caminho de acesso não registrado invalida a garantia inteira — exige revisão de segurança específica.

---

*US-145 · Épico E-14 · Pacote 004_DonaBetinha · Replay Studio.*