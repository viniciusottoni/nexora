# US-154 · Gestão de planos e configuração comercial

|  |  |
|---|---|
| **Épico** | [E-15 · Gestão Geral da Plataforma](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | S — Should have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Terceiro incremento da E-15 |
| **Requisitos funcionais** | RF-PLT-13 |
| **Regras de negócio** | RN-004, RN-016 |
| **ADRs** | ADR-013, ADR-020, ADR-032 |
| **Eventos** | EVT-057 `tenant.plan_changed`, EVT-054 `tenant.config_updated` |
| **Aplicações** | web-platform, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** consultar e alterar o plano comercial de um estabelecimento com vigência e histórico,
> **para** manter o produto entregue coerente com o contrato sem regras específicas em código.

## 2. Contexto e motivação

O provisionamento já recebe `plan`, mas o domínio atual pode persistir um valor padrão diferente. Isso cria divergência entre o que a interface confirmou, o que a API devolveu e o que ficou no banco. Além disso, strings livres não são um catálogo comercial.

Esta história cria uma fonte única para planos e registra mudanças temporais. Cobrança, preço e emissão fiscal continuam fora do escopo até a decisão do modelo comercial.

## 3. Escopo

### 3.1 Dentro desta história

- Catálogo versionado de planos com código, nome, estado e capacidades
- Plano escolhido no provisionamento persistido sem substituição silenciosa
- Consulta do plano atual e das capacidades efetivas
- Upgrade/downgrade com data de vigência, motivo e confirmação de impacto
- Histórico de alterações com autor e snapshot anterior/novo
- Validação de que códigos desativados não sejam atribuídos a novos tenants
- Feature flags e limites derivados de configuração, nunca de `if` por cliente
- Detecção de divergência entre plano comercial e configuração efetiva

### 3.2 Fora desta história

- Cobrança, assinatura, boleto, cartão, imposto ou nota fiscal
- Prorrata e cálculo financeiro de upgrade/downgrade
- Preço negociado individualmente
- Código exclusivo por tenant

## 4. Critérios de aceite

```gherkin
Funcionalidade: Plano comercial do estabelecimento

  Cenário: Provisionamento preserva o plano solicitado
    Dado que o formulário enviou o plano COMPLETO
    Quando o tenant for criado
    Então o plano persistido e retornado deve ser COMPLETO
    E nenhuma camada deve substituí-lo por STANDARD

  Cenário: Mudança de plano com vigência
    Dado um tenant ACTIVE no plano GESTAO
    Quando o administrador agendar COMPLETO para uma data futura
    Então o plano atual deve permanecer até a vigência
    E a mudança deve aparecer no histórico e emitir tenant.plan_changed na efetivação

  Cenário: Plano desconhecido
    Dado um código ausente do catálogo ativo
    Quando alguém tentar atribuí-lo
    Então deve receber 422 PLAN_NOT_AVAILABLE
    E nenhuma configuração parcial deve ser aplicada

  Cenário: Divergência detectada
    Dado que capacidades efetivas não correspondem ao plano
    Quando o detalhe for aberto
    Então deve exibir alerta administrativo
    E deve oferecer reconciliação idempotente e auditada
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Ação sensível registra autor e antes/depois | Toda mudança ou reconciliação entra no histórico e audit_log |
| RN-016 | Regra específica é configuração, nunca código | Capacidades e limites pertencem ao catálogo/plano versionado |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-057 | `tenant.plan_changed` | Plano entra em vigência | tenantId, previousPlan, plan, effectiveAt, actorId | ↓ |
| EVT-054 | `tenant.config_updated` | Capacidades efetivas mudam | configVersion, source=PLAN | ↓ |

## 7. Contrato de API

```http
GET /v1/platform/plans
→ 200 { "data": [{ "code": "COMPLETO", "name": "Completo", "active": true, "capabilities": ["..."] }] }

GET /v1/platform/tenants/{id}/plan
→ 200 { "current": "GESTAO", "effectiveCapabilities": ["..."], "scheduled": null, "consistent": true }

PUT /v1/platform/tenants/{id}/plan
Idempotency-Key: <uuid>
If-Match: "<version>"
{ "plan": "COMPLETO", "effectiveAt": "...", "reason": "Aditivo contratual #32" }
→ 200 { "current": "GESTAO", "scheduled": { "plan": "COMPLETO", "effectiveAt": "..." } }
```

## 8. Modelo de dados

| Tabela | Papel | Campos relevantes |
|---|---|---|
| `platform_plan` | Catálogo | code, name, version, capabilities, limits, active |
| `tenant` | Referência atual | `plan` |
| `tenant_plan_history` | Linha do tempo | previous, next, requested/effective timestamps, reason, actor |
| `tenant_config` | Capacidades efetivas | configVersion e flags derivadas |

## 9. Comportamento offline

Gestão exclusiva de nuvem. Mudanças que afetam o edge são propagadas por eventos; a UI mostra o estado de propagação e não promete aplicação imediata enquanto a instalação estiver offline.

## 10. Interface e experiência

- Comparação clara de capacidades antes da mudança
- Data de vigência e motivo obrigatórios
- Downgrade destaca perdas e dependências incompatíveis
- O plano exibido vem do servidor, nunca do valor submetido ainda não confirmado
- Divergência usa alerta acionável, sem correção automática silenciosa

## 11. Métricas, alertas e observabilidade

- Tenants por plano e mudanças por período
- Divergências de configuração por plano
- Tempo de propagação da mudança ao edge
- Tentativas de atribuição de plano inválido

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Resolução de capacidades e vigência |
| Integração | Persistência do plano no provisionamento, histórico e evento |
| Contrato | Códigos idênticos no catálogo, API e frontend |
| Segurança | Apenas P9 pode alterar; leitura respeita mascaramento comercial |
| E2E | Alterar plano, confirmar impacto e visualizar histórico |

## 13. Dependências

**Depende de:** US-002, US-142, US-152, US-153  
**Habilita:** governança comercial e futura integração de billing

## 14. Definition of Ready e Definition of Done

**DoR**

- [ ] Modelo comercial e nomes dos planos confirmados
- [ ] Capacidades/limites de cada plano aprovados
- [ ] Política de vigência e downgrade definida

**DoD**

- [ ] Provisionamento persiste exatamente o plano validado
- [ ] Catálogo, histórico e reconciliação implementados
- [ ] Mudança é idempotente, concorrente e auditada
- [ ] Configuração por plano respeita ADR-013
- [ ] Contratos e testes de propagação atualizados

## 15. Riscos, premissas e pendências

- **[PENDÊNCIA BLOQUEANTE]** Propriedade do produto, modelo comercial, preços e composição final dos planos ainda precisam de decisão formal.
- Não misturar “plano comercial” com “estado da instalação”; são dimensões independentes.

---

*US-154 · Épico E-15 · Pacote 004_DonaBetinha · Replay Studio.*
