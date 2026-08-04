# US-153 · Ciclo de vida do estabelecimento

|  |  |
|---|---|
| **Épico** | [E-15 · Gestão Geral da Plataforma](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Segundo incremento da E-15 |
| **Requisitos funcionais** | RF-PLT-12 |
| **Regras de negócio** | RN-004, RN-015 |
| **ADRs** | ADR-020, ADR-021, ADR-022, ADR-023 |
| **Eventos** | EVT-056 `tenant.status_changed` |
| **Aplicações** | web-platform, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** ativar, suspender, reativar ou encerrar um estabelecimento com segurança,
> **para** manter o ciclo de vida comercial e operacional coerente e auditável.

## 2. Contexto e motivação

Hoje há divergência entre estados internos (`Trial`, `Active`, `Suspended`, `Cancelled`) e o contrato operacional (`PROVISIONED`, `INSTALLING`, `ACTIVE`, `SUSPENDED`). A ausência de uma máquina de estados única já permitiu que uma resposta de sucesso fosse tratada como erro.

Esta história normaliza estados e transições antes de expor ações administrativas. Nenhuma ação destrói registros; encerramento é lógico e preserva auditoria.

## 3. Escopo

### 3.1 Dentro desta história

- Máquina de estados canônica: `PROVISIONED`, `INSTALLING`, `ACTIVE`, `SUSPENDED`, `CANCELLED`
- Migração/mapeamento explícito dos estados legados
- Ativação automática ou assistida quando pré-condições forem cumpridas
- Suspensão com motivo, data efetiva e impacto visível
- Reativação com validação das dependências técnicas
- Cancelamento lógico com confirmação reforçada
- Histórico de transições com ator, motivo, origem e correlação
- Idempotência e controle de concorrência em toda transição

### 3.2 Fora desta história

- Exclusão física de tenant ou dados históricos
- Cobrança automática ou inadimplência (a suspensão pode receber origem comercial futura)
- Atualização do software do parque (US-146)
- Bloqueio de acesso de suporte auditado aos registros históricos

## 4. Critérios de aceite

```gherkin
Funcionalidade: Ciclo de vida do estabelecimento

  Cenário: Ativação após implantação
    Dado um tenant INSTALLING com instalação registrada e proprietário ativo
    Quando as pré-condições de ativação forem satisfeitas
    Então o status deve mudar para ACTIVE
    E a transição deve ser registrada e emitir tenant.status_changed

  Cenário: Suspensão administrativa
    Dado um tenant ACTIVE
    Quando o administrador confirmar a suspensão com motivo
    Então novas sessões de gestão e operação devem seguir a política de suspensão
    E o impacto e a possibilidade de reversão devem ficar registrados

  Cenário: Transição inválida
    Dado um tenant CANCELLED
    Quando alguém tentar ativá-lo diretamente
    Então deve receber 409 TENANT_STATUS_TRANSITION_INVALID
    E nenhum estado parcial deve ser persistido

  Cenário: Repetição da mesma intenção
    Dado que uma suspensão foi concluída
    Quando a mesma Idempotency-Key for reenviada
    Então a resposta original deve ser repetida
    E não deve existir uma segunda transição

  Cenário: Concorrência
    Dado que dois administradores abriram o mesmo detalhe
    Quando ambos tentarem mudanças incompatíveis
    Então apenas a primeira versão válida deve vencer
    E a segunda deve receber conflito com estado atual
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação sensível registra contexto | Mudança conserva ator, motivo, antes/depois e correlação |
| RN-015 | Isolamento total | Mudança afeta exclusivamente o tenant informado e seus acessos |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-056 | `tenant.status_changed` | Transição concluída | tenantId, previousStatus, status, reason, effectiveAt, actorId | ↓ |

Consumidores edge aplicam a política correspondente sem apagar dados locais necessários para auditoria/recuperação.

## 7. Contrato de API

```http
POST /v1/platform/tenants/{id}/status-transitions
Authorization: Bearer <PlatformAdmin>
Idempotency-Key: <uuid>
If-Match: "<version>"
{ "targetStatus": "SUSPENDED", "reason": "Solicitação contratual #482", "effectiveAt": "..." }
→ 200 { "tenantId": "...", "previousStatus": "ACTIVE", "status": "SUSPENDED", "version": 8, "changedAt": "..." }
```

Erros: `409 TENANT_STATUS_TRANSITION_INVALID`, `409 CONCURRENCY_CONFLICT`, `422 REASON_REQUIRED`.

## 8. Modelo de dados

| Tabela | Papel | Campos relevantes |
|---|---|---|
| `tenant` | Estado atual | `status`, `status_version`, `updated_at`, `deleted_at` |
| `tenant_status_history` | Histórico imutável | anterior, novo, motivo, ator, origem, efetivação, correlação |
| `audit_log` | Auditoria sensível | before/after e contexto da requisição |

## 9. Comportamento offline

Mudança de status é exclusiva da nuvem. O edge recebe o fato por sincronização; enquanto não receber, a nuvem mostra “propagação pendente”. Nunca se oferece transição offline no navegador.

## 10. Interface e experiência

- Status com rótulo, explicação e próxima transição permitida
- Modal mostra impacto antes da confirmação
- Motivo obrigatório e substantivo
- Cancelamento exige digitar o slug do estabelecimento
- Estado de propagação para instalações aparece separado do estado comercial

## 11. Métricas, alertas e observabilidade

- Tenants por status e tempo médio em `PROVISIONED`/`INSTALLING`
- Suspensões e reativações por motivo
- Falhas de propagação ao edge
- Transições recusadas e conflitos de concorrência

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Matriz completa de transições válidas e inválidas |
| Integração | Transação, histórico, evento, idempotência e concorrência |
| Contrato | Estado canônico idêntico no .NET e TypeScript |
| Segurança | Apenas P9; nenhuma ação cross-tenant acidental |
| E2E | Suspender, visualizar impacto e reativar |

## 13. Dependências

**Depende de:** US-001, US-002, US-152  
**Habilita:** US-154, US-157

## 14. Definition of Ready e Definition of Done

**DoR**

- [ ] Matriz de estados e impactos aprovada por produto, suporte e segurança
- [ ] Estratégia de migração de estados legados definida
- [ ] Política de acesso durante suspensão acordada

**DoD**

- [ ] Um único enum/contrato canônico usado em todas as camadas
- [ ] Histórico imutável e evento emitido atomicamente
- [ ] Idempotência e concorrência cobertas
- [ ] Migração de dados legados testada
- [ ] Nenhuma exclusão física exposta pela UI/API

## 15. Riscos, premissas e pendências

- **[PENDÊNCIA]** Definir efeitos exatos da suspensão sobre menu público, edge e acesso do proprietário.
- **[PENDÊNCIA]** Definir política de retenção após cancelamento conforme contrato e LGPD.

---

*US-153 · Épico E-15 · Pacote 004_DonaBetinha · Replay Studio.*
